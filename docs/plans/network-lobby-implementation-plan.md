# Running-world server browser and player admission

Status: revised proposal, 2026-10-27. Planning only; implementation has not started.

For the smaller host/client message-exchange epic, see the [vertical slice refinement discussion](network-vertical-slice-refinement-plan.md). It proposes NET-14 as the first implementation story and separates basic transport proof from the larger gameplay admission scope below.

The host runs a persistent world for the lifetime of its process. Players discover a running game, join, leave, and join again while the host continues playing. Steam lobbies implement discovery/membership; the product is a server browser, not a pre-match waiting room. A player's departure must not tear down the host's world. Durable saves across host restarts are separate work.

This increment establishes sessions, lists running games, verifies gameplay compatibility and explicit initialization, and invokes a host-owned spawning method that logs for now. It defines the world synchronization boundary required before real players can join a running world. Player instantiation, world replication, prediction, and generated-content distribution are subsequent work; connection/checksum success must not be reported as completion of those features.

## Plan split and implementation ownership

Reorganized 2026-10-31 into four executable workstreams. This file remains the shared contract and integration reference; workflow details live in their respective plans.

| Plan | Boundary |
| --- | --- |
| [Create game as host](network-host-implementation-plan.md) | Final Host action, server setup, local initialization/admission, publication |
| [Create game as client](network-client-implementation-plan.md) | Selected target through connection and remote admission; implements both sides of admission |
| [Create the lobby list](network-lobby-list-implementation-plan.md) | Discovery, snapshots, selection and handoff to client creation |
| [Heartbeat and round-trip measurement](network-heartbeat-rtt-implementation-plan.md) | Connected-peer liveness, RTT generation and removal requests |

Implement common ownership/native verification once in the host workstream, then prove host/client admission over ENet. Integrate health and Steam, then real browser discovery and NET-12. Browser UI and pure health logic can proceed against fixtures/contracts earlier. The delivery table below preserves existing story identifiers; it is not an additional fifth implementation plan.

### Godot Nodes versus ordinary C# types

| Type / responsibility | Decision and lifetime |
| --- | --- |
| NetworkManager | Persistent Node child of GameFlowManager; sole session/peer owner; ticks ordinary state machines |
| SessionProtocolNode | Deterministically named persistent Node child of NetworkManager; same RPC path on all peers; created before peer assignment |
| GameFlowManager / GamePlayManager | Existing Nodes; navigation and scene initialization respectively |
| SteamworksHelper | Existing persistent Node for Steam runtime/callback pumping; no duplicate autoload |
| Menu and lobby entry Controls | Scene Nodes; subscribe/unsubscribe on actual activation/deactivation |
| HostSessionFlow / ClientSessionFlow | Ordinary classes owned by NetworkManager; one cancellable attempt, no independent tree lifetime |
| SteamLobbyService / SteamServerBrowser | Ordinary classes; explicit callback ownership/disposal; service outlives browser and drains late operations |
| SteamPeerAdapter / EnetPeerAdapter | Ordinary classes managing engine peer objects and explicit signal subscriptions; peer objects are not scene children |
| PeerAdmission / PeerHealthMonitor | Ordinary state machines with monotonic clock; manager drives ticks; no per-peer Nodes/Timers |
| Compatibility policy, hashing, requests, DTOs, enums, metrics | Ordinary immutable data/pure logic where applicable; one file per type |
| WorldJoinCoordinator / PlayerSpawnCoordinator | Ordinary coordinators for the initial stub; future actual players/world objects use scene Nodes as required |

A Godot API or signal dependency alone does not require inheriting Node. Inject the needed API/context and detach subscriptions explicitly; keep engine work on the main thread. Configure the persistent owner/endpoint for supported pause processing and test background behavior. Do not use Task.Run for engine calls. CollisionTypes.cs is an existing example of a plain enum: leave it in CollisionSystem; network enums likewise do not need Nodes.

Suggested organization under `sMPGWM.Game/Scripts/Network`: root owner and role/type, `Hosting`, `Joining`, `Lobby`, `Transport`, `Protocol`, `Admission`, `Health`, and `Compatibility` as responsibilities become implemented. Keep namespaces aligned with paths and normalize existing `scripts`/`Scripts` casing through semantic refactoring during implementation. These are folders, not additional managers or mandatory interfaces.

## Recommendation and comparison

Keep Godot high-level multiplayer and the installed Steam MultiplayerPeer. Replace the package's lifecycle/application protocol. Another transport would not fix its ownership, cancellation, or readiness bugs. Use one persistent session owner, small adapters, one scene initialization result, and a short host-authoritative admission exchange.

| Proven solution | Relevant behavior | Application here |
| --- | --- | --- |
| [Godot 4.6 SceneMultiplayer](https://docs.godotengine.org/en/4.6/classes/class_scenemultiplayer.html) | Authentication can withhold normal traffic and peer_connected until both sides finish | Prefer a short native protocol gate; scene/world readiness follows separately |
| [Unity Netcode approval](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/basics/connection-approval.html) | Bounded pending approval, rejection reasons, optional player creation | Approval is explicit and does not imply an instantiated player |
| [Mirror NetworkManager](https://github.com/MirrorNetworking/Mirror/blob/master/Assets/Mirror/Core/NetworkManager.cs) | Authentication, scene readiness, and adding a player are separate operations | Initialize, compare gameplay data, synchronize the world, then spawn |
| [Photon Fusion scene loading](https://doc.photonengine.com/fusion/v2/manual/scene-loading) | Loading/linking scene objects has an explicit owner | Await dependency completion instead of a frame delay |
| [Steam matchmaking](https://partner.steamgames.com/doc/features/multiplayer/matchmaking) | Lobby discovery/membership differs from the game connection | Present running-game entries; distinguish membership, connection, and admission |

These are design comparisons, not verification of the custom native build. NET-00 checks that build before implementation depends on its features. Retain the README's host-authoritative shooter model. A backend replacement, lockstep, or rollback framework is not justified by this increment.

| Status quo / former proposal | Revised decision | Reason |
| --- | --- | --- |
| Two persistent networking managers | One NetworkManager with ordinary admission/health classes | One owner of cancellation, peer assignment, session state, and cleanup |
| SteamNetwork mixes discovery, membership, and transport | Separate lobby service/browser from peer adapter | Browsing must not create/own a game connection |
| Everything uses custom RPCs | Prefer native auth for short protocol preflight; RPCs afterward | Reuse the engine's pre-RPC gate where verified |
| Ready/offer/ack/commit/ack | HostReady -> ClientReady -> AdmissionAccepted | Reliable control traffic already retransmits; another acknowledgement cannot make activation atomic |
| General readiness registry | Explicit scene composition result awaiting actual dependencies | Avoid a framework for hypothetical dynamic participants |
| Hash configs, schemas, defaults wholesale | Editable comparison policy over effective gameplay values | Exclude unrelated settings and immutable validation files |
| Reread paths after loading | Fingerprint validated effective models from captured inputs | No second-read race; describes what the session uses |
| Global mutable checksum authority | Immutable session fingerprint; later content revisions | Prevent stale admission state |
| Pre-match lobby / close joins once playing | Open server browser for the running host world | Join/leave is per-player and the world continues |

## Scope and defaults

- Preserve main menu -> Host/Join -> LAN/Steam navigation. Allocate resources on the final Host/Join action. No waiting room, ready-up screen, or match Start button.
- Support Steam and ENet through identical application rules. LAN uses the address field/configured UDP port; discovery is outside scope. ENet is also the local multi-process development path.
- Publish a joinable running game after transport, host initialization, and fingerprint succeed. Keep it open while the host runs, subject to capacity and temporary synchronization/content barriers. Playing does not close the server.
- Rejoining performs fresh connection/admission/world synchronization with new peer ID/token. Stable Steam identity is distinct from connection identity. Automatic reconnect and restoration of player progress are separate features.
- MaxPlayers includes host; ENet remote slots equal MaxPlayers - 1. Validate supported limits and explicitly handle host-only capacity rather than assuming a zero ENet limit means the intended thing.
- Keep configured name plus suffix, public discovery, Worldwide search, and exact game_id/version filters each request. Display host-selected mode without requiring equal local preference. Worldwide means reach, not low latency. Preserve Steam discovery order until a local sort is selected.
- Steam failure disables Steam actions with a reason and leaves LAN usable. Initialize/pump/shutdown Steam conditionally and idempotently.
- Compare gameplay definitions/rules, not comments, file formatting, local cosmetics, schemas, or unrelated settings. Compatibility is cooperative; no anti-cheat or tamper-proofing is intended.
- Carry initially empty PlayerJoinData through admission/spawn signatures. Keep bidirectional application health checks.

No product question blocks this plan. Native behavior is an investigation, not a question for the user to guess. Existing user edits to ChecksumGenerator.cs and MpManager.cs remain untouched by this planning pass.

## Findings in the current code

- GameFlowManager.StartGame ignores role/type and loads immediately; SceneChanged can enter InGame without admission. Exit/scene-load failure do not release sessions.
- NetworkManager lacks production wiring, reads operation inputs from GameFlowManager, and uses static completion events. BaseNetwork.Cleanup clears every subscriber. Transport failures call navigation directly.
- IActiveNetwork is fire-and-forget and requires a lobby ID even for LAN. Replace that contract rather than preserving the accidental abstraction; a base carrying static events is unnecessary.
- SteamNetwork can complete hosting on creator-join before validating its peer. It lacks operation deadlines, client membership tracking, LeaveLobby cleanup, and reliable owned-peer clearing.
- SteamMultiplayerPeerWrapper may remain non-null after native construction fails. Its class belongs to the custom engine; absence of a repository GDExtension file does not prove it unavailable.
- EnetNetwork ignores CreateClient's immediate result and cannot cancel waits. Both adapters need actual main-thread connection outcomes and owned cleanup.
- MpManager is currently uncommented legacy code referencing missing former-game globals, controller/spawner/ship types, and lifecycle overrides. Replace it in the first buildable slice; do not resurrect its RPCs. Record baseline failures first.
- ChecksumGenerator has no production caller. It hashes decoded text per file, concatenates fixed-width lowercase digests in caller order, then hashes that concatenation. Missing files call QuitWithOsError and continue; empty inventories yield a valid-looking digest. Read failure is not distinguished from empty content.
- GameConfig's checksum properties remain unassigned. Current config.cfg contains logging, Steam, address, timeout, discovery and host settings, not shared gameplay rules.
- GamePlayManager loads category/item definitions and schemas, builds services/UI/input, then unconditionally defers Start. Replace that activation with initialization plus admission.
- FileLoader already captures text before validation; use those inputs. Effect/ship/weapon FilePaths constants do not prove those files are consumed.
- SteamworksHelper is persistent and uses App ID 480, making custom game_id filtering essential. Its initialization failure currently terminates the app.
- Use scenes/ui/menu/start_menu/join_steam_game_menu.tscn and the existing toggle entry scene. Replace twelve sample rows. Preserve shared FilterToolbar child paths and inventory behavior.
- Hidden StartMenuManager history screens remain alive. Suspend browser listeners/publication explicitly on deactivation, not only _ExitTree.

## Ownership and contracts

GameFlowManager owns navigation and one persistent NetworkManager child. NetworkManager owns session state, adapters, subscriptions, cancellation, peer assignment, admission/health, fingerprint, and teardown. Create its deterministic Protocol child after config is available and before any peer assignment. No second multiplayer manager or new autoload is necessary.

| Component | Responsibility |
| --- | --- |
| GameFlowManager | Await connection, change scene once, coordinate initialization/local activation/exit |
| NetworkManager | One session/attempt, API settings, peer ownership, terminal outcomes, cleanup |
| SteamLobbyService | Serialized lobby operations, metadata/membership, late callback cleanup |
| SteamPeerAdapter / EnetPeerAdapter | Create/validate peer, observe connection/loss, release resources; no UI/navigation |
| SteamServerBrowser | Filtered running-game snapshots and loading/empty/failure/inactive behavior |
| SessionProtocolNode | Stable RPC boundary; capture sender synchronously; decode bounded primitives and delegate |
| PeerAdmission / PeerHealthMonitor | Small ordinary classes, per-peer state, controllable monotonic clock; health probes also supply RTT/ping metrics |
| GamePlayManager | One explicit initialization result for data/services/UI/input/world/spawn dependencies |
| GameplayCompatibilityPolicy | Small typed comparison inventory matching the editable table; no directory scanner |
| ChecksumGenerator | Pure SHA-256 utility; callers select/encode content; no shutdown/dialog/navigation |
| WorldJoinCoordinator | Current-world snapshot/catch-up contract; explicitly named no-replication stub initially |
| PlayerSpawnCoordinator | Host-only StartSpawning(int peerId, PlayerJoinData data), logging only |
| Server browser UI | Render snapshot, one selected ID, refresh/search/sort, validated request |

These are responsibilities, not mandatory interfaces/services per row. Prefer ordinary classes unless RPC/tree/callback lifetime requires a Node. Follow README no-LINQ/file-per-type guidance. Use Rider semantic refactoring when implementation changes declarations/usages, signatures, or namespaces.

Use immutable requests with Steam lobby ID or LAN endpoint as transport-specific targets. Replace global events with Task results, cancellation and instance loss events. Subscribe before native work, inspect immediate errors, handle reentrancy and one terminal outcome. Keep Godot/Steam operations on the main thread; observe exceptions at async engine callback boundaries.

Session data includes role/transport, local/server peer IDs, pinned Steam host/lobby, host-issued session identity, shared scene/world epoch and content revision. Local scene-generation tokens are distinct from host epochs; independent local counters need not match.

- Local flow: StartMenu -> Connecting -> Loading -> Synchronizing -> InGame; exit through Disconnecting.
- Peer state: Authenticating -> Loading -> AwaitingCompatibility -> SynchronizingWorld -> Admitted, or Removed.
- Publication: Creating -> Preparing -> Joinable -> Closing.

Pending clients do not suspend the active host or each other. Host local admission needs no remote client. Removal cancels that peer's transfers/admission and frees capacity; it does not delete world-owned entities. Avatar/inventory retention after departure belongs to actual player persistence work.

## NET-00: verify the custom native build

Use a repeatable small harness with the exact custom Godot executable. Record engine/GodotSteam/MultiplayerPeer build identity. Verify creation/error/status, server peer ID, signal ordering, disposal/retry, targeted reliable RPCs, channels, refusal/disconnection, peer-to-Steam-ID mapping where exposed, and a third peer. ENet is the control. Do not infer support from names or another implementation's documentation.

Prefer AuthCallback/SendAuth/CompleteAuth for a short game/protocol/engine-build preflight with finite timeout, set before peer assignment. Count pending connections toward capacity. For Steam verify actual native identity against lobby membership where mapping exists; a claimed ID in a DTO is not authentication.

Do not hold native auth open for gameplay loading: normal RPCs are unavailable then. Waiting for a scene load triggered by ConnectedToServer would deadlock. Scene/world readiness follows preflight.

If demonstrated native incompatibility prevents this gate, record evidence and select one bounded RPC preflight before scene loading for this build. Enforce pending/admitted allowlists on every handler. Do not maintain two runtime-selectable protocols or silently switch. Document absent native identity mapping as a limitation.

## Shared connection and cleanup rules

ENet follows the same application rules, validates endpoint, checks immediate creation errors and actual connection outcome. A refused/failed client cannot stop the host.

The checked-in Matchmaking wrapper returns void for create/join/search and exposes global callbacks without request handles. Local generations cannot identify indistinguishable native callbacks. Keep one persistent operation owner, serialize each kind, correlate lobby IDs where possible, and retain canceled native operations for draining. A late successful membership must be left and cannot start gameplay. Audit installed call-result support before extending bindings. Native creator-join/ownership behavior is documented in [ISteamMatchmaking](https://partner.steamgames.com/doc/api/ISteamMatchmaking).

If an uncorrelatable callback never returns, mark that operation kind unavailable for the current Steam runtime and show a restart-required reason. Do not risk attributing its callback to a replacement operation. Safe other operations/LAN remain usable. Test that policy.

Cleanup invalidates session/attempt first; cancels initialization/admission/health/transfers; disables input; removes peers; unpublishes owned hosting; leaves memberships; clears only its assigned peer; closes/disposes native resources; detaches owned callbacks once safe. Retain only outstanding late-operation drain until completion/shutdown. Reset fingerprint/DTO/state/timers/API settings. Cleanup is idempotent on cancel, scene failure, menu return and exit, before Steam shutdown. A crash cannot promise OnSuddenExit execution.

## Lobby list

Workflow moved to [Create the lobby list](network-lobby-list-implementation-plan.md).

## Editable gameplay comparison policy

This is the reviewable policy table. Mirror it during implementation in one small typed inventory with logical ID, provider, comparison mode and serializer version. Adding a source means updating its row and its actual loader provider; do not scan all files or let clients omit required entries. Policy/version disagreement is a compatibility error.

| Logical entry | Source / effective value | Compare? | Comparison rule | Availability |
| --- | --- | --- | --- | --- |
| item-categories | Effective ItemCategoryDefinitions.yml after default/user handling | Yes | Canonical gameplay identities/relationships; exclude presentation labels/resources | Current loader |
| items | Effective ItemDefinitions.yml after default/user handling | Yes | Canonical IDs and gameplay-affecting values/references | Current loader |
| gameplay-config-overrides | Future gameplay config: defaults merged with overrides | Yes when introduced | Resulting effective rules including fallback, not raw override text/existence | No shared gameplay rules file currently identified |
| passives | Effective passive definitions including accepted custom ones | Yes when loaded | IDs/effects/parameters/stacking/triggers/references | Future provider |
| effects / weapons / ships | Definitions actually consumed by future loaders | Yes when loaded | Simulation-affecting values/references | Constants alone do not enable comparison |
| generated-definitions | Host-accepted runtime item/passive registry | Yes before use | Canonical identities/content plus revision; available to affected peers before activation | Future runtime provider |
| schemas | Packaged *Schema.yml / Schemas.yml / JSON schemas | No | Immutable validators; validate locally, exclude file contents from equality | Current/future loaders |
| packaged-default-files | Immutable packaged defaults as standalone files | No | Same base release; no redundant file hashing. Effective gameplay values derived from them still participate above | Packaged data |
| local-config | Current config.cfg: logging, Steam, address/port, timeout, lobby/discovery preferences | No | Local operation/discovery, not gameplay definitions | ConfigLoader |
| host-session-settings | Mode/capacity and future host-owned world rules | Communicate | Host selects. Include simulation-affecting rules in shared effective rules; do not require equal local preferences | Session/future rules |
| presentation | Icons/textures/UI/localization/cosmetic labels | No by default | Include a field only if it changes simulation | Relevant projections |
| player-instance-state | Ownership/inventory/rolled instance state/position/progress | Synchronize, not global equality | Players may own different items; peers agree on referenced definitions and authoritative instance values | Future world/player replication |

Edit this table and the typed inventory together. Enabled entries require exactly one successful provider output. Missing/duplicate/unsupported required entries fail compatibility. Excluded/future entries do not silently contribute empty hashes. Version the inventory contract. Schema/default file reads alone do not make them comparison entries.

Use actual merge/replacement semantics: FileLoader consumes the effective user definition file after copying defaults when needed; ConfigLoader resolves user keys with default fallback. Compare resulting gameplay values. An override equal to its default must match no override; a gameplay difference must reject before spawning. Different logging/address must not reject. This does not assume unsupported per-key merging for current YAML files.

## Fingerprint and ChecksumGenerator

Keep SHA-256 and the existing successful combination primitive: hash each input text, concatenate fixed-width lowercase digests in deterministic order, hash that concatenation. Add a pure content-input API; retain ComputeCombinedHash(paths) as a raw-file convenience if useful. The generator does not choose files, filter keys, parse YAML, or call GameFlowManager. Missing/open/read failures and empty inventories yield explicit failure, never partial usable checksums.

For admission, the policy supplies canonical representations of validated effective gameplay models. Use explicit per-domain projections/serializers rather than reflection or YAML reformatting. Include logical entry ID/serializer version in each encoded input; sort entries/ID-keyed collections ordinally, preserve order where gameplay depends on it, and use invariant deterministic numeric/enum/bool/null/default encoding. Reject unsupported/nonfinite values. Exclude presentation fields deliberately; new simulation fields require serializer review/versioning.

Capture text once, parse/validate/build the model, then fingerprint that model. Later disk edits cannot alter what this session claims to use. Comments, whitespace/property order, absolute paths and cosmetic changes must not affect gameplay equality. Equal effective values with different override layout match; changed damage/behavior/category reference/trigger/stacking differs. Raw path hashing is diagnostics, not the admission contract.

Immutable SessionFingerprint carries policy/canonical-format versions and gameplay digest. Check game/protocol/engine-build separately; editable GameConfig.Version alone is not a reliable build contract. No recursive scanner/global manifest manager or identity system is needed. A small declared comparison inventory is necessary for this editable policy.

Freeze gameplay data for the host session in this increment. Runtime generation later uses content revisions and immutable registered definitions rather than mutating the meaning of a live ID. Reset session fingerprint at teardown; old static GameConfig values cannot authorize a new player. ConfigDataChecksum may be diagnostic, never admission authority.

Mismatch UI identifies the logical entry where available and requests matching gameplay definitions/mod data. Bounded per-entry digests may supplement the combined hash for diagnosis, without exchanging files automatically. This is cooperative compatibility, not anti-cheat.

### Future generated items and passives

Identical items means equal definitions wherever simulated, not identical inventories. A player may generate/supply a definition; host validates supported gameplay rules, registers immutable canonical content under a stable/content-derived identity, and distributes it before instances reference it. Conflicting content under an existing ID must reject or receive a new identity, never overwrite live meaning.

Static overrides must match on admission in this increment; automatic mod downloads are excluded. Runtime contributions later require submit -> validate/register -> distribute/acknowledge -> activate. Include affected admitted peers and joining registry snapshots. Tie snapshots/fingerprints to a content revision and buffer/retry catch-up when revision advances while keeping the world running. A joiner must not silently rewrite existing world rules. Do not restart the host for every generated item.

Reserve the provider now; implement registry/distribution when generated gameplay exists. Comparing local file lists alone cannot meet that future requirement.

## Initialization, world synchronization, admission

GamePlayManager exposes one result/task per local scene generation. It completes after gameplay data/fingerprint, services, UI/input binding, current world setup and spawn coordinator are ready. Await actual async/deferred dependencies. Failure, required-node exit, cancellation, timeout or stale generation invalidates success. _Ready/SceneChanged/frame counts/empty registries do not certify initialization. Controls/navigation stay inactive until admission.

Use one deterministic persistent Protocol endpoint with matching RPC path/declarations and targeted primitive/container messages. Disable object decoding. Capture sender before awaits. Envelopes carry protocol, host session identity, shared world epoch, content revision and fresh host-issued per-peer token. Keep local generation distinct. PlayerJoinData remains empty/immutable; identity comes from actual sender/state. Follow [Godot RPC requirements](https://docs.godotengine.org/en/4.6/tutorials/networking/high_level_multiplayer.html).

After short preflight, use reliable control traffic:

1. **HostReady:** host initialization succeeds; send current epoch/content fingerprint and per-peer token. Persistent endpoint can receive it while client loads.
2. **ClientReady:** local initialization succeeds; client checks host fingerprint and sends its own plus DTO. Host checks actual sender/token, current readiness/revision, capacity, required entries and DTO. Both parties have compared gameplay data.
3. **World synchronization boundary:** WorldJoinCoordinator snapshots authoritative state at a known tick/sequence and catches up later changes before activation. Input stays blocked. In this logging-only increment it is explicitly a no-replication stub, never reported as real world synchronization.
4. **AdmissionAccepted:** after compatibility/current world-join contract succeed, record admission and invoke host StartSpawning once, then send acceptance. Client activates once on matching acceptance and valid local result. Stub failure removes peer rather than announcing success.

Three admission messages suffice for the stub; real snapshot/catch-up adds a bounded transfer protocol before real spawning. Implement that synchronization before enabling player instantiation. Do not substitute closing the server after host activation for the required persistent-world join behavior.

Host local admission uses the same validity/spawn gates without self-RPC or clients. Duplicate ClientReady resends acceptance without respawn; duplicate acceptance does not reactivate. Pending peers may only use preflight/readiness/rejection/world-sync/health messages. Future input handlers independently require admitted sender. Bound admission payloads initially to 4 KiB, pending peers by capacity and message rates. Future large snapshots use separate bounded/chunked transfer.

Immediate send failure removes peer. A queued acceptance may be lost after host logged spawning; treat as admission then removal. Exactly-once is per live host session/peer admission, not atomic activation across machines or crash recovery. Actual spawning later attaches removal/despawn/retention policy to the same path.

Reject wrong sender/token/epoch/revision, missing/mismatching fingerprint, malformed/oversized/unsupported content. Send a bounded reason with short forced-disconnect grace, never indefinite delivery waits. One failed join cannot return host/other clients to menu. Disable client relay if the native star topology supports it; still validate senders.

Keep setup/auth, scene, admission and future snapshot budgets independent. Initially use ConnectionTimeout for setup, 60 seconds scene initialization and 15 seconds admission after local readiness; bound waiting for HostReady too. Add a measured world-transfer budget when snapshots exist. Pongs never extend phase deadlines.

## Heartbeat and RTT

Workflow moved to [Heartbeat and round-trip measurement](network-heartbeat-rtt-implementation-plan.md).

## Delivery stories and acceptance

Preserve NET-01..NET-12 identifiers; revised scopes supersede the old decomposition. NET-00 verifies native behavior; NET-13 makes the world-join seam explicit. Cleanup is part of every story.

| Story | Outcome and acceptance | Depends on |
| --- | --- | --- |
| NET-00 | Native capability matrix/traces: auth/signals/errors/RPC/channels/identity/close/retry/third peer. Choose one evidenced preflight. | None |
| NET-01 | Persistent owner, requests/results/states, cancel/cleanup, endpoint. Replace legacy events/MpManager sufficiently for a buildable slice. No stale/duplicate transition. | NET-00 |
| NET-03 | ENet endpoint/capacity/actual outcomes; test occupied port, refusal, cancel/leave/retry and Steam-unavailable LAN startup. | NET-01 |
| NET-06 | Editable inventory, canonical gameplay projections, pure hash API/fingerprint. Test equal overrides/default fallback, formatting/order/cosmetics independence, gameplay differences, excluded schemas/local config, required entries, capture/read failures and reset. | NET-01 |
| NET-07 | Explicit initialization/dependency tasks; no unconditional Start. Failure/exit/cancel/stale/deadline cannot activate input. | NET-06 |
| NET-08 | Preflight/envelopes/token/epoch/revision/DTO/allowlist. Test spoofed host/wrong sender, malformed/oversized/unsupported messages, pending capacity, actual RPC delivery. | NET-00, NET-01 |
| NET-13 | WorldJoinCoordinator contract and explicit no-replication stub. Acceptance waits for result; cancel one join independently. Test moving host world tick while joining; record real snapshot/catch-up remains unimplemented. | NET-07, NET-08 |
| NET-09 | Readiness/acceptance per peer; test ordering/matches/mismatches/duplicates/stale/timeouts/revision invalidation and independent clients. | NET-06, NET-07, NET-08, NET-13 |
| NET-10 | Host/local-host spawn log and accepted-client activation once per fresh admission; none on failed readiness/mismatch. Postdecision loss removes without duplicate spawn. | NET-09 |
| NET-11 | Correlated probes/common removal plus latest/smoothed RTT and UI-facing ping metrics from the same messages. With a controllable clock test known RTT, first-sample initialization, EMA (100 ms then 180 ms yields 110 ms), stale/unavailable/reset, duplicate/out-of-order/spoofed replies, phase deadlines, pause/loading/congestion and nonresponsive host/client. No extra ping traffic or remote-clock dependency. | NET-08, NET-09 |
| NET-02 | Steam membership/peer/publication/pinned host/session/callback draining/recovery. No creator double-completion or owner-transfer migration. Optional Steam failure leaves LAN. | NET-01, NET-07, NET-08, NET-10 |
| NET-04 | Running-game discovery/filters/serialized refresh/persona/counts/deactivation. Test empty/invalid/stale/full and late callback timeout/drain. | NET-02 |
| NET-05 | Actual menus/entries/selection/status/Refresh/search/sort/address/Back; keyboard, stable ID, duplicate actions, inventory toolbar smoke regression. | NET-03, NET-04 |
| NET-12 | Repeated join/leave/rejoin while host stays active; independent clients; host loss; init/scene/data/health failures; cleanup before Steam shutdown. Record native/stub limits. | All previous |

Order: NET-00 -> NET-01 -> NET-03 -> NET-06/07 -> NET-08/13/09/10 -> NET-11, then Steam NET-02/04/05 and integration NET-12. Prove connection -> effective gameplay compatibility -> initialization -> world seam -> admission -> stub before browser work obscures protocol failures.

Run focused NUnit state/codec/canonicalization/hash tests, a game build, Godot scene smoke checks and multi-process integration. Establish baseline build failures first, especially legacy MpManager. Use exact custom engine and two Steam accounts/devices; include three peers to prove rejected/dead client isolation. Repeat joins to the same host and race for last slot while host world/session identity remains unchanged. A .NET build cannot verify native Steam. No runtime tests are claimed by this planning pass.

## Handoff and next increment

Read this plan, sMPGWM.Game/README.md, flow/gameplay/checksum/config/file-loading code, Network, SteamworksHelper, StartMenuSystem and actual join/entry scenes before implementation. Next action: NET-00 then ENet vertical slice. This pass edits the plan only; code changes begin when implementation is requested.

Before replacing the spawn stub, implement authoritative snapshot/catch-up, world-owned versus player-owned lifetimes, identity/progress restoration policy, input validation, simulation/prediction/interpolation and bandwidth budgets. Add runtime definition registry/distribution when generation exists, using reserved policy provider/content revisions. Shared definitions are not synchronized world state; the required product remains an open running-world server browser.
