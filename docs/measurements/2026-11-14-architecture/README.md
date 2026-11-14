# Architecture follow-up measurements

2026-11-14. Godot 4.7.2 Mono, Release, headless ENet peers on one Windows machine. One short run per population/rate; this is regression evidence, not sustained or Steam/GPU certification.

## Results

| Rate / players | All peers passed | Host step p95 / p99 ms | Publication p99 ms | Host callback KiB/s, before to after | GC pause ms | Pool high-water / drops |
| --- | --- | --- | --- | --- | --- | --- |
| 15/s / 5 | True | 0.454 / 0.678 | 33.95 | 1103.2 to 630.8 | 15.99 | 216 / 0 |
| 15/s / 10 | True | 0.961 / 2.775 | 34.84 | 2279.4 to 864.9 | 0.00 | 343 / 129 |
| 60/s / 5 | True | 0.595 / 0.820 | 34.07 | 1364.7 to 741.4 | 16.45 | 384 / 7566 |
| 60/s / 10 | True | 1.680 / 2.326 | 34.25 | 2946.7 to 1204.8 | 42.84 | 384 / 35749 |

## Interpretation and scope

- Measurements use seconds 15-29 and nearest-rank percentiles. Before values are recalculated over that same window from the archived hitscan runs. Allocation covers the coordinator callback thread, not the entire engine; GC pause is the cumulative process counter change in that window.
- Both runs equip nine Vulcans per player and stage reactor generation at 128. Current capacity runs explicitly stage friendly fire off. Real game configuration is unchanged. ENet uses 20-60 ms delay each direction, 1% loss, and delay-induced reordering.
- Current runs release both triggers at global seconds 12-14 and press again before the measurement window. Earlier runs did not have that second release; reload phases and total shot counts therefore differ. The observed allocation reduction is encouraging but not a controlled attribution to any single optimization.
- Normal ten-player p99 callback time increased from 1.542 to 2.775 ms despite lower allocation and p95 time. Treat these short samples as capacity/regression checks; repeat longer runs before declaring a tail-latency improvement.
- Cosmetics remain bounded and can drop. The high-rate ten-player run recorded 35,749 drops; default per-profile budgets have not been inflated. CPU cadence passing does not certify visual quality at that ceiling.
- Some host logs still contain ENet channel-0 errors during staggered departure. Exact counts are in summary.json. Final runs had zero process exit codes and no native shutdown crash, but are not error-free teardown certification.
- Steam transport, sustained hostile shield hits/deaths, rendered multiplayer GPU cost, shader first-use warmup, long-duration memory behavior, and reliable backlog bounds remain open.

## Startup input finding and excluded evidence

The first capacity attempts inherited friendly_fire=true, which invalidated comparison with the older cooperative workload. A later friendly-fire-disabled run still showed a held-input startup edge. A bounded host capture proved affected ships were alive and powered with matching weapon revisions and held fire input, but their weapon controllers were unarmed. The authoritative controller had not accepted the required released-trigger state; continuous holding cannot rearm it. Client pointer filtering and sampled input were valid.

Production release/arming safety was not loosened. The capacity fixture now explicitly releases and re-presses after warmup. This establishes steady combat capacity, not initial held-input readiness under impairment. That startup behavior remains a separate follow-up.

smg15-nine-powered.zip retains the initial failed run including a native shutdown/finalizer crash. smg15-nine-retry.zip retains its retry. input-investigation.zip retains later failed and debugger runs; debugger-paused runs are excluded from timing conclusions. Motion collision wrappers now release their managed native reference after use, and the fixture waits two process frames after stopping the session before exiting. Subsequent verification did not reproduce the shutdown crash; this does not isolate its precise native cause.

## Validation

- Render-enabled .NET suite: 424 top-level tests passed, including the wrapper running 304 Godot cases.
- Added coverage for prepared asset sharing/lifetime, VFX session lifecycle, topology comparison, repeated collision contacts, hitscan batch reuse, and weapon delta atomicity/configuration/recovery. Delta application allocation is tested directly.
- Gameplay-data validation: 136 definitions across 12 catalogues; 22 invalid-content checks rejected.
- Existing UiIcon.Size hiding and NUnit2009 warnings remain.

## Reproduction

```powershell
python -B tools/run_performance_matrix.py --output docs/measurements/2026-11-14-architecture/smg15-nine-warmup --players 5 10 --repeats 1 --seconds 30 --scenario combat --combat-weapons 9 --combat-rate 15 --impairment moderate
python -B tools/run_performance_matrix.py --output docs/measurements/2026-11-14-architecture/smg60-nine-warmup --players 5 10 --repeats 1 --seconds 30 --scenario combat --combat-weapons 9 --combat-rate 60 --impairment moderate
python -B tools/summarize_architecture.py
```

Extract the two warmup archives into this directory before regenerating the summary if only tracked archives are present. Manifests retain staged source hashes, configuration, workload arguments, and engine paths. Raw CSVs, logs and workload reports remain in the archives; extracted working directories are ignored.
