## Paused at user request — 2026-11-12

Bulk evidence is now in a verified local, Git-ignored ZIP. See [archive and extraction instructions](../measurements/README.md) before resuming analysis. Extract the bulk files before running the summarizer; do not restart completed benchmarks.

All benchmark processes were stopped and process inspection confirmed only the user's Godot editor remains. Do not resume automatically.

- Main Release matrix: all six runs completed, all 45 peer exit codes zero. Existing summary/report still cover only the first five runs; final aggregation and validation remain open. Preserve these runs; do not repeat them merely to resume analysis.
- Follow-ups: stopped during the first five-player restored scenario. Treat its artifacts as partial unless independently validated. Ten-player restoration, late join at both scales, moderate/stress impairment, observer on/off and Debug/development measurements remain deferred.
- The original waiting queue had a Windows PowerShell array-count bug. Fixed its JSON array expression before replacing that waiting process; no main benchmark was restarted or duplicated.
- Remaining analysis: aggregate the sixth main run, validate all peer artifacts/configuration/checksums/drop counts/logs, analyze the completed follow-ups when available, and finalize the report. The post-measurement test run and harness traceability/syntax checks were not performed on this resume.
- Performance acceptance remains open: repeated equipment-burst GC-associated overruns and unexplained publication/accepted-snapshot timing gaps need attribution and later measured fixes.
- The user subsequently authorized the audited cleanup. Implementation status and regression results are recorded in the cleanup audit; performance comparisons remain deferred. Archived measurements remain the pre-cleanup baseline.
- Remote Steam remains deferred. Other verification work remains closed for this pass; do not reopen it.

# Handoff: finish local performance analysis and cleanup recommendations

The instructions below describe the earlier measurement pass. The user later paused benchmarks and then authorized the audited code cleanup. See [cleanup implementation status](../measurements/2026-11-12-full/cleanup-audit.md); preserve the archived pre-cleanup evidence when resuming comparisons.

Updated 2026-11-12 23:15 Vienna time (22:15 UTC). **Benchmarks are still running. Do not restart or overlap them.**

## User request and scope

The user requested full local performance measurements and a cleanup audit with **suggested** changes, then a report. They explicitly closed the other verification work for this pass and deferred another full remote Steam test until after upcoming features. Do not reopen Steam setup, request accounts, or apply the proposed cleanup/performance optimizations now. Latest request: write this handoff because context is nearly full.

Workspace: `C:\Users\lober\Documents\Godot\sMPGWM`. Base revision: `a57352656b8ca7179bc81730772b6d477ea64022`.

Follow AGENTS.md: C#, prefer var, avoid LINQ, namespaces match files, one class/file, no forced GC. Every new test/profiling declaration or executable line must have a unique `TEST-SUPPORT` marker linked to the fully qualified test method. All new harness files use `sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix`. Do not remove those markers.

## Running jobs: resume these, do not launch duplicates

Use the command-monitor skill/tool family through `functions.exec`. Tools are deferred; discover names via `ALL_TOOLS.filter(t => t.name.includes('cmd_monitor'))` if necessary. Commands persist after a turn; monitor documentation says they can stop when the session ends, so preserve the current IDE/session while they run. Context compaction itself should not require a restart. If reconnecting to a different session, first inspect artifacts and running processes before doing anything.

| Job | Monitor task ID | Last cursor | State at handoff |
| --- | --- | ---: | --- |
| Main Release matrix | `5c0a524c-0512-43a5-8a42-0d11084e4b23` | 1098 | Running sixth/final run, `10-players-run-3`, port 57172. First five runs all exited zero. |
| Queued follow-ups | `242e12e3-a862-4eb3-8174-e31aeac3b5ee` | 0 | Running PowerShell script, waiting for six entries in main `runs.json`. No follow-up has started yet. |

Example wait:

```javascript
// @exec: {"yield_time_ms": 250000, "max_output_tokens": 1000}
text(await tools.mcp__cmd_monitor__monitor_wait({
  task_id: "5c0a524c-0512-43a5-8a42-0d11084e4b23",
  cursor: 1098, timeout_seconds: 60, until: "completion"
}));
```

Carry forward returned cursors. Keep user updates within about a minute during ongoing work. Avoid blocking waits longer than 60 seconds. If an exec cell yields, resume that cell rather than calling monitor_wait twice for the same pending wait.

Main command:

```text
python tools/run_performance_matrix.py --output docs/measurements/2026-11-12-full/release-matrix
```

Queued command:

```text
powershell -NoProfile -File tools/run_performance_followups.ps1 -WaitForMatrix
```

Main started 22:21 Vienna and should finish about **23:24–23:25**. Follow-ups should finish approximately **23:56**, then allow analysis/verification time. The user asked for duration at 23:12 and was told **45–50 minutes more**. Give updated estimates if conditions change.

## What is being measured

Main: 5 then 10 participants; three independent process runs per scale. Each run lasts 630 seconds plus host grace:

| Nominal process time plus launch offset | Phase | Measured window |
| --- | --- | --- |
| 0–210 s | Idle | 30–210 s |
| 210–420 s | Sustained movement and supported shield ability requests | 240–420 s |
| 420–630 s | Movement/abilities plus equipment transfers (target five requests/s/peer including one stale request/s) | 450–630 s |

All peers are headless ENet on this one machine, explicitly capped with engine `--max-fps 60`, physics60/publication30. Staged Release project, Info logging, development=false. No other benchmark groups or builds run simultaneously. Summary analysis was done only during subsequent run warm-up. First5/10 participants start staggered3–7 seconds; assertions check max participant count, actual movement and accepted abilities. Release configuration and JIT optimizer flags are recorded per peer.

Queued follow-ups, sequential, each in a fresh isolated staged project:

1. `restored`: 5 and10 peers, one210s run each; host capture/normal restart before fresh clients join at10s. Checks restored entity identity.
2. `latejoin`: 5 and10 peers, one210s active run each; last client starts at60s.
3. `moderate`: ten peers210s; UDP proxy seed1042, uniform20–60ms delay each direction,1% datagram loss.
4. `stress`: ten peers210s; seed1042,60–140ms each way,5% loss.
5. `observer-on`: ten peers, two90s active runs.
6. `observer-off`: ten peers, two90s active runs with step hooks omitted and `PERF_COLLECT=0`.
7. `debug-development`: ten peers210s, Debug build and original Debug/development config.

These targeted follow-ups are one-run diagnostics (controls two shorter runs), not three180s repetitions of every cell in the original broad plan. State the actual coverage and duration. Script records exit codes in `followups.json` and continues to other groups if one fails; inspect failures rather than assuming completion means success.

## Files and artifacts

Everything current is under `docs/measurements/2026-11-12-full/`:

- `release-matrix/manifest.json`, `runs.json`, `build.log`, `initial.patch`, exact configuration/definitions and hashes.
- Main staged project: `C:\Users\lober\AppData\Local\Temp\smpgwm-performance-2k6e0ef9\project`. Per-dataset manifests give other staged paths.
- Per run/peer: `steps.csv`, `publications.csv`, `arrivals.csv`, `frames.csv`, `process.csv`, `markers.csv`, `recorder.json`, `workload.json`, `engine.log`, isolated user data.
- `release-matrix/summary.md` and `summary.json`: currently **five completed runs**, not the sixth.
- `cleanup-audit.md`: complete source audit and suggested changes, no new production edits.
- `allocation-audit.md`: source-derived allocation candidates/counts, not profiler-attributed bytes.
- `timing-analysis.md` and `release-matrix/timing-analysis-data.json`: deeper inspection of first two five-player runs.
- `methodology.md`: measurement scope, commands and important caveats.
- `environment.json`, `dotnet-runtimes.txt`: machine details. Windows11 Pro26200, Ryzen7 7700 (8cores/16threads), about64GiB RAM, RTX4060Ti, Balanced power; headless GPU coverage is not claimed.
- `smoke/`: failed harness compile (System.Environment ambiguity, fixed).
- `smoke2/`: successful short uncapped5-player harness smoke; not a main acceptance run.
- `report.md`: completed **provisional** report using firstfive runs. Inspect it and finalize after all results arrive.

Tools added:

- `tools/run_performance_matrix.py`: stages copies, injects recorder hooks/phase markers, builds, verifies Release loading by recorded metadata, launches peers, optionally shapes datagrams. It never modifies normal user data or production source.
- `tools/run_performance_followups.ps1`: queued sequence above.
- `tools/fixtures/PerformanceRecorder.cs.txt`: bounded recorder, no forced GC.
- `tools/fixtures/PerformanceVerification.cs.txt`: normal-startup workload node.
- `tools/summarize_performance.py`: summaries and timestamped overruns/GC bucket correlations.
- `sMPGWM.Tests/PerformanceProcessTests.cs`: explicit opt-in wrapper for the main matrix. Direct Python execution is the reliable reproduction command; an earlier filtered NUnit explicit test was skipped by the adapter.

## Results already established (five main runs)

Host CPU work, milliseconds, MB/s decimal:

| Players/phase | p99 range so far | Maximum | Over16.67ms per180s window | Allocation MB/s |
| --- | ---: | ---: | --- | --- |
| 5 idle | .443–.463 |15.304|0,0,0|.747 |
| 5 active |.448–.461|11.918|0,0,0|.748–.760 |
| 5 burst |.777–.965|25.052|18,17,19|1.828–1.863 |
| 10 idle (two runs) |.718–.748|15.573|0,0|1.366–1.384 |
| 10 active (two runs) |.818–.848|11.441|0,0|1.418–1.423 |
| 10 burst (two runs) |1.464–1.619|29.349|41,39|3.914–3.921 |

Mean host publication rates29.994–30.000/s. This is **not** evidence of jitter-free30Hz. Five-player burst max publication intervals54–58ms; ten-player firsttwo57–62ms. Repeated burst work overruns strongly correlate with Gen1 GC pauses in the enclosing one-second sample bucket. First5 run all18 correlate; ten-player burst aggregate pauses~824–837ms/180s. Do not claim precise event-level causality without trace evidence.

First5 run had302 burst publication intervals>40ms, concentrated late; second had12. Most first-run long intervals were not immediate short-next catch-ups. Every inspected publication advanced exactly2 ticks. First5 client3 had a293ms gap in **accepted** snapshots while frames continued, with a~29ms GC pause and~44ms frame gap insufficient to explain it. Rejected snapshots/transport/scheduling remain possible, unproven. Preserve this unresolved timing finding.

Cold preparation first5 run~381ms; first ship resource instance8.7ms, reused median~.1ms. No justification yet for an additional application resource cache.

## Important measurement caveats

1. `steps.csv` directly brackets the synchronization coordinator, not every engine node or the GPU. Godot process/physics monitor values in `frames.csv` are **cached sampled values**, not per-frame CPU event traces. Actual wall frame intervals are derived from adjacent monotonic timestamps.
2. Publications are host method invocations after serialization, not NIC packet captures. Arrivals are accepted snapshots after validation, not raw datagrams. Proxy counters describe imposed UDP loss/delay, not ENet retransmission counts.
3. Frozen raw `process.csv` headers `rtt_ms`/`jitter_ms` are mislabeled: the underlying values are **seconds**. Summarizer converts*1000 and documents this. They are smoothed application clock estimates.
4. GC pause is cumulative runtime pause time sampled1Hz. Heap size reflects last collection. Correlations have one-second resolution.
5. Recorder allocates about14MiB of bounded storage per process. Working set includes it. Controls retain static arrays but disable recurring observation; they estimate recurring overhead, not a recorder-free startup footprint.
6. Fixture writes workload JSON before recorder final cutoff, so endpoint totals include that output cost. The180s steady windows end before output and exclude it. Control endpoint CPU comparisons need this qualification and run-to-run variance.
7. Phase alignment uses scheduled launch offset plus each process clock, not precision synchronized cross-process clocks.
8. The source project has **no explicit production60FPS cap**; the runner supplies it. Recommend explicit cap. The smoke process loop ran~140Hz uncapped.
9. Follow-ups have not run yet at handoff. Do not present them as passed.

## Cleanup suggestions already audited

Keep recommendations separate from implementation; user requested suggestions:

- Remove pure `ItemRegistry.LoadItemCategoryDefinitions` forwarding and redirect parser tests/caller. Retain item parsing wrapper because it precompiles stats; retain Lazy/reset/cache for failed-preparation retry.
- Make internal-only `GamePlayManager.SetGameState`, `StopSession`, and `GameFlowManager.CompleteLoading` private. Remove unused `OnGameStateChanged` only if not an intended external mod API.
- Remove obsolete FileLoader navigation import. `AbortLoad` itself was removed in the previous turn, not this measurement pass.
- Separate follow-up: replace `ShipController.Loadout` lazy unrelated-store construction with explicit initialization, including safe partial-construction failure cleanup in ShipFactory.
- Retain readiness/cancellation/stale-callback/authority/revision/transaction/rebinding guards and new/restored distinctions.
- Performance priorities: explicit60FPS cap; avoid unchanged topology capture (currently90captures/s before transfer replies); reduce full owner item snapshot allocations (140Godot arrays per128-slot inventory+9equipment snapshot, including empty slots); movement codec buffers later. Source counts are not measured attributed bytes. Do not apply these before baseline analysis.

## Remaining work for the next context

1. Resume monitors above. Main final run then queued follow-ups proceed automatically. Inspect artifacts and errors after each finishes. Do not run builds/heavy analysis during measured windows; wait for completion or keep light analysis inside the next warm-up.
2. Confirm all expected datasets exist, all expected peers recorded success, Release/Debug metadata correct, recorder drop counts zero, checksums consistent within each run, and engine logs do not contain unexpected exceptions. If a follow-up fails, inspect its raw log before retrying only that scenario; keep failure evidence.
3. After all benchmarks stop, regenerate summaries:

   `python tools/summarize_performance.py docs/measurements/2026-11-12-full/release-matrix`

   `python tools/summarize_performance.py docs/measurements/2026-11-12-full`

   Full summary may take tens of seconds; use command-monitor. It also includes smoke2, so distinguish smoke from full measurements in the report.
4. Analyze final host/client distributions, overrun GC correlations, CPU/memory/GC rates, late-join trace around60s, restored prepare-vs-reuse timings, imposed loss/RTT and controlled outcomes, and observer-on/off plus Debug/development overhead. Do not manufacture attribution for unexplained jitter. Compare observer totals cautiously (same duration/workload, endpoint startup/output included, two repeats).
5. Finalize provisional `report.md`, actual coverage/commands, and plan status. User closed other checks; do not leave Steam as a newly blocking requirement. Measurements can be complete while performance acceptance fails due to documented spikes. No claimed removal of those spikes.
6. Run `dotnet test sMPGWM.sln --no-restore` with TRX output under this measurement directory after workloads finish. Earlier prior-turn baseline/cleanup both had302managed passes plus one wrapper reporting227in-engine passes. Current new test is explicit; do not accidentally launch another hour-long matrix through testing. Preserve separate wrapper/engine counts.
7. Recheck new test-support IDs for uniqueness and correct qualified test reference. Previous scan found811markers before the follow-up script additions, no duplicates. Syntax-check Python/PowerShell as needed. Avoid unrelated production changes.
8. Clean only generated `tools/__pycache__` if desired after verifying absolute path inside workspace. Raw samples may be large; optionally compress per-dataset raw CSVs into a verified archive and document extraction before summarizing. Do not delete raw evidence without verified archive contents; keep summaries/manifests/workloads/markers accessible. Staged temp projects intentionally remain for reproduction.
9. Final answer: concise results with a small5/10 comparison, explicit recurring burst/GC issue, report/audit links and prioritized cleanup suggestions. State remote Steam deferred. No approval question needed; user asked for suggestions, not application.

## Working tree and previous work

Before this turn there were modified `FileLoader.cs` (unused AbortLoad removed), modified plan, and untracked `docs/measurements/2026-11-12-final` plus `docs/plans/evidence`. Keep them. Current new harness/doc files are untracked; no commits were made. No production code was modified in this measurement turn.

Agents: `/root/cleanup_audit` completed the provisional report; `/root/recorder` wrote summarizer/timing analysis; `/root/measurement_review` wrote fixture/impairment support/allocation audit. All agents have finished. Their findings are in the files above. No agent should launch competing benchmarks.
