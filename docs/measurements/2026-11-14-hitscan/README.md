# Hitscan combat process measurements

2026-11-14. Godot 4.7.2 Mono, Release build; all peers headless on one Windows machine. Each successful run lasts about 30 seconds, with one repeat per population. These are short regression and capacity checks, not sustained performance certification.

| Workload | Players | Pass | Host shots | Physics p50/p95/p99 ms | Publication p95/p99 ms | Host step allocation KiB/s | Host GC pause ms | Host outbound KiB/s | Pool high-water / drops |
| --- | ---: | :---: | ---: | --- | --- | ---: | ---: | ---: | --- |
| smg15-nine | 5 | False | 0 | 0.151/0.372/0.510 | 33.61/34.91 | 681.2 | 13.78 | 77.6 | 0 / 0 |
| smg15-nine-powered | 5 | True | 12285 | 0.315/0.639/0.821 | 33.70/34.05 | 1105.6 | 46.37 | 191.8 | 171 / 0 |
| smg15-nine-powered | 10 | True | 23004 | 0.521/1.374/1.617 | 33.98/34.82 | 2301.2 | 96.19 | 769.2 | 297 / 8 |
| smg15-one-powered | 5 | True | 1377 | 0.219/0.531/0.719 | 33.67/34.19 | 958.1 | 15.18 | 91.3 | 19 / 0 |
| smg15-one-powered | 10 | True | 2557 | 0.439/0.946/1.246 | 33.80/34.14 | 1736.4 | 50.28 | 358.6 | 33 / 0 |
| smg60-nine-powered | 5 | True | 27243 | 0.388/0.727/0.916 | 33.74/33.98 | 1369.7 | 77.71 | 319.0 | 362 / 283 |
| smg60-nine-powered | 10 | True | 49572 | 0.802/2.169/3.738 | 34.27/45.48 | 2949.9 | 139.83 | 1217.0 | 384 / 27165 |

Physics/publication percentiles use host recorder timestamps from seconds 10 through 29, after staggered joins. Physics work is the synchronization coordinator, including combat and publication; it is not total engine-frame time. Allocation is that callback's thread allocation. GC pause is cumulative process-pause change between the first and last one-second samples in the window. Outbound traffic is actual UDP datagram payload observed at the proxy over the entire run, including loading, joins, retransmission and teardown; it is not just combat payload. All peers' raw CSVs are retained in the evidence archive.

## Workloads and limits

- Both fire-group actions are held through normal player input after a released-trigger interval. Slot 2 explicitly belongs to group 2 in the nine-weapon setup. The one-weapon setup exercises group 1 while both trigger inputs remain held.
- The isolated benchmark equips one or nine Vulcans per player and increases reactor generation from 24 to 128 so the nine-weapon demand of 27 can be supplied. Initial battery charge remains zero. Production defaults are unchanged.
- SMG magazines remain 60 rounds and reloads remain 90 ticks. The synthetic 60/s definition exercises one shot per weapon per physics tick during bursts; reloads lower its sustained average.
- ENet traffic passes through a seeded localhost UDP proxy: uniform 20?60 ms delay in each direction, 1% independent packet loss and delay-induced reordering. No Steam multiplayer transport measurement was performed. Steam initialization in the normal startup smoke does not establish Steam transport coverage.
- This cooperative workload produces mostly misses and ally exclusions. It checks actual shot admission, input delivery, resource/weapon-state traffic, reload cycles and presentation pools. It does not establish dense hostile-hit/death performance, mixed-supply convergence, exact synchronized 90-shot bursts, recovery, restored-session behavior, long-duration stability, or reliable queue backlog bounds.
- The initial `smg15-nine` run intentionally failed the shot/movement checks because the packaged reactor could not supply nine equipped weapons. Its evidence is retained as a power-starvation case, not a passing combat benchmark or a comparable performance baseline.
- Some runs report ENet channel-0 errors during staggered client teardown and resources still in use when the harness quits immediately after requesting scene cleanup. Exact per-peer error counts are retained in summary.json and raw logs. Exit-zero and shot-count checks do not assert error-free teardown.
- No before-change combat baseline exists. The one-weapon versus nine-weapon rows compare workload scaling on the changed implementation; older noncombat measurements are not substituted for an equivalent baseline.

## Reproduction

`python tools/run_performance_matrix.py --output <directory> --players 5 10 --repeats 1 --seconds 30 --scenario combat --combat-weapons 9 --combat-rate 15 --impairment moderate`

Use `--combat-weapons 1` for one weapon or `--combat-rate 60` for the declared rate ceiling. `tools/summarize_hitscan.py` regenerates summary.json and this table from unarchived evidence. Each manifest contains source hashes, staged data/configuration, engine path and arguments; each run retains workloads, process/physics/publication CSVs, logs and proxy counters. Raw artifacts are archived without deleting their content.
