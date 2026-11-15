# Combat performance measurements

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

## Final working-tree verification

The final working tree passed 631 non-engine tests plus the integration wrapper executing 376 Godot tests. Movement codec coverage includes zero managed allocation across 1,000 warmed encode/decode cycles; this is a codec guarantee, not a whole-game guarantee.

`python tools/run_shield_slice.py --players 10 --impaired --variants` also passed: all ten processes exited successfully and 6,616 observations across 2,526 entity revisions converged. The seeded transport applied 20-60 ms delay in each direction, 1% loss and reordering, with the final client joining at 12 seconds. This checks configuration/effect/resource replication and joining against the final source, separately from the earlier performance snapshots.

The network protocol is now 14; older builds are intentionally incompatible. Resource shields still absorb damage at hull contact because this branch has no authored independent shield collider. The development loadout's heavy hitscan weapon now executes its definition after removal of name-based weapon admission.
