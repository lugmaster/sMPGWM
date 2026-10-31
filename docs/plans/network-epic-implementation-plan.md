# Network implementation plan

Status (2026-10-31): NET-14, NET-01 and NET-03 Verified. Remaining NET-00 verification deferred by user decision; it no longer blocks implementation. Next: reconcile checkpoint 5 (NET-02 private Steam), then checkpoint 6 (NET-08 protocol/preflight). See the single current handoff below.

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
| `SteamMultiplayerPeer` | Typed GodotSteam GDExtension peer privately owned by `SteamNetwork`; old wrapper removed in the 4.7.2 migration |
| `SteamworksHelper` | Existing mandatory Steam initialization/pumping/shutdown, including for LAN |
| `SteamLobbyService` / `SteamServerBrowser` | Internal helpers for serialized native operations/membership and immutable discovery snapshots; active-session operations remain behind `IActiveNetwork`, with no independent session owner |
| `PeerAdmission` / `PeerHealthMonitor` | Plain state machines driven by manager and monotonic clock |
| `GamePlayManager` | Explicit scene initialization result; activation only after admission |
| Compatibility, world-join and spawn helpers | Plain data/logic/coordinators; no additional session owner |

Keep `BaseNetwork` and `IActiveNetwork`; do not replace this structure with peer adapters or a second manager. Replace static completion events and mutable global session inputs as needed. All consumers access the manager directly as `NetworkManager.Instance`; never cache it in a field/local variable or resolve it with `GetNode`. This rule concerns manager access, not the internal conversion of an active network implementation to a Node for `AddChild`.

Follow README no-LINQ, namespace/path and file-per-type conventions. All logging, including diagnostic exchange and spawn logs, uses `using Logger = sMPGWM.Scripts.LoggingSystem.Logger;` and the appropriate `Logger` methods, never `GD.Print`.

Use the installed GodotSteam typed GDExtension bindings: SteamMultiplayerPeer for peers and the retained GDExtension.Wrappers.Steam singleton for other Steam operations. The old wrapper was removed. Keep native conversions, errors and ownership inside the transport/runtime boundary. Verify installed native APIs before encapsulating any missing binding through a dynamic call.

### Scene entry and transport boundary

- Main menu selects `NetworkType` and `NetworkRole` and hands them over as startup data when navigating to `main_game.tscn`. A selected Steam lobby ID or LAN endpoint is target data, not connection state. Menu browsing may display discovery snapshots, but the menu never creates a session, hosts/joins, observes connection state or retains `NetworkManager`.
- Deliver startup data before the manager starts. Store role/type in `NetworkManager`, consume and clear temporary handover data, and read session choices only from `NetworkManager.Instance` thereafter. Start hosting/joining once the loaded main-game scene's manager, RPC endpoint and active child are ready; no connection attempt precedes scene entry.
- Manager constructs the selected `BaseNetwork` implementation, retains it as `IActiveNetwork`, adds it as a child and starts it only after child readiness. It stays alive for the manager's lifetime. Subscribe to connection/failure events and configure the shared Multiplayer API before peer creation/assignment.
- `IActiveNetwork` is the sole access point for transport-specific operations and underlying peer ownership. Keep peers/wrappers private to their implementing Node; neither manager nor gameplay code creates or assigns them. Expose required Steam session interactions through this same interface. ENet implements intentionally unsupported Steam-only methods by throwing `NotSupportedException` with a clear reason; `NotImplementedException` is reserved for unfinished code. Do not use exceptions as normal transport selection flow.
- ENet host calls `var result = _peer.CreateServer(GameConfig.Port, maxConnections);`, checks `Error.Ok`, then assigns `Multiplayer.MultiplayerPeer = _peer;` in `EnetNetwork`. Client similarly captures/checks `_peer.CreateClient(GameConfig.ServerIpAddress, GameConfig.Port)` before the same assignment. Creation failure exits the local attempt; never assign a failed peer. Host reports listening without waiting for clients; client awaits actual connection/failure with cancellation and timeout.
- Steam host starts its peer only from successful `OnLobbyCreated`; Steam client starts its peer only from successful `OnLobbyJoined`. After successful typed HostWithLobby / ConnectToLobby, SteamNetwork assigns its owned SteamMultiplayerPeer to Multiplayer.MultiplayerPeer. A failed callback for the chosen role, or peer creation failure, ends the local attempt and returns to the main menu after cleanup. Host's incidental `LobbyJoined` never starts a client or completes host startup. Ignore unrelated/stale callbacks; drain late membership as described below.
- After peer assignment, gameplay networking uses Godot's high-level Multiplayer API and Node RPCs, with matching endpoint paths and the same Multiplayer API context. Steam lobby operations and transport cleanup stay behind `IActiveNetwork`; no custom peer packet path for gameplay.
- World changes require a complete quit to the main menu and fresh entry. Manager, network child, ticker, protocol endpoint and world never persist across that transition. Scene presence alone does not authorize gameplay; initialization/admission still gate activation.

Engine/Steam calls stay on the main thread. Subscribe before native work, handle reentrancy and observe async callback exceptions. Each attempt has one terminal result; stale callbacks cannot complete a newer attempt. Membership, connection, diagnostic success and gameplay admission are separate outcomes.

### Timing, capacity and cleanup

- `_PhysicsProcess` is the sole ticker source on every process. Manager/deadline checks run each physics step; gameplay network cycles run every second step (30/second at 60 Hz). The README now documents this cadence. Host tick observations never drive or overwrite the client's local tick. Keep normal engine multiplayer polling; no second polling/catch-up loop.
- Control/diagnostic/health replies are prompt. Monotonic time determines deadlines/RTT independently of tick counts, time scale and gameplay cadence. Test supported pause/background behavior and blocking-load stalls.
- Target typically 5 players, maximum 10 including host; pending peers count toward capacity. ENet remote slots equal `MaxPlayers - 1`; explicitly handle host-only configuration. Reuse bounded storage, avoid unnecessary recurring allocations, never force GC, and measure timing/GC under representative load.
- Capture `GameConfig.ConnectionTimeout` per attempt in milliseconds (currently `60000`). Diagnostic, preflight, scene, admission and health budgets remain independent. Initial scene budget: 60 seconds; admission after local readiness: 15 seconds. Bound waiting for `HostReady` too. Pongs never extend phase budgets.
- Cleanup invalidates the attempt/session first, cancels work, disables input and detaches owned subscriptions, including Multiplayer, Steam, GameFlowManager and static event subscriptions. Through `IActiveNetwork`, unpublish hosting, leave membership, clear only the implementation's assigned peer and close/dispose native transport resources. Clear session static references and singleton `Instance` on exit (only if it still refers to that instance), the retained active-network reference and pending handover data; reset fingerprint, DTOs, timers, counters and API settings. Let Godot free the scene-owned Node subtree; no manual duplicate Node-disposal scheme. Godot scene freeing does not substitute for native peer/lobby cleanup or removal of external/static references.
- Outstanding native callbacks may require an application-lifetime drain owned by the existing Steam runtime helper. Such draining retains only operation identifiers/cleanup data, never the old manager, scene Nodes or their delegates, and cannot complete a fresh session. No live session state survives return to the menu.
- Cleanup is idempotent on cancel, scene failure, menu return and exit, and precedes Steam shutdown. Local session-ending errors clean up before `QuitToStartMenuBlocking(reason)`; reserve `QuitWithOsError` for unrecoverable whole-game failures. Failed remote clients remove only their own pending work/slot, preserving host and healthy clients.

## Execution order

Statuses: Pending, In progress, Needs verification, Deferred verification, Verified. Deferred verification means explicit permission to proceed with outstanding evidence, not a pass. A partial checkpoint does not complete its larger story.

| Order | Story checkpoint | Status |
| --- | --- | --- |
| 1 | NET-14: ENet diagnostic exchange | Verified |
| 2 | NET-01: shared ownership completion | Verified |
| 3 | NET-03: ENet completion | Verified |
| 4 | NET-00: Steam capability gate | Deferred verification; implementation may proceed |
| 5 | NET-02: private Steam diagnostic exchange | In progress; exchange/lifecycle evidenced, reconcile remaining scope |
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

Validate address/UDP port and capacity; distinguish immediate creation errors from connection/refusal. Preserve configured LAN inputs. Creation-result checks, signal-driven connection and bounded cancellation are implemented within EnetNetwork and the shared IActiveNetwork contract. Verify intentionally unsupported Steam operations throw `NotSupportedException` on ENet.

**Acceptance:** occupied port, invalid endpoint/capacity, host-only configuration, pending slots, refusal, cancellation, leave/retry and mandatory Steam startup behavior for LAN.

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

**Acceptance:** real exchange with two accounts/devices; single creator completion; cancellation at each stage; late membership cleanup; wrong-pool/self/full/stale targets; construction/metadata/publication failures; host loss/owner transfer; repeated host/exit/retry; no leaked membership/callbacks. Public hosting proves readiness gates and unchanged host/world during client churn. Mandatory Steam startup remains unchanged; session failures never switch transport.

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
- `Scripts/StartMenuSystem/LobbyList/LobbyList.cs` implements supplied-ID lookup and selection. Extend it for public discovery snapshots without making it a session owner.
- `Scripts/StartMenuSystem/HostSteamGameMenu.cs` enters the private diagnostic flow through StartSteamDiagnostic; extend the existing scene-entry handoff for production hosting.
- `Scripts/StartMenuSystem/JoinSteamGameMenu.cs` passes the selected stable lobby ID through diagnostic startup data. Extend this for production joining. Correct its current "Start Steam Game" title to match the join screen.
- `Scripts/Util/UiComponents/FilterToolbar.cs` is an `HBoxContainer` scaffold; reuse it and `scenes/ui/base/filter_toolbar.tscn` for search/sort. Inventory already uses this scene and directly resolves `FilterToolbar/LineEdit` and `FilterToolbar/SortSelector`; preserve those paths and inventory behavior when extending it.
- `scenes/ui/menu/start_menu/lobby_list_entry.tscn` already supplies a toggle Button with lobby-name, host-name and player-count labels. Bind real snapshot values and stable-ID selection to this row. Its current `focus_mode = 0` must be adjusted for the planned keyboard selection; preserve its visual layout.

Preserve main menu -> Host/Join -> LAN/Steam; final action hands off role/type and target data and enters `main_game.tscn`. Only then does `NetworkManager` allocate its active network child and begin hosting/joining. Replace sample rows in actual Steam join/entry scenes through `StartMenuProvider`. No waiting room or ready-up/Start screen. Main menu has no connection/session state.

Auto-refresh discovery on activation; show loading/empty/error and Refresh/Back. One keyboard-accessible selection by stable lobby ID; Join requires visible valid selection and no pending search/navigation. Sorting preserves selection; filter/refresh removal clears it. Client revalidates handed-off target inside the main-game scene. LAN uses address/configured port. Menu Back deactivates discovery before navigation; cancellation after scene entry goes through `NetworkManager.Instance`, cleans up and returns to the main menu. Steam capability failure disables Steam actions with a reason; this is runtime availability, not connection state.

**Acceptance:** scene navigation, status, LAN input, target handoff, keyboard selection, sort/filter/refresh, duplicate Join/Refresh prevention and shared inventory `FilterToolbar` paths/options regression.

### NET-12: integration

Exercise production flows: repeated host/client startup, join/leave/rejoin, three-peer isolation and last-slot races with unchanged host session/world identity. Cover host loss, scene/init/data/health failures, late callbacks, cleanup before Steam shutdown, mandatory-Steam LAN, pause/background and representative-load timing/GC. Verify world change requires quit to menu and fresh scene entry, with no retained singleton/static session references or external subscriptions. Verify role-specific Steam callback failures exit locally and cannot initialize the other role. Finish NET-00 multi-peer matrix and every story's acceptance; record stub/native limitations.

## Current handoff — 2026-10-31

This section replaces all historical carry-over instructions. User decision: continue the epic; defer remaining NET-00 checks to relevant playtests. Do not mark deferred checks Verified or repeat passed authentication tests without a relevant change. Update this handoff in place rather than appending another chronological handoff.

### Resume point

Execution checkpoint **6 is NET-08 protocol/preflight**. Numeric story **NET-06** is compatibility at checkpoint 8. Checkpoints 1-3 are Verified. Much of checkpoint 5 was implemented ahead of the gate to enable Steam testing; private exchange works, but the full checkpoint was never formally closed.

1. Inspect the working tree and preserve unrelated changes. Reconcile private Steam implementation against checkpoint 5 using existing evidence; finish concrete missing implementation without reopening deferred NET-00 checks.
2. Continue checkpoint 6, NET-08. Current native auth exchanges a diagnostic protocol/game-version marker with a ten-second deadline; it is not final preflight/build/content validation. SessionProtocolNode currently handles diagnostic hello/reply, not admission envelopes/tokens/epochs. Native auth works; no fallback protocol is needed.
3. Follow the remaining execution order. Public hosting, discovery, admission and gameplay activation remain future checkpoints. Diagnostic success never authorizes gameplay.

Checkpoint 5/6 reconciliation must account for independently generated lobby session_id and endpoint HostSession, metadata without a build fingerprint, and authentication currently enabled only for diagnostics. Coordinate final identities/preflight with their planned owners instead of treating diagnostics as completed admission.

### Baseline and constraints

- Scene-owned manager/transport/protocol/ticker, immutable consumed-once startup targets, local ticks, ENet validation/capacity/cancellation and idempotent cleanup are implemented. Steam create/join callbacks are serialized/drained without retaining canceled scene delegates.
- Private Steam host/join uses existing menus and supplied-ID lookup/selection. Startup carries the selected ID; clients validate metadata and pin host/session. Host opens its private diagnostic lobby after peer assignment. Overlay displays lobby ID, result and log path. This is not public discovery; gameplay remains inactive.
- Diagnostic native auth is configured before peer assignment, validates exact marker and Steam identity mapping/membership, and has a production ten-second deadline. Cleanup restores API settings; object decoding is disabled.
- Use Godot 4.7.2 Mono and typed GodotSteam GDExtension bindings. The old 4.6 compiled module/wrapper is obsolete. Preserve mandatory Steam startup even for LAN, transport exclusivity (no failure fallback), and existing UI layout.
- Temporary auth enum/UID, startup field/parameter, UI selector, injection branches, client timeout override and selector-only harness assertions are removed. Keep production validation and automated adversarial-peer regression tests.
- Logger append fix and engine user://game.log configuration are retained. No Logger changes were needed for auth results. Warning/error backtraces alone are not thrown exceptions; do not suppress logging globally. Legacy “Click to return” wording belongs to later menu/error presentation work.

### Retained evidence

| Scope | Result and evidence |
| --- | --- |
| NET-14 | ENet matching RPCs, bounds/senders/correlation, local ticks, timeout/cancel/retry and cleanup. [Diagnostic excerpts](evidence/net14/) |
| NET-01 / NET-03 | Shared lifetime ownership, callback cancellation/draining, native host cycles and ENet capacity/lifecycle matrix. [Ownership/ENet excerpts](evidence/net01-net03/) |
| Steam lifecycle, 22:00-22:07 | Client leave/rejoin and completed exchange against retained host session c379d2d6c62f457cbe5acafcdc1eb634; graceful host departure cleans up to menu; same client process reconnects successfully to fresh hosts. Abrupt loss is not covered. |
| Native auth/identity, 22:22 | Both peers authenticate and map IDs/membership with valid=True before exchange; matching correlation 61975c1005254cebb2403b3914f2ad04, session d4f0f644782a42e6bcde9c9665894d7e. |
| Silent Steam auth | Host peer 1450781207: auth_send 22:46:55, auth_failed 22:47:05, no completion/exchange; client cleans up before its test-only 30-second deadline. |
| Malformed Steam auth | Host peer 1641169724 rejected with bytes=1 at 22:53:58, same timestamp second as auth_send; no completion/exchange; client cleanup and automatic menu return. |
| Recovery after both faults | Same lobby 109775243039927683/session 8ea3cd3130914eb49d2f52c1669ecc4b; normal peer 869856707 completes auth/exchange, both logs matching correlation f466cb81d44c484784d15c06244fc0f2. Client restarted between malformed and normal; same-client-process fault retry is not established. |
| Export/logging | Local Windows export/headless startup passes, Steam initializes and game.log is retained. [Logging/export review](evidence/net00/logging-export-review.md). Not remote exported-package acceptance. |
| Latest validation | After source cleanup: build succeeds with existing warnings; focused Network **7/7**, including ENet malformed/silent rejection and recovery. sMPGWM.Tests/TestResults/net00-auth-cleanup.trx; no temporary source-control references; diff check passes. |

Remote evidence is summarized from user-supplied conversation logs; full remote log files are not claimed saved locally. Device clocks differ: correlate IDs and use same-device timing. Client logs include editor execution, so they establish runtime behavior, not exported deployment. Shared payload text “ENet diagnostic hello” also appears over Steam.

Older native-host/source evidence under evidence/net00 describes the pre-migration runtime; use the logging/export review for current DLL hashes and provenance limits. Current runtime reports relay support; older relay=false observations do not apply.

### Deferred verification — not implementation blockers

| Check | Revisit |
| --- | --- |
| Abrupt process/network loss, bounded cleanup/retry | Session/lifecycle playtests and NET-12; graceful host loss already passed |
| Additional channels/transfer modes and separation | Before relying on them; current reliable exchange is evidenced |
| Two-device exported-build acceptance | Next meaningful complete-build playtest / NET-12 |
| Native steamnetworkingsockets_internal.h (769): !IsLocked() | Track through playtests; prioritize if associated with hangs/crashes/failed connections. Recurred before malformed auth; cause unknown, not proven harmless. |
| Duplicate silent auth_failed output | Observation retained; callback duplication versus duplicated output undiagnosed |
| Remaining matrix, third-peer isolation and last-slot races | NET-12/checkpoint 16 with required devices/accounts |

### Validation and future user tests

Set GODOT_BIN explicitly to C:/Users/lober/godot_engine/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe; test fallback is obsolete 4.6. Run `dotnet test sMPGWM.Tests --filter FullyQualifiedName~Network` for relevant changes. Reuse valid evidence; follow repository NUnit conventions and focused runtime testing.

Known unrelated full-suite baseline: UnknownDefinitions_AreRejectedIncludingDefaultAndZeroQuantity at InventoryControllerTests.cs:131 expects UnknownDefinition, receives NoChange. Focused 7/7 is not a clean full-suite claim.

No manual test is pending. Future requests must provide a complete exported game launched normally through UI, build identity, device/account prerequisites, exact menu steps, expected results and both log locations. No editor actions, flags or harness commands for the user. Logs: %APPDATA%/Godot/app_userdata/sMPGWM/game.log, absolute path also displayed in overlay; preserve before relaunch. Prepare implementation/local validation before requesting user time.

### Temporary artifact housekeeping

Source controls are removed and clean source was rebuilt/tested. Automatic approval review again rejected deletion after the user's cleanup request with “blocked by policy”; these remain:

- artifacts/net00-auth-windows
- artifacts/net00-auth-smoke-user
- artifacts/net00-auth-export.ps1
- C:/Users/lober/AppData/Local/Temp/sMPGWM-export-bc0b8529793c467baae1ff57ec8fa346

Preserve evidence logs, SHA256 manifests and test results. The old auth package contains removed fault controls; it is not the current clean build. Housekeeping does not block epic implementation.
