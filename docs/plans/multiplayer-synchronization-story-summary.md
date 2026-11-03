# Multiplayer synchronization — Implementation plan

Epic: [Multiplayer ships](backlog/multiplayer-ships-w-local-player-control.md).

Status: planned. Implementation and measured network quality remain to be verified.

## Stories

1. [Network timing and snapshot scheduling](multiplayer-synchronization-story-01-network-timing-and-snapshot-scheduling.md)
2. [Client input delivery and authoritative host movement](multiplayer-synchronization-story-02-client-input-and-host-movement.md)
3. [Local prediction, reconciliation, and correction smoothing](multiplayer-synchronization-story-03-prediction-and-reconciliation.md)
4. [Remote ship interpolation](multiplayer-synchronization-story-04-remote-ship-interpolation.md)
5. [Synchronization lifecycle and network-condition verification](multiplayer-synchronization-story-05-lifecycle-and-verification.md)

## Verified foundation

GameSessionFactory constructs PlayerController, MpManager, and NetworkManager. SessionScope owns cleanup, including partial startup. SessionCoordinator handles readiness/failure. MpManager owns participant and spawn-record dictionaries; MpController associates controlling peers with ships through ControlledShipChanged.

PlayerController currently samples input and calls ShipController.Simulate. Simulate requires LocallyControlled, applies MovementPhysics, and invokes CharacterBody2D.MoveAndSlide. ShipState captures local transforms and velocity; Teleport accepts global transforms. NetworkTicker has a static counter and emits HostDelta every second callback on both host and clients, without guaranteeing completed world movement.

NetworkManager's reliable handshake uses RequestSpawn, ReceiveInitialSpawns, ConfirmSpawnReady, and JoinComplete. Stored ShipSpawnRecord transforms describe initial construction, not live movement. PlayerController owns a persistent smoothed camera parented to the ship; ShipController exposes ShipVisuals through DisplayedPosition.

[Foundation stories 1–8](closed/multiplayer-ships-story-summary.md) are implemented. Their recorded host/client playtest passed on 2026-11-03. Three-player synchronization validation remains required.

## Target architecture

A session-owned synchronization coordinator orders input acquisition, command selection, fixed movement, capture of movement and inputs after the completed step, and publication. The host simulates every ship exactly once; clients predict only their own ship. PlayerController retains input/UI policy, binding, and camera ownership. Separate local input authority from simulation eligibility.

Use the active Network and Movement code. NetworkManager remains the protocol/lifecycle boundary; timing, queues, histories, reconciliation, and presentation have separate responsibilities and session lifetime. Do not build on Network/legacy.

Wire messages carry a host-issued shared session identity, entity ID, control generation, and host tick. Snapshot scope is currently movement and inputs, not complete world state. Movement uses global transforms, velocity, audited movement state, movement-related presentation flags, and discontinuity identity. Keep the shared envelope and scheduling separate from system-specific payloads so other systems can extend snapshots later without redesigning timing or publication. ShipRegistry retains immutable configuration.

| Setting | Initial target |
|---|---|
| Simulation | Fixed 60 Hz |
| Snapshots | Every second completed host step, 30 Hz |
| Commands | One per physics step, batches every second step |
| Redundancy | Four previous commands plus two new commands |
| Missing input | Hold at most six ticks, then neutral |
| Remote buffering | Two snapshot intervals beyond estimated transit |
| Histories, recovery, tolerances | Explicit bounded configuration measured in Story 5 |

## Delivery and acceptance

Implement Story 1 then Story 2. Stories 3 and 4 share their contracts. Integrate lifecycle hooks throughout; Story 5 completes recovery and validation. Prove collision replay feasibility before completing prediction.

Validate host H and clients A/B: all ships move everywhere, input ownership holds, simulation never doubles, snapshots represent complete ticks, and stale sessions cannot affect current state. Preserve focus/UI release, camera continuity, and failure cleanup.

Keep CharacterBody2D and polygon colliders. Full-world rollback, custom crash response, persistence, host migration, effects implementation, and broader deferred epic work are outside scope. Closed foundation stories are implementation context; these five stories are the active delivery plan.
