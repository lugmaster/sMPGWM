# Multiplayer synchronization — Implementation plan

Epic: [Multiplayer ships](backlog/multiplayer-ships-w-local-player-control.md).

Status: implementation integrated; automated ENet evidence recorded in the summary. Final acceptance remains open for the documented transport, collision-quality, and hands-on checks.

## Stories

1. [Network timing and snapshot scheduling](multiplayer-synchronization-story-01-network-timing-and-snapshot-scheduling.md)
2. [Client input delivery and authoritative host movement](multiplayer-synchronization-story-02-client-input-and-host-movement.md)
3. [Local prediction, reconciliation, and correction smoothing](multiplayer-synchronization-story-03-prediction-and-reconciliation.md)
4. [Remote ship interpolation](multiplayer-synchronization-story-04-remote-ship-interpolation.md)
5. [Synchronization lifecycle and network-condition verification](multiplayer-synchronization-story-05-lifecycle-and-verification.md)

## Foundation at planning time

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

## Implementation evidence (2026-11-03)

The active pipeline is implemented in SynchronizationCoordinator, MovementWire,
MovementPresentation, NetworkManager, and ShipController. Protocol compatibility is
now version 4. Story acceptance remains open for the transport and human evaluation
items below; compilation alone is not completion evidence.

- Session lifetime, 60 Hz ordering, completed-step publication, and synchronous stop
  are integrated through GameSessionFactory and NetworkManager.
- Commands use authenticated ownership, session/entity/control identity, six-command
  redundancy, a 32-tick future window, bounded queues, duplicate conflict rejection,
  and six-tick held-input fallback.
- Snapshots carry global movement, input policy, applied movement flags, completed
  tick, discontinuity revision, and input-arrival feedback. Ten records occupy
  805 bytes before Godot/transport framing, below the enforced 1,400-byte payload cap.
- Prediction keeps 128 states; corrections compare matching ticks with independent
  0.1-position, 0.1-velocity and 0.001-radian tolerances plus discrete input state.
  Replay is capped at 64 steps. Corrections decay over 80 ms; offsets over 100 units
  snap. Remote buffers hold at most 64 samples, interpolate shortest angles and
  discrete lower-bracket flags, and hold at underrun.
- Lead is bounded to 3�24 ticks using measured RTT/jitter; catch-up is capped at two
  explicit fixed steps per callback. Remote delay is four ticks beyond estimated
  inbound transit. Clock corrections slew by at most one tick per observation.
- Reliable join baselines, recovery request identity/session checks, 500 ms host
  request limits, one-second retries, five-second recovery timeout, replacement
  bindings, control release, despawn cleanup, and fresh session identity are wired.
  A callback gap over 500 ms or snapshot silence over one second invokes recovery.
- The replay gate uses CharacterBody2D.MoveAndCollide with explicit displacement and
  at most four slides, preserving polygon geometry. Twelve restored historical
  steps in one Godot physics callback reproduced wall sliding at (39.99978, 36).
  Replay has no input sampling, RPC, teleport notification, or camera-reset effects.
  Remote collision bodies use newest authoritative placement, independently from
  delayed visuals; moving-ship contacts remain an approximation without world rollback.

Reproduction on Windows, Godot 4.7.2 Mono, .NET 8:

    $env:GODOT_BIN = 'C:/Users/lober/godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
    dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --verbosity quiet
    & $env:GODOT_BIN --headless --path sMPGWM.Game --script ../sMPGWM.Tests/Network/replay_gate.gd
    powershell -NoProfile -File tools/test_synchronization.ps1
    powershell -NoProfile -File tools/test_synchronization.ps1 -Impaired -Lifecycle -ReverseJoin

The NUnit command passed 226 outer cases, including the Godot integration runner.
Its session checks verify exactly 30 publications after 60 completed host steps and
no callbacks after teardown. Added checks cover command deadlines, conflicting
duplicates, fallback release, payload corruption, capacity, angle wrap, underrun,
discontinuity snapping, and stale replacement bindings.

Three-process ENet tests passed in normal and reversed join order with 60 FPS host,
30 FPS client A and 144 FPS client B. The headless fixture supplies window focus
only; thrust, afterburner and braking pass through the normal input pipeline.
Each peer observed all three ships moving. The lifecycle profile also observed the
replacement entity moving after a teleport and replacement.

The deterministic movement-traffic impairment profile uses 25�75 ms delay each way,
10% random loss, 5% duplication, arrival reordering, a 250 ms loss burst, and one
750 ms client stall. Reliable lifecycle traffic is not artificially delayed.
Earlier reversed/lifecycle run: host 1,194 completed ticks / 597 publications;
client A 18 corrections / one recovery; client B 19 corrections / two recoveries.
Maximum queue was 12, maximum history 30, maximum replay cost 0.159 ms, maximum
same-tick correction 3.333 world units. Observed RTT estimates were 102�162 ms.
These are local headless measurements, not a performance guarantee.

Outstanding acceptance: Steam multiplayer coverage, human responsiveness/visual/
camera review, a dedicated
moving-ship collision artifact assessment. The impairment fixture is an application
delivery simulation, not an OS-level network emulator. Raw smoke logs may include
platform account identifiers; the harness now writes detailed logs under TEMP.

Final verification update: the 30 FPS startup race found in the capacity rerun is
fixed by a bounded prediction warm-up phase after reliable baselines. The baseline
remains visible while fixed steps build lead; routine snapshots cannot repeatedly
trigger recovery before the first predicted history exists.

The final ten-process run passed: all ten peers observed ten moving ships, no
client recovered, host completed 2,872 ticks and 1,436 publications. Maximum command
queue was six, maximum history 12, maximum replay cost 0.233 ms, maximum correction
5.157 units, and peak capture/publication cost 3.706 ms including startup. Host ran
48 seconds, including approximately nine seconds with all ten peers connected.
Machine: AMD Ryzen 7 7700, 16 logical processors, approximately 63 GiB RAM.
Repeat with: powershell -NoProfile -File tools/test_synchronization.ps1 -Players 10.
This is a capacity smoke, not a long-duration soak.

Final impaired/reversed/lifecycle rerun also passed after warm-up integration:
host 1,193 ticks / 596 publications; first joining client B 16 corrections / two
recoveries; client A 17 corrections / one recovery. Maximum queue 11, history 32,
replay 0.178 ms, correction 4.673 units; RTT estimates 131–159 ms. Both clients
observed the replacement ship moving. Final NUnit run remains 226 outer cases
passed, including transformed-parent, session cleanup and replacement tests inside
Godot. The whitespace diff check passed.

Generated artifacts from earlier runs remain under docs/plans/evidence/synchronization
and sMPGWM.Tests/TestResults: automatic approval review rejected their cleanup as
blocked by policy. They are not required source files and should not be committed.
