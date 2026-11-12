# Gameplay architecture cleanup plan

Status: implemented and verified with managed/in-engine tests, multi-process ENet, and local GodotSteam host lifecycle. Remote multi-account Steam verification remains outstanding as recorded below.

Implementation (2026-11-12): `PlayerSession` owns personal inventory, `GameWorld.Items`
is the session authority, and `ShipController.Loadout` owns installed equipment, grants,
and loadout integration. Control changes preserve ship state. `PlayerItemState` is removed.
`ShipRuntime` replaces `ShipGameplayState`; `ShipStatState` exposes effective values while
`ResourcePool` only maintains current/capacity invariants. Explicit power, shield, movement,
ability admission, and effect components are staged at the simulation boundary.
`GameplaySimulation` executes authenticated queued transfers and abilities, resource phases,
and controlled movement. Networking authenticates and replicates protocol 9 state.
`LocalPlayerBindings` revokes edits/targeting and rebinds UI and hotbar without transferring
ship ownership or losing hotbar assignments. Private item baselines carry player, generation,
ship, container, and revision identity; old binding acknowledgements and snapshots are discarded.

The initial baseline passed 267 outer tests and 182 in-engine tests. Two existing warnings
were CS0108 (`UiIcon.Size`) and NUnit2009 (`AbilityArchitectureTests`). The initial startup
smoke reached the menu and initialized Steam. Updated test/process evidence is recorded below;
remote multi-account Steam verification is distinct from the local host lifecycle smoke.

Lifetime map: `SessionScope` registers cleanup as resources are acquired, including failed
construction. `GameWorld` composes player inventory and ship loadouts before spawn publication,
freezes released ships, and destroys the controlled ship on disconnect. Destruction removes
store indexes and runtime registrations synchronously before deferred Godot deletion. UI unbind
only removes subscriptions/edit permissions. Intended changes are ship-owned equipment/grants,
control-scoped item commands, and complete installed-state teardown; gameplay formulas,
spawn-only initialization, host authority, and movement reconciliation are preserved.

Date: 2026-11-12.

Establish clear player and ship ownership, one effective stat view, and ship systems with complete attachment and removal lifecycles. Preserve the working inventory transactions, gameplay rules, host authority, and movement prediction while removing the integration responsibilities currently concentrated in `PlayerItemState`, `ShipGameplayState`, and `NetworkManager`.

The intended result is straightforward to navigate: a player references its controlled ship and personal inventory; the ship owns its equipment, effective stats, resources, and capabilities. Changing control changes references and permissions. It does not move installed equipment or rebuild a ship's gameplay state.

This plan follows [equipment stats and resources](gameplay-increment-03-equipment-stats-and-resources.md) and the ownership direction in [the earlier architecture work](backlog/game-architecture-refactorings.md). It supersedes their player-bound equipment integration where specified below. Existing gameplay formulas remain in force, with the current afterburner rule recorded explicitly below.

## Scope and boundaries

Implement the ownership migration, stat pipeline cleanup, detachable runtime systems, presentation rebinding, and required protocol changes as ordered increments. Each increment must build and pass its relevant checks before the next starts. Preserve `SessionCoordinator` and `SessionScope` as the existing session lifetime boundary.

Do not include weapons, damage, progression, persistence, cargo gameplay, AI, arbitrary mod code loading, a full ECS, a dependency injection framework, or a universal event bus. Prepare concrete extension points using the systems that already work. Unsupported authored ability mechanics remain unsupported.

The current inventory becomes the player's personal inventory. Future ship cargo is a separate ship-owned container using the same item authority; it must not silently reuse the personal inventory. Player progression, when introduced, belongs to the player and may contribute to a controlled ship through an explicitly owned attachment.

## Current problems and migration targets

Source paths in the tables are relative to `sMPGWM.Game/Scripts`.

| Current integration | Problem | Target |
| --- | --- | --- |
| `World/PlayerSession.cs` and `NetworkSystem/NetworkManager.Items.cs` | Control and inventory require unrelated lookups; networking constructs and owns player items | `PlayerSession` is the player aggregate; networking accesses gameplay-owned players |
| `Items/PlayerItemState.cs` | Equipment follows control; switching removes old grants but leaves old equipment-derived stats | Ship-owned equipment and loadout integration survive control changes |
| `Items/ItemStore.cs` | Per-player stores prevent transfers to independently owned ship equipment; disposal leaves store registrations | One session item authority, explicit container ownership and destruction |
| `StatSystem/EquipmentStatCalculator.cs` and `GamePlaySystem/ResourcePool.cs` | Equipment and effects resolve at different levels; `Stats.ShieldCapacity` is not the final shield maximum | One effective stat view, separately named inputs, resources containing current amounts |
| `GamePlaySystem/ShipGameplayState.cs` | Resource simulation, effects, capabilities, faction, revisions, and snapshot confirmation share one implementation | A small ship runtime aggregate with composed state and systems |
| `ShipSystem/ShipController.cs` | Godot body, control flags, simulation, configuration, and presentation are connected directly | Godot body adapter consuming typed movement configuration and a movement motor |
| `NetworkSystem/NetworkManager.Gameplay.cs` | Transport implements gameplay stepping and gameplay mutation order | Gameplay simulation owns fixed-step work; transport validates and delivers commands and state |
| `GamePlaySystem/GameSessionFactory.cs`, hotbar, targeting, and UI adapters | Equipment and transfer references are fixed at construction | An explicit local binding lifecycle for the player and current ship |

## Target ownership and dependencies

```text
SessionScope
  GameWorld
    Players: PlayerSession
      Identity and control generation
      ControlledShip reference
      Personal inventory container
    Ships: ShipController and ShipRuntime
      Definition and installed equipment container
      Stat state and effective stat view
      Resource state and effect instances
      Attached systems
  ItemStore
    Item identities, locations, and registered containers
  GameplaySimulation
    Gameplay commands and ordered fixed-step work
  NetworkManager and SynchronizationCoordinator
    Transport, authentication, replication, timing, and reconciliation
  Local player bindings
    Input, camera, inventory UI, transfers, hotbar, and targeting
```

The store owns item identity/location integrity; player and ship aggregates determine the lifetime and access policy of their containers. All authoritative containers share one store so transfers remain atomic. Clients maintain only the replicas they are permitted to inspect.

Retain the name `PlayerSession` initially. Expanding its responsibility is sufficient; a cosmetic rename is not a prerequisite. Public read access should support `player.Inventory`, `player.ControlledShip.Equipment`, `player.ControlledShip.Stats`, and `player.ControlledShip.Resources`. The ship-facing members may forward to `ShipRuntime`; they must not duplicate mutable state. A nullable `player.Stats` convenience view may forward to the controlled ship if an actual consumer needs it.

Domain code must not depend on `NetworkManager`, UI nodes, or local input. Equipment integration may depend on stat contributions and ability grants. Movement consumes a typed configuration and input. Power consumes resolved production, demand, and energy state; shield consumes power availability and shield configuration. The simulation calls these in a fixed order. Avoid callbacks between systems that recursively trigger more simulation.

## Contracts to preserve

| Contract | Required behavior |
| --- | --- |
| Stat formula | `max(0, (baseline + sum of flats) * (1 + sum of fractions))`, followed by stat-specific validation; do not clamp the baseline-plus-flat stage prematurely |
| Presence | A missing installed component suppresses its stats and associated modifiers; a flat bonus does not create an engine or generator |
| Aggregation | Mass includes intrinsic mass and every equipped item, excluding personal inventory. Passive demand includes every equipped item's demand |
| Afterburner | Current code adds 25% of engine demand while afterburner is active. Preserve `AfterburnerResourceTests`; the older increment 3 statement of no extra demand is stale |
| Resource capacity | Increasing maximum does not fill current; decreasing maximum clamps current |
| Battery | Removing or replacing its item identity discards stored energy; changing capacity on the same item preserves current within the new maximum |
| Shield | Atomic generator replacement preserves charge within the new maximum and preserves downtime. Separate removal empties the shield; reattachment does not refill it |
| Initialization | Resolve the initial loadout, then initialize shield full and energy empty exactly once. Binding, replication, and later attachment never repeat spawn initialization |
| Effects | Already-applied timed effects survive removal of the granting item. Their contribution is suppressed while the required component is absent and resumes if it returns before expiry |
| Power | Deficit at zero stored energy disables powered systems without erasing velocity. Configured demand remains present during failure to avoid oscillation |
| Timing | 60 Hz physics; resources advance once per authoritative tick; publication every second step at 30 Hz |
| Prediction | Configuration-changing movement samples rebase incompatible history. Replay never advances resources or runs authoritative effects |
| Authority | Host validates source, target, current control generation, revisions, and command identity. Clients cannot mutate authoritative gameplay through UI or snapshots |

Keep the current policy that released ships remain motion-frozen, while existing world-entity resource/effect advancement continues. Enabling uncontrolled movement or AI requires a separate entity-based movement protocol change. Preserve the current explicit disconnect policy that destroys the controlled ship; an ordinary control switch must not destroy either ship.

## Stat resolution and system lifecycle

### One effective stat view

Use one ship-owned stat state to collect typed equipment inputs, ship flats, and supported source-owned modifiers. Resolve dirty values after committed configuration changes. Expose baseline/equipment inputs under explicit names and publish final supported values through `ShipStatsView` or an equivalently named typed result. In particular, the effective shield maximum in the stat view must equal the resource capacity.

Keep current shield and energy in resource state. Dynamic operating demand, afterburner activity, power availability, and shield protection belong in operational state; they must not overwrite configured passive demand or force all equipment baselines to resolve every step. `ResourcePool` becomes a holder of current/capacity invariants, not a second modifier engine.

Retain the existing calculator arithmetic. Compile or cache authored numeric values outside simulation. Use typed keys or compiled indices and bounded reusable storage for the supported stat set. The contribution mechanism may support flat and fraction operations generally, but do not enable unsupported ability definitions merely because the mechanism can represent their values.

Use a typed contribution source containing a kind and runtime identity. An equipped item owns its installed stat and grant contributions; an applied effect owns its own contributions independently. Remove by source, in a stable order. Removing an item source must not remove another item's copy of the same grant or an effect already cast from that item.

### Attachment and removal

A small ship-owned component collection provides typed access and lifecycle ownership. Use a minimal lifecycle contract only where multiple components need it; provide explicit simulation phase calls rather than giving every component an unordered `Update` callback. Composition and supported component types are explicit in code for this cleanup.

Attaching a component validates prerequisites and proposed resolved values before publication, registers its contributions and subscriptions, and joins the appropriate simulation phase. Failed attachment leaves previous state intact. Detaching stops ticking and command admission, removes that attachment's contributions and subscriptions, updates dependent availability, and publishes one coherent change. Repeated detach/dispose must be harmless.

Distinguish removing an installed item from removing a runtime component. Item changes update hardware presence and equipment-owned contributions. Removing a component implementation retains those configured equipment stats and grants but gates operation through separate runtime-component presence. Only registrations owned by that component are revoked. Reattaching reads the current loadout; it does not reinstall or duplicate equipment sources. Power loss suppresses consumers rather than destroying their component instances.

Without the power component, energy stepping stops and powered consumers are unavailable even if stored energy remains. Without the shield component, protection and regeneration/drain stop while charge and the absolute downtime deadline remain. Removing the movement capability disables driven movement; passive body integration and collision handling remain in the body adapter and do not call the removed component. Preserve velocity. Replicate component presence separately from hardware presence and energy depletion, and validate operational flags against both. Movement samples must still carry everything needed to reproduce their configuration independently of reliable gameplay packet arrival.

Separate ability admission from effect maintenance. Removing ability admission blocks new activations but existing effect instances continue their ordinary expiry through ship-owned maintenance. If an effect-processing component is removed, suppress its contributions while absent; retain absolute deadlines, and remove overdue instances before reattachment can publish contributions. Never pause a timed effect merely by detaching its processor. Include this distinction in replicated component state and admission checks; a retained grant alone is not permission to activate through a removed ability component.

Resource amounts and continuity metadata, including shield downtime and battery identity, remain in ship-owned state so component replacement cannot reset them. Physical generator/battery removal still applies the explicit resource rules above. Destruction of the ship removes all its state and source registrations; it does not affect another ship or the player's inventory.

Attach/detach operations are staged at authoritative step boundaries, not performed from UI callbacks or while iterating the systems being ticked. External observers see a committed configuration. Coalesce notifications for a committed operation; do not expose a transient empty generator during an atomic replacement. Required dependencies are validated before installation; unavailable optional dependencies produce a documented disabled state.

### Authoritative step order

1. Capture authenticated commands and consume the step's movement input.
2. Commit admitted item/configuration/attachment changes; expire effects due at this tick; resolve dirty stats and availability.
3. Execute admitted abilities against current control and source state; resolve resulting supported modifiers.
4. Derive afterburner operating demand from the consumed input and advance energy once.
5. Advance shield regeneration/drain/downtime using the resulting power state.
6. Install any changed movement configuration and simulate currently controlled ships.
7. Commit completed tick state; publish gameplay, item, and movement updates on the network step.

Keep configuration/stat revisions separate from current-resource revisions. Only a changed movement configuration or movement availability advances the movement configuration revision. Reliable gameplay arrival must never roll back parameters installed from a newer movement sample.

## Ordered implementation increments

### 1 Establish the regression baseline and ownership boundaries

- [x] Record the current build/test baseline and existing failures without treating historical plan counts as current results.
- [x] Map creation, binding, unbinding, destruction, and disposal through `GameSessionFactory`, `GameWorld`, `PlayerSession`, `PlayerItemState`, and `SessionScope`.
- [x] Add focused regression cases for current resource arithmetic, spawn-only initialization, source lifetime, and the new ship-switch ownership contract in the increment that changes it. Avoid maintaining a permanently failing intermediate branch.
- [x] Record the intended behavior changes: equipment/grants stay on ships, item commands acquire control identity, and installed state has complete teardown. Preserve all other gameplay behavior.

Acceptance: the migration can distinguish intended ownership changes from regressions. Relevant existing suites include `EquipmentGrantLifecycleTests`, `EquipmentGrantSwapTests`, `EquipmentResourceTests`, `AfterburnerResourceTests`, `ResourcePoolTests`, `ShipSystem/PlayerSpawningTests`, and `NetworkSystem/WorldTopologyTests`.

### 2 Move item ownership with its UI and protocol integration

This is one coordinated feature slice. Do not land ship-owned equipment with the old player-relative transfer protocol still enabled.

- [x] Construct one session `ItemStore`; inject it into player inventory and ship equipment. Add explicit container removal that removes associated item/location indexes under a defined destruction operation. A view unbind must never destroy an authoritative container.
- [x] Give `PlayerSession` its personal inventory and each ship its equipment. Extract ship-owned loadout integration from `PlayerItemState`; remove the old control-following stat/grant bridge when callers migrate.
- [x] Prepare candidate item, loadout, grant, stat, and resource changes before committing an equipment transaction. Currently loadout validation runs in an equipment-change subscriber after item commit; replace that ordering. A rejected combined loadout must leave item identities, both container revisions, grants, stats, and resources unchanged. Publish observers only after the coherent commit; an observer exception remains a programming error and must not be reported as a rejected or rolled-back transaction.
- [x] Let `EquipmentController` accept validated slot definitions. Pass existing defaults through ship composition initially; retain current slot keys and category multiplicity rules. Do not add a ship schema redesign just to inject the existing layout.
- [x] Move starter loadout creation from `NetworkManager.Items` to gameplay spawning/composition. Resolve and initialize it once before exposing the ship as ready. Creating an explicitly bare ship remains supported.
- [x] Rebind local inventory/equipment UI, transfer service, hotbar availability, and targeting through one disposable local binding scope. Revoke edit tokens and cancel drag/targeting first; preserve player hotbar assignments while resolving equipment slots against the new ship. Cache keys include ship/container identity or binding generation, not revisions alone.
- [x] Handle `UiCanvas.BindInventory` clearing hotbar bindings. Update fixed references in `PlayerController`, `ItemHotbarController`, `InventoryEquipmentTransferService`, and `AbilityTargetingController`; do not leave any subscriber on the previous ship.
- [x] Extend equipment-sensitive transfer commands with controlled ship identity and control generation. Resolve permitted containers on the host from the authenticated player; never grant access merely because a client supplied a valid container ID. Reject outdated commands at execution time.
- [x] Change item snapshots to carry explicit ownership/container scope and revision identity. Generalize `ItemStore.Confirm` to validate and atomically apply a bounded batch of affected containers. Duplicate identities across retained containers and equal-revision conflicting payloads must be rejected without partial mutation.
- [x] On client control loss, evict the old private equipment detail replica while retaining public confirmed ship gameplay. On new control, install a matching equipment baseline before enabling item edits or equipment abilities. Do not compare revisions from different containers. Discard delayed old-binding snapshots and acknowledgements.
- [x] Preserve owner-only personal inventory and controlled-ship equipment detail visibility. Update join/recovery barriers, protocol compatibility, and codecs with the ownership change. Remove `_playerItems` and `LocalItems` as gameplay owners; transport may retain publication/request bookkeeping keyed by identity.

Primary files: `Items/ItemStore.cs`, `Items/PlayerItemState.cs`, inventory/equipment controllers, `World/PlayerSession.cs`, `World/GameWorld.cs`, `ShipSystem/ShipFactory.cs`, `GamePlaySystem/GameSessionFactory.cs`, `NetworkSystem/NetworkManager.Items.cs`, `NetworkSystem/ItemSnapshotCodec.cs`, transfer requests, hotbar, targeting, and UI bindings.

Acceptance: two players swap differently equipped ships without moving items, grants, stats, charge, or cooldown metadata between ships. Personal inventories remain with their players. Same-revision ship switches refresh the UI. Delayed transfers/abilities cannot apply to replacement ships. With no ship, personal inventory is readable and equipment actions are unavailable. Destruction and failed startup leave no orphan store entries or subscriptions. A candidate loadout with overflowing combined stats is rejected before any item or gameplay state changes. All item transfers remain atomic.

### 3 Centralize effective stats and contribution ownership

Depends on increment 2.

- [x] Extract `ShipStatState` and a typed effective stat view; distinguish equipment inputs from effective values. Keep `EquipmentStatCalculator` as a pure input/calculation helper if useful.
- [x] Move supported timed shield modifiers out of `ResourcePool` into the shared contribution store. Migrate equipment, ship flats, and effects without introducing an intermediate clamp or double-applying percentages.
- [x] Resolve and validate proposed stat changes before publishing them. Update resource capacities through production APIs preserving current amounts and continuity metadata.
- [x] Make the ship-facing stat view and resource capacity agree. Update HUD and gameplay consumers to use the intended effective or operational view rather than reconstructing values.
- [x] Remove the split `ShieldFlatModifier` transport interpretation when the effective snapshot contract replaces it. Update codec validation and compatibility in the same increment; clients install confirmed results without resolving modifiers again.
- [x] Resolve only when inputs change. Retain separate movement and gameplay revisions, and avoid allocating collections on unchanged per-step reads.

Primary files: `StatSystem/EquipmentStatCalculator.cs`, `ResolvedShipStats.cs`, `StatValueCalculator.cs`, `GamePlaySystem/ResourcePool.cs`, `ShipGameplayState.cs`, `AbilitySystem/AbilityRuntime.cs`, `AbilityGrants.cs`, `NetworkSystem/GameplaySnapshotCodec.cs`, and `UiSystem/HudSystem/PlayerStatsPanel.cs`.

Acceptance: equipment and effect contributions produce the same effective result through one path; source removal preserves unrelated contributions; missing systems suppress bonuses; capacity changes never refill resources; signed flats and overflow validation remain correct. `EquipmentStatCalculatorTests`, `ResourcePoolTests`, `ShieldBoostTests`, `AbilitySnapshotTests`, `ResourceSnapshotTests`, and HUD tests cover the changed contract.

### 4 Extract composed ship systems and complete their lifecycles

Depends on increment 3.

- [x] Introduce a ship runtime aggregate owning stats, resources, effect state, faction, and explicitly attached components. Migrate `ShipGameplayState` behind this aggregate, then remove its old implementation or retain only a single-state forwarding facade until its last consumer moves.
- [x] Extract power and shield stepping into cohesive systems. Keep ability definitions/execution session-owned and grants/effect instances ship-owned. Extract effect admission/expiry from the aggregate without broadening authored ability support.
- [x] Extract the movement motor from `ShipController` while retaining `CharacterBody2D` collision execution and `ShipPresentation` as Godot adapters. Preserve `MovementPhysics` formulas and typed movement configuration.
- [x] Implement validated attach, detach, failure rollback, and idempotent disposal. Track attachment-owned registrations explicitly. Empty/absent capability reads return a deliberate unavailable state; callers must not require an artificial engine or shield to exist.
- [x] Define each component's dependencies and phase in composition. A power availability change disables consumers; component removal unregisters its work. Preserve temporal metadata in ship state across component replacement.
- [x] Route gameplay mutations through the ship runtime's commit boundary. Snapshot application installs confirmed state without executing lifecycle side effects that grant resources or apply modifiers twice.
- [x] Extend versioned gameplay snapshots and validation to include runtime-component presence independently of installed equipment. Update effective availability and ability admission checks, join/recovery baselines, and movement sampling together. A detached power/shield component must have the same observable availability on the host and every client.

Acceptance: attach/remove/reinstall engine, power, shield, and ability components independently; verify disabled behavior and dependency handling. Failed attachment leaves state unchanged. Repeated removal does not throw, tick, publish phantom revisions, or retain listeners. Test detach through an effect deadline and reattach: no expired contribution returns. Confirm late-joining clients see detached capabilities correctly even with hardware and stored resources present. Ship destruction cleans every source and subscription. Battery/generator replacement and effect lifetime rules continue to pass.

### 5 Move simulation orchestration out of networking

Depends on increment 4. Move admission of configuration changes to the step boundary as part of this increment if not already required by increment 2.

- [x] Extract `GameplaySimulation` with the explicit phase order above and bounded command queues. Move `AdvanceGameplay` logic into it and inject it through `GameSessionFactory`.
- [x] Keep RPC/session authentication, sender rate limits, transport responses, readiness, and publication in networking. Domain validation and execution use the current player/ship aggregates; `AbilityCommandProcessor` no longer needs a second player-item dictionary.
- [x] Have `SynchronizationCoordinator` remain the sole fixed-clock caller. It supplies accepted input and calls gameplay phases exactly once, then invokes publication after the completed step. Avoid separate component `_PhysicsProcess` clocks.
- [x] Keep client prediction on the movement-only path. Update `MovementStateAdapter`, snapshot application, and prediction consumers to the new ship accessors without merging authoritative, predicted, and presented state.
- [x] Verify control-loss, despawn, and stop paths clear queued commands and binding-specific histories before disposing their targets.

Acceptance: offline/domain-driven gameplay checks execute without constructing `NetworkManager`; transport integration retains exactly-once resource advancement, expiry-before-activation, and power-before-movement. Configuration changes rebase prediction correctly. Uncontrolled ships do not begin moving as a side effect of iterating runtime components.

### 6 Remove migration scaffolding and validate the integrated result

Depends on increments 1 through 5.

- [x] Remove obsolete `PlayerItemState` ownership, old forwarding APIs, duplicate stat storage, and dead subscriptions after all callers have moved. Avoid retaining both old and new implementations as permanent alternatives.
- [x] Align namespaces and directories; split affected multi-type files where required by `AGENTS.md`. Use semantic IDE refactoring for declaration/reference changes. Keep comments useful, use straightforward loops, and preserve existing user changes.
- [x] Update architecture and gameplay documentation to describe the implemented boundaries and current afterburner demand. Mark this plan complete only with implementation and verification evidence.
- [x] Run integrated behavior, network, restart, and performance checks below. Update process fixtures that currently inject instrumentation by matching source text; a successful fixture launch alone must not count as evidence that the instrumentation still ran.

Acceptance: all consumers use the new ownership model, no old bridge remains, relevant regression suites pass, and lifecycle/network evidence demonstrates the required behavior. Any unavailable transport verification is explicitly recorded as outstanding rather than reported as passed.

## Verification and completion gates

Prefer existing production APIs over test-only accessors. Every executable declaration or line introduced solely for tests or manual verification, including process instrumentation, must carry the unique `TEST-SUPPORT` ID and fully qualified consuming test names required by `AGENTS.md`. Preserve and update existing markers when migrating tests.

| Area | Required scenarios and existing coverage to extend |
| --- | --- |
| Ownership | Two-player ship swap, unbind/rebind, independently destroyed ship/player, failed startup and session restart; `PlayerSpawningTests`, `WorldTopologyTests`, `GameSessionFactoryTests` |
| Item integrity | Atomic cross-owner transfer in one store, container destruction, duplicate identities, stale/equal-conflicting revisions, authorization and control-generation rejection; `ItemSystemTests`, `EquipmentTransferTests`, `ItemTransferBoundaryTests` |
| Local bindings | Same revisions on different ships, drag/target cancellation, missing ship, preserved hotbar assignments, new baseline readiness, old acknowledgements; hotbar, inventory boundary, targeting integration, and HUD coverage |
| Stats and resources | Formula, suppression, grants from multiple sources, source removal, effect expiry, spawn initialization, battery identity, shield replacement/downtime, afterburner deficit and recovery; existing equipment/resource/ability suites |
| Component lifecycle | Failed attachment rollback, removal while powered/moving, reattachment without refill, no repeated tick/notification after detach, explicit dependent-unavailable state |
| Prediction | Engine removal, mass change, power exhaustion/recovery while input is held, reordered gameplay/movement arrival, stale configuration and control changes; `EquipmentMovementTests`, movement history and synchronization suites |
| Replication | Full joins, late join during resource drain/downtime, ship transfer before/after private baselines, malformed batch rejection, mixed protocol rejection, reconnect/recovery; snapshot, compatibility, join and process tests |
| Scale | Normal and impaired runs at 5 and 10 participants, ongoing resource changes and equipment/system transitions, stable bounded histories/queues and 30 Hz publication |

Use `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj` for the managed suite and its Godot wrapper. Inspect the in-engine report as well as the outer result. Build with `dotnet build sMPGWM.Game/sMPGWM.csproj` when needed independently. Run `python tools/validate_gameplay_data.py` if definitions, schemas, or validators change; this cleanup should not require unrelated content migrations.

Extend the existing `tools/run_shield_slice.py` and `tools/fixtures/EquipmentAbilityVerification.cs.txt` scenarios for ownership and attachment changes. Exercise a host plus two clients for functional cases, then five and ten participants with normal/impaired delivery and held movement input through transitions. Verify the metrics and scenario assertions still execute after source extraction. Run the available GodotSteam lifecycle smoke separately and record whether actual remote Steam peers were exercised.

Record before/after host step timing, publication intervals, serialized bytes, allocations, GC counts/pauses, and scenario duration on the same workload. Keep movement packets within `MovementWire.MaxBytes` (currently 1,400 bytes for at most 10 players). Do not raise packet limits to accommodate the refactor. Resource-only changes must not cause repeated movement-history resets. Retain automatic GC; never force collection. A shared store enlarges current transaction dictionary copies, so measure transfer cost at representative inventory sizes before replacing the transaction implementation.

Completion means a new ship capability can be added through explicit composition, contribute supported stats/grants, and be removed with all registrations cleaned up, without adding a player-item lookup or gameplay rule to the transport layer. UI can discover the controlled ship, effective stats, and personal inventory through one player reference. The existing game still plays and synchronizes under its established rules.

## Follow-on features enabled by this cleanup

| Future feature | Intended extension point |
| --- | --- |
| Ship-specific equipment layouts | Validated layout input to ship equipment; authoring/schema expansion remains separate |
| Weapons and damage | Attached weapon runtime consuming effective stats and power; damage uses resource/protection APIs |
| Player passives and skills | Player-owned source attached to the controlled ship with explicit source cleanup on control loss |
| Cargo, stations, loot | Additional owner-scoped containers and permission rules through the same item authority |
| AI-controlled ships | Shared ship runtime and input contract; entity-based movement scheduling/replication is additional work |
| Movement-affecting abilities | Stat contributions plus a proven effective-tick transition/replay contract; the current rebase policy is not a general effect-history implementation |

These features are not completion requirements for the cleanup and must not be implemented merely to demonstrate abstraction.

## Implementation verification evidence

The integrated suite passes 272 outer tests and 199 in-engine tests with zero failures.
The explicit game build and final test run after the last allocation adjustment pass.
The fresh Godot XML reports 199 passed, zero failed. `git diff --check` passes.
The new regression cases cover two-player ownership swaps, atomic invalid-loadout rejection,
retained item identity collisions and equal-revision conflicts, private baseline scope, failed
startup/restart, same-revision hotbar rebinding/edit revocation, queued command generation checks,
component detach/reattach, expiry during processor absence, and effective snapshot validation.

The controlled transfer microbenchmark uses 400 transfers and inventories with 128 items, with
ordinary automatic GC enabled. It compares isolated versus shared store costs in the updated
implementation; it is not a historical executable comparison:

| Participants | Store | Total time (ms) | Allocated bytes | Gen 0 collections |
| --- | --- | ---: | ---: | ---: |
| 5 | Isolated | 14.059 | 8,195,712 | 1 |
| 5 | Shared | 13.470 | 24,323,712 | 1 |
| 10 | Isolated | 8.502 | 8,195,712 | 1 |
| 10 | Shared | 16.119 | 38,813,312 | 2 |

Shared dictionary copies increase transaction allocations. At ten participants this measured
about 40 microseconds per committed transfer. The transaction implementation is retained;
this is a measured cost, not a claim of allocation-free transfers.

### Functional multiplayer runs

Each run uses the normal game startup in separate Godot processes with held movement,
equipment/resource transitions, all five runtime-component detach/reattach cycles, and a
two-player ship swap and restoration. Assertions check that scenario and metric injection
actually executes. Impairment uses the existing seeded delivery proxy. Every successful run
below exited zero and converged. Source hashes and staging times are recorded in the logs.
The host scenario lasts 89 seconds; late clients have shorter lifetimes.

| Participants / delivery | Converged observations | Host mean / max step (ms) | Mean publication interval (ms) | Movement bytes | Host allocated MB | GC 0/1/2 | GC pauses (ms) |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 3 normal | 617 | 0.1435 / 36.6498 | 33.2871 | 358 | 68.87 | 4/2/1 | 67.09 |
| 5 normal | 1,675 | 0.1648 / 14.1164 | 33.2906 | 580 | 94.27 | 5/4/2 | 58.38 |
| 5 impaired | 1,670 | 0.1512 / 13.0889 | 33.2942 | 580 | 93.74 | 5/4/2 | 61.77 |
| 10 normal | 6,657 | 0.2490 / 16.0283 | 33.2878 | 1,135 | 149.77 | 8/7/2 | 84.61 |
| 10 impaired | 6,635 | 0.2487 / 16.4173 | 33.2903 | 1,135 | 148.64 | 8/7/2 | 86.51 |

Functional timing includes some concurrent managed build/test activity; use the separate
uncontended common-workload comparison for before/after performance. Allocations include
verification, logging, UI, and networking, not just domain simulation. Automatic GC is retained.
The 1,400-byte movement packet limit is unchanged. Input was extended from 45 to 65 seconds
in the later functional scenarios to cover component transitions as well as hardware changes.

Evidence: [3 normal](evidence/cleanup-equipment-resources-enet-3-moving.txt),
[5 normal](evidence/cleanup-equipment-resources-enet-5-moving.txt),
[5 impaired](evidence/cleanup-equipment-resources-enet-5-impaired-moving.txt),
[10 normal](evidence/cleanup-equipment-resources-enet-10-moving.txt),
[10 impaired](evidence/cleanup-equipment-resources-enet-10-impaired-moving.txt).
The first ten-player normal attempt found a fixture race: a target chosen near maximum range
moved outside range before host admission. The host correctly rejected it. The fixture now
uses an inner safety margin, applied equally to the common-workload comparison; production
range validation was not relaxed. The [failed attempt](evidence/cleanup-equipment-resources-enet-10-moving-fixture-range-failure.txt)
is retained separately from the successful rerun.

### Same-workload before/after comparison

The runner restores the original `HEAD` source into an isolated staging copy for the baseline,
retaining materialized Git LFS native/assets rather than substituting archived pointer text.
`--comparison-workload` uses the same original scenario, target safety margin, held input until
45 seconds, participant count, and 89-second host duration on the updated implementation.
New component/swap scenarios are disabled only in these comparison runs. Builds/tests were
paused during the four measurements. Each run passed and executed 5,340 host steps.

| Participants | Version | Host mean / max step (ms) | Publication interval (ms) | Movement / gameplay bytes | Host allocated MB | GC 0/1/2 | GC pauses (ms) |
| --- | --- | ---: | ---: | --- | ---: | --- | ---: |
| 5 | Before | 0.1344 / 11.6344 | 33.2884 | 580 / 1,828 | 81.56 | 4/3/2 | 54.626 |
| 5 | After | 0.1443 / 15.6064 | 33.2863 | 580 / 1,828 | 92.48 | 5/4/2 | 53.874 |
| 10 | Before | 0.2297 / 26.2633 | 33.2889 | 1,135 / 3,648 | 136.18 | 8/7/2 | 84.270 |
| 10 | After | 0.2444 / 16.8356 | 33.2883 | 1,135 / 3,648 | 153.20 | 9/7/2 | 90.883 |

These single-run measurements show modest mean-time and allocation increases, stable publication
cadence, and unchanged packet sizes. Maxima vary with scheduling and automatic GC; the result
is not a claim of zero pauses or a statistically established speedup.
Evidence: [before 5](evidence/baseline-equipment-resources-enet-5-moving.txt),
[after 5](evidence/comparison-equipment-resources-enet-5-moving.txt),
[before 10](evidence/baseline-equipment-resources-enet-10-moving.txt),
[after 10](evidence/comparison-equipment-resources-enet-10-moving.txt).

### Steam lifecycle

The [GodotSteam host smoke](evidence/cleanup-steam-host.txt) created a real lobby, entered the
game, simulated 840 physics steps, and shut down cleanly with exit code 0. Shield was 100/100,
energy 28/100, and the one-player movement packet was 136 bytes. This verifies the local
single-account host lifecycle. Actual remote multi-account Steam peers were not exercised;
that transport verification remains outstanding, distinct from the passing multi-process ENet runs.

### Late join during detached power, downtime and drain

The additional [three-player late-join run](evidence/cleanup-detached-latejoin-equipment-resources-enet-3-moving.txt)
passed with all three processes exiting zero and 615 observations across 232 entity revisions
converging. At 11 seconds the host detached power from existing ships, emptied one shield to
start its absolute downtime, and left another shield draining without power. The late client
joined at 12 seconds. Every process positively asserted detached power with future downtime,
a decreasing positive shield on the other ship, and later restoration of the same entity.
The late client observed detached state at tick 673 with deadline 840 and restoration at tick
962. The run also completed the ordinary ability/resource checks, all ten component transition
stages, and both ownership swaps. This is actual process evidence in addition to codec tests.

Reproduction commands (using the configured Godot .NET/GodotSteam executable):

```text
dotnet build sMPGWM.Game/sMPGWM.csproj
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj
python tools/run_shield_slice.py --players 3 --movement
python tools/run_shield_slice.py --players 5 --movement
python tools/run_shield_slice.py --players 5 --movement --impaired
python tools/run_shield_slice.py --players 10 --movement
python tools/run_shield_slice.py --players 10 --movement --impaired
python tools/run_shield_slice.py --players 5 --movement --baseline
python tools/run_shield_slice.py --players 5 --movement --comparison-workload
python tools/run_shield_slice.py --players 10 --movement --baseline
python tools/run_shield_slice.py --players 10 --movement --comparison-workload
python tools/run_shield_slice.py --steam-smoke
python tools/run_shield_slice.py --players 3 --movement --detached-latejoin
```

The baseline option restores `HEAD`; these recorded before measurements used the pre-cleanup
HEAD (`1076f5c114e38473672b2c4e4250c9404e2807b0`). To reproduce that historical comparison after committing this work, preserve/use that
original revision rather than treating a later HEAD as the same baseline.
