# Network implementation plan

Status: NET-14, NET-01 and NET-03 Verified, 2026-10-31. Next checkpoint: NET-00 native capability gate. See the final handoff below.

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
| `GameFlowManager` | Navigation and startup-data handover into `main_game.tscn`; does not own network state or a persistent network manager |
| `NetworkManager : AbstractSingleton<NetworkManager>` | Node in `main_game.tscn`; sole attempt/session owner; stores `NetworkType` and `NetworkRole`, retains its active network child through `IActiveNetwork`, coordinates API settings/subscriptions, state, ticker and teardown |
| `NetworkTicker` | Session-scoped Node supplying local physics ticks to the manager |
| `SessionProtocolNode` | Session-scoped matching name/path RPC endpoint, ready before peer assignment |
| `HostSessionFlow` / `ClientSessionFlow` | Plain classes sequencing immutable requests, cancellation and explicit results |
| `EnetNetwork` / `SteamNetwork` | Nodes extending `BaseNetwork` and implementing `IActiveNetwork`; create and assign their own peers, handle transport-specific operations and native cleanup; children of `NetworkManager` |
| `SteamMultiplayerPeerWrapper` | Existing native Steam peer bridge used by `SteamNetwork`; also supplies typed wrapper methods for required GodotSteam calls missing from the C# bindings |
| `SteamworksHelper` | Existing Steam initialization/pumping/shutdown; failure leaves LAN usable |
| `SteamLobbyService` / `SteamServerBrowser` | Internal helpers for serialized native operations/membership and immutable discovery snapshots; active-session operations remain behind `IActiveNetwork`, with no independent session owner |
| `PeerAdmission` / `PeerHealthMonitor` | Plain state machines driven by manager and monotonic clock |
| `GamePlayManager` | Explicit scene initialization result; activation only after admission |
| Compatibility, world-join and spawn helpers | Plain data/logic/coordinators; no additional session owner |

Keep `BaseNetwork` and `IActiveNetwork`; do not replace this structure with peer adapters or a second manager. Replace static completion events and mutable global session inputs as needed. All consumers access the manager directly as `NetworkManager.Instance`; never cache it in a field/local variable or resolve it with `GetNode`. This rule concerns manager access, not the internal conversion of an active network implementation to a Node for `AddChild`.

Follow README no-LINQ, namespace/path and file-per-type conventions. All logging, including diagnostic exchange and spawn logs, uses `using Logger = sMPGWM.Scripts.LoggingSystem.Logger;` and the appropriate `Logger` methods, never `GD.Print`.

Use `SteamMultiplayerPeerWrapper` for native Steam multiplayer peer access. For other Steam interactions, use the existing bindings in `sMPGWM.Game/addons/godotsteam_csharpbindings` wherever available. If a required GodotSteam operation is not exposed there, add a typed method to `SteamMultiplayerPeerWrapper` that invokes the GodotSteam method exposed to GDScript via Godot's dynamic call API on the appropriate native object/singleton. Keep method names, argument/Variant conversion, return validation and errors inside the wrapper; do not scatter raw dynamic calls through manager, gameplay or UI code. Verify the native method/signature against the installed GodotSteam build; a missing C# binding alone does not mean the native capability is unsupported. The wrapper remains an implementation detail behind `IActiveNetwork` for active-session interactions.

### Scene entry and transport boundary

- Main menu selects `NetworkType` and `NetworkRole` and hands them over as startup data when navigating to `main_game.tscn`. A selected Steam lobby ID or LAN endpoint is target data, not connection state. Menu browsing may display discovery snapshots, but the menu never creates a session, hosts/joins, observes connection state or retains `NetworkManager`.
- Deliver startup data before the manager starts. Store role/type in `NetworkManager`, consume and clear temporary handover data, and read session choices only from `NetworkManager.Instance` thereafter. Start hosting/joining once the loaded main-game scene's manager, RPC endpoint and active child are ready; no connection attempt precedes scene entry.
- Manager constructs the selected `BaseNetwork` implementation, retains it as `IActiveNetwork`, adds it as a child and starts it only after child readiness. It stays alive for the manager's lifetime. Subscribe to connection/failure events and configure the shared Multiplayer API before peer creation/assignment.
- `IActiveNetwork` is the sole access point for transport-specific operations and underlying peer ownership. Keep peers/wrappers private to their implementing Node; neither manager nor gameplay code creates or assigns them. Expose required Steam session interactions through this same interface. ENet implements intentionally unsupported Steam-only methods by throwing `NotSupportedException` with a clear reason; `NotImplementedException` is reserved for unfinished code. Do not use exceptions as normal transport selection flow.
- ENet host calls `var result = _peer.CreateServer(GameConfig.Port, maxConnections);`, checks `Error.Ok`, then assigns `Multiplayer.MultiplayerPeer = _peer;` in `EnetNetwork`. Client similarly captures/checks `_peer.CreateClient(GameConfig.ServerIpAddress, GameConfig.Port)` before the same assignment. Creation failure exits the local attempt; never assign a failed peer. Host reports listening without waiting for clients; client awaits actual connection/failure with cancellation and timeout.
- Steam host starts its peer only from successful `OnLobbyCreated`; Steam client starts its peer only from successful `OnLobbyJoined`. After successful wrapper host/client creation, `SteamNetwork` executes `Multiplayer.MultiplayerPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();`. A failed callback for the chosen role, or peer creation failure, ends the local attempt and returns to the main menu after cleanup. Host's incidental `LobbyJoined` never starts a client or completes host startup. Ignore unrelated/stale callbacks; drain late membership as described below.
- After peer assignment, gameplay networking uses Godot's high-level Multiplayer API and Node RPCs, with matching endpoint paths and the same Multiplayer API context. Steam lobby operations and transport cleanup stay behind `IActiveNetwork`; no custom peer packet path for gameplay.
- World changes require a complete quit to the main menu and fresh entry. Manager, network child, ticker, protocol endpoint and world never persist across that transition. Scene presence alone does not authorize gameplay; initialization/admission still gate activation.

Engine/Steam calls stay on the main thread. Subscribe before native work, handle reentrancy and observe async callback exceptions. Each attempt has one terminal result; stale callbacks cannot complete a newer attempt. Membership, connection, diagnostic success and gameplay admission are separate outcomes.

### Timing, capacity and cleanup

- `_PhysicsProcess` is the sole ticker source on every process. Manager/deadline checks run each physics step; gameplay network cycles run every second step (30/second at 60 Hz). Verify physics settings and reconcile the README's older 30 ms wording during implementation. Host tick observations never drive or overwrite the client's local tick. Keep normal engine multiplayer polling; no second polling/catch-up loop.
- Control/diagnostic/health replies are prompt. Monotonic time determines deadlines/RTT independently of tick counts, time scale and gameplay cadence. Test supported pause/background behavior and blocking-load stalls.
- Target typically 5 players, maximum 10 including host; pending peers count toward capacity. ENet remote slots equal `MaxPlayers - 1`; explicitly handle host-only configuration. Reuse bounded storage, avoid unnecessary recurring allocations, never force GC, and measure timing/GC under representative load.
- Capture `GameConfig.ConnectionTimeout` per attempt in milliseconds (currently `60000`). Diagnostic, preflight, scene, admission and health budgets remain independent. Initial scene budget: 60 seconds; admission after local readiness: 15 seconds. Bound waiting for `HostReady` too. Pongs never extend phase budgets.
- Cleanup invalidates the attempt/session first, cancels work, disables input and detaches owned subscriptions, including Multiplayer, Steam, GameFlowManager and static event subscriptions. Through `IActiveNetwork`, unpublish hosting, leave membership, clear only the implementation's assigned peer and close/dispose native transport resources. Clear session static references and singleton `Instance` on exit (only if it still refers to that instance), the retained active-network reference and pending handover data; reset fingerprint, DTOs, timers, counters and API settings. Let Godot free the scene-owned Node subtree; no manual duplicate Node-disposal scheme. Godot scene freeing does not substitute for native peer/lobby cleanup or removal of external/static references.
- Outstanding native callbacks may require an application-lifetime drain owned by the existing Steam runtime helper. Such draining retains only operation identifiers/cleanup data, never the old manager, scene Nodes or their delegates, and cannot complete a fresh session. No live session state survives return to the menu.
- Cleanup is idempotent on cancel, scene failure, menu return and exit, and precedes Steam shutdown. Local session-ending errors clean up before `QuitToStartMenuBlocking(reason)`; reserve `QuitWithOsError` for unrecoverable whole-game failures. Failed remote clients remove only their own pending work/slot, preserving host and healthy clients.

## Execution order

Statuses: Pending, In progress, Needs verification, Verified. A partial checkpoint does not complete its larger story.

| Order | Story checkpoint | Status |
| --- | --- | --- |
| 1 | NET-14: ENet diagnostic exchange | Verified |
| 2 | NET-01: shared ownership completion | Verified |
| 3 | NET-03: ENet completion | Verified |
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

Use launch arguments or a developer harness entering `main_game.tscn` through the production scene-owned manager, endpoint, `EnetNetwork` child and immutable startup data/results. Browser, fingerprint, health, roster and gameplay activation are unnecessary for this first proof; preserve the production scene-entry/lifetime contract.

1. Enter the main-game scene in both processes with role/type startup data; prepare manager, child and endpoint and subscribe before child-owned peer creation/assignment.
2. Host reports `Listening` without waiting for a client; create a fresh diagnostic session identity.
3. Client validates LAN endpoint, checks `CreateClient` immediately and awaits actual connection with cancellation/timeout.
4. Client sends reliable bounded `DiagnosticHello`: protocol version, fresh correlation ID and small text payload.
5. Host captures actual sender before awaiting, validates current connection membership/message bounds, logs receipt and sends `DiagnosticReply` to that sender with matching correlation ID and host session identity.
6. Client accepts only its connected server's reply for the current attempt/outstanding correlation. Both processes log role, actual sender, message kind and correlation. Only then report `MessageExchangeSucceeded`.
7. Client leaves; host retains session identity and accepts another attempt. Diagnostic messages never authorize gameplay handlers.

**Acceptance:** actual matching payloads via RPC between two processes; both tick locally without host tick packets. Verify host-alone startup, leave/rejoin, occupied port, unreachable endpoint, immediate creation/send errors, timeout, cancellation, duplicate starts, wrong-sender/unsupported/oversized/unmatched messages, late replies and clean retry. Record executable/build, inputs and both logs.

### NET-01: ownership completion

Finish shared lifetime contracts as resources are introduced: one active attempt, startup-data handoff, instance loss events and an endpoint lasting only for the current main-game scene/session. Use distinct LAN endpoint and Steam lobby targets. World changes always quit to menu and create a fresh manager/session on reentry.

**Acceptance:** late callbacks, scene exit, repeated cleanup, duplicate starts and retry cannot cause stale transitions, leaked ownership or a second completion. Repeated main-menu round trips clear singleton/static session references and external subscriptions; each entry constructs a fresh manager and active child. No menu connection attempt or cached/GetNode-resolved manager access exists.

### NET-03: ENet completion

Validate address/UDP port and capacity; distinguish immediate creation errors from connection/refusal. Preserve configured LAN inputs. Current `EnetNetwork.cs` ignores `CreateClient`'s result and polls connection status without cancellation; fix those behaviors within `EnetNetwork` and the shared `IActiveNetwork` startup contract. Verify intentionally unsupported Steam operations throw `NotSupportedException` on ENet.

**Acceptance:** occupied port, invalid endpoint/capacity, host-only configuration, pending slots, refusal, cancellation, leave/retry and Steam-unavailable LAN startup.

### NET-00: native capability gate

Use the exact custom Godot executable with ENet as control. Record engine/GodotSteam/MultiplayerPeer identity and evidence for native construction/errors/status, server peer ID, signal ordering, targeted reliable RPC, channels, auth, identity mapping, refusal/loss and close/retry. Complete third-peer checks at checkpoint 16.

Prefer `AuthCallback`/`SendAuth`/`CompleteAuth` for finite game/protocol/build preflight configured before peer assignment. Never hold native auth open for world/data initialization: normal RPCs are unavailable then. If the build demonstrably cannot support this, record evidence and select one bounded RPC preflight before admission; no selectable dual protocols or silent fallback. Both paths start after main-game scene entry.

Where supported, compare native Steam identity with lobby membership. Claimed DTO identity is not authentication; document unavailable mapping. Disable client relay if supported while still validating senders.

**Acceptance:** traces prove every relied-on capability; unsupported/unverified behavior stays explicit. A .NET build cannot verify native behavior.

### NET-02: Steam hosting and joining

`SteamNetwork` exposes active-session Steam interactions through `IActiveNetwork`, using internal lobby helpers as needed. Menu discovery exposes only snapshots/selection, never active-session state. Inspect installed correlation; serialize each operation kind, correlate lobby IDs where possible and drain canceled work through the Steam runtime helper without retaining exited scenes. Leave late successful membership. Local generations cannot distinguish identical native callbacks. If an uncorrelatable operation never returns, mark that kind unavailable until Steam runtime restart, with a reason; safe operations/LAN remain usable.

**Metadata:** publish/check `game_id` equal to configured `lobby_game_id` (`f6c96d72-6afb-4ac1-a14b-ee12daba65c6`), name, mode, version, protocol/build, original `host_steam_id`, `session_id` and state. `CoOP` is display mode, not pool identity. Initially use `DefaultHostedGameName = "sMPGWM TestLobby"` plus session suffix captured in the request; future input replaces only the base. Lobby/session IDs remain identity.

**Host:** create private unjoinable lobby; successful `OnLobbyCreated` records membership, checks metadata writes with state=preparing, creates/checks the server peer on virtual port 0 and assigns it inside `SteamNetwork`. Failed `OnLobbyCreated` or peer creation cleans up and exits to the main menu. Creator `LobbyJoined` is bookkeeping, never another completion/client connection. At checkpoint 5, make the private diagnostic lobby joinable once its endpoint is usable, without advertising a ready game. At checkpoint 13, await fingerprint, initialization and local-host admission before state=ready/public/joinable publication. Publish/update admitted-player count and capacity separately from membership count. Publication failure cleans up with a reason.

**Client:** join selected ID; successful `OnLobbyJoined` records membership, revalidates pool/version/protocol/build and diagnostic/gameplay readiness, rejects self/full/stale targets and pins original host/session. Require host membership/current ownership; create/check the peer to that host on virtual port 0, assign inside `SteamNetwork` and await connection/preflight. Failed `OnLobbyJoined`, peer creation or connection cleans up and exits to the main menu. Membership and `CreateClient` success are intermediate. Revalidate on metadata/membership changes; pinned-host departure, owner/session inconsistency or server loss ends the client session. Ownership transfer never migrates the world. Rejoin uses a fresh main-game scene/session locally and fresh admission identity/token against the host's current world/content state.

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

`GamePlayManager` returns one result/task per local scene generation after data/fingerprint, services, UI/input binding, world setup and spawn coordinator dependencies complete. Replace unconditional deferred `Start`. Flow is main-game scene entry -> Connecting -> Initializing -> Synchronizing -> InGame; connection starts in the loaded scene, and scene arrival alone cannot activate gameplay. Initialization may prepare dependencies while connecting, but activation awaits all gates.

**Acceptance:** await actual async/deferred dependencies. Failure, required-node exit, cancellation, stale completion and deadlines cannot activate input; admission gates activation.

### NET-13: world-join contract

`WorldJoinCoordinator` returns an explicit no-replication stub result tied to current host world identity and per-join cancellation. Admission awaits it while host progresses. Before real spawning, replace it with authoritative snapshot/catch-up at known tick/sequence and a bounded transfer budget.

**Acceptance:** success/failure gates admission; cancel one join without stopping host/others. Record that snapshots/catch-up remain unimplemented.

### NET-09: admission

Implement both ends together over reliable control:

1. `HostReady`: after host initialization, send current epoch/content fingerprint and per-peer token; the scene-owned endpoint receives while the client initializes its world/data.
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

Preserve main menu -> Host/Join -> LAN/Steam; final action hands off role/type and target data and enters `main_game.tscn`. Only then does `NetworkManager` allocate its active network child and begin hosting/joining. Replace sample rows in actual Steam join/entry scenes through `StartMenuProvider`. No waiting room or ready-up/Start screen. Main menu has no connection/session state.

Auto-refresh discovery on activation; show loading/empty/error and Refresh/Back. One keyboard-accessible selection by stable lobby ID; Join requires visible valid selection and no pending search/navigation. Sorting preserves selection; filter/refresh removal clears it. Client revalidates handed-off target inside the main-game scene. LAN uses address/configured port. Menu Back deactivates discovery before navigation; cancellation after scene entry goes through `NetworkManager.Instance`, cleans up and returns to the main menu. Steam capability failure disables Steam actions with a reason; this is runtime availability, not connection state.

**Acceptance:** scene navigation, status, LAN input, target handoff, keyboard selection, sort/filter/refresh, duplicate Join/Refresh prevention and shared inventory `FilterToolbar` paths/options regression.

### NET-12: integration

Exercise production flows: repeated host/client startup, join/leave/rejoin, three-peer isolation and last-slot races with unchanged host session/world identity. Cover host loss, scene/init/data/health failures, late callbacks, cleanup before Steam shutdown, optional-Steam LAN, pause/background and representative-load timing/GC. Verify world change requires quit to menu and fresh scene entry, with no retained singleton/static session references or external subscriptions. Verify role-specific Steam callback failures exit locally and cannot initialize the other role. Finish NET-00 multi-peer matrix and every story's acceptance; record stub/native limitations.

## Verification and handoff

For each checkpoint read repository instructions, this plan and relevant current code; preserve unrelated edits. Establish baseline failures, implement, then run focused deterministic state/codec/hash/clock tests, game build, relevant Godot scene checks and real multi-process integration. Follow existing NUnit conventions in `sMPGWM.Game/README.md`; discover the actual custom executable. Do not repeat passed checks without changes/unresolved concerns.

Record checkpoint/status, changed files, exact commands/build/inputs, results, evidence paths and limitations without account secrets. Mark Verified only when acceptance passes. Missing accounts/devices/tooling means Needs verification with precise next action; do not silently advance past the gate. End each checkpoint with a fresh-context handoff; unfinished work resumes the same checkpoint.

**Message milestone:** checkpoints 1-5 Verified, including actual separate-process ENet and Steam exchanges. **Full planned slice:** all checkpoints Verified, public discovery reaches production admission and host survives churn. Neither claims real replication/player spawning.

**Current checkpoint:** checkpoints 1-3 complete; next is checkpoint 4, NET-00. Earlier handoffs are superseded by the final NET-01/NET-03 handoff.

### NET-14 implementation handoff (2026-10-31)

Status: **Verified** for the diagnostic checkpoint. No Steam transport exchange, admission, replication, or gameplay activation is claimed.

User constraint overrides the optional-Steam wording elsewhere in this plan: the game retains its original mandatory Steam startup and startup failure behavior, including when LAN is the selected session transport. LAN and Steam are mutually exclusive for a session; a selected transport failure exits the session to the menu, never switches transport. SteamworksHelper is unchanged.

Implementation:
- Opt-in --enet-host / --enet-client arguments enter main_game.tscn through GameFlowManager, supplying an immutable NetworkStartupData consumed once by NetworkManager. Ordinary application startup remains unchanged.
- The scene-owned manager creates the named protocol endpoint and local ticker before the ENet child creates/assigns its peer. Client RPC starts only after ConnectedToServer. Host listening needs no remote peer.
- ENet checks creation errors, captures the attempt timeout, handles connection signals on the main thread, bounds capacity including host-only mode, and cleans native resources/subscriptions idempotently.
- Reliable bounded hello/reply messages validate version, sender, correlation, payload and host session. Each client attempt has one immutable result. Host responses are limited to one per connected peer; departure clears that peer's entry.
- Diagnostic gameplay stays inactive. Physics ticks remain local; the README now documents every-second-step cadence. Normal engine multiplayer polling remains enabled.
- Scene exit, cancellation and menu return stop the endpoint before transport teardown. QuitGame cleans the session before the existing Steam shutdown.
- No access levels were lowered and no test-only production methods were added. The external GDScript test driver is marked @test-context. MpManager remains untouched.

Validation:
- dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network: **5 passed**, including four standalone protocol tests and one real multi-process scenario matrix; game and test assemblies built successfully. Timestamps in retained evidence identify the final run.
- dotnet test sMPGWM.Tests: **177 passed, 1 failed**. The existing Godot inventory case UnknownDefinitions_AreRejectedIncludingDefaultAndZeroQuantity expects UnknownDefinition but receives NoChange at InventoryControllerTests.cs:131. This does not exercise network code.
- Runtime: Godot v4.6.2.stable.mono.custom_build.001aa128b, local GodotSteam .NET console executable under C:/Users/lober/godot_engine/Godot_v4.6.2_mono_steam/editor/. Steam initialized successfully using the unchanged startup.
- Integration tests stage real scripts, compiled assembly, scenes and assets in a temporary project with isolated user data. The test-only autoload drives existing production methods; it does not implement another transport or manager. Logs are attached by NUnit as enet-*.log.
- Inputs: headless processes, --enet-host / --enet-client, 127.0.0.1 and an OS-selected free UDP port, capacity 10 (also 1), connection budget 1000 ms, independent diagnostic budget 10000 ms. Default game connection budget remains 60000 ms. Each retained log excerpt records its exact command and port.

Acceptance evidence:

| Check | Evidence |
| --- | --- |
| Matching RPC payload and correlation in separate processes | Host and both client logs; production main_game.tscn endpoint |
| Host alone, local ticks, unchanged host identity | Listening before any client; increasing host tick values, independent client ticks; four exchanges against one session |
| Leave/rejoin and clean retry | Each of two client processes returns to menu and re-enters; new scene/manager/correlation, same host session; host port reused after cleanup |
| Duplicate starts | Repeated CreateNetwork, AssignRole and SendHello produce exactly one success per client attempt |
| Occupied port / immediate creation error | Second host reports CreateServer failure and exits its session |
| Unreachable endpoint / connection timeout / invalid endpoint | No host at loopback port; deadline cleanup; invalid address rejected |
| Cancellation | In-flight client cancellation returns to menu without diagnostic success |
| Diagnostic timeout | Connected host endpoint deliberately suppresses replies; client times out independently of connection budget |
| Immediate send error | Detached test endpoint attempts send without an assigned transport; native error is captured and terminal failure reported |
| Unsupported / oversized / unmatched / invalid-session replies | Actual RPC fault injection from host is rejected, then correct reply succeeds |
| Wrong sender, duplicate completion, late reply after cancel, fresh correlation | Standalone DiagnosticExchange tests |
| Bounds | UTF-8 byte boundary tests; 1-player host listens and cleans up |
| Cleanup and runtime errors | Successful client/rejoin/host-only paths assert no engine ERROR; shutdown occurs after session cleanup |

Retained evidence: [NET-14 log excerpts](evidence/net14/). Excerpts include engine version, commands/inputs and diagnostic/lifecycle lines, omitting account identifiers and unrelated logs. Full generated logs remain NUnit attachments.

Fresh-context handoff: resume at NET-01 if requested. Preserve the user's mandatory Steam startup constraint. The diagnostic harness deliberately does not activate gameplay. Existing menu startup and Steam ownership still need their later checkpoints; the full inventory suite failure remains separate.

### NET-01 handoff (2026-10-31)

Status: **In progress**, not Verified. Menu startup now uses the consumed-once immutable handoff; removed retained GameFlowManager session choices and manager fallback. Transport and readiness events are instance-scoped; role assignment is guarded. ENet clears event ownership even without a peer. Steam cleanup detaches OnSuddenExit, leaves recorded membership and suppresses pending connection continuations after cleanup. Mandatory Steam startup is unchanged.

Validation: dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network: all 5 passed, including multi-process ENet lifecycle scenarios; game/test builds successful with compiler warnings. git diff --check passed.

Remaining before acceptance: native Steam operation serialization/draining, role-specific callback and terminal completion guards, main-thread loss handling instead of polling, clearing only the owned peer, distinct typed transport targets, input shutdown and delayed menu-transition generation guards. Verify canceled native operations and repeated Steam round trips using the custom engine. Do not advance to NET-03; existing ENet tests do not prove Steam ownership acceptance.

### NET-01 continuation (2026-10-31)

Status: **In progress**. `SteamNetwork.cs` now guards duplicate starts and startup completion, restricts native callbacks to the selected role/target, completes hosting only after successful peer creation, replaces asynchronous status polling with Multiplayer API signals and a monotonic physics deadline, detaches those signals on cleanup, unpublishes host membership before leaving, and clears only its owned peer. Mandatory Steam initialization remains unchanged.

Validation: `dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network` passed all 5 tests (including the ENet process matrix); game and tests compiled with warnings. `git diff --check` passed. These tests do not verify native Steam signal ordering or callbacks.

Remaining: application-lifetime native operation serialization/draining (including canceled create/join and indistinguishable callbacks across retries), typed transport targets, input shutdown, delayed menu-transition generation guards, and real Steam round-trip/loss/cancellation verification. Resume NET-01; do not advance to NET-03 or treat the current role/target guards as protection against indistinguishable callbacks from an older attempt.

### NET-01 / NET-03 final handoff (2026-10-31)

Status: **Verified** for shared ownership and ENet completion. This supersedes the earlier NET-01 unfinished-work lists. It does not verify the later Steam exchange, public hosting, compatibility or admission stories.

Implementation:
- SteamworksHelper owns serialized create/join callback slots. Cancellation drops the scene delegate immediately, retains only operation/target state, blocks that operation kind until its callback returns, and leaves late successful membership. A callback that never returns keeps that kind unavailable with an explicit runtime-restart reason. Mandatory Steam initialization/failure behavior is preserved.
- SteamNetwork guards startup completion and role/target, uses connection/loss signals and a captured monotonic timeout, creates private lobbies, completes hosting after peer assignment, handles callback exceptions and clears only its owned peer. Native peer cleanup disposes its reference. Creator LobbyJoined cannot start or complete a client.
- Startup exposes distinct immutable LanEndpoint and SteamLobbyTarget values consumed by the selected transport. Existing constructor inputs remain compatible with callers.
- Session stop disables gameplay input/navigation and ticker processing. Deferred gameplay start respects a stopped session. Menu transitions carry generations so an earlier delayed quit cannot remove a newer scene. Scene exit clears singleton ownership; cleanup removes external Steam/Multiplayer subscriptions.
- ENet validates host port as well as client endpoint/capacity, refuses starts after cleanup, and reserves MaxPlayers minus one native remote slots. Host-only mode explicitly disconnects arriving peers: native RefuseNewConnections alone allowed a client-side connection followed by diagnostic timeout on this executable. Host-only verification checks server loss and no diagnostic reply.
- IActiveNetwork currently exposes no Steam-only operations and has no NotImplementedException stubs. Add unsupported ENet implementations when later Steam-specific interface operations are introduced.

Validation:
- `dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network`: **7 passed**, including two callback ownership tests and the production multi-process matrix. The subsequent full suite also passed the expanded matrix with pending-slot and delayed-transition checks.
- `dotnet test sMPGWM.Tests`: **179 passed, 1 failed**. The unchanged Godot inventory case `UnknownDefinitions_AreRejectedIncludingDefaultAndZeroQuantity` at InventoryControllerTests.cs:131 still expects UnknownDefinition and receives NoChange (same baseline failure as NET-14). Its inner Godot report has 129 passed and this one failure. Game/test builds succeeded with warnings.
- Runtime: Godot v4.6.2.stable.mono.custom_build.001aa128b, `C:/Users/lober/godot_engine/Godot_v4.6.2_mono_steam/editor/godot.windows.editor.x86_64.mono.console.exe`. Tests stage production scripts/assembly/scenes with isolated user data. ENet uses loopback, an OS-selected UDP port, a 1000 ms connection budget, capacities 1, 2, 10 and rejected 0/11. Steam lifecycle uses a 10000 ms budget and the existing local Steam account.
- Native Steam test cancels creation before callback delivery, observes late membership draining, then hosts/leaves twice with fresh managers/peers. Successful paths contain no engine ERROR. Deterministic tests additionally cover canceled join target matching, unrelated callbacks, blocked retries, late success/failure, reentrancy and duplicate completion.
- ENet matrix proves exchange/rejoin, duplicate starts, repeated cleanup, delayed quit invalidation across reentry, occupied port, invalid address/zero port/capacity, timeout, cancellation, host-only refusal, pending-slot capacity and retry. Existing protocol fault-injection checks still pass.
- Retained sanitized lifecycle excerpts: [NET-01/NET-03 evidence](evidence/net01-net03/). Full logs are NUnit attachments (`enet-*.log`); NET-14 evidence records full diagnostic commands and RPC exchange.

Scope boundaries: Steam-unavailable LAN is excluded by the user's mandatory Steam startup constraint recorded above. Two-account Steam exchange, client native signal/identity capability matrix, publication and host-loss policy remain NET-00/NET-02 work. Shared canceled-operation ownership is verified with native creation/draining plus deterministic join-callback tests; no two-account Steam exchange is claimed.

Fresh-context handoff: next is checkpoint 4, NET-00. Preserve mandatory Steam startup and transport exclusivity. NET-01/NET-03 completion does not imply completion of NET-00/NET-02: execution order, not numeric ID, defines these checkpoints.
