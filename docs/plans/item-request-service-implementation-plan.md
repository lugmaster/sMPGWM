# Item ownership and request service implementation plan

Status: proposed, 2026-10-30. Documentation only; implementation has not started.

Every live item entity must belong to exactly one container. Player inventory controllers and the world's `ItemController` request changes through one shared `ItemRequestService`. Containers remain simple storage with acceptance rules. The service owns authoritative item mutation and commits transactions atomically.

This plan records the agreed direction, proposes a migration from the current implementation, and lists decisions that must be settled before their dependent work starts. It does not authorize implementation of trading, multiplayer, or persistence.

Related plans: [Inventory UI implementation](inventory-ui-implementation-plan.md) and [Tag-driven item classification](tag-inventory-integration.md). This plan proposes the new mutation boundary; it preserves existing inventory presentation behavior. The tag plan remains responsible for classification and acceptance queries.

## Agreed architecture

| Component | Responsibility |
| --- | --- |
| `ItemRegistry` | Load and expose shared item and category definitions. No live entity creation or ownership. |
| `ItemController` | World-facing item operations, including requests to create items in the game. Delegates mutations to `ItemRequestService`. |
| `InventoryController` | Player inventory behavior and read presentation. Requests creation, movement, transfer, deletion, splitting, and merging through the service. |
| `EquipmentController` | Equipment-specific behavior and slot policies. Uses the same service and ownership model. |
| `ItemRequestService` | Shared authority for validation and atomic item-state changes. Owns or privately accesses the live entity and location registers. |
| `ItemContainer` | Holds slots and acceptance policies. Does not coordinate transactions or implement world, player, or trading behavior. |

`ItemRequestService` is the current chosen name for the authority previously discussed as `ItemTransactionService`. They are one responsibility, not two services. Request processing can be synchronous; the name does not require a queue or network transport.

```text
World ItemController       Player InventoryController       EquipmentController
          \                         |                         /
                         ItemRequestService
                                 |
                  Live entity and location registers
                                 |
                  World, player, equipment, chest containers
```

Create one service per running game world and inject it into participating controllers. Player controllers must not each create an independent authority. A global register means authoritative within that world, not a process-wide static singleton or a register copied to every client.

## Ownership and identity rules

1. Every registered live entity ID resolves to exactly one current state and one container location. Container slots and the location register must agree.
2. Creation requires a destination. Registering an entity and placing it into a container are one commit. No unlocated live entity is exposed between those steps.
3. Movement uses source and destination locations, including rearrangement within one container. Equipment and world pickups follow the same ownership rules.
4. Deletion and consumption remove or reduce an entity through the service. Removing its last unit also removes its live registration and location.
5. Transfers, splits, and merges conserve quantity. Creation and destruction are explicit exceptions.
6. Failure leaves authoritative item state unchanged. Observers cannot see half a transfer.
7. Actual slot sorting is one batch transaction. Display-only sorting and filtering do not mutate containers and need no transaction.
8. A container cannot be removed while its contents are left live and ownerless. Transfer, destruction, save/unload, and complete world teardown need explicit lifecycle paths.

Keep stable string definition IDs from YAML. Do not reintroduce YAML-order numeric definition IDs. Display names may change independently. Renaming or removing definition IDs requires a future compatibility policy for saves.

Entity identity is distinct from definition identity. The current `RuntimeIds` allocator is process-local and explicitly does not promise persistence. Saved entity and container identities need a separate decision before save/load is implemented.

## Current implementation and migration targets

| Current code | Migration |
| --- | --- |
| `ItemRegistry.CreateEntity` | Remove the unregistered fixture helper. Adapt its tests to create located entities through the service, or use isolated test-only data where a detached value is intentional. Do not replace it with another production bypass. |
| `ItemController` entity, location, and container dictionaries | Move authoritative storage into `ItemRequestService`, optionally behind a private `ItemEntityStore`. Keep `ItemController` as the world-facing client. |
| `ItemController.CreateItems`, `TransferItem`, `Commit`, and `Publish` | Reuse their validation and transaction behavior in the service, extending the contract for explicit operation kinds. |
| `InventoryController.Items` and `EquipmentController.Items` | Replace their dependency on the world controller as mutation authority with the shared service. Preserve controller-specific views and behavior. |
| Default controller constructors creating `new ItemController()` | Replace production wiring with an explicitly shared authority. Tests may create isolated worlds deliberately. |
| `InventoryEquipmentTransferService` | Retain UI session/address translation where useful, but delegate commits to the common service. It must not become a second transaction engine. |
| Controller disposal directly marking a container disposed | Separate presentation unsubscription from authoritative container removal. Resolve occupied-container lifecycle first. |

The current `ItemEntity` is an immutable reference type. Containers and snapshots can reference the same object without copying its contents. Count changes currently create a replacement state object with the same entity ID, allowing older snapshots to remain stable.

Retain the distinction between one authoritative current state and historical/read-only copies. Requiring one permanent object reference would complicate snapshots and cannot extend across network processes. Keep immutable read snapshots unless profiling and an explicit replacement contract justify changing them.

The existing `Commit` copies the entire entity and location dictionaries for every operation. These are shallow copies, but their cost grows with all registered items, even when two slots change. Avoid making that the long-term design for a busy world. Any replacement must preserve atomicity; changing dictionaries in place without a failure strategy is not sufficient.

## Request and commit contract

Requests identify the operation, source/destination where applicable, item identity, quantity, and relevant expected revisions and slot occupants. Creation identifies a definition and destination instead of an existing source entity. Reuse current stale-state checks; UI previews do not reserve items or authorize later commits.

Placement behavior must be explicit. Moving to an empty slot, swapping, merging, and finding available space must not be silently interchangeable. In particular, pickup must not accidentally swap a player's existing item onto the ground.

Proposed processing sequence:

1. Validate the caller's authority, containers, expected identities/revisions, quantities, and operation-specific conditions.
2. Compute the affected next states, including both sides of a swap and all resulting stack counts. Check acceptance policies against resulting placements.
3. Prepare necessary allocations and bookkeeping before publishing any state. Define rollback or a commit path without expected failures for incremental register updates.
4. Install all participating states and locations with no callbacks between installations. Advance each changed container revision once.
5. Publish the completed changes. A rejected request or no-op advances no revision and publishes no change.

Initially serialize mutation on the game thread. Reject or defer reentrant requests while publishing; do not introduce multithreaded writes as part of this migration. Acceptance functions must be side-effect-free and cannot initiate transactions.

A request entry point is not automatic permission: player requests to create or delete items require a gameplay-authorized reason. Future network clients submit intent; the host validates it and allocates authoritative identities.

## World pickup and drop

The world holds items in containers. World scene nodes represent those entities by ID rather than owning separate item copies. `ItemController` handles world context and requests changes from the shared service.

- Pickup requests move an existing entity or quantity from world storage into player storage.
- Drop requests move an existing entity or quantity from player storage into world storage.
- Loot spawning requests creation directly into world storage.
- Failed pickup or drop leaves ownership and the existing world representation unchanged.

World position and other durable placement data must be coordinated with ownership. Define which part is authoritative transaction data and which part is presentation reconstructed after commit. Do not make deleting or creating a scene node the authoritative ownership change. A presentation failure after commit needs reconciliation, not a second item or a repeat transfer.

One world container is a valid starting point. Region/chunk containers can be introduced later. The choice affects revision contention, save/unload boundaries, and lookup cost, so it remains open.

## NPC sales and atomic exchange

Proposed boundary: a `TradeService` owns offers, prices, eligibility, and purchase requests. NPC inventory uses ordinary containers and the same item authority. `ItemController` can request initial stock creation, but does not need to own commerce rules.

For finite stock, a sale transfers an existing item from NPC stock to the player. It does not create a replacement item merely because ownership changes. Catalog or unlimited-stock vendors require an explicitly authorized creation policy.

A purchase must commit payment, stock changes, and item delivery together. Full player inventory, changed stock, changed price, or insufficient funds must leave the whole exchange unchanged.

- If currency is an item, the service can commit both item movements as one batch.
- If currency is a wallet balance, define a transaction coordinator spanning wallet and item state. Two sequential calls to deduct payment and deliver an item are insufficient.

Trade orchestration and currency representation remain proposals, not settled implementation requirements. Implement commerce only after those decisions are made. Retry behavior must also prevent a repeated purchase request from charging twice.

## Implementation phases

### Phase 1 Establish ownership and request contracts

Document operation kinds, typed results, caller authorization, shared-world lifetime, and permitted placement behavior. Choose the transaction publication/failure contract before moving code. Keep item-domain request types independent of inventory UI and equipment address types.

Acceptance: creation, deletion, same-container movement, cross-container transfer, swap, split, merge, and batch operations each have defined success, rejection, and no-op behavior. No controller owns an independent mutation engine.

### Phase 2 Extract the shared authority and rewire controllers

Move existing registers and transaction machinery from `ItemController` into `ItemRequestService`. Wire one instance through the gameplay composition root. Adapt inventory, equipment, UI transfer adapters, mock fixtures, and tests. Leave `ItemController` as the world-facing interface that delegates requests.

Remove `ItemRegistry.CreateEntity` and migrate its callers in the same phase. Preserve registry definition-sharing tests; tests of entity creation and placement belong with the service. Fixture-specific invalid-count expectations must not accidentally redefine bulk gameplay creation semantics.

Acceptance: current inventory/equipment/hotbar behavior is preserved; all production creation and movement enter the same service; the registry only supplies definitions; two controllers share entity lookup and ownership consistently.

### Phase 3 Complete lifecycle and stack operations

Add explicit deletion/consumption, splitting, merging, swapping, and atomic batches. Define identity outcomes and container removal rules. Proposed stack identities: whole-stack movement preserves ID; splitting allocates a new ID; a merge retains the destination ID and retires an exhausted source ID. Per-instance properties must participate in stack compatibility when introduced.

Acceptance: operations conserve quantities except authorized creation/destruction; no live zero-count stacks or duplicate locations remain; rejected operations preserve all participating state; physical sorting commits as a single batch.

### Phase 4 Integrate world storage and placement

Introduce world container ownership and ID-based scene representations. Route pickup, drop, and loot creation through `ItemController` and the service. Add world placement metadata using the chosen atomicity boundary.

Acceptance: two competing pickups cannot both succeed; a rejected pickup leaves the world item intact; a rejected drop leaves the player's item intact; successful operations have one authoritative location and recoverable presentation.

### Phase 5 Bound transaction cost

Measure operation time and allocations as total live entities increase while the touched containers remain fixed. Compare sparse world storage and dense inventories. Replace whole-world dictionary copies with staged affected-entry updates, persistent structures, or another justified approach that preserves the selected failure guarantees.

Acceptance: moving one item does not require copying all unrelated registered entities. Keep immutable reader state and verify that old snapshots remain unchanged. Performance budgets and workload sizes must be selected before this phase is considered complete.

### Future phases Persistence multiplayer and trading

Design save/load identity, definition migrations, host authority, request retry handling, replication visibility, and trade coordination before implementing these systems. Clients should receive relevant snapshots and changes, not the complete global register. Unloaded regions and offline inventories may remain in persistence rather than requiring all entities to stay resident.

An in-memory atomic commit is not crash-safe persistence. Durable purchases and saves require a storage transaction or recovery strategy in addition to the service's game-thread transaction boundary.

## Validation plan

- Verify the bidirectional invariant between occupied slots and registered locations after every operation in service tests.
- Cover creation into accepted/rejected/full destinations, invalid quantities, and unknown definitions.
- Cover same-container and cross-container moves, stale source/target expectations, incompatible swaps, partial/full merges, splits, and deletion.
- Verify no state or revision changes on rejection, and one revision increment per changed container on success.
- Verify snapshots and notifications expose only complete committed state; test reentrancy and observer failures according to the selected contract.
- Exercise world pickup competition, drop failures, and reconstruction of missing presentation from authoritative state.
- Cover occupied-container removal, world teardown, and eventually unload/reload without item loss or duplication.
- Retain current UI session, equipment compatibility, hotbar, and inventory projection regressions. Run Godot-dependent integration checks separately from ordinary domain tests.
- When trading is added, test rejected payment/delivery, changed offers, competing buyers, and retries with no duplicate charge or item delivery.

## Open decisions

| Question | Proposed direction or alternatives | Resolve before |
| --- | --- | --- |
| Is `ItemController` also responsible for world positions and scene lifecycle? | Keep world-facing requests there; consider a separate presentation adapter if scene responsibilities become large. | World integration |
| One global world container or region/chunk containers? | Start simple, but assess revision contention and unload boundaries. | World integration |
| Which controllers can request creation and deletion, under what authority? | Shared API with trusted gameplay authorization; remote callers cannot grant themselves arbitrary items. | Request contract |
| Which requests allow partial completion? | Default to all-or-nothing requested quantity; explicit partial merge/pickup policy if desired. Atomicity still applies to the accepted result. Existing transfer can merge only what fits. | Stack operations |
| How are empty slots chosen and containers grown? | Controllers may express intent; the service validates and commits a complete placement plan. Decide deterministic selection and explicit growth limits. | Request contract |
| What identifies a compatible stack? | Definition ID suffices for current data; durability, modifiers, ownership bindings, and other instance data may require stricter matching. | Stack operations |
| Must the entity store be a separate class? | A private service-owned register is sufficient initially. Public read APIs should not expose mutable storage. | Authority extraction |
| How do incremental register updates retain failure atomicity? | Compare preallocated/no-failure commit, reversible staged changes, and persistent data structures. Do not sacrifice atomicity to remove copies. | Register optimization |
| What happens when an observer throws? | Current code propagates exceptions and can stop later notifications; the earlier UI plan calls for logging and continuing. Prefer reliable remaining notifications, but explicitly settle the conflict and adapt tests. Never report committed state as an uncommitted rejection. | Authority extraction |
| What happens to occupied containers on disposal or unload? | Separate detaching views, destroying a container, unloading persistent state, and ending a world. Choose explicit content handling for each. | Lifecycle operations |
| How are world position updates coordinated with item ownership? | Commit authoritative placement with membership; derive scene objects after commit and reconcile failures. Exact data ownership remains open. | World integration |
| How do entity/container IDs survive save/load? | Persistent allocator/high-water mark, world-scoped IDs, or durable IDs mapped to runtime handles. YAML definition IDs remain stable strings in every option. | Persistence |
| How are renamed or removed YAML definitions handled? | Explicit aliases/migrations and a missing-definition policy; never silently substitute another item. | Persistence |
| How do currency and vendor stock work? | Item currency versus wallet; finite stock versus catalog creation. These determine the trade transaction boundary. | Trading |
| Where do multiplayer authorization and retry tracking live? | Host-side request boundary with request IDs and deduplication; define visibility and reconnect behavior separately from storage. | Multiplayer |
| How does this intersect with tag classification? | Keep acceptance rules pluggable; do not add new display-name-based category rules. Coordinate stable category IDs versus the planned tag queries. | Acceptance policy migration |
| What workload and latency targets justify optimization? | Set entity counts, container sizes, transaction rates, and allocation budgets based on intended gameplay, then measure. | Performance validation |

## Scope of the first implementation

The first implementation should establish the shared service, retain player and world controllers, remove live entity creation from `ItemRegistry`, and route existing creation/transfers through the new boundary. Complete lifecycle operations and world integration in subsequent reviewable phases. Keep trading, networking, and persistence as explicit follow-up work until their open decisions are resolved.
