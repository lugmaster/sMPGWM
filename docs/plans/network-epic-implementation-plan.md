# Network epic implementation handoff

Status: ready for implementation, 2026-10-31. No implementation or runtime verification is claimed.

Start each fresh context with this file. It owns execution order and progress; linked plans supply detailed contracts. This order supersedes older delivery orders. Refinement completion is not implementation completion.

## Workflow for every context

1. Read repository instructions, this file and the references for the first unfinished checkpoint. Inspect current code and working-tree changes; preserve unrelated work.
2. Implement that checkpoint through production classes. Introduce types only as needed. A harness supplies inputs/evidence, never a parallel networking implementation.
3. Verify meaningful implementation steps with focused checks, then run the checkpoint acceptance gate. Separate baseline failures from regressions. A build alone cannot verify native/engine behavior.
4. Update this file with status, changed files, exact commands/executable/build, results, evidence paths and limitations. Mark Verified only when required checks pass. If accounts/devices/tooling prevent a required check, record Needs verification and the precise next action; do not silently advance past that gate.
5. End the context after the checkpoint. Start the next checkpoint in a fresh context. If work spans contexts, resume the same checkpoint using its recorded next action.

Record execution-changing decisions, not understanding questions or discussion transcripts. Completion does not authorize publishing, merging or deployment.

## Scope and milestones

Checkpoints 1–5 deliver the refined message-exchange milestone: separate host/client processes exchange a correlated application request/reply over ENet and then Steam, with repeatable cleanup and failure handling. A supplied Steam lobby ID is sufficient. These checkpoints deliver portions of broader stories without declaring those stories complete.

The remaining checkpoints finish the existing running-game vertical slice across NET-00–NET-14: compatibility, initialization, admission, logging-only world/spawn seams, health, public Steam publication and real browser/menu integration. They are continuation work, not prerequisites for the first exchange. Track the two milestones separately.

Excluded: real world replication, real avatar spawning, roster/controller synchronization, per-system movement/combat/inventory replication schedules, prediction, automatic reconnect and durable saves. The world-join seam is explicitly a no-replication stub; spawn is a log, not a synchronized playable world.

## Shared decisions

- `GameFlowManager` owns navigation and persistent `NetworkManager`; the manager owns one attempt/session, peer assignment, persistent protocol endpoint, ticker and idempotent cleanup. Flows/adapters/browser/lobby service/health logic are plain classes unless Node/RPC behavior requires otherwise. Replace legacy ownership and static completion events; retain no second session manager.
- The identically named/path `SessionProtocolNode` exists before peer assignment. Capture actual sender before awaiting. Use bounded primitive messages with object decoding disabled. Connection, diagnostic success and gameplay admission are separate results.
- `_PhysicsProcess` is the single ticker source. Manager checks can run every physics step; gameplay network cycles run every second physics update (30/second at 60 Hz). Verify actual physics configuration. This replaces the nominal 30 ms target; reconcile older README wording during implementation. Control/diagnostic/health replies remain prompt. Per-system rates are deferred.
- Target typically 5 players, maximum 10 including host; pending peers count toward capacity. Avoid unnecessary recurring allocations, reuse bounded storage, never force GC, and measure engine timing/GC under representative load.
- Capture `GameConfig.ConnectionTimeout` per attempt (milliseconds, currently 60000). Diagnostic, gameplay-phase and health deadlines remain independent. Engine/Steam operations stay on the main thread.
- Invalidate attempts before idempotent cleanup of resources, subscriptions and membership. Local session-ending errors use `QuitToStartMenuBlocking(reason)` after cleanup; reserve `QuitWithOsError` for unrecoverable whole-game errors. Failed remote clients must not terminate the host or healthy clients.
- `SteamworksHelper` owns Steam initialization/pumping/shutdown; failure leaves LAN usable. Persistent `SteamLobbyService` serializes/drains canceled operations and leaves late successful membership. Inspect installed callback correlation before allowing overlap.
- Pool `game_id` equals configured `lobby_game_id` (`f6c96d72-6afb-4ac1-a14b-ee12daba65c6`): exact filter on every search, validate results and all joins including supplied IDs. Mode (`CoOP`) is display metadata. Initially use `DefaultHostedGameName = "sMPGWM TestLobby"` plus a session-specific suffix; future host input replaces the base. Lobby/session IDs remain identity.
- Private diagnostic hosting is separate from public ready/joinable publication, which requires compatibility, initialization and local-host admission. Pin original host/session; ownership transfer does not migrate the authoritative world.
- Browser snapshots are immutable; UI owns stable-ID selection and local search/sort. Browsing creates no peer/membership. Subscribe only while active, including hidden screens retained in history. Admitted count differs from membership count. No invented browser ping.
- Health uses bounded correlated probes and local monotonic timestamps; manager owns removal. Initial 2-second probes / 10-second deadline are accepted during loading, independent of gameplay cycles. Validate loading/grace and channel behavior. EMA alpha 0.125; unavailable/stale/reset metrics are explicit. Health replies cannot extend admission deadlines.

## Execution tracker

Statuses: Pending, In progress, Needs verification, Verified. All begin Pending. Store detailed evidence below the table.

| Order | Story checkpoint | Status | Work and verification gate |
| --- | --- | --- | --- |
| 1 | NET-14: ENet diagnostic exchange | Pending | Minimal owner, endpoint, ticker, ENet adapter, flows and immutable requests/results. Follow the [seven-step contract](network-vertical-slice-refinement-plan.md#first-story-net-14-host-and-client-exchange-a-diagnostic-message). Two actual processes exchange matching Hello/Reply; verify host-alone listening, leave/rejoin preserving host identity, occupied port, unreachable endpoint, cancellation, timeout, invalid messages, duplicate starts and clean retry. |
| 2 | NET-01: shared ownership completion | Pending | Finish persistent owner lifetime, one active attempt, instance results/loss events, resource handoff and legacy replacement. Verify late callbacks, scene exit, repeated cleanup, duplicate starts and retry. Extend checkpoint 1 rather than creating a second manager. |
| 3 | NET-03: ENet completion | Pending | Finish endpoint/capacity validation, pending slots, host-only configuration, actual connection/refusal outcomes and Steam-unavailable LAN startup. Verify occupied port, cancel/leave/retry and optional Steam failure. |
| 4 | NET-00: Steam capability gate | Pending | On exact custom engine verify native creation/status/errors, signal order, targeted reliable RPC, channels, auth/identity support and close/retry. Choose evidenced preflight/channel behavior; record unsupported features. Use minimal capability probes in the existing harness before production integration. Multi-peer checks requiring later flows remain explicitly open until checkpoint 16. |
| 5 | NET-02 diagnostic subset: Steam exchange | Pending | Add lobby service/Steam adapter and supplied-ID host/client flows. Private diagnostic lobby, checked metadata, membership bookkeeping and pinned host/session. Verify real request/reply with two accounts/devices, creator callback single completion, canceled/late membership cleanup, wrong-pool/self/full/stale targets, host loss/owner transfer, repeatability and LAN survival. No public publication yet. Closes message milestone only after actual Steam checks pass. |
| 6 | NET-08: protocol/preflight completion | Pending | Extend bounded protocol with verified preflight, admission envelopes and sender/session/token/epoch/revision/state checks. Test malformed/oversized/unsupported/spoofed/wrong-state messages, finite preflight, pending capacity and actual RPC delivery. No unverified auth/channel assumptions. |
| 7 | NET-11: heartbeat and RTT | Pending | Start after preflight, including loading; common manager removal and read-only metrics. Clock tests: known RTT, 100 then 180 ms gives 110 ms EMA, duplicate/expired/wrong-peer/session/out-of-order replies, bounded storage, unavailable/stale/reset. Engine tests: pause/loading/stalls/loss/congestion and host versus client expiry isolation. Does not require gameplay admission. |
| 8 | NET-06: compatibility | Pending | Typed effective-data policy, fingerprint and pure hash utility. Verify overrides/default fallback, formatting/order/cosmetics independence, gameplay differences, required entries, read/capture failure and reset. Exclude unrelated local config and schemas. |
| 9 | NET-07: explicit initialization | Pending | Replace unconditional activation with explicit initialization/dependency results. Failure/cancel/scene exit/stale completion/deadlines cannot activate input. Scene arrival alone is insufficient. |
| 10 | NET-13: world-join contract | Pending | Explicit no-replication stub, current-world identity and per-join cancellation. Verify admission waits for result, individual cancellation and host progression. Do not claim snapshot/catch-up replication. |
| 11 | NET-09: admission | Pending | Both ends of HostReady -> ClientReady -> world-join result -> AdmissionAccepted. Verify ordering/mismatch/stale/duplicate/token/revision/timeouts and independent clients. Phase deadlines remain independent of health. |
| 12 | NET-10: spawn log/activation | Pending | Local-host and remote spawn logging once per fresh admission; matching acceptance activates client once. Verify duplicates, postdecision loss and no spawn on failure. No real player scenes or roster synchronization. |
| 13 | NET-02 completion: public hosting | Pending | Gate ready/public/joinable publication on transport, fingerprint, initialization and local-host admission. Publish/update admitted count/capacity. Verify publication failure cleanup, single setup completion, host continuity during client churn and pinned-host loss; regress diagnostic Steam checks. |
| 14 | NET-04: discovery | Pending | Immutable snapshots, exact pool/version/protocol/build filters every refresh, Worldwide, valid ready entries, persona fallback, bounded results, serialized refresh/drain. Fixtures plus real publication discovery; empty/error/stale/full/self/wrong-pool and late inactive responses. Search/sort stay local; browsing never joins. |
| 15 | NET-05: menus | Pending | Actual menus/entry scenes, target handoff, LAN address, status/Refresh/Back and duplicate-action prevention. Verify keyboard access, stable-ID selection across sort, clearing removed/hidden selections, local search, client revalidation and shared inventory toolbar regression. |
| 16 | NET-12 integration / remaining NET-00 matrix | Pending | Repeated join/leave/rejoin, three-peer isolation, last-slot races and unchanged host session/world. Host loss, init/scene/data/health failures, late callbacks, cleanup before Steam shutdown, optional-Steam LAN, pause/background and representative-load timing/GC. Close remaining native capability checks and every story's acceptance. Record stub limits. |

Use existing test/build conventions: focused deterministic state/codec/hash/clock tests, game build, relevant Godot scene smoke checks and real multi-process integration. Discover the actual custom executable rather than inventing its path. Record exact inputs and both process logs without account secrets. Do not repeat passed checks unless changes or unresolved failures justify it.

## Detailed story references

- [Refinement and NET-14 sequence](network-vertical-slice-refinement-plan.md): accepted architecture and diagnostic boundary.
- [Shared story acceptance](network-lobby-implementation-plan.md#delivery-stories-and-acceptance): NET-00–NET-13 details, compatibility and admission. Older order superseded here.
- [Host workstream](network-host-implementation-plan.md): NET-00/01/02/03, shared NET-06/07 and local-host NET-10.
- [Client workstream](network-client-implementation-plan.md): NET-02/03 client path, NET-08/09/13 and remote NET-10.
- [Lobby list](network-lobby-list-implementation-plan.md): NET-04 and browser NET-05.
- [Heartbeat/RTT](network-heartbeat-rtt-implementation-plan.md): NET-11 and NET-12 health checks; may run after preflight without admission.
- [Roster follow-up](network-roster-synchronization-plan.md): deferred, outside this epic.

## Current checkpoint record

Next: **1 — NET-14 ENet diagnostic exchange**.

Implementation: not started. Verification: none run; preceding work edited plans only.

Next context: inspect repository instructions, game README, solution/test conventions, GameFlowManager, Network, SteamworksHelper and NET-14 contract. Establish baseline build and exact engine availability, then implement the minimal ENet exchange.

At each handoff append a compact durable record: checkpoint/story, changed files, commands/results/evidence, limitations. Update tracker and replace this current record with the precise next action. Keep incomplete verification visible.

## Completion gates

Message milestone: checkpoints 1–5 Verified, including actual separate-process ENet and Steam exchanges, failure handling and repeatable cleanup.

Full planned vertical slice: all checkpoints Verified; all NET-00–NET-14 acceptance has evidence; no required native check remains open; real public discovery hands a target to production join/admission; host survives client churn. World/spawn stubs remain explicit. Mark only the milestone actually achieved complete.
