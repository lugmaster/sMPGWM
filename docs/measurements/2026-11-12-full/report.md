## Paused at user request — 2026-11-12

Bulk evidence has been archived; reports and measurement metadata remain readable. See [archive and extraction instructions](../README.md) for the verified local ZIP and access to raw samples/logs. Measurement conclusions are unchanged.

All benchmark processes were stopped and process inspection confirmed only the user's Godot editor remains. Do not resume automatically.

- Main Release matrix: all six runs completed, all 45 peer exit codes zero. Existing summary/report still cover only the first five runs; final aggregation and validation remain open. Preserve these runs; do not repeat them merely to resume analysis.
- Follow-ups: stopped during the first five-player restored scenario. Treat its artifacts as partial unless independently validated. Ten-player restoration, late join at both scales, moderate/stress impairment, observer on/off and Debug/development measurements remain deferred.
- The original waiting queue had a Windows PowerShell array-count bug. Fixed its JSON array expression before replacing that waiting process; no main benchmark was restarted or duplicated.
- Remaining analysis: aggregate the sixth main run, validate all peer artifacts/configuration/checksums/drop counts/logs, analyze the completed follow-ups when available, and finalize the report. The post-measurement test run and harness traceability/syntax checks were not performed on this resume.
- Performance acceptance remains open: repeated equipment-burst GC-associated overruns and unexplained publication/accepted-snapshot timing gaps need attribution and later measured fixes.
- The user subsequently authorized the audited cleanup. Implementation status and regression results are recorded in the cleanup audit; performance comparisons remain deferred. Archived measurements remain the pre-cleanup baseline.
- Remote Steam remains deferred. Other verification work remains closed for this pass; do not reopen it.

# Local multiplayer performance and proposed cleanup

**Provisional: five of six main Release repetitions summarized.** All six main runs completed; three five-player repetitions and two ten-player repetitions are summarized below. Follow-up diagnostics are paused. Update this report from the final summaries before treating the results as complete. These are pre-cleanup measurements; the subsequent authorized implementation is recorded in the [cleanup audit](cleanup-audit.md).

The measured implementation has substantial typical CPU headroom, but equipment bursts repeatedly produce long GC-associated work steps and uneven publication timing. Average publication frequency remains approximately 30 Hz; that does not establish smooth cadence or close the tail-latency issue. No production performance optimization or proposed cleanup has been applied in this measurement pass.

The user closed the other correctness/visual verification work for this pass and deferred another remote Steam test until after upcoming features. That decision is recorded as scope, not as successful Steam or rendered verification. This report covers the local workloads actually measured; it does not claim completion of every cell in the original plan.

## Environment and method

Revision `a57352656b8ca7179bc81730772b6d477ea64022`, with the tracked dirty patch retained per dataset. Windows 11 Pro build 26200; AMD Ryzen 7 7700, eight cores/sixteen logical processors; approximately 64 GiB RAM; NVIDIA GeForce RTX 4060 Ti plus integrated Radeon graphics; Balanced power plan. Godot 4.7.2 Mono and .NET SDK 10.0.100. Exact machine data is in [environment.json](environment.json), and installed runtimes are in [dotnet-runtimes.txt](dotnet-runtimes.txt).

All host/client processes run headless on this one machine using ENet loopback. They compete for the same CPU, memory and OS scheduler. This measures local simulation, serialization, process scheduling and transport behavior; it supplies no GPU/rendered FPS or remote-network result. The runner explicitly passes `--max-fps 60`; the production project currently does not set a 60 FPS cap. Physics is 60 Hz and host publication occurs every second completed step.

The main run uses Release assemblies, Info logging and `development=false` in an isolated staged project with separate user-data directories and allocated ports. Per-peer output confirms `assemblyConfiguration=Release` and JIT optimization enabled. Exact staged configuration, definitions, hashes, patch, commands and loaded compatibility fingerprints are retained in the dataset. The instrumentation and workload fixture are staged into the copy; production runtime files in the working tree are not instrumented.

At both five and ten participants, three independent launches each run idle, movement/supported shield ability use, and equipment-burst phases. Every phase lasts 210 seconds: 30 seconds of warm-up followed by 180 seconds measured. Equipment bursts request five transfers/second/participant, with stale-revision attempts included. Ability cooldown, energy, authority and revision checks stay enabled. Actual distance travelled, accepted abilities, inventory revision changes and observed peer counts are recorded; success does not rely only on a process exit code.

The [methodology](methodology.md) defines every recorder, clock and measurement limit. Reproduce the main matrix with:

```text
python tools/run_performance_matrix.py --output docs/measurements/2026-11-12-full/release-matrix
```

## Main Release results so far

Values below are ranges across completed repetitions, not pooled percentiles. Step work is measured directly around the synchronization coordinator; nominal work budget is 16.67 ms. Allocation rates and cumulative GC pauses use the one-second process samples within each measured phase.

| Scale / phase | Host step p99 | Host step maximum | Steps above 16.67 ms per run | Host allocation | Host GC pause per 180-second phase |
| --- | ---: | ---: | ---: | ---: | ---: |
| Five, idle | 0.443–0.463 ms | 11.905–15.304 ms | 0 | 0.747 MB/s | 69.432–75.800 ms |
| Five, movement/ability | 0.448–0.461 ms | 8.986–11.918 ms | 0 | 0.748–0.760 MB/s | 60.275–73.761 ms |
| Five, equipment bursts | 0.777–0.965 ms | 24.010–25.052 ms | 17–19 | 1.828–1.863 MB/s | 363.549–390.703 ms |
| Ten, idle | 0.718–0.748 ms | 13.721–15.573 ms | 0 | 1.366–1.384 MB/s | 117.428–126.600 ms |
| Ten, movement/ability | 0.818–0.848 ms | 10.858–11.441 ms | 0 | 1.418–1.423 MB/s | 119.505–132.731 ms |
| Ten, equipment bursts | 1.464–1.619 ms | 26.909–29.349 ms | 39–41 | 3.914–3.921 MB/s | 823.590–837.102 ms |

All 35 peers in the five completed repetitions reported success. Observed loading-to-active times range from 0.502 to 0.851 seconds. These are same-process loading markers, not process-launch-to-gameplay or first-rendered-frame timings. Each run includes accepted supported ability requests and equipment revision changes.

The host sustained 29.994–30.000 publication opportunities/second in the reported windows, with all inspected publication tick increments remaining two. However, burst publication maxima reached 54.205–61.964 ms, and the first five-player burst had 302 intervals above 40 ms. Client burst step maxima reached 29.020–34.053 ms. Accepted client snapshot gaps reached 293.057 ms in one five-player run. Accepted snapshots are measured after application validation: this is not evidence of a 293 ms network delay or a particular packet-loss rate.

Detailed host/client distributions, publication/non-publication work, over-budget counts and time, startup phases, allocation/GC/memory/CPU samples, and workload outcomes are in [summary.md](release-matrix/summary.md) and [summary.json](release-matrix/summary.json). Raw CSVs and engine logs remain in each run/peer directory. Percentiles use nearest rank. Client summaries pool observations within a run/phase and sum client process rates; they should not be compared with host rates as though they describe one process.

## What explains the tail

The [timing analysis](timing-analysis.md) examines the first two five-player repetitions. All 18 host work overruns in the first burst fall into one-second buckets with a Gen1 collection; the pause increments closely match the measured long steps. This strongly associates those work overruns with GC. It does not attribute all irregular publication intervals to GC: most of the additional late-burst publication delays occur on sub-millisecond coordinator steps. The existing data cannot identify a specific OS scheduling or engine task responsible for them.

The 293 ms accepted-snapshot gap also exceeds nearby client frame and GC delays. Equipment changes can invalidate movement configurations, and the recorder observes accepted messages rather than rejected snapshots or datagrams. Its exact cause remains unproven. Further attribution requires a scheduler/engine trace and snapshot rejection/recovery markers or a transport capture; removing initialization guards would not address the evidence.

Godot's process/physics monitors are cached sampled values, not independent frame CPU traces. Actual frame intervals are computed from monotonic timestamps; direct coordinator timing is reported separately. GC pause values are cumulative, not individual pause events. These distinctions prevent averages and cached monitor readings from concealing the unresolved tails.

## Follow-up measurements

These diagnostic runs are queued after the six main repetitions and are not included in the provisional conclusions above:

| Workload | Planned execution for this pass | Interpretation |
| --- | --- | --- |
| Restored host plus fresh clients | Five and ten participants, one run each | Measures normal restored-host startup/continuation; does not represent reconnecting saved remote owners. |
| Late join while active | Five and ten participants, one run each | Measures the existing host/clients while the final peer joins. |
| Moderate and stress impairment | Ten participants, one run/profile | Seeded UDP proxy; exact parameters and counters retained. Host scheduling and accepted application arrival remain separate. |
| Observer comparison | Recorder enabled/disabled, two 90-second repetitions each | Recurring collector overhead estimate. Both retain static recorder storage and the same workload driver; this is not a recorder-free startup footprint. |
| Debug/development comparison | One 210-second run | Diagnostic/fixture overhead comparison against matching Release behavior. |

These are deliberately narrower one-run diagnostics, not the original plan's full three-repeat matrix for every restoration/impairment/failure scenario. The observer comparison is shorter than the main steady windows. Record those deviations with the results; do not extrapolate them to unmeasured future abilities, enemies, remote Steam sessions or rendered GPU cost.

## Suggested next changes

The [allocation audit](allocation-audit.md) identifies concrete source candidates; it is not an allocation-profiler call tree and does not assign measured pauses to specific allocations. Apply performance changes separately and compare the same workloads before claiming improvement.

1. **Make the production frame cap explicit.** Set the intended 60 FPS limit in production configuration rather than depending on a benchmark launch flag or monitor-dependent VSync. Retain the 60 Hz physics guard.
2. **Avoid capturing unchanged topology.** The host currently captures topology 90 times/second before deciding whether identity changed. Track membership/control/entity changes and skip unchanged captures, while preserving fresh transforms and full snapshots for joins/recovery. This is the first allocation reduction to measure.
3. **Reduce owner-item snapshot allocations during bursts.** A full 128-slot inventory plus equipment snapshot creates arrays even for empty slots. Profile encoding/Variant costs, then consider a sparse or bounded binary format with the same identities, capacities, revisions, validation and snapshot-before-ack recovery ordering. This is a protocol change, not a guard deletion.
4. **Apply the small registry/API cleanup.** Remove `ItemRegistry.LoadItemCategoryDefinitions`' pure forwarding wrapper; retain item stat precompilation and cache reset/retry. Narrow the internally used lifecycle mutators and remove the unsubscribed game-state event if it is not an external mod API. The [cleanup audit](cleanup-audit.md) records callers, invariants and affected regressions, plus a separate explicit-loadout-construction proposal.

Movement codec buffers/history and conditional full-world gameplay snapshots are secondary candidates after topology and owner-state work. Keep indexed transactions, bounded queues, cached join membership, stale-session/revision validation and runtime rebinding. No cleanup recommendation has been applied in this pass, and the repeated burst tail issue is not claimed resolved.
