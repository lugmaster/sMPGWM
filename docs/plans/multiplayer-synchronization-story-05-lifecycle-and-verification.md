# Synchronization Story 5 — Synchronization lifecycle and network-condition verification

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md).

Status: planned. Implementation and measured network quality remain to be verified.

As a player, I can join, recover, leave, and restart without stale movement.

## Implementation

1. Own synchronization through GameSessionFactory/SessionScope and NetworkManager.StopSession. Disable callbacks before deferred node deletion; preserve SessionCoordinator failure/loading transitions and cancellation during acquisition.
2. Extend RequestSpawn → ReceiveInitialSpawns → ConfirmSpawnReady → JoinComplete with live movement captured at one completed tick and a timing reference. SpawnRecords supply construction identity/configuration, not current transforms. Reliably distribute host-issued session identity and control/discontinuity generations.
3. Gate movement readiness on entity creation, binding, baseline application, and timing initialization. Preserve join phase validation/timeouts; host command acceptance and client prediction activation must agree.
4. Handle reliable baseline/spawn versus unreliable snapshot races with bounded pending state and per-entity freshness. Never let an older baseline overwrite newer state. Unknown movement cannot create entities. Expiry and generation/tombstone rules prevent resurrection after despawn.
5. Keep existing participants running during late join; send their current movement without resetting ships/history.
6. On ControlledShipChanged invalidate old queues/history/presentation, release held input, and establish the new generation/baseline. Preserve camera continuity. This is lifecycle support, not a new ship-switching feature.
7. Remove per-peer queues, feedback, and buffers alongside MpManager.Despawn. Host loss uses existing exit flow. Restart issues fresh shared session identity and resets clocks, subscriptions, histories, static ticker usage, and pending packets.
8. Define a reliable bounded resynchronization request/response with authority, generation, request identity, rate/retry limits, and timeout behavior. After stalls/history exhaustion stop prediction, discard obsolete commands, apply current baseline/time, then resume. Never send seconds of accumulated movement.
9. Route partial baseline/spawn failure through existing cleanup so no partially active local ship or service survives.

## Verification

Extend SessionCoordinatorTests, GameSessionFactoryTests, JoinProtocolTests, PlayerSpawningTests, and LocalCameraTests as appropriate; add focused synchronization tests and reuse spawning smoke infrastructure.

Use H plus clients A/B with exchanged join order. Select and record concrete RTT, jitter, random/burst loss, duplication/reordering, stall, and render-rate profiles before execution. Exercise ENet and Steam where available; explicitly record missing transport coverage.

| Scenario | Required result |
|---|---|
| Three-player flight | Own input only; every ship moves everywhere |
| Late join during movement | Current coherent baseline; others remain active |
| Loss/reordering/duplication | No duplicate movement, stale rollback, or unbounded storage |
| Lost release/focus/UI blocking | Neutral commands or bounded fallback release requests |
| Clock drift/stalls | Bounded work and explicit baseline recovery |
| Teleport/reassignment | Generation reset, visual snap, persistent camera |
| Disconnect/host loss/restart | Correct removal and no old packet/callback effects |
| Partial startup/baseline timeout | Complete cleanup and usable next session |
| Walls/moving ships | Measured authoritative behavior and prediction artifacts |
| Capacity/payload pressure | Enforced message limits and acceptable publication cost |

## Completion evidence

Record reproducible commands, player count, transport, network profiles, machine performance, and packet sizes. Measure arrival age, late/fallback counts, prediction lead, publication cadence, occupancy/underruns, corrections, replay cost, and maximum queue/history usage.

Record final bounds, tolerances, smoothing/snap controls, redundancy, fallback, and remote delay. Automated tests establish ownership, ordering, protocol validation, and cleanup; H/A/B hands-on evaluation establishes responsiveness, visual quality, and camera behavior. Resolve blockers and document remaining approximations. The foundation's two-player playtest does not establish this acceptance.

Dependencies: Stories 1–4. Persistence, migration, effects, and broader deferred epic work remain outside scope; synchronization cleanup and validation are mandatory.
