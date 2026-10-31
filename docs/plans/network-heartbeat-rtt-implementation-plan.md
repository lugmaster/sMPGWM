# Heartbeat and round-trip measurement

Status: proposed, 2026-10-31. Planning only; no implementation in this split.

Shared ownership, compatibility, initialization, protocol and cleanup contracts: [network plan](network-lobby-implementation-plan.md). Target code: `sMPGWM.Game/Scripts/Network`.

## Scope and ownership

Measure application liveness and RTT for host-to-client and client-to-host connections using the same probes. No client mesh and no server-list ping protocol.

`PeerHealthMonitor`, per-peer probe state and metrics snapshots are ordinary C# types with an injected monotonic clock. `NetworkManager._Process` drives their tick on the main thread, including supported paused states. `SessionProtocolNode` sends/receives probes and captures sender identity. Do not create one Node or Timer per peer or a separate health autoload. Existing SteamworksHelper keeps pumping Steam; health does not initialize another Steam runtime.

## Steps

1. Consume the verified NET-00 channel behavior and NET-08 session/preflight boundary. Define bounded challenge/reply fields and validation shared by both roles.
2. Implement a deterministic clock-driven monitor: create probes, track bounded outstanding challenges, consume valid replies, expire deadlines and emit removal requests/metrics.
3. Wire protocol messages and manager ticks, beginning after preflight and stopping before peer/session teardown. Health may run while loading; gameplay admission remains independently gated.
4. Route expiry through the manager's common idempotent removal path. Publish read-only metrics for UI and diagnostics; rendering belongs to existing UI Nodes.

## Probe and RTT rules

Native events detect transport loss; application probes verify session processing. Probe host <-> each client after preflight, including loading/synchronization, without a mesh. Start with 2-second interval/10-second response deadline/startup grace, tune from measured loading. Use monotonic time, correlated sequence/token/session, and a fixed bounded outstanding set. Only live matching replies refresh health.

Reuse those probes to measure RTT; no additional ping messages or faster probe loop are needed. Store the local monotonic send timestamp with each outstanding challenge. On its matching reply, calculate RTT in milliseconds as local receive time minus that stored send time. Echo the challenge identity promptly from the persistent endpoint; do not depend on remote timestamps or synchronized clocks. Consume each reply once. Duplicate, unsolicited, expired, wrong-peer or wrong-session replies produce neither RTT samples nor health refreshes. Older out-of-order replies must not overwrite a newer published latency sample.

Expose per-peer latest RTT, smoothed RTT, sample count, and age of the last accepted sample through a read-only session metrics snapshot/event. Initialize smoothing from the first valid sample, then use an exponential moving average with alpha = 0.125. UI-facing Ping (ms) means smoothed round-trip time, not RTT divided by two; this does not measure one-way latency. Host metrics cover each client, and each client measures its host independently. Label the measurement as application RTT: it includes transport queuing, frame scheduling and remote reply processing. Reliable fallback can also include retransmission delays.

Before the first sample, show unavailable rather than zero. Once the last sample is older than the health response deadline, mark latency stale/unavailable even if removal is still pending. Reset all samples on peer removal, teardown and fresh admission/rejoin. Smoothing affects presentation only; health and admission still use their independent monotonic deadlines. These metrics describe connected peers; unconnected server-list entries must not display an invented ping from metadata or discovery order. Choosing a concrete HUD placement can follow the existing UI when implementation reaches it.

Prefer unreliable probes on a verified separate channel to avoid reliable backlog. If native peer requires reliable control, document/test congestion behavior before choosing it. Do not build another reliability layer. Keep endpoint and Steam callbacks processing during supported pauses/background operation.

Measure main-thread stalls and use supported nonblocking loading where needed. A persistent Node does not make blocking loads nonblocking; its watchdog cannot run during the stall. Remote peers may time out first. Test deliberate stalls/loss/delay/congestion and tune deadlines accordingly.

Host timeout removes only that client/transfers/admission, frees capacity and invokes future player-removal hook. Client timeout/server loss returns that client to menu. Leaving/rejoining cannot restart the host world. Removal/rejection/disconnect/teardown share idempotent paths.

## Acceptance and dependencies

Own NET-11. Test with a controllable clock: known RTT, first sample, EMA (100 ms then 180 ms gives 110 ms), missing/expired/duplicate/unsolicited/wrong-peer/wrong-session replies, out-of-order samples, bounded storage and unavailable/stale/reset behavior. Test loading, pause, deliberate stalls, loss and reliable-channel congestion using the actual engine. Host timeout removes only the affected client; client timeout exits that client. Probe traffic never extends admission deadlines. Integrate after protocol foundations; pure monitor tests can be written earlier. Feed loss/isolation cases into NET-12.
