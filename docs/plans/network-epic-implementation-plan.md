# Network implementation plan

Status: NET-14, NET-01 and NET-03 Verified; NET-00 Needs verification, 2026-10-31. Next action: remaining native capability verification, not automatic implementation of another story. Follow the latest increment below.

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
| 4 | NET-00: Steam capability gate | Needs verification |
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

User layout constraint (2026-10-31): reuse `sMPGWM.Game/scenes/ui/menu/start_menu/join_steam_game_menu.tscn` and its existing `JoinSteamGameMenu.cs`, `lobby_list_entry.tscn` and `StartMenuProvider` navigation. This scene already provides the intended layout: lobby/host/player-count columns, scrollable lobby entries and the Join Steam Game button. Preserve that layout and wire real discovery, selection and joining into it; replace the sample rows with live entries. Add only required missing controls/status within the established design. Do not create a replacement join screen or a separate user-test UI.

Existing component inventory (inspected 2026-10-31; reuse and extend these files):
- `Scripts/StartMenuSystem/LobbyList/LobbyList.cs` is currently an empty plain-class scaffold. Implement the menu list/selection logic here as appropriate, consuming discovery snapshots without becoming a session owner.
- `Scripts/StartMenuSystem/HostSteamGameMenu.cs` already sends the Host/Steam choice through `StartMenuManager.InitializeGame`; preserve this scene-entry handoff.
- `Scripts/StartMenuSystem/JoinSteamGameMenu.cs` already wires the Join button but currently supplies only Client/Steam, without a selected lobby target. Complete its query/selection/join TODO and pass the selected stable lobby ID through startup data. Correct its current "Start Steam Game" title to match the join screen.
- `Scripts/Util/UiComponents/FilterToolbar.cs` is an `HBoxContainer` scaffold; reuse it and `scenes/ui/base/filter_toolbar.tscn` for search/sort. Inventory already uses this scene and directly resolves `FilterToolbar/LineEdit` and `FilterToolbar/SortSelector`; preserve those paths and inventory behavior when extending it.
- `scenes/ui/menu/start_menu/lobby_list_entry.tscn` already supplies a toggle Button with lobby-name, host-name and player-count labels. Bind real snapshot values and stable-ID selection to this row. Its current `focus_mode = 0` must be adjusted for the planned keyboard selection; preserve its visual layout.

Preserve main menu -> Host/Join -> LAN/Steam; final action hands off role/type and target data and enters `main_game.tscn`. Only then does `NetworkManager` allocate its active network child and begin hosting/joining. Replace sample rows in actual Steam join/entry scenes through `StartMenuProvider`. No waiting room or ready-up/Start screen. Main menu has no connection/session state.

Auto-refresh discovery on activation; show loading/empty/error and Refresh/Back. One keyboard-accessible selection by stable lobby ID; Join requires visible valid selection and no pending search/navigation. Sorting preserves selection; filter/refresh removal clears it. Client revalidates handed-off target inside the main-game scene. LAN uses address/configured port. Menu Back deactivates discovery before navigation; cancellation after scene entry goes through `NetworkManager.Instance`, cleans up and returns to the main menu. Steam capability failure disables Steam actions with a reason; this is runtime availability, not connection state.

**Acceptance:** scene navigation, status, LAN input, target handoff, keyboard selection, sort/filter/refresh, duplicate Join/Refresh prevention and shared inventory `FilterToolbar` paths/options regression.

### NET-12: integration

Exercise production flows: repeated host/client startup, join/leave/rejoin, three-peer isolation and last-slot races with unchanged host session/world identity. Cover host loss, scene/init/data/health failures, late callbacks, cleanup before Steam shutdown, optional-Steam LAN, pause/background and representative-load timing/GC. Verify world change requires quit to menu and fresh scene entry, with no retained singleton/static session references or external subscriptions. Verify role-specific Steam callback failures exit locally and cannot initialize the other role. Finish NET-00 multi-peer matrix and every story's acceptance; record stub/native limitations.

## Verification and handoff

For each checkpoint read repository instructions, this plan and relevant current code; preserve unrelated edits. Establish baseline failures, implement, then run focused deterministic state/codec/hash/clock tests, game build, relevant Godot scene checks and real multi-process integration. Follow existing NUnit conventions in `sMPGWM.Game/README.md`; discover the actual custom executable. Do not repeat passed checks without changes/unresolved concerns.

Record checkpoint/status, changed files, exact commands/build/inputs, results, evidence paths and limitations without account secrets. Mark Verified only when acceptance passes. Missing accounts/devices/tooling means Needs verification with precise next action; do not silently advance past the gate. End each checkpoint with a fresh-context handoff; unfinished work resumes the same checkpoint.

### Required user testing on a second device

User constraint (2026-10-31): manual testing requires a complete exported game build, launched normally and operated entirely through the UI. The user can return the full console log from each device. Do not require editor access, launch arguments, command files, developer harness commands, or manual RPC injection for user testing. Automated local harnesses remain available to the implementing agent.

This constraint overrides the earlier strict checkpoint sequencing only as needed to prepare a usable UI test build. Continue the necessary Steam host/join, menu and logging implementation across NET-02/NET-05 (and their required dependencies) while NET-00 remains **Needs verification**. Do not interpret permission to implement ahead as verification of native behavior. Earlier instructions to stop before NET-02 or dependent implementation are superseded for this purpose.

- **First request:** as soon as a complete build supports the required Steam host/join flow through actual menus, request the two-device test explicitly; do not wait for the entire epic if that flow is ready earlier. Prepare and locally validate the export and UI path first. Supply the build identity, normal launch instructions, two distinct Steam account prerequisites, exact menu/button sequence, expected visible outcomes and console-log location. Any lobby-ID exchange must be supported by UI controls; do not require command-line target selection.
- **Concrete first test:** host on device A, join through the UI on device B, leave and rejoin against the still-running host, close the host and observe the client outcome, then restart and retry. Diagnostic exchange, auth and identity observations must run automatically within the production scene/session flow and write useful console evidence. Logs should identify the build, transport, role, session/correlation, signal ordering, auth outcome, actual sender and channel observations, identity-validation result and cleanup outcome. Use the existing Logger; avoid account secrets. The user checks visible behavior and returns the complete console logs from both devices; the agent analyzes technical evidence.
- **Additional capability cases:** provide UI-only steps for refusal or other negative cases when available. Cases requiring fault injection remain agent-run checks or explicitly unverified; never ask the user to simulate unsupported harness actions. Method availability alone does not verify native auth, RPC/channel behavior or identity mapping. Keep any provisional preflight implementation and outstanding native assumptions explicit; no silent fallback.
- **After the result:** inspect both full console logs, record pass/fail or missing evidence per capability, fix failures and request focused UI reruns as needed. Keep NET-00 and affected acceptance criteria unverified until evidence exists. Request remaining NET-02 two-account cases through the same built-game UI workflow.
- **Final request: NET-12 integration.** Supply a separate UI-only end-to-end test for public discovery, compatibility/admission, host loss and repeated join/leave/rejoin, collecting full console logs again. Third-peer isolation and last-slot checks require a third peer; availability is not assumed, and two devices alone do not prove that matrix.

Every request must identify the checkpoint, exact UI steps, expected outcomes and logs to return. Prepare all feasible implementation and local validation before asking for the user's time. This section governs future handoffs and supersedes earlier requests for a user-operated capability harness.

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

### NET-01 handoff (2026-10-31, historical — superseded)

Status: **In progress**, not Verified. Menu startup now uses the consumed-once immutable handoff; removed retained GameFlowManager session choices and manager fallback. Transport and readiness events are instance-scoped; role assignment is guarded. ENet clears event ownership even without a peer. Steam cleanup detaches OnSuddenExit, leaves recorded membership and suppresses pending connection continuations after cleanup. Mandatory Steam startup is unchanged.

Validation: dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network: all 5 passed, including multi-process ENet lifecycle scenarios; game/test builds successful with compiler warnings. git diff --check passed.

Remaining before acceptance: native Steam operation serialization/draining, role-specific callback and terminal completion guards, main-thread loss handling instead of polling, clearing only the owned peer, distinct typed transport targets, input shutdown and delayed menu-transition generation guards. Verify canceled native operations and repeated Steam round trips using the custom engine. Do not advance to NET-03; existing ENet tests do not prove Steam ownership acceptance.

### NET-01 continuation (2026-10-31, historical — superseded)

Status: **In progress**. `SteamNetwork.cs` now guards duplicate starts and startup completion, restricts native callbacks to the selected role/target, completes hosting only after successful peer creation, replaces asynchronous status polling with Multiplayer API signals and a monotonic physics deadline, detaches those signals on cleanup, unpublishes host membership before leaving, and clears only its owned peer. Mandatory Steam initialization remains unchanged.

Validation: `dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network` passed all 5 tests (including the ENet process matrix); game and tests compiled with warnings. `git diff --check` passed. These tests do not verify native Steam signal ordering or callbacks.

Remaining: application-lifetime native operation serialization/draining (including canceled create/join and indistinguishable callbacks across retries), typed transport targets, input shutdown, delayed menu-transition generation guards, and real Steam round-trip/loss/cancellation verification. Resume NET-01; do not advance to NET-03 or treat the current role/target guards as protection against indistinguishable callbacks from an older attempt.

### NET-01 / NET-03 final handoff (2026-10-31)

Status: **Verified** for shared ownership and ENet completion. This supersedes the earlier NET-01 unfinished-work lists. It does not verify the later Steam exchange, public hosting, compatibility or admission stories.

Completion reconfirmed after closing the `_Ready()` exception gap: `SteamNetwork` catches native peer construction/setup exceptions locally and reports the original reason through `NetworkManager` cleanup to `QuitToStartMenuBlocking(reason)`. The focused network suite was rerun: **7 passed**. Native construction failure itself was not fault-injected; that path was checked by source inspection. Mandatory Steam autoload startup is unchanged.

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

### NET-00 partial handoff (2026-10-31)

Status: **Needs verification**. Do not declare the gate complete. Supporting implementation ahead of the gate is permitted only as described under required user testing and the final fresh-context handoff.

Added opt-in NET00_CAPABILITIES=1 tracing inside the existing native wrapper and enabled it in the process matrix. Native execution proves SteamMultiplayerPeer construction, disconnected initial status, successful host creation, connected host status, server ID 1, and two close/recreate cycles. The peer reports relay support false. Runtime method inspection finds both peer/Steam identity mapping methods and SceneMultiplayer auth/send/complete methods; presence is not proof of remote behavior or signatures. No preflight fallback has been selected.

Validation: dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network: 7 passed, including ENet control and native Steam lifecycle. Game/test assemblies built with existing warnings. Runtime/executable and inputs are unchanged from the preceding handoff; the added environment variable is the only test input change. Sanitized trace: [native-host.txt](evidence/net00/native-host.txt).

Remaining gate work: establish exact GodotSteam module revision/signatures; use two accounts/devices to trace Steam client signal ordering, targeted reliable RPC/channels, finite native authentication, actual identity mapping against membership, refusal/loss and client close/retry. The local run uses one account and cannot verify those remote behaviors. Retain explicit unknowns; third-peer checks remain checkpoint 16. Resume NET-00, preserving mandatory Steam initialization and transport exclusivity.

### Fresh-context handoff: verify past work before advancing (2026-10-31)

This is the authoritative next-action handoff, superseding earlier sequencing instructions where they differ. The current turn only updates this plan; implementation, builds, test execution and user-test requests resume in a fresh context.

**First action in the fresh context:** review the current working tree, the latest implemented checkpoint and retained evidence to determine which past work is verified and whether a useful exported-build user test is already possible. Preserve unrelated/uncommitted edits. Do not begin the next story before this review. Reuse valid passing evidence; rerun checks only for changes since that evidence, failures or unresolved concerns.

Current known baseline:
- NET-14, NET-01 and NET-03 have recorded verification, including the ENet process matrix and local Steam ownership/lifecycle checks. Their verification does not establish two-device Steam exchange.
- NET-00 is the most recently implemented checkpoint, and is only partially implemented/verified: opt-in native host capability traces and process assertions were added; the focused network suite passed 7 tests. Remote auth, identity, RPC/channels, refusal/loss and client retry remain unverified. The full-suite inventory failure recorded above remains a separate known baseline issue.
- The existing Steam join scene has the intended layout, but its script currently hands off Client/Steam without a selected lobby target. LobbyList and FilterToolbar remain scaffolds. The latest inspection therefore does **not** establish a usable two-device Steam test through the UI. Recheck the actual repository in the fresh context rather than assuming this remains unchanged.

Apply this decision order:
1. **Close verification gaps in already implemented work first.** Inspect relevant code and evidence and run only needed local checks. Correct failures before treating a checkpoint as complete. Distinguish local validation from acceptance that requires the user's second device.
2. **If a useful UI-only build test is already possible, explicitly request it now in that fresh context.** Prepare a concrete test package: exact build/export instructions or an available build, build identity, device/account prerequisites, numbered menu/button steps, expected visible outcomes and where to obtain the full console logs from both devices. Specify exactly which past checkpoint/capabilities this test verifies. Request the test before adding unrelated story work; retain Needs verification while awaiting results. Never ask for editor actions, launch flags or harness commands. Analyze returned logs before deciding acceptance.
3. **If the build/UI cannot exercise the outstanding acceptance, identify the precise missing path and implement the minimum necessary test-enabling work next.** Start with completing the current NET-00 local work; use the authorized NET-02/NET-05 dependencies where needed to make the remote test possible. Reuse the existing menus, LobbyList, row scene and FilterToolbar listed above. Log diagnostic evidence automatically in the exported UI flow. Do not request a non-actionable user test or mark remote checks passed. Reassess test readiness after each completed increment and request the user test as soon as it is actionable.
4. **Once the latest implemented story is fully verified and the backlog of past verification is up to date, start the next pending checkpoint in the execution order.** At present that means NET-02 private Steam diagnostic exchange after NET-00; account for any test-enabling pieces already implemented. If verification remains outstanding, continue the current checkpoint or its explicitly identified test-enabling dependency rather than silently advancing.

End the next work session with updated statuses, evidence, outstanding user-test results and one concrete next action. Preserve mandatory Steam startup, transport exclusivity, the existing UI layout, and the user's full-build/UI-only testing constraint.

### NET-00 readiness review (2026-10-31)

Status remains **Needs verification**. Reviewed the clean working tree, wrapper, retained host traces and join menu. Reused prior passing evidence. Local engine/module source provenance and native signatures are now recorded in [local source review](evidence/net00/local-source-review.md); exact compiled module revision and remote behavior remain unverified. No code changed or tests rerun in this increment. The join menu still lacks a selected lobby target, so no UI-only user test is actionable yet.

Concrete next action: implement the minimum selected-lobby UI and private diagnostic dependencies, preserving layout and mandatory Steam startup, then reassess exported-build test readiness. NET-00 remains the active checkpoint; no later story is marked complete.


### NET-00 UI test-enabling increment (2026-10-31)

Status: **Needs verification**; NET-02/NET-05 supporting work implemented, neither story complete. This supersedes the previous next action to implement lobby selection.

- Implemented LobbyList in the existing panel and row scene. The existing FilterToolbar container holds private-lobby ID entry and lookup; successful metadata lookup creates a selectable row. Invalid input, missing/incompatible/self-hosted targets and lookup timeout keep Join disabled. Changing the ID clears selection. Exit detaches the Steam metadata callback. This is supplied-ID lookup, not public discovery; sorting/search remain deferred.
- Join passes the selected lobby ID through immutable startup data. Host and Join Steam menu buttons now enter the production diagnostic flow. Gameplay remains inactive. The host shows its private lobby ID; a diagnostic overlay shows connection/result, the full log path and Return to menu.
- Host checks metadata writes and joinability results, remains private, and becomes joinable after peer assignment with state=diagnostic. Clients revalidate pool/version/protocol, pin original host/session, check host membership, and terminate on host departure or metadata inconsistency. Build fingerprint comparison, native identity comparison and native authentication remain outstanding.
- Native capability tracing is automatic for these diagnostic sessions, including CreateClient result/status. Reliable hello/reply logs use the existing production endpoint. No preflight fallback has been selected.
- Validation: `dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network`: **7 passed** after extending the real-engine driver to instantiate the join scene, assert initially disabled Join and empty rows, reject invalid ID input, and press the actual host button for two private diagnostic host/leave cycles. The matrix asserts diagnostic-ready logging and no engine ERROR. Builds retain existing compiler warnings. `git diff --check` passed. Full-suite known inventory failure was not rerun.
- These local tests do not prove remote metadata lookup, selection of a remote row, successful remote exchange, visual layout or export packaging. No two-account test is claimed or requested yet.

Concrete next action: prepare and smoke-test a Windows exported build with the installed custom Steam templates, then request the UI-only two-device test with build identity and both log files. No export preset currently exists in the repository. Test sequence: host uses Host Steam Game and shares the displayed private lobby ID; second account/device uses Join Steam Game, enters ID, clicks Look up, selects the row and clicks Join; expect MessageExchangeSucceeded on client and matching correlated hello/reply in both logs. Client returns to menu and repeats against the same host; then verify host loss. Logs are `user://game.log`, with the absolute location displayed in the diagnostic UI. Auth, remote identity, channel coverage and the rest of NET-00 remain separate outstanding acceptance checks even if this first exchange passes.


### Godot 4.7.2 migration (2026-10-31)

Restored the locally verified baseline using the installed Godot 4.7.2 Mono executable and GodotSteam GDExtension. Steam calls now use the retained GDExtension.Wrappers.Steam singleton, Signal events and current initialization return types. Removed SteamMultiplayerPeerWrapper; the typed peer uses ServerRelay, HostWithLobby and ConnectToLobby. Unsigned domain IDs are converted at native boundaries. Manual callbacks continue while paused; create/join ensure subscriptions, which survive menu transitions to drain canceled requests.

Validation: game builds; focused Network suite passes 7/7, including production ENet exchange and native Steam cancel/drain plus repeated diagnostic host/leave UI cycles. Test staging now includes addons and the extension list. Executable: C:/Users/lober/godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe. Full runtime logs are attached by the test suite (enet-*.log). Two-account remote Steam exchange and export packaging remain unverified, as before migration. No later epic checkpoint is declared complete.

### Returned host user test (2026-10-31 21:06:22)

User-supplied trace confirms Godot 4.7.2-stable (official), native SteamMultiplayerPeer construction, host result Ok/Connected, server ID 1, relay_supported=True, private diagnostic lobby 109775243029891879 ready, and Listening session 7dec815b715a4484923fbaa54d59811e. Method availability includes identity mapping and native auth; remote behavior remains unverified. This supersedes the earlier build's relay=false observation for this tested runtime. NET-00 remains Needs verification.

The supplied excerpt contains no failure or quit call. Review found the diagnostic Return to menu button classified voluntary departure as an error through CancelDiagnostic/QuitToStartMenuBlocking. It now uses the existing normal QuitToStartMenu flow, which stops the session before deferred navigation. Gameplay remains intentionally inactive; host listening alone does not authorize activation in GamePlayManager. An unsolicited blocking exit remains unexplained by this excerpt and requires the subsequent log lines if it recurs.

Validation: dotnet build sMPGWM.Game --no-restore succeeded (20 existing warnings, zero errors); git diff --check passed. No remote test or runtime validation of this button change is claimed.

Concrete next action: two-account UI-only diagnostic exchange using the same updated build on both devices. Host selects Host Steam Game and stays on the diagnostic screen. On the other device/account select Join Steam Game, enter the newly displayed private lobby ID, Look up, select the row, and Join. Expect MessageExchangeSucceeded and matching hello/reply correlations. Return the client to menu and repeat against the same host; then leave the host while the client is connected. Retain both full game.log files at the absolute paths displayed in the overlay. Do not reuse the recorded lobby ID after the original host session ends. Remote auth, identity and channel coverage remain outstanding even if this exchange passes.

### Returned two-device client exchange (2026-10-31 21:57:18-21:57:20)

User confirms host on the second machine and client joining by supplied lobby ID on the development machine. Client runs Godot 4.7.2-stable official via --path sMPGWM.Game; this is not export-packaging evidence. Client trace confirms successful lobby callback, native client creation Ok/Connecting, then Connected and MessageExchangeSucceeded. DiagnosticHello from local peer 1310395808 and DiagnosticReply from server peer 1 have matching correlation 859aa9520fc44d0291543c072e1767dd and payload ENet diagnostic hello. Reply identifies host session e13a960ce5e5403386a9d5b03a17d698. The payload wording is inherited from the shared diagnostic; the active transport is Steam.

This verifies the first remote Steam diagnostic request/reply from client-side evidence. Matching host logs have not yet been supplied. NET-00 remains Needs verification; native auth, actual identity mapping, broader channel coverage, refusal/loss and retry remain outstanding. NET-02 is not declared complete. No code changes or test reruns in this evidence-only increment.

User clarified the earlier perceived blocking-exit issue was the expected diagnostic-only screen, not an unsolicited exit or startup failure. There is no outstanding failure investigation for that report.

Concrete next action: while both devices are available, return the client to menu and rejoin the same running host; expect another MessageExchangeSucceeded with a fresh correlation and the same host session. Then return the host to menu while the client is connected; expect client cleanup and return to menu with a host-loss reason. Retain both devices' logs for these checks.

### Returned Steam lifecycle tests (2026-10-31 22:00-22:07)

Reviewed user-supplied host and client logs against the committed baseline. NET-00 remains Needs verification, with these additional verified behaviors:

- Host session c379d2d6c62f457cbe5acafcdc1eb634 survives client departure and handles a subsequent hello/reply. The rejoin correlation c12638371e464257b1fde63bd4c32ea1 and peer 649968205 match across both logs. Client voluntarily leaves immediately after sending hello, before logging receipt, so this particular attempt proves host-side rejoin handling and client cleanup, not completed client exchange. Host/client wall-clock timestamps differ; correlate by IDs, not cross-device elapsed time.
- A later client exchange succeeds against that same host session: correlation 1f3204b06cf6476a86ab7c256d4fd9a4 at client time 22:04:07. This supplies completed exchange evidence against the retained host session.
- Host departure at client time 22:04:12 triggers lobby-invalid/host-lost detection, native cleanup, session stop, scene-owned singleton exits and return to menu. The same process subsequently connects and completes exchanges with fresh host sessions 66ebdc4857cf4eb48e3bdef610b5e1cf (correlation cb06d4f4c97f4869b208735ae54ae300) and 096ef53897174680aedf4975e876f3de (correlation 9d9f25e0a4954e3d8c2ad5d80424c1a5). The second host-loss cleanup at 22:06:29 also returns to menu before successful retry.
- The reported Godot error stack is explicitly emitted by QuitToStartMenuBlocking -> Logger.Error -> GD.PushError for the session-ending reason. It is not evidence of a thrown cleanup exception; subsequent navigation and reconnection succeed. The legacy text 'Click to return to Main Menu' is misleading because navigation is automatic (notification code is commented out). Retain this presentation issue for the menu/error-reporting work; do not suppress Logger.Error globally.

This records user-tested graceful host-loss and retry, not abrupt transport loss/refusal or full native capability acceptance. No code changes or test reruns were needed to assess these logs. Export packaging, native auth, actual peer/Steam identity mapping, broader channel behavior and remaining NET-00 matrix remain unverified.

Concrete next implementation action: add bounded native-auth and identity-mapping verification to the production diagnostic path, configure auth before peer assignment, validate locally with ENet as control, and prepare a new two-device UI test. Do not request repeats of the now-evidenced lifecycle scenarios without a relevant implementation change.

### NET-00 bounded native-auth increment (2026-10-31)

Status remains **Needs verification**. Diagnostic sessions configure SceneMultiplayer authentication before transport assignment, exchange an exact protocol/game-version marker, and use a 10-second native auth deadline. This is capability verification, not the final build fingerprint/preflight contract. Capacity includes authenticating peers; host-only rejection occurs before RPC eligibility. Cleanup restores prior auth callback/timeout and removes owned handlers.

Steam verifies native peer-to-Steam and reverse mapping during authentication, requires current lobby membership, and on clients requires server peer 1 to match the pinned host. Mapping failure disconnects the peer; no fallback is selected. Evidence logs use Logger: NET00 auth_send, auth_complete, auth_failed and identity (including member/valid). Remote timing/availability of these mapping methods remains unverified.

Validation: focused Network suite passes 7/7 with GODOT_BIN=C:/Users/lober/godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe. ENet control asserts successful native auth and exchange; process matrix covers refusal, retry and Steam host cleanup. Fixed issues exposed during testing: conditional Steam handler removal and capacity rejection during auth. The default test executable fallback still targets obsolete 4.6; explicitly set GODOT_BIN. Existing compiler warnings remain. No exported build or remote auth/identity success is claimed.

Concrete next action: run the updated build on both Steam accounts/devices. Through menus, Host Steam Game; on the other device Join Steam Game, enter the newly displayed lobby ID, Look up, select row, Join. Expect MessageExchangeSucceeded. Collect both full game.log files from the paths displayed in the overlay, including NET00 identity valid=True and auth_complete result=Ok before exchange. A failed identity check is evidence to investigate, not permission to bypass it. Export packaging, malformed/silent auth-peer coverage, channel coverage and remaining NET-00 matrix remain outstanding. Previous lifecycle evidence is retained.

### Returned two-device native-auth and identity test (2026-10-31 22:22)

Both supplied logs identify Godot 4.7.2-stable Mono official ed1daf0bf. The happy path for remote Steam native authentication and identity mapping is now **verified**; NET-00 as a whole remains **Needs verification**.

- Client: auth_send peer=1 result=Ok; identity peer=1 steam=76561198611770214 reverse=1 member=True valid=True; auth_complete peer=1 result=Ok; then Connected and MessageExchangeSucceeded.
- Host: lobby 109775243038245658 records client membership for Steam ID 76561198011927782; auth_send peer=268647482 result=Ok; identity maps that peer to the same Steam ID and back, with member=True valid=True; auth_complete result=Ok precedes DiagnosticHello receipt and DiagnosticReply.
- Both logs match correlation 61975c1005254cebb2403b3914f2ad04 and host session d4f0f644782a42e6bcde9c9665894d7e. Client-side host identity matches the host Steam initialization log. The shared payload text 'ENet diagnostic hello' does not identify the transport; this run uses Steam.
- The host reports two native SteamNetworkingSockets assertions at steamnetworkingsockets_internal.h (769): !IsLocked(), before authentication. The session subsequently authenticates and exchanges successfully. Cause is unknown; retain this observation for investigation, without claiming it harmless or an application cleanup failure. Wall clocks differ between devices; use correlation/session IDs, not cross-device timestamps, to match events or infer latency.

No code changes or test reruns were needed for this evidence review. This does not verify timeout/rejection of malformed or silent auth peers, broader channels, abrupt transport loss, export packaging or the remaining native matrix. Previous lifecycle evidence remains valid; do not request a repeat of this successful happy path without a relevant change.

Concrete next action: add deterministic malformed/silent-auth coverage to the existing production-path ENet control harness, asserting bounded rejection, host survival and successful subsequent valid admission to the diagnostic exchange; retain Steam negative-path and channel checks as outstanding NET-00 acceptance. Investigate the native assertion's source/provenance before closing the gate.

### Fresh-context handoff after successful Steam auth test (2026-10-31)

User requested recording the outcome and continuing in a new context; no further implementation or test execution in this context. This is the authoritative next-action handoff.

Outcome: the two-device Steam happy path above passed on both sides, including native auth, identity mapping/membership checks and matched diagnostic exchange. Local focused Network suite previously passed 7/7 with Godot 4.7.2. NET-00 remains Needs verification for the outstanding cases; no later story is declared complete.

No manual E2E test is currently requested or awaiting the user. The next tests are agent-owned automation: extend the existing ENet production-path harness with invalid authentication data and a silent authentication peer; assert rejection/10-second timeout, no diagnostic exchange for rejected peers, host survival and successful subsequent valid-client exchange. Normal menus cannot produce these malformed/silent cases. Do not ask the user to reproduce them through the UI or repeat the successful Steam happy path now.

Start the new context by reviewing and preserving the existing uncommitted changes in NetworkManager.cs, NetworkInternal/SteamNetwork.cs, EnetProcessTests.cs and this plan. Reuse retained passing evidence; set GODOT_BIN explicitly to the documented 4.7.2 executable because the test fallback is obsolete. Keep mandatory Steam startup, transport exclusivity and the existing UI layout. Logger.cs was not changed.

After automated negative-path coverage, reassess remaining NET-00 acceptance, including the unexplained native !IsLocked() assertions, Steam negative paths, channels and packaging. If another manual Steam E2E test becomes necessary, first prepare a concrete testable build and give its identity, device/account prerequisites, exact numbered menu/button steps, expected outcomes and both log locations. User testing must remain full-build/UI-only; no editor steps, launch flags or harness commands. Do not represent native-auth happy-path success as timeout/rejection coverage or full NET-00 completion.

### NET-00 automated negative-auth control (2026-10-31)

Extended the existing ENet process harness with malformed and silent native-auth peers against the production host. Malformed data is rejected within five seconds; the silent peer times out between eight and fifteen seconds against the configured ten-second budget. Both cases require client-side authentication-failure evidence, no host auth completion or diagnostic hello, and no host session stop. Existing subsequent valid-client exchanges verify the same host session remains usable. The adversarial peers use a thirty-second local auth timeout so it cannot cause the host's ten-second timeout result.

Validation: focused Network suite passed 7/7 with the documented Godot 4.7.2 executable explicitly selected via GODOT_BIN. Initial test assumption corrected: explicit host DisconnectPeer does not emit its own auth_failed signal; malformed rejection now requires auth_rejected plus remote disconnect evidence. No production code or Logger changes were needed.

NET-00 remains Needs verification. Steam negative paths, channel coverage, abrupt loss, export packaging and native !IsLocked() source/provenance remain outstanding. Next action: investigate retained native assertion provenance and prepare remaining capability checks; no repeat happy-path manual test is requested.

### NET-00 logging and exported-build readiness increment (2026-10-31)

Fixed Logger direct-file truncation and configured normal Godot output at user://game.log to match the diagnostic overlay. The new engine-run logger regression passed; the integration suite retains its known unrelated inventory failure. Prior Network 7/7 evidence is retained, not rerun for this logging change.

Prepared artifacts/net00-windows using Godot 4.7.2 Mono and the installed Windows debug template from an isolated project copy. Final export has no engine ERROR lines. Exported startup smoke passed with Steam initialization, main-menu load and complete game.log output. This is local headless packaging evidence, not remote or visual UI acceptance.

Reviewed current DLL hashes and the public Valve assertion source; a matching configuration-value lock assertion was found, but exact binary provenance and the triggering native call remain unverified. Details, build identity, evidence locations and remaining test preparation are in [logging/export review](evidence/net00/logging-export-review.md).

Next action: prepare bounded Steam negative-auth/channel verification controls and continue native assertion provenance investigation before requesting another UI-only two-account test. No manual test is pending. NET-00 remains Needs verification.
