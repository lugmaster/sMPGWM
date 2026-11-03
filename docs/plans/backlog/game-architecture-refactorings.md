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
