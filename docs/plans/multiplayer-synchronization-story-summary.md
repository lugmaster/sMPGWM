# Multiplayer synchronization — Implementation plan

Epic: [YAML-defined multiplayer ships with local player control](backlog/multiplayer-ships-w-local-player-control.md).

Status: discussion decisions documented; implementation pending. The five stories below replace original epic Story 8. Their numbering is local to this synchronization plan; camera following is now local ships Story 8 (formerly epic Story 9); original epic Stories 10–11 are out of current scope and retained for later reconsideration.

## Stories

1. [Network timing and snapshot scheduling](multiplayer-synchronization-story-01-network-timing-and-snapshot-scheduling.md)
2. [Client input delivery and authoritative host movement](multiplayer-synchronization-story-02-client-input-and-host-movement.md)
3. [Local prediction, reconciliation, and correction smoothing](multiplayer-synchronization-story-03-prediction-and-reconciliation.md)
4. [Remote ship interpolation](multiplayer-synchronization-story-04-remote-ship-interpolation.md)
5. [Synchronization lifecycle and network-condition verification](multiplayer-synchronization-story-05-lifecycle-and-verification.md)

## Agreed architecture

- The host owns authoritative movement for all ships. Each client predicts its own ship and interpolates the other ships.
- Simulation uses fixed 60 Hz physics steps. The host captures all ships after every second completed physics step and publishes snapshots at 30 Hz. Rendering is not the source of network time.
- Clients generate one input command per physics step and send batches every second step. Commands target host simulation ticks; clients estimate host time using RTT and input-arrival feedback.
- `PlayerController` retains local input/UI policy and participant binding from [original Story 3](multiplayer-ships-story-03-local-player-controller.md). Timing, transport, history, reconciliation, and interpolation are separate responsibilities.
- Host RPCs may execute locally with `CallLocal`; destinations remain explicit. Input goes to the host, while snapshots go to clients and may invoke a host-local handler. Host-local delivery must not simulate twice or reconcile authoritative state against itself.
- Keep `CharacterBody2D` and existing polygon colliders. Custom bounce, knockback, crash damage, and advanced collision prediction are deferred to playtest findings.
- Joining creates a fresh player/ship lifecycle. Late joiners still receive existing players' current state. Persistence and host migration are outside scope.

## Initial settings and unfinished implementation checks

| Setting | Initial plan | Qualification |
|---|---|---|
| Simulation | 60 Hz | Fixed timestep on host and predicting clients. |
| Snapshot publication | 30 Hz | After every second completed host physics step. |
| Input transmission | 30 Hz | Two new 60 Hz commands per ordinary packet. |
| Input redundancy | Four preceding commands | Initial tuning value; up to three transmission opportunities per command. |
| Missing-input fallback | 100 ms / six physics ticks | Initial configurable limit; then release active requests. |
| Remote buffer | Two snapshot intervals, about 67 ms | Additional buffering beyond transit delay; tune through playtests. |
| Numerical tolerances and visual smoothing | Separate configurable controls | Select values using world scale, speeds, serialization precision, and playtests. |

The discussion establishes the design direction, not measured network quality. Before implementing replay, verify how movement and Godot collision calls can safely execute multiple historical steps. Audit all mutable state that influences movement. Define bounded queue/history sizes, timing margins, correction thresholds, and recovery limits during implementation; record measured settings and the tested network envelope in Story 5.

## Delivery and acceptance

Implement Story 1 first, then Story 2. Stories 3 and 4 build on their tick/state contracts; Story 5 integrates the complete lifecycle. Observe the original Stories 1–7 milestone playtest before implementing this follow-up.

All multiplayer validation uses at least host H and clients A/B. All three ships must move on all instances; each participant supplies only its own input. The host simulates all ships once per step, clients immediately predict local movement, and remote ships use buffered presentation. Snapshot publication and input batching must not discard intermediate simulation/input steps.

Local ships Story 8 (formerly Story 9) still owns persistent camera behavior; it must follow the locally presented ship. Original Story 10 is deferred effects and animation work; this plan supplies replicated applied presentation state. Synchronization cleanup and adverse-network verification belong to synchronization Story 5; original Story 11 is deferred broader camera/UI/input integration and regression work. Required lifecycle cleanup and verification in the active stories remain in scope.

## References used in refinement

- [Godot high-level multiplayer and RPC semantics](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html)
- [Godot CharacterBody2D](https://docs.godotengine.org/en/stable/classes/class_characterbody2d.html)
- [Unity prediction and simulation/network rates](https://docs.unity.cn/Packages/com.unity.netcode@1.5/manual/intro-to-prediction.html)
- [Unity network time and arrival feedback](https://docs.unity.cn/Packages/com.unity.netcode@1.2/api/Unity.NetCode.NetworkTimeSystem.html)
- [Fusion prediction and interpolation](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/network-simulation-loop)
- [Fusion input history](https://doc.photonengine.com/fusion/v2/manual/input/player-input)

These are architectural references, not a decision to adopt another engine or networking framework.
