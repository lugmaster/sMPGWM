# Combat performance measurements

## Deferred allocation findings (2026-11-15)

The current allocation pass is limited to its implemented changes and verification. Further findings are recorded here for later work; these are not claims of measured bottlenecks. Keybinding menus, networking decomposition, rewind, separate shield colliders, and VFX capacity tuning remain out of scope.

| Location | Remaining allocation/work | Follow-up |
| --- | --- | --- |
| `CollisionSystem/HitscanQuery.Cast` | Godot creates an `IntersectRay` result dictionary for each cast. Query parameters are reused and results are disposed. | Measure cost per actual shot and maximum shooting-entity workload before replacing collision queries. |
| Network RPC boundary | Engine marshalling, incoming byte arrays, and `AsGodotArray` wrappers remain even when application buffers are reused. | Profile managed and native costs separately; do not describe the game as allocation-free. |
| `NetworkSystem/GameplayDeltaState.Encode` | A warmed resource update measured 336 managed bytes. A control performing its three nested Godot-array assignments also measured 336 bytes; runtime health mutation alone measured zero (1,000 iterations, Godot 4.7.2/.NET 8 x64). | Investigate array-to-Variant interop later. The test enforces the measured budget and application-buffer reuse, not a zero-allocation claim. |
| `NetworkSystem/GameplayDeltaReceiver.Apply` | Configuration/effect changes still assemble and validate full snapshot state; resource-only updates use the fast path. | Consider reusable transactional decoding only if change frequency makes this material. Preserve atomic validation and epoch checks. |
| Topology and inventory publication | Actual joins, ownership/equipment changes, and recovery still construct snapshots. | Measure spikes during late join and equipment changes; unchanged topology now avoids constructing snapshots. |
| `NetworkSystem/HeartbeatMonitor` | Periodic request/response log strings remain, normally on a five-second interval. | Consider logging guards or sampling only if profiling warrants it. |
| HUD text | Displayed numeric changes still require strings; fractional resource changes no longer rebuild identical rounded labels. | Retain accurate bars; measure presentation costs during rendered playtests. |
| `VfxSystem/ShotPresentationController` | Fixed pools can drop visuals/audio at extreme firing rates. This is bounded overflow, not evidence of an allocation leak. | Evaluate representative rendered playtests before capacity changes. |


The opt-in driver copies the game into an isolated temporary project, applies workload and recorder instrumentation there, builds Release, and launches one host plus local ENet clients. It does not modify production definitions. `source.zip` retains copied executable source including instrumentation and untracked changes; `manifest.json` records source hashes, revision, arguments and placement. Raw measurements are under the ignored `docs/measurements/` directory.

Run comparable moving-fire measurements before and after a change:

```powershell
python tools/run_performance_matrix.py --output docs/measurements/fire-before --players 5 10 --repeats 3 --seconds 210 --scenario firing --weapons 9 --weapon-rate 60
python tools/summarize_performance.py docs/measurements/fire-before
```

Use a new output directory for each run. The driver refuses to overwrite existing evidence. The default engine can be overridden with `GODOT_BIN`.

Run shield-impact pressure:

```powershell
python tools/run_performance_matrix.py --output docs/measurements/combat-after --players 5 10 --repeats 3 --seconds 210 --scenario combat --weapons 9 --weapon-rate 60
python tools/summarize_performance.py docs/measurements/combat-after
```

`PerformanceProcessTests.CombatPerformanceMatrix` exposes this second workload as an explicit test. Combat keeps shooters stationary, creates one hostile uncontrolled ship 70 units ahead of each shooter, and raises shield capacity to 1,000,000 in the isolated definitions. Each peer must observe shield hits for the run to pass. Nine SMGs retain normal ammunition and reload behavior, so 60 is the configured shot cadence during firing, not the sustained shot rate. The summary reports actual accepted and presented shot counts.

For network impairment, add `--impairment moderate` (20-60 ms delay per direction, 1% datagram loss) or `--impairment stress` (60-140 ms, 5% loss). The seeded proxy records its own packet/byte counters. Use the existing `matrix` scenario separately for movement, abilities, and equipment bursts; `--latejoin` adds a peer at 60 seconds.

The recorder captures coordinator step time/allocation, publication and accepted-arrival intervals, 1 Hz process allocation/GC/CPU/memory samples, and lifecycle markers. Percentiles use nearest rank. Steady summaries exclude the first 30 seconds. Whole-run combat counters include startup/warmup and expose queue, staging, pending, deduplication, VFX pool and audio limits.

## Interpretation limits

- All processes share one machine. Headless runs do not measure rendered FPS, GPU work, texture upload, audible quality, or Steam transport behavior.
- Coordinator step duration includes simulation and publication work, not every engine physics operation. Cached engine monitor samples are also retained, but are not independent per-frame CPU observations.
- GC pause is aggregate runtime pause time; this is not a distribution of individual pause events.
- Shot bytes count application event payload after fan-out. Movement payload sizes are separate. These are not total network bandwidth; reliable gameplay traffic, ENet framing, and retransmissions are omitted unless measured externally. Proxy counters cover all proxied UDP traffic but include setup and teardown.
- Short 75-second runs retain 45 seconds after warmup and are smoke evidence, not the three-repeat 180-second steady acceptance matrix.
- VFX/audio drops are deliberate bounded overflow behavior. Report them separately from gameplay or network command loss; a passing workload does not mean every effect was displayed or every sound played.

## Recorded runs

Results for the architecture refactor are in `measurements/2026-11-15-combat/`. The original `before` snapshot was copied from a clean working tree before implementation edits; its `initial.patch` is empty. 

### Moving-fire smoke comparison, 2026-11-15

One 75-second run per scale and source snapshot; table covers seconds 30-75. Both datasets completed with every peer successful. Host publication mean was 30.000 Hz in all four runs. These shared-machine runs overlapped development activity, so allocation direction is useful evidence but timing is not an isolated causal benchmark.

| Players | Source | Host allocation MB/s | Clients allocation MB/s (sum) | Host step p95 / p99 ms | Host GC 0/1/2 | Aggregate GC pause ms | Publication p99 / max ms |
| ---: | --- | ---: | ---: | --- | --- | ---: | --- |
| 5 | before | 1.098 | 2.158 | 0.632 / 0.772 | 2/2/0 | 28.957 | 34.050 / 42.609 |
| 5 | after | 0.942 | 1.537 | 0.798 / 1.013 | 4/3/1 | 60.677 | 34.243 / 46.257 |
| 10 | before | 2.268 | 7.879 | 1.496 / 1.885 | 6/6/0 | 94.771 | 34.076 / 50.374 |
| 10 | after | 1.936 | 5.590 | 1.812 / 2.198 | 5/5/0 | 101.957 | 34.248 / 57.555 |

Observed host allocation fell about 14-15%, and aggregate client allocation about 29%. Host step and publication tails did not improve in these runs. The game is still allocating during gameplay; this result does not establish allocation-free execution or a guaranteed frame budget. Repeat the full matrix on an otherwise idle machine before treating timing differences as regressions or improvements.

The `after` and `shields` directories each identify their own immutable instrumented source snapshot in `source.zip` and `manifest.json`. Further epoch anchoring, monotonic validation, eager publisher setup and entity-map cleanup were made in the working tree after the moving-fire snapshot. Treat these results as measurements of the archived snapshots, not certification of every later working-tree edit.

### Shield-impact pressure

Both scales passed on every peer. Every host-accepted shot hit a shield. There were no host shot staging drops. These runs add one hostile target entity per player, so their cost is not directly comparable to the moving-fire workload.

| Players | Accepted shield hits (whole run) | Host allocation MB/s (steady) | Host step p95 / p99 ms | Publication p99 / max ms | Shot event MB sent (whole run, peer fan-out) |
| ---: | ---: | ---: | --- | --- | ---: |
| 5 | 76,815 | 1.615 | 1.038 / 1.302 | 34.071 / 67.129 | 20.252 |
| 10 | 149,184 | 3.292 | 2.506 / 2.907 | 34.426 / 61.290 | 88.157 |

Host presentation drops, whole run:

| Players | Muzzle | Tracer | Impact | Shield | Audio | VFX queue |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 5 | 0 | 20 | 4,660 | 1,516 | 71,004 | 0 |
| 10 | 11,352 | 36,366 | 57,571 | 50,764 | 142,977 | 0 |

The existing visual/audio capacities intentionally discard effects at this modified maximum-rate workload. Capacity tuning remains deferred; the measured drops must not be described as full visual coverage. Pooling keeps memory bounded, but 30 Hz mean publication does not imply every interval meets 33.33 ms: the ten-player combat run reached a 61.29 ms host publication interval and 266.72 ms maximum accepted client arrival interval.

Archived source provenance:

| Dataset | Snapshot started UTC | Source archive SHA-256 |
| --- | --- | --- |
| before | 2026-11-15T13:19:42Z | `7fa48a053fd26dce0eea9119fed6f15af2a2ea12494cc478d82f8c0216df7b14` |
| after | 2026-11-15T13:27:36Z | `db1bfce81b30565a46543f30182c89b91ab6e7191abc3436ab21c495589050a1` |
| shields | 2026-11-15T13:30:30Z | `101632825f03523675c3288ad9aaa2e3cb456455cdfc5fce79a3e3a5b25258b5` |

## Earlier architecture-refactor verification

The final working tree passed 631 non-engine tests plus the integration wrapper executing 376 Godot tests. Movement codec coverage includes zero managed allocation across 1,000 warmed encode/decode cycles; this is a codec guarantee, not a whole-game guarantee.

`python tools/run_shield_slice.py --players 10 --impaired --variants` also passed: all ten processes exited successfully and 6,616 observations across 2,526 entity revisions converged. The seeded transport applied 20-60 ms delay in each direction, 1% loss and reordering, with the final client joining at 12 seconds. This checks configuration/effect/resource replication and joining against the final source, separately from the earlier performance snapshots.

That archived verification used network protocol 14; older builds were intentionally incompatible. Resource shields still absorb damage at hull contact because this branch has no authored independent shield collider. The development loadout's heavy hitscan weapon now executes its definition after removal of name-based weapon admission.

## Allocation and initialization pass, 2026-11-15

This is a separate comparison from the earlier architecture-refactor snapshots above. The baseline is clean commit `dc434b45691e0fc312c141fa7c3d399d3450f236`, staged in a detached worktree before implementation. After snapshots contain the allocation, initialization, and numeric-reference changes; wire protocol is 15 and handshake version is 3. All 16 runs passed, covering 120 peer executions.

Each scenario ran once at 5 and 10 players for 75 seconds, with statistics covering seconds 30-75. Workloads ran serially on one machine using Release builds and headless ENet. Other development/test activity overlapped parts of the baseline. These short samples support an allocation comparison, not an isolated timing experiment or the full three-repeat acceptance matrix. Rendered FPS, GPU work, and Steam transport were not measured.

`active` exercises movement and repeated ability activation. `firing` adds nine weapons per player at a configured 60-shot/second cadence; ammunition/reloads still apply. `combat` keeps shooters stationary, adds one hostile shield target per player, and raises target shield capacity in the isolated definitions.

| Workload | Players | Host allocation MB/s, before -> after | Reduction | Clients aggregate MB/s, before -> after | Host step p99 ms, before -> after |
| --- | ---: | --- | ---: | --- | --- |
| idle | 5 | 0.697 -> 0.179 | 74.4% | 2.050 -> 2.020 | 0.675 -> 0.495 |
| idle | 10 | 1.257 -> 0.297 | 76.4% | 5.507 -> 5.447 | 1.093 -> 0.821 |
| active | 5 | 0.718 -> 0.188 | 73.8% | 2.167 -> 2.149 | 0.683 -> 0.491 |
| active | 10 | 1.304 -> 0.317 | 75.7% | 7.540 -> 7.262 | 1.052 -> 0.838 |
| firing | 5 | 0.944 -> 0.354 | 62.5% | 1.561 -> 1.530 | 1.050 -> 0.890 |
| firing | 10 | 1.943 -> 0.722 | 62.8% | 5.608 -> 5.492 | 2.000 -> 1.959 |
| combat | 5 | 1.619 -> 1.051 | 35.1% | 2.476 -> 2.423 | 1.411 -> 1.153 |
| combat | 10 | 3.288 -> 1.784 | 45.7% | 8.277 -> 8.176 | 2.980 -> 2.515 |

Host allocation decreased across all workloads; client allocation decreased only modestly. The game still allocates through engine queries, RPC/Variant interop, and other recorded residuals. The warmed gameplay resource-row control measured 336 managed bytes/update from its three nested array assignments; this pass does not claim zero-allocation replication.

All after runs averaged 30.000 Hz host movement publication, but worst intervals did not improve consistently: ten-player firing increased from 53.847 to 81.992 ms, and five-player combat increased from 68.936 to 116.283 ms. Lower allocation and improved step p99 do not establish a guaranteed 60 FPS frame budget or a 33.33 ms maximum publication interval.

Combat accepted 76,824 / 149,724 shield hits at 5 / 10 players with zero host shot-staging drops. Existing visual/audio overflow remains deliberately unchanged; these whole-run host counters include warmup:

| Players | Muzzle drops | Tracer drops | Impact drops | Shield drops | Audio drops | VFX queue drops |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 5 | 0 | 24 | 5872 | 1774 | 70950 | 0 |
| 10 | 6912 | 16447 | 52447 | 42629 | 143607 | 0 |

Evidence is retained under `docs/measurements/2026-11-15-allocation-pass/`: combined `summary.md`/`summary.json`, per-run raw CSVs, and per-scenario `manifest.json`, `initial.patch`, and immutable instrumented `source.zip`. SHA-256 values below refer to each scenario's `source.zip`:

| Dataset directory | Snapshot UTC | Source archive SHA-256 |
| --- | --- | --- |
| `after-active` | 2026-11-15T14:31:42Z | `3c12bd019678678b7d147352f7f1173362196c29473dae475dd287de09380003` |
| `after-combat` | 2026-11-15T14:37:27Z | `bf985f1efba38075ef4d2f32ed446bdd438eb3908b0c57dd9e8027dd2d982ac6` |
| `after-firing` | 2026-11-15T14:34:35Z | `16f03c76b063a247cb28871bd91ef18ea9def620e0195ea0b6ad1f8ab9f1db8f` |
| `after-idle` | 2026-11-15T14:28:45Z | `1fd7187761a3ae7dba25e611f13f0bc0aaabadc50763967480175ea21c75130b` |
| `before-active` | 2026-11-15T14:20:05Z | `3304362f164f7574dc2205185262dcaa9216542a73a2afc7898d6e515b93498e` |
| `before-combat` | 2026-11-15T14:25:52Z | `bf5b0ee003422ad8197c71894390d08e26fae0f8a3a33868701912e060c9f521` |
| `before-firing` | 2026-11-15T14:22:59Z | `32272ce2c43b7cbbc55d5c145b4a614cdcb3ac07c60bc19a36ffb6a17045483f` |
| `before-idle` | 2026-11-15T14:17:10Z | `0244cec57de523d11156caef674f65c8ee19bc0128bcd727dbfe916e41b74953` |

Final correctness verification passed 633 outer tests, including the integration wrapper executing 393/393 Godot tests. The separate `python tools/run_shield_slice.py --players 10 --impaired --variants` run also passed all ten processes: 6,641 observations across 2,146 entity revisions converged. It applied seeded 20-60 ms delay in each direction, 1% loss, and delay-induced reordering, with the last client joining at 12 seconds. It exercises abilities, equipment/components, ownership changes, and authoritative resource convergence; it does not measure rendered combat. Evidence and raw logs are retained in `measurements/2026-11-15-allocation-pass/impaired-latejoin.txt` and `impaired-latejoin-logs/`.
