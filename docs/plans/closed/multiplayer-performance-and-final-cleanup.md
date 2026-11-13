## Paused at user request — 2026-11-12

Bulk evidence is now in a verified local, Git-ignored ZIP. See [archive and extraction instructions](../measurements/README.md) before resuming analysis. Reports and measurement metadata remain readable; completed benchmarks do not need restarting.

All benchmark processes were stopped and process inspection confirmed only the user's Godot editor remains. Do not resume automatically.

- Main Release matrix: all six runs completed, all 45 peer exit codes zero. Existing summary/report still cover only the first five runs; final aggregation and validation remain open. Preserve these runs; do not repeat them merely to resume analysis.
- Follow-ups: stopped during the first five-player restored scenario. Treat its artifacts as partial unless independently validated. Ten-player restoration, late join at both scales, moderate/stress impairment, observer on/off and Debug/development measurements remain deferred.
- The original waiting queue had a Windows PowerShell array-count bug. Fixed its JSON array expression before replacing that waiting process; no main benchmark was restarted or duplicated.
- Remaining analysis: aggregate the sixth main run, validate all peer artifacts/configuration/checksums/drop counts/logs, analyze the completed follow-ups when available, and finalize the report. The post-measurement test run and harness traceability/syntax checks were not performed on this resume.
- Performance acceptance remains open: repeated equipment-burst GC-associated overruns and unexplained publication/accepted-snapshot timing gaps need attribution and later measured fixes.
- The user subsequently authorized the audited cleanup. Implementation status and regression results are recorded in the cleanup audit; performance comparisons remain deferred. Archived measurements remain the pre-cleanup baseline.
- Remote Steam remains deferred. Other verification work remains closed for this pass; do not reopen it.

# Multiplayer performance measurements and final cleanup review

Status: performance measurements remain paused. The user subsequently authorized implementing the audited cleanup; its changes and regression results are recorded in the [cleanup audit](../measurements/2026-11-12-full/cleanup-audit.md). Remote Steam and performance comparisons remain deferred. The other verification work remains closed for the measurement pass. See [initial evidence](../measurements/2026-11-12-final/report.md) and the archived local measurement artifacts in `docs/measurements/2026-11-12-full`.

Resume instructions: [performance analysis handoff](multiplayer-performance-handoff.md). Its paused-state notice supersedes historical running-job instructions. No measurements are active; do not restart them without a new request.

This is the standalone plan for the two remaining workstreams after session initialization and restored-state installation: measure multiplayer runtime behaviour, then simplify the implementation using the verified lifecycle guarantees. It includes the environment, contracts, workloads, evidence, and completion criteria needed to execute the work without another plan.

## Project and runtime contract

sMPGWM is a Godot 4.7.2 C# top-down cooperative space shooter. The host owns authoritative simulation. Supported transports are ENet and GodotSteam MultiplayerPeer. A typical session has five participants and the maximum is ten, including the host.

| Requirement | Meaning for measurement and cleanup |
| --- | --- |
| 60 FPS and 60 physics steps/second | The nominal frame/physics interval is approximately 16.67 ms. Measure work duration separately from capped frame intervals. |
| 30 Hz network publication | Publish every second completed physics step. The nominal publication interval is approximately 33.33 ms; publication CPU work still shares that frame's 16.67 ms budget. |
| Host authority | Clients do not independently commit inventory, equipment, abilities, or movement outcomes. |
| Complete initialization | Gameplay starts only after definitions, world/player state, resources, grants, hotbar, local presentation/input bindings, and required authoritative baselines are ready. |
| New state | Install the intended production loadout, then initialize shield full and energy empty exactly once. Development fixtures are optional. |
| Restored state | Preserve supplied entity/item/container identities, ownership, equipment, resource values, and continuity metadata. Do not apply starter equipment or fresh-resource defaults. Validate the actual restoration entrypoints and their tests before measuring them. |
| Late join | Initialize only the joining participant while the active host and existing players keep simulating. |
| GC and allocation | Avoid unnecessary recurring allocations and reuse storage where useful. Measure automatic GC; do not force collection or claim that zero GC is required. |

Save-file formats, storage, migrations, new abilities, enemies/AI, general dependency injection, and a universal event bus are outside this plan.

Restoration entrypoints are `GamePlayManager.CaptureState()` (active authoritative session at a completed-step boundary), `NetworkStartupData.RestoredWorld` (normal host startup), and `GameWorld.RestoreState` (validated in-memory installation). The world installer preserves multiple supplied participant bindings and uncontrolled ships; normal network restart accepts host-only participant bindings because prior ENet peer numbers cannot authenticate returning players. For restored multiplayer measurements, restore a host-only participant state and admit fresh clients normally. Do not represent that workload as reconnecting previously saved remote owners. Persistent authenticated reconnect mapping and save storage are outside these two workstreams. The snapshot retains local hotbar/resource/effect/identity continuity, while transport sessions, pending commands, prediction history, and edit tokens start fresh.

## Lifecycle and ownership to preserve

The lifecycle is StartMenu -> visible Loading -> initialization -> Active -> teardown -> StartMenu. All Host/Join choices for both transports enter through `GameFlowManager.StartNetworkGame`.

| Owner, under `sMPGWM.Game/Scripts` | Responsibility |
| --- | --- |
| `GameFlowSystem/GameFlowManager` | Navigation, loading/result presentation, and rejection of obsolete navigation continuations. Rendered startup waits for `FramePostDraw`; headless startup uses a process-frame boundary. |
| `GamePlaySystem/SessionCoordinator` and `SessionScope` | The single transition to Active and disposal of session resources, including partial construction. |
| `GamePlaySystem/SessionInitialization` | Explicit definition preparation, construction, synchronization, binding verification, and completion stages. |
| `GamePlaySystem/GameSessionFactory` | Construct/connect components and route network readiness through initialization. |
| `GameConfigSystem/GameConfig`, registries, and focused loaders/parsers | Prepare one validated definition set and compatibility fingerprint. Successful data may be reused; failed preparation clears partial caches/captured inputs so retry can succeed. |
| `WorldSystem/GameWorld` and state installation code | Own entity lifetimes and install new or supplied authoritative state before participant readiness. |
| `PlayerControllerSystem/LocalPlayerBindings` | Bind local inventory, equipment, hotbar, targeting, and presentation; retain runtime control-switch support. |
| `NetworkSystem/NetworkManager` and join protocols | Transport/replication milestones, ownership/revision/session validation, private-state visibility, timeouts, and completion exchange. |
| `NetworkSystem/SynchronizationCoordinator` | Fixed-step simulation and every-second-step publication. |

The client applies topology, local control, movement, gameplay, and private item baselines before acknowledging readiness. Acknowledgement/completion identify the session, entity, and control generation. Before completion, the host sends current topology, full gameplay state, and owner items reliably in order. Live movement remains monotonic and continues during loading. A local control reassignment during initial synchronization currently causes a controlled rejoin failure; normal control reassignment after activation remains supported. Preserve this distinction or change it only with explicit coverage.

Connection state, application lifecycle, and entity lifetime are separate concerns. Initialization completion does not make untrusted-message checks, revision checks, runtime rebinding checks, or cancellation protection redundant.

## Workstream 1: multiplayer performance measurements

### 1. Establish a reproducible baseline

- [ ] Record the exact revision and dirty-tree patch, Godot/.NET/Steam integration versions, OS, CPU/GPU/RAM, graphics/render settings, display refresh, power settings, and host/client placement. Separate dedicated machines from multiple processes competing on one machine.
- [ ] Run `dotnet test sMPGWM.sln --no-restore` and retain current results. This command runs managed cases and a wrapper that launches the Godot tests. Record the two sets of counts separately; do not add the wrapper to the in-engine count as an independent engine case.
- [ ] Identify the restored-state installation entrypoints and regression cases present in the measured revision. Verify identity/ownership, item allocation after restore, resource/grant continuity, and failure rollback before using restored workloads.
- [ ] Save exact config and definition/schema inputs plus compatibility fingerprints for every run. The repository config currently uses `Debug` logging, `development=true`, `reload_config_on_start=true`, ten maximum players, loopback address, port 55555, and a 60-second connection timeout. These are reproducibility inputs, not production benchmark assumptions.
- [ ] Establish the first measurement of the current implementation as the baseline before performance edits or final cleanup. If a historical comparison is required, use an isolated compatible checkout and record its workload differences. Never invent missing historical measurements.
- [ ] Use isolated user-data directories and available ports for concurrent processes. Keep test data/config changes out of the user's normal game state. Record cold-process definition preparation separately from reuse within a running application.

Run the main budget assessment in Release without an attached debugger, with explicitly recorded normal logging and fixture settings. Repeat representative cases using the supplied Debug/development configuration to quantify diagnostic and fixture overhead. Keep detailed tracing runs separate from ordinary measurements and quantify collector overhead with equivalent uninstrumented runs.

### 2. Collect the measurements that explain stalls

- [ ] Capture per-frame and per-physics-step work duration, frame intervals, publication-step versus non-publication-step duration, and host publication timestamps/tick numbers. Report p50, p95, p99, maximum, and counts/time spent above the nominal budget.
- [ ] Report host emission cadence separately from client arrival intervals, packet loss, retransmissions, RTT, jitter, and recovery. Network impairment can delay arrivals even when the host publishes on time.
- [ ] Capture allocation bytes/second, bytes/physics step where available, collection counts by generation, GC pause durations, managed heap size, process memory, and relevant CPU/GPU utilization. Do not use forced GC to make samples look stable.
- [ ] Timestamp loading start, first rendered loading frame where applicable, preparation completion, state installation, baseline application, completion acknowledgement, and first active frame. Correlate late-join work with stalls experienced by players already active.
- [ ] Capture phase costs for entity construction, initial inventory creation, equipment installation, derived-stat/grant updates, topology capture, snapshot encoding/delivery, and completion exchange. Use bounded recording buffers or external profiling where practical; avoid per-frame string formatting in the normal measurement path.
- [ ] Distinguish actual resource reads/decoding from engine cache lookups. Measure retained resource lifetime and cache reuse around repeated spawns before adding resource caches or changing scope ownership.

Retain raw samples and event markers, not just averaged FPS or screenshots. Samples must identify the process/peer, scenario, run number, and configuration so a later spike can be traced to a workload event.

### 3. Execute the workload matrix

For steady scenarios, start with a 30-second warm-up and a 180-second measured interval, repeated three times. Record startup separately without excluding its cost from the loading report. For joins/disconnects, use scripted event times and retain the whole transition. Increase duration if rare pauses or drift remain unresolved; document any deviation from these initial settings.

| Scenario | Five and ten total participants | Required observations |
| --- | --- | --- |
| Idle after initialization | New state and representative restored state | No repeated domain initialization, registry parsing, stat compilation, or hotbar setup; stable memory after warm-up. |
| Movement and supported combat | All participants move/turn/use existing available abilities with reproducible inputs | Frame/physics budget, publication cadence, authority and state convergence. Preserve ability cooldown/energy rules. |
| Equipment/inventory bursts | Defined transfer rate, source/destination slots, successful and rejected operations | Preparation/install cost, allocations, revisions, grants, resource effects, and publication consistency. Include unrelated stored items to expose accidental global scans. |
| Late joins | Four -> five and nine -> ten, while existing players continue movement/combat; repeated departures/rejoins | Existing-player stalls, complete initial state, completion duration, private-state visibility, and absence of global reinitialization. |
| Joining an evolving world | Remote spawns/removals, equipment changes, and movement while another client loads | Completion exchange covers current state and never rolls newer revisions back. |
| Cancellation/failure/retry | Cancel during loading, disconnect during baselines, omit/delay required state, retry after failure | Controlled failure or continued loading, cleanup, no stale activation, and successful fresh attempt. |
| Post-restore continuation | Non-default resources, equipment, identities, and continuity metadata | No starter/default replay, duplicate items/grants, resource refill, or identifier reuse; normal gameplay continues. |

Run ENet healthy cases first, then reproducible impairment cases. Define and record delay in each direction, jitter distribution, loss, reordering/duplication if exercised, bandwidth limits, random seed, and the mechanism applying them. Use at least one moderate profile and one stress profile; choose their numeric values before the compared runs and retain them unchanged. Distinguish actual transport shaping from synthetic message delay/drop logic.

Existing explicit tests `AbilityGameplayProcessTests.TenPlayersWithTwoAuthoredBehavioursConverge` and `TagSystemIntegratedTests.Stress_30Tps_BaselineGameplayFixture_WritesBenchmarkArtifacts` are starting points. They are not run by the ordinary suite. Inspect their current workload and instrumentation before reuse; run them explicitly and extend only where the matrix requires it. A headless synthetic benchmark cannot establish rendered FPS or GodotSteam behaviour.

- [ ] Verify rendered Host/Join loading and the first active frame for both ENet and Steam. Confirm immediate movement, available supported ability use, and correct inventory/menu state without deferred repair.
- [ ] Run actual GodotSteam MultiplayerPeer sessions with distinct peers/accounts on the intended network setup. Keep transport-specific results separate from ENet results.
- [ ] If accounts, machines, or network controls are unavailable, record the exact unexecuted cells and required setup. ENet success must not be reported as Steam verification.

### 4. Audit transactions and resolve measured regressions

- [ ] Audit `ItemStore` and loadout preparation/install callbacks, including restored-state installation. Preparation must validate and reserve capacity before commit, leave published state unchanged on expected failure, and publish only after domain state and indexes agree.
- [ ] Verify that authoritative restoration retains IDs/revisions/ownership and advances identity allocation without copying replica-only behaviour into authoritative transactions.
- [ ] Keep incremental indexes, bounded command storage, cached join membership snapshots, and direct slot-based stat calculation. Confirm equipment transfers do not regain work proportional to unrelated store contents.
- [ ] Attribute overruns to specific work and distinguish GC, logging, rendering, resource acquisition, serialization, simulation, and external scheduling. Use identical traces/workloads to compare a proposed fix with the baseline.
- [ ] If spawn/loadout work produces a material spike, prepare validated initial state together or stage bounded work before admitting that participant. Keep existing host simulation running and preserve snapshot/transaction consistency.
- [ ] Retain resources during initialization only where measurements justify it. Document ownership and release on success, cancellation, and failure; avoid keeping session objects alive through an application cache.
- [ ] Re-run changed scenarios and relevant correctness tests after each fix. Report both improvements and regressions, including tail latency and allocations.

Acceptance: on the documented test environment, healthy supported workloads sustain the required cadence without unexplained repeated budget overruns. Report every observed overrun and its cause; averages alone do not establish acceptance. For impaired workloads, demonstrate controlled recovery/failure and continued host cadence, separately from network delivery timing. If a workload cannot meet the contract, keep that issue open with the raw evidence and proposed next action.

## Workstream 2: final cleanup review

Begin this review after the measurement baseline and relevant correctness scenarios exist. Keep cleanup in a separate reviewable change from measured performance fixes.

### 5. Review candidates against explicit invariants

- [ ] Review `GameFlowManager`, `SessionCoordinator`, `SessionInitialization`, `GamePlayManager`, `GameSessionFactory`, network join state, `GameWorld`, new/restored state installation, local bindings, and registry access.
- [ ] Find duplicate readiness flags, overlapping transitions, repeated readiness/null checks, deferred setup callbacks, lazy setup fallbacks, and repeated binding work that the verified initialization contract makes unnecessary.
- [ ] Review loader/registry forwarding, source descriptors, startup-only adapters, and caches. Combine or remove responsibilities only where ownership becomes clearer and measured runtime needs remain satisfied.
- [ ] Examine `FileLoader.AbortLoad` as a known candidate: if still unused, remove the loader-to-navigation presentation helper now that session initialization reports failures. Verify usages at execution time rather than relying on this document.
- [ ] Review new/restored installation paths for duplicated defaults, repeated grant/resource initialization, and test-only migration adapters. Retain explicit distinctions needed to preserve restored state.
- [ ] Keep protection at untrusted message boundaries, authority/ownership/revision checks, transaction safeguards, legitimate optional components, cleanup after partial construction, and stale callbacks after cancellation. Do not merge protocol, application, and entity states merely because their names look similar.

For every candidate, record its path/symbol, the invariant that would permit removal, the evidence, the change or reason for retaining it, and affected checks. No deletion is required where a candidate still has a distinct responsibility. Do not use this workstream for unrelated repository-wide style changes.

### 6. Apply and validate the simplifications

- [ ] Make the smallest justified changes; use IDE semantic refactoring for declarations/usages when available. Preserve namespaces and one file per class/enum/record, subject to the existing embedded-class exception.
- [ ] Run relevant managed and Godot regressions, then the required integration suite. Cover preparation failure/repair/retry, cancellation, stale/duplicate messages, local rebinding, ownership/transfers, equipment grants, resource initialization, and restored-state continuity.
- [ ] Repeat the same performance scenarios when runtime paths or allocations change. Compare raw samples and tail results against the pre-cleanup baseline.
- [ ] Update the final lifecycle/ownership documentation and candidate ledger. Remove temporary adapters/instrumentation only if they have no ongoing verification or production purpose.

Acceptance: no undocumented second activation mechanism or migration-only fallback remains. Every removed guard has an explicit invariant and supporting validation; every retained similar-looking state has a distinct purpose. Correctness and measured cadence remain intact.

## Evidence and delivery

Store a dated report and machine-readable samples under a clearly named measurement directory in `docs` or the repository's established artifact destination. Include the following in each report:

| Evidence | Required contents |
| --- | --- |
| Run manifest | Revision/patch, environment, exact commands, configuration/definition fingerprints, transport, peer placement, workload, random seed, warm-up/duration/repeats, profiler and logging settings. |
| Results | Frame/physics/publication distributions, maxima and overruns, allocation/GC measurements, startup/late-join timing, convergence and failure outcomes. |
| Comparison | Baseline and changed runs under the same conditions; explanations for differences and collector/logging overhead. |
| Coverage | Executed/unexecuted matrix cells, rendered versus headless results, real Steam account/machine coverage, and outstanding setup needs. |
| Cleanup ledger | Candidate, invariant, decision, implementation, affected tests, and performance evidence where relevant. |

Follow the repository coding rules: prefer `var`, avoid LINQ in new implementations, use efficient arrays/storage where appropriate, and keep comments useful and minimal. Every declaration or executable line added solely for automated/manual verification, profiling, injection, or test support must have a unique `TEST-SUPPORT` ID and the fully qualified test method(s) using it. Check uniqueness and references, including helpers added to application code. Do not mark ordinary production restoration or initialization logic as test-only merely because tests exercise it.

The plan is complete only when the required measurement matrix has reported results, material regressions have been resolved, and the cleanup review has an evidence-backed disposition for its candidates. A successful ordinary test run alone does not close either workstream. Report unavailable environments or unresolved regressions as partial completion with exact follow-up requirements; unexecuted mandatory transport/scale verification keeps the corresponding workstream open.
