# Game architecture refactorings

Status: implemented. Automated regression coverage added; live multi-client and Steam verification remain outstanding.

The architecture review identified three urgent refactorings: establish one session lifecycle owner, make game-system initialization transactional, and consolidate join protocol state. The implementation preserves the existing join validation and readiness guarantees.

## 1. Establish one session lifecycle owner

The reviewed implementation routed failure handling through `NetworkManager.FailSession`, `MpManager.HostLost`, `GameFlowManager.QuitToStartMenuBlocking`, and `NetworkManager.StopSession`. Membership removal also called between managers. `MpManager.Spawn` presented an error and rethrew to another failure handler. Shared flags and call ordering prevented repeated work.

- [x] Introduce a session coordinator responsible for startup, cancellation, failure, and teardown.
- [x] Have transport, join, and spawn components report failures to that owner without initiating navigation or presenting dialogs.
- [x] Keep scene navigation and error presentation in the presentation layer.
- [x] Give membership removal and resource cleanup explicit owners and an idempotent teardown order.

Acceptance: each failure produces one teardown and one user-facing outcome. Repeated disconnect, timeout, and cleanup callbacks are harmless. Returning to the menu and starting another session leaves no stale subscriptions, peers, or world objects.

Primary files: [NetworkManager](../../../sMPGWM.Game/Scripts/Network/NetworkManager.cs), [MpManager](../../../sMPGWM.Game/Scripts/Network/MpManager.cs), [GameFlowManager](../../../sMPGWM.Game/Scripts/GameFlowSystem/GameFlowManager.cs).

## 2. Make game-system initialization transactional

The reviewed `GamePlayManager.OnReady` constructed services, populates fixtures, binds UI, creates the local player, subscribes to events, and starts networking. There was no explicit rollback boundary for partial initialization. Mock player behavior and messages were also embedded in production composition independently of `PopulateMockInventory`.

- [x] Extract a session factory or composition root with a disposable session scope.
- [x] Separate construction, initialization, and activation; start networking only after required gameplay dependencies are ready.
- [x] Roll back successfully initialized resources and subscriptions when a later step fails.
- [x] Inject development fixtures explicitly while preserving the intended development experience.
- [x] Route initialization failures through the session lifecycle owner from item 1.

Acceptance: failures at each initialization stage release previously acquired resources, report one failure, and permit a subsequent session start. Development fixture selection is explicit. Normal shutdown and failed startup use the same ownership rules.

Primary file: [GamePlayManager](../../../sMPGWM.Game/Scripts/GamePlaySystem/GamePlayManager.cs).

## 3. Consolidate join protocol state

The reviewed host join state was spread across `_joins`, `_joinDeadlines`, `_answered`, `_rejected`, and `MpManager._ready`. Client state was represented by token/deadline fields and `_spawnRequested`/`_spawnApplied`. Multiple components could therefore update different parts of one logical lifecycle.

- [x] Introduce explicit host per-peer and client join state with defined transitions and deadlines.
- [x] Establish a single source of truth for readiness and rejection; membership consumers observe its transitions.
- [x] Keep RPC serialization and delivery separate from protocol transition logic.
- [x] Preserve authentication, payload validation, duplicate entity detection, deadlines, and local control binding before readiness acknowledgement.
- [x] Route protocol failure and cancellation through the session lifecycle owner from item 1.

Acceptance: verify duplicate messages, invalid transitions, timeout at each phase, rejection after spawn allocation, disconnect during snapshot application, and cleanup followed by a fresh join. Transport connection, applied world state, and player readiness remain distinct states.

Primary files: [NetworkManager](../../../sMPGWM.Game/Scripts/Network/NetworkManager.cs), [MpManager](../../../sMPGWM.Game/Scripts/Network/MpManager.cs).

## Implementation and verification

- `SessionCoordinator` owns the session lifetime. `SessionScope` releases resources in reverse acquisition order and continues cleanup after an individual cleanup failure. `GameFlowManager` handles navigation and presentation after session outcomes.
- `GameSessionFactory` composes gameplay services, UI bindings, participant storage, and networking. `GamePlayManager` publishes the completed composition and explicitly starts networking after scene readiness. Development behavior is supplied through `DevelopmentGameFixture`.
- `HostJoinProtocol` and `ClientJoinProtocol` own protocol transitions and deadlines. `NetworkManager` adapts RPCs and owns peer removal; `HeartbeatMonitor` publishes timeouts. `MpManager` owns participant and ship construction/removal. `SessionNotifications` handles membership messages.
- Teardown restores the previous multiplayer peer and authentication configuration. Deferred node deletion permits cleanup during RPC/signal callbacks.
- Added 18 plain C# protocol/lifecycle tests and 5 Godot integration cases covering failed fixture population, failed action setup, invalid network role, invalid endpoint, and two consecutive real LAN host startups with local ship binding and cleanup.
- Full suite result: 221 managed tests passed; the Godot wrapper failed because one pre-existing inventory test expects `UnknownDefinition` for an unknown item with quantity zero, while unchanged `ItemRequestService.CreateItems` returns `NoChange`. Inside Godot, 144 tests passed and that one inventory test failed. All 23 new regression cases passed.
- Live multi-client RPC sequencing and Steam transport have not been exercised in this change.

## Scope decisions

- Configuration compatibility hashing is deferred for later work.
- Movement replication and current-state synchronization are known future work covered by the [multiplayer synchronization plan](../multiplayer-synchronization-story-summary.md).
- `reload_config_on_start = true` is intentional in the development environment. It is not a defect or an urgent refactoring; retain the current configuration behavior.


## World and item ownership revision (2026-11-03)

The current workspace implementation now uses `Scripts/WorldSystem/GameWorld.cs` instead of
`Network/MpManager.cs`. GameWorld is session-owned, not a singleton. It owns ship
nodes and the player binding table. `PlayerSession` replaces `MpController` with a
plain C# object: changing control never reparents or destroys a ship. One scene
node per participant and its associated cleanup are eliminated. `SpawnEntity`
provides ship construction independently of player connections; topology loading
and player spawning share this path. Disconnect still explicitly destroys the
controlled ship, preserving the existing game policy until persistence is added.

Items, inventory, equipment, and hotbar now live together in `Scripts/Items`, with
matching namespaces. `ItemStore` is the existing item authority under a clearer
name; the forwarding `ItemController` object is removed. Container ownership,
revision checks, atomic transfers, equipment restrictions, and UI snapshots are
retained. Storage `ItemLocation` and UI `ItemCellAddress` have distinct names.
The unused global item-authority accessor is removed from GamePlayManager.

The session factory and cleanup scope remain the composition and lifetime boundary.
Prediction history, rendered positions, control generations, and authoritative
movement are intentionally distinct. They are not duplicate state to merge.

### Rules for the next gameplay features

- World entities belong to the world, eventually to active chunks. A connection
  references the ship it controls; it never owns that ship through scene parenting.
- Chunk persistence stores durable entity IDs and gameplay data, not live Godot
  nodes or network peer IDs. Runtime item/container IDs currently remain session
  IDs: durable allocation and restoration must precede item persistence.
- Player progression owns learned skills, attributes, and passives. Ships own hull,
  shields, cargo, and installed equipment. This is the initial design assumption.
- Cargo, station storage, wreck loot, and equipment use ItemStore containers.
  Equipment adds slot rules. Pickups transfer items through the same authority.
- Affixes and passives feed one stat calculation path. Start with ship requirements;
  the broader StateEngine documents are future design, not implemented runtime.
- AI and players provide commands to shared ship behavior. Bosses add encounter
  behavior, not another item, movement, or stat implementation.
- Asteroids and scrap fields are chunk content with persistent depletion and loot.
  They do not require full player progression components.
- Station services operate through docking and interaction commands. Walking
  interiors can be added later without moving inventory authority into the UI.

### Remaining roadmap, in dependency order

1. Persistent chunk load/change/unload/reload slice; durable identities and save
   schema; explicit boundary behavior. Inactive simulation initially freezes.
2. Host-authoritative item commands and per-entity container ownership. The current
   item authority is still local; do not treat it as replicated multiplayer state.
3. Mining, pickups, and saved depletion as the first complete gameplay loop.
4. Hull/shields, weapons, damage, destruction, basic AI, and dropped cargo.
5. Stat modifiers, affixes, skills/passives, different ships, then boss encounters.
6. Docking and station services; walking interiors remain undecided.

The current movement protocol remains player-oriented, and WorldTopology still
contains ship records with a 128-entity limit. Chunk interest management and AI
replication must extend those contracts before a large universe is supported.
No chunk persistence, AI simulation, combat, or RPG runtime is claimed by this refactor.

Validation: `dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --no-restore --verbosity quiet`
passed 230 outer tests including the Godot wrapper; its report contains 157 passing
Godot cases. Added coverage for world-owned uncontrolled ships, duplicate entity
rejection, control changes without reparenting, and cleanup. Existing transfer,
camera, prediction, topology, startup rollback, and restart coverage passed.
Live ENet/Steam multi-process playtests have not been repeated after this refactor.
