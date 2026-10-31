# Network implementation plan

Status: ready for implementation, 2026-10-31. All checkpoints are Pending; no runtime verification is claimed.

Single source for NET-00 through NET-14, replacing the host, client, lobby, lobby-list, heartbeat and refinement plans. Target: `sMPGWM.Game/Scripts/Network`.

## Scope

1. **Message exchange:** separate host/client processes exchange a correlated request/reply over ENet, then Steam, with cancellation, failure handling and repeatable cleanup. A supplied Steam lobby ID is sufficient.
2. **Running-game admission:** compatibility, explicit initialization, admission, health, public Steam discovery and real menus. The host keeps running while clients join, leave and rejoin.

World joining is an explicit **no-replication stub**; spawning logs only. Real replication/avatars, prediction, automatic reconnect, durable saves, generated-definition distribution and per-system replication schedules are deferred. [Roster/controller synchronization](network-roster-synchronization-plan.md) is a separate follow-up.

Introduce types only when needed. The harness uses production classes; avoid generic registries, event buses and parallel networking implementations.

## Shared contracts

### Ownership

| Component | Responsibility |
| --- | --- |
| `GameFlowManager` | Navigation; owns persistent `NetworkManager`; coordinates loading/activation |
| `NetworkManager` | Sole attempt/session owner; peer assignment, API settings/subscriptions, state, ticker and teardown |
| `NetworkTicker` | Persistent Node supplying local physics ticks to the manager |
| `SessionProtocolNode` | Persistent matching name/path RPC endpoint, created before peer assignment |
| `HostSessionFlow` / `ClientSessionFlow` | Plain classes sequencing immutable requests, cancellation and explicit results |
| `EnetPeerAdapter` / `SteamPeerAdapter` | Peer creation, immediate errors, actual connection/loss and native cleanup; no navigation |
| `SteamworksHelper` | Existing Steam initialization/pumping/shutdown; failure leaves LAN usable |
| `SteamLobbyService` / `SteamServerBrowser` | Plain classes for serialized operations/membership and immutable discovery snapshots |
| `PeerAdmission` / `PeerHealthMonitor` | Plain state machines driven by manager and monotonic clock |
| `GamePlayManager` | Explicit scene initialization result; activation only after admission |
| Compatibility, world-join and spawn helpers | Plain data/logic/coordinators; no additional session owner |

Replace static completion events, mutable global request inputs, legacy `MpManager`, `BaseNetwork` and `IActiveNetwork.GetNode()` as needed for the first buildable slice. Reuse useful native calls without retaining a second manager. Follow README no-LINQ, namespace/path and file-per-type conventions.

Engine/Steam calls stay on the main thread. Subscribe before native work, handle reentrancy and observe async callback exceptions. Each attempt has one terminal result; stale callbacks cannot complete a newer attempt. Membership, connection, diagnostic success and gameplay admission are separate outcomes.

### Timing, capacity and cleanup

- `_PhysicsProcess` is the sole ticker source on every process. Manager/deadline checks run each physics step; gameplay network cycles run every second step (30/second at 60 Hz). Verify physics settings and reconcile the README's older 30 ms wording during implementation. Host tick observations never drive or overwrite the client's local tick. Keep normal engine multiplayer polling; no second polling/catch-up loop.
- Control/diagnostic/health replies are prompt. Monotonic time determines deadlines/RTT independently of tick counts, time scale and gameplay cadence. Test supported pause/background behavior and blocking-load stalls.
- Target typically 5 players, maximum 10 including host; pending peers count toward capacity. ENet remote slots equal `MaxPlayers - 1`; explicitly handle host-only configuration. Reuse bounded storage, avoid unnecessary recurring allocations, never force GC, and measure timing/GC under representative load.
- Capture `GameConfig.ConnectionTimeout` per attempt in milliseconds (currently `60000`). Diagnostic, preflight, scene, admission and health budgets remain independent. Initial scene budget: 60 seconds; admission after local readiness: 15 seconds. Bound waiting for `HostReady` too. Pongs never extend phase budgets.
- Cleanup invalidates the attempt/session first, cancels work, disables input, removes peers, unpublishes hosting, leaves membership, clears only its assigned peer, closes/disposes resources and detaches owned subscriptions. Reset fingerprint, DTOs, timers, counters and API settings; retain only outstanding native callback draining until completion/shutdown.
- Cleanup is idempotent on cancel, scene failure, menu return and exit, and precedes Steam shutdown. Local session-ending errors clean up before `QuitToStartMenuBlocking(reason)`; reserve `QuitWithOsError` for unrecoverable whole-game failures. Failed remote clients remove only their own pending work/slot, preserving host and healthy clients.

## Execution order

Statuses: Pending, In progress, Needs verification, Verified. A partial checkpoint does not complete its larger story.

| Order | Story checkpoint | Status |
| --- | --- | --- |
| 1 | NET-14: ENet diagnostic exchange | Pending |
| 2 | NET-01: shared ownership completion | Pending |
| 3 | NET-03: ENet completion | Pending |
| 4 | NET-00: Steam capability gate | Pending |
| 5 | NET-02: private Steam diagnostic exchange | Pending |
| 6 | NET-08: protocol/preflight completion | Pending |
| 7 | NET-11: heartbeat and RTT | Pending |
| 8 | NET-06: compatibility | Pending |
| 9 | NET-07: explicit initialization | Pending |
| 10 | NET-13: world-join contract | Pending |
| 11 | NET-09: admission | Pending |
| 12 | NET-10: spawn log/activation | Pending |
| 13 | NET-02: public hosting completion | Pending |
| 14 | NET-04: discovery | Pending |
| 15 | NET-05: menus | Pending |
| 16 | NET-12: integration and remaining NET-00 matrix | Pending |

### NET-14: diagnostic exchange

Use a developer scene or launch arguments through the owner, endpoint, ENet adapter, small flows and immutable requests/results. No gameplay scene, browser, fingerprint, health or roster is needed for this first proof.

1. Create persistent owner/endpoint in both processes; subscribe before assigning peers.
2. Host reports `Listening` without waiting for a client; create a fresh diagnostic session identity.
3. Client validates LAN endpoint, checks `CreateClient` immediately and awaits actual connection with cancellation/timeout.
4. Client sends reliable bounded `DiagnosticHello`: protocol version, fresh correlation ID and small text payload.
5. Host captures actual sender before awaiting, validates current connection membership/message bounds, logs receipt and sends `DiagnosticReply` to that sender with matching correlation ID and host session identity.
6. Client accepts only its connected server's reply for the current attempt/outstanding correlation. Both processes log role, actual sender, message kind and correlation. Only then report `MessageExchangeSucceeded`.
7. Client leaves; host retains session identity and accepts another attempt. Diagnostic messages never authorize gameplay handlers.

**Acceptance:** actual matching payloads via RPC between two processes; both tick locally without host tick packets. Verify host-alone startup, leave/rejoin, occupied port, unreachable endpoint, immediate creation/send errors, timeout, cancellation, duplicate starts, wrong-sender/unsupported/oversized/unmatched messages, late replies and clean retry. Record executable/build, inputs and both logs.

### NET-01: ownership completion

Finish shared lifetime contracts as resources are introduced: one active attempt, explicit resource handoff, instance loss events and persistent endpoint across scene changes. Use distinct LAN endpoint and Steam lobby targets.

**Acceptance:** late callbacks, scene exit, repeated cleanup, duplicate starts and retry cannot cause stale transitions, leaked ownership or a second completion.

### NET-03: ENet completion

Validate address/UDP port and capacity; distinguish immediate creation errors from connection/refusal. Preserve configured LAN inputs. Current `EnetNetwork.cs` ignores `CreateClient`'s result and polls connection status without cancellation; replace those behaviors through the shared adapter/flow contract.

**Acceptance:** occupied port, invalid endpoint/capacity, host-only configuration, pending slots, refusal, cancellation, leave/retry and Steam-unavailable LAN startup.

### NET-00: native capability gate

Use the exact custom Godot executable with ENet as control. Record engine/GodotSteam/MultiplayerPeer identity and evidence for native construction/errors/status, server peer ID, signal ordering, targeted reliable RPC, channels, auth, identity mapping, refusal/loss and close/retry. Complete third-peer checks at checkpoint 16.

Prefer `AuthCallback`/`SendAuth`/`CompleteAuth` for finite game/protocol/build preflight configured before peer assignment. Never hold native auth open for scene loading: normal RPCs are unavailable then. If the build demonstrably cannot support this, record evidence and select one bounded RPC preflight before loading; no selectable dual protocols or silent fallback.

Where supported, compare native Steam identity with lobby membership. Claimed DTO identity is not authentication; document unavailable mapping. Disable client relay if supported while still validating senders.

**Acceptance:** traces prove every relied-on capability; unsupported/unverified behavior stays explicit. A .NET build cannot verify native behavior.

### NET-02: Steam hosting and joining

`SteamLobbyService` outlives screens and owns create/join/search callbacks. Inspect installed correlation; serialize each operation kind, correlate lobby IDs where possible and drain canceled work. Leave late successful membership. Local generations cannot distinguish identical native callbacks. If an uncorrelatable operation never returns, mark that kind unavailable until Steam runtime restart, with a reason; safe operations/LAN remain usable.

**Metadata:** publish/check `game_id` equal to configured `lobby_game_id` (`f6c96d72-6afb-4ac1-a14b-ee12daba65c6`), name, mode, version, protocol/build, original `host_steam_id`, `session_id` and state. `CoOP` is display mode, not pool identity. Initially use `DefaultHostedGameName = "sMPGWM TestLobby"` plus session suffix captured in the request; future input replaces only the base. Lobby/session IDs remain identity.

**Host:** create private unjoinable lobby, record membership immediately, check metadata writes with state=preparing, validate server peer on virtual port 0 and assign through manager. Creator `LobbyJoined` is bookkeeping, never another completion/client connection. At checkpoint 5, make the private diagnostic lobby joinable once its endpoint is usable, without advertising a ready game. At checkpoint 13, await fingerprint, initialization and local-host admission before state=ready/public/joinable publication. Publish/update admitted-player count and capacity separately from membership count. Publication failure cleans up with a reason.

**Client:** join selected ID, record membership, revalidate pool/version/protocol/build and diagnostic/gameplay readiness, reject self/full/stale targets and pin original host/session. Require host membership/current ownership; create peer to that host on virtual port 0 and await connection/preflight. Membership and `CreateClient` success are intermediate. Revalidate on metadata/membership changes; pinned-host departure, owner/session inconsistency or server loss ends the client session. Ownership transfer never migrates the world. Rejoin uses fresh admission identity/token and current world/content state.

**Acceptance:** real exchange with two accounts/devices; single creator completion; cancellation at each stage; late membership cleanup; wrong-pool/self/full/stale targets; construction/metadata/publication failures; host loss/owner transfer; repeated host/exit/retry; no leaked membership/callbacks. Public hosting proves readiness gates and unchanged host/world during client churn. Optional Steam failure leaves LAN usable.

### NET-08: protocol

Use targeted bounded primitives/containers, object decoding disabled, matching RPC declarations/path and synchronous sender capture. Admission envelopes carry protocol, host session identity, world epoch, content revision and fresh host-issued per-peer token. Local scene generation differs from host epoch. `PlayerJoinData` starts empty/immutable; identity comes from transport/state.

Bound admission payloads initially to 4 KiB, message rates and pending peers by capacity. Pending peers may use only preflight/readiness/rejection/world-join/health messages; gameplay handlers independently require admitted senders. Reject with bounded reason and short forced-disconnect grace, never indefinite delivery waits. Future snapshots use separate bounded/chunked transfer.

**Acceptance:** finite preflight, actual RPC delivery, pending capacity and malformed/oversized/unsupported/spoofed/wrong-state/session/token/epoch/revision messages. Use only evidenced native auth/channel behavior.

### NET-11: heartbeat and RTT

After preflight, probe host <-> each client including loading, with correlated sequence/token/session and bounded outstanding storage. Start with 2-second interval and 10-second response deadline/startup grace; validate against loading. Prefer unreliable probes on a verified separate channel; document/test congestion for reliable fallback. No mesh, extra ping traffic, peer Nodes or timers.

Local monotonic receive minus stored send time yields application RTT. Consume live matching replies once; only these refresh health. Duplicate, unsolicited, expired, wrong-peer/session replies do neither; older out-of-order replies cannot overwrite newer latency. Publish read-only latest/smoothed RTT, sample count and age. Initialize EMA from first sample, then alpha=0.125. UI Ping means smoothed round-trip milliseconds including queuing/scheduling, never RTT/2.

Show unavailable before sampling and stale/unavailable after the response deadline. Reset on removal, teardown and fresh admission/rejoin. Manager owns removal: host expiry removes one client; client expiry exits that client. A stalled main thread cannot run its own watchdog.

**Acceptance:** clock tests for known RTT, first sample, 100 then 180 ms -> 110 ms EMA, invalid/duplicate/out-of-order replies, bounded storage and unavailable/stale/reset. Engine tests cover loading, pause/background, stalls, loss/delay/congestion and isolation. Health never extends gameplay deadlines.

### NET-06: gameplay compatibility

One versioned typed policy carries logical ID, provider, comparison mode and serializer version; update table and implementation together. Each enabled entry requires exactly one successful provider output. Missing/duplicate/unsupported required entries fail; no directory scanner or silent empty hashes.

| Entry | Comparison |
| --- | --- |
| Effective item categories | Canonical gameplay IDs/relationships; exclude presentation |
| Effective item definitions | Canonical IDs, gameplay values/references |
| Future gameplay config overrides | Effective rules after defaults/fallback, once a provider exists |
| Future passives/effects/weapons/ships | Simulation values only when actually loaded; path constants are insufficient |
| Future generated definitions | Reserve provider; immutable host-accepted content/revision before use; distribution deferred |
| Schemas / packaged default files | No standalone hashing; validate locally and compare effective values above |
| Local config / presentation | Exclude logging, Steam/address/timeout/discovery preferences, cosmetics/labels |
| Host session settings | Communicate mode/capacity; include future simulation rules in effective gameplay policy, not local preference equality |
| Player instance state | Future synchronization, not global definition equality |

Capture inputs once, validate/build effective models and fingerprint those models. Honor loader semantics: YAML effective user file/default copy; config key fallback. Do not invent per-key YAML merging. Freeze session gameplay data.

Canonical projections include logical ID/serializer version, ordinally sorted entries/ID-keyed collections, gameplay-significant order and invariant numeric/enum/bool/null/default encoding. Reject unsupported/nonfinite values. Exclude presentation deliberately; review/version new simulation fields.

Make `ChecksumGenerator` pure: SHA-256 each canonical input, concatenate fixed-width lowercase digests deterministically, then hash the combination. Empty inventories/read failures return failure, never partial checksums or navigation. Raw path hashing may remain diagnostic. Immutable `SessionFingerprint` carries policy/canonical-format versions and gameplay digest; game/protocol/build checks remain separate. Reset at teardown; static config checksums cannot authorize admission. Report logical mismatches where available; no automatic mod downloads or anti-cheat claim.

**Acceptance:** equal effective overrides/default fallback match; formatting/property order/path/cosmetics are irrelevant; gameplay changes reject. Check required entries, excluded schemas/local config, read/capture failure and reset.

### NET-07: explicit initialization

`GamePlayManager` returns one result/task per local scene generation after data/fingerprint, services, UI/input binding, world setup and spawn coordinator dependencies complete. Replace unconditional deferred `Start`. Flow is Connecting -> Loading -> Synchronizing -> InGame; scene arrival alone cannot activate gameplay.

**Acceptance:** await actual async/deferred dependencies. Failure, required-node exit, cancellation, stale completion and deadlines cannot activate input; admission gates activation.

### NET-13: world-join contract

`WorldJoinCoordinator` returns an explicit no-replication stub result tied to current host world identity and per-join cancellation. Admission awaits it while host progresses. Before real spawning, replace it with authoritative snapshot/catch-up at known tick/sequence and a bounded transfer budget.

**Acceptance:** success/failure gates admission; cancel one join without stopping host/others. Record that snapshots/catch-up remain unimplemented.

### NET-09: admission

Implement both ends together over reliable control:

1. `HostReady`: after host initialization, send current epoch/content fingerprint and per-peer token; persistent endpoint receives while client loads.
2. `ClientReady`: after initialization and host fingerprint comparison, send client fingerprint/DTO. Host checks actual sender, token, current readiness/revision, capacity, policy entries and DTO.
3. Await current world-join result; input remains blocked.
4. `AdmissionAccepted`: record admission, invoke host spawn seam once, then send acceptance. Client activates only with matching acceptance and valid local initialization.

Local host uses the same gates without self-RPC or waiting for clients. Duplicate `ClientReady` may resend acceptance but never respawn; duplicate acceptance never reactivates. Immediate send failure removes peer. Acceptance lost after spawn log means admission then removal, not cross-machine atomicity/crash recovery.

**Acceptance:** ordering, matching/mismatching fingerprints, stale/duplicate messages, token/revision invalidation, independent deadlines and simultaneous clients. One failed join cannot terminate host/another client.

### NET-10: spawn log and activation

Host-only `StartSpawning(int peerId, PlayerJoinData data)` logs once per fresh local/remote admission. Matching acceptance activates client once. No player scenes, controllers or world replication.

**Acceptance:** no spawn on readiness/compatibility/world-join failure; duplicates/postdecision loss never repeat spawn or activation. Rejoin uses fresh admission state.

### NET-04: discovery

Browser produces immutable bounded snapshots: stable lobby ID, name, host/persona fallback, mode, version/protocol/build, state, admitted count and capacity. Browsing never joins membership or creates a peer/ticker.

Each refresh reapplies exact pool/version/protocol/build filters and Worldwide search; validate results and exclude invalid/self/not-ready entries. Counts are advisory; host pending/admitted state settles last-slot races. Preserve discovery order until local sort. Name/host search and sorting with stable-ID tie-breaks operate locally.

Serialize refresh through lobby service; deactivate UI listeners/publication while service drains work. Hidden menu-history screens remain in tree, so `_ExitTree` is insufficient. No invented browser ping from peer RTT/discovery order.

**Acceptance:** fixtures plus real published-host discovery; exact filters each request; empty/error/stale/full/wrong-pool/self entries; persona fallback; bounded results; canceled/late inactive responses. Search/sort make no discovery calls.

### NET-05: menus

Preserve main menu -> Host/Join -> LAN/Steam; allocate resources only on final action. Replace sample rows in actual Steam join/entry scenes through `StartMenuProvider`. No waiting room or ready-up/Start screen.

Auto-refresh on activation; show loading/empty/error and Refresh/Back. One keyboard-accessible selection by stable lobby ID; Join requires visible valid selection and no pending search/connect. Sorting preserves selection; filter/refresh removal clears it. Client revalidates handed-off target. LAN uses address/configured port. Back cancels setup before navigation; Steam failure disables Steam actions with a reason.

**Acceptance:** scene navigation, status, LAN input, target handoff, keyboard selection, sort/filter/refresh, duplicate Join/Refresh prevention and shared inventory `FilterToolbar` paths/options regression.

### NET-12: integration

Exercise production flows: repeated host/client startup, join/leave/rejoin, three-peer isolation and last-slot races with unchanged host session/world identity. Cover host loss, scene/init/data/health failures, late callbacks, cleanup before Steam shutdown, optional-Steam LAN, pause/background and representative-load timing/GC. Finish NET-00 multi-peer matrix and every story's acceptance; record stub/native limitations.

## Verification and handoff

For each checkpoint read repository instructions, this plan and relevant current code; preserve unrelated edits. Establish baseline failures, implement, then run focused deterministic state/codec/hash/clock tests, game build, relevant Godot scene checks and real multi-process integration. Follow existing NUnit conventions in `sMPGWM.Game/README.md`; discover the actual custom executable. Do not repeat passed checks without changes/unresolved concerns.

Record checkpoint/status, changed files, exact commands/build/inputs, results, evidence paths and limitations without account secrets. Mark Verified only when acceptance passes. Missing accounts/devices/tooling means Needs verification with precise next action; do not silently advance past the gate. End each checkpoint with a fresh-context handoff; unfinished work resumes the same checkpoint.

**Message milestone:** checkpoints 1-5 Verified, including actual separate-process ENet and Steam exchanges. **Full planned slice:** all checkpoints Verified, public discovery reaches production admission and host survives churn. Neither claims real replication/player spawning.

**Current checkpoint:** 1 — NET-14 ENet diagnostic exchange. Implementation not started; no runtime checks run. Next: inspect flow/network/Steam code and test conventions, establish baseline build/engine availability, then implement minimal owner/endpoint and ENet host.
