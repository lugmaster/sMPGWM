# Local performance measurements

Nearest-rank percentiles; milliseconds unless stated. Each steady phase excludes its first 30 seconds. Raw CSVs remain adjacent to each peer.

Host results:

| Dataset / run | Phase | Step p50 / p95 / p99 / max | Steps >16.67 ms | Publication p99 / max | Publications/s | Intervals >40 / >50 ms | Allocation MB/s | GC 0/1/2; pause ms | CPU % of one core |
| --- | --- | --- | ---: | --- | ---: | --- | ---: | --- | ---: |
| 10-players-run-1 | idle | 0.200 / 0.566 / 0.748 / 15.573 | 0 | 33.808 / 48.291 | 30.000 | 4 / 0 | 1.384 | 16/13/1; 126.600 | 8.497 |
| 10-players-run-1 | active | 0.213 / 0.597 / 0.818 / 10.858 | 0 | 33.897 / 50.054 | 30.000 | 7 / 1 | 1.418 | 15/15/0; 119.505 | 8.625 |
| 10-players-run-1 | burst | 0.562 / 1.161 / 1.619 / 29.349 | 41 | 34.578 / 61.964 | 30.000 | 33 / 29 | 3.921 | 41/41/0; 837.102 | 12.916 |
| 10-players-run-2 | idle | 0.211 / 0.566 / 0.718 / 13.721 | 0 | 33.759 / 46.351 | 30.000 | 9 / 0 | 1.366 | 15/15/0; 117.428 | 8.401 |
| 10-players-run-2 | active | 0.246 / 0.686 / 0.848 / 11.441 | 0 | 33.900 / 49.980 | 30.000 | 10 / 0 | 1.423 | 15/15/0; 132.731 | 9.270 |
| 10-players-run-2 | burst | 0.632 / 1.109 / 1.464 / 26.909 | 39 | 34.388 / 57.136 | 30.000 | 25 / 20 | 3.914 | 42/42/0; 823.590 | 12.911 |
| 5-players-run-1 | idle | 0.136 / 0.316 / 0.463 / 14.807 | 0 | 33.672 / 47.803 | 30.000 | 3 / 0 | 0.747 | 8/8/0; 72.713 | 7.648 |
| 5-players-run-1 | active | 0.147 / 0.329 / 0.456 / 11.918 | 0 | 33.700 / 50.609 | 30.000 | 7 / 1 | 0.760 | 8/8/0; 73.761 | 7.877 |
| 5-players-run-1 | burst | 0.326 / 0.652 / 0.890 / 24.848 | 18 | 46.821 / 54.205 | 30.000 | 302 / 8 | 1.863 | 20/20/0; 383.492 | 8.274 |
| 5-players-run-2 | idle | 0.135 / 0.320 / 0.448 / 11.905 | 0 | 33.733 / 47.089 | 30.000 | 26 / 0 | 0.747 | 8/8/0; 69.432 | 7.188 |
| 5-players-run-2 | active | 0.153 / 0.329 / 0.461 / 9.954 | 0 | 33.716 / 43.060 | 30.000 | 6 / 0 | 0.751 | 8/8/0; 62.631 | 7.400 |
| 5-players-run-2 | burst | 0.294 / 0.578 / 0.777 / 24.010 | 17 | 33.905 / 54.637 | 29.994 | 12 / 9 | 1.845 | 19/19/0; 363.549 | 8.540 |
| 5-players-run-3 | idle | 0.140 / 0.327 / 0.443 / 15.304 | 0 | 33.683 / 49.985 | 29.994 | 3 / 0 | 0.747 | 8/8/0; 75.800 | 7.700 |
| 5-players-run-3 | active | 0.149 / 0.332 / 0.448 / 8.986 | 0 | 33.678 / 41.247 | 30.000 | 2 / 0 | 0.748 | 8/8/0; 60.275 | 7.422 |
| 5-players-run-3 | burst | 0.285 / 0.660 / 0.965 / 25.052 | 19 | 34.166 / 58.113 | 29.994 | 17 / 17 | 1.828 | 20/20/0; 390.703 | 9.595 |

Client samples pooled within each run/phase; process rates are summed across clients:

| Dataset / run | Phase | Step p99 / max | Frame interval p99 / max | Arrival interval p99 / max | Allocation MB/s | Maximum RTT p99 |
| --- | --- | --- | --- | --- | ---: | ---: |
| 10-players-run-1 | idle | 0.215 / 25.171 | 17.211 / 48.053 | 33.669 / 66.939 | 6.118 | 21.642 |
| 10-players-run-1 | active | 0.220 / 27.532 | 17.255 / 49.606 | 33.745 / 66.864 | 6.803 | 21.260 |
| 10-players-run-1 | burst | 0.233 / 32.178 | 17.632 / 53.124 | 49.834 / 90.717 | 16.827 | 22.380 |
| 10-players-run-2 | idle | 0.203 / 30.334 | 17.192 / 49.338 | 66.652 / 82.986 | 6.067 | 19.642 |
| 10-players-run-2 | active | 0.222 / 26.862 | 17.299 / 48.189 | 49.495 / 133.340 | 6.871 | 20.123 |
| 10-players-run-2 | burst | 0.235 / 34.053 | 17.602 / 58.754 | 49.990 / 99.708 | 16.866 | 25.513 |
| 5-players-run-1 | idle | 0.223 / 28.572 | 17.103 / 46.341 | 33.635 / 62.731 | 1.569 | 17.280 |
| 5-players-run-1 | active | 0.230 / 26.801 | 17.103 / 47.792 | 33.678 / 66.838 | 1.609 | 20.262 |
| 5-players-run-1 | burst | 0.263 / 29.020 | 30.535 / 47.147 | 48.258 / 293.057 | 4.627 | 20.977 |
| 5-players-run-2 | idle | 0.235 / 22.614 | 17.131 / 63.960 | 49.925 / 65.862 | 1.570 | 19.845 |
| 5-players-run-2 | active | 0.237 / 28.911 | 17.118 / 45.581 | 33.838 / 58.616 | 1.570 | 20.474 |
| 5-players-run-2 | burst | 0.191 / 31.226 | 17.373 / 50.451 | 34.108 / 67.378 | 4.563 | 20.758 |
| 5-players-run-3 | idle | 0.216 / 27.009 | 17.115 / 46.076 | 33.626 / 66.689 | 1.569 | 18.518 |
| 5-players-run-3 | active | 0.240 / 27.812 | 17.097 / 44.719 | 33.679 / 67.210 | 1.554 | 17.110 |
| 5-players-run-3 | burst | 0.241 / 29.261 | 17.442 / 54.719 | 34.414 / 99.996 | 4.424 | 19.737 |

Startup and workload outcomes:

| Dataset / run | Peers complete / expected | Loading → active min / max seconds | Accepted abilities | Transfer attempts | Item rejections |
| --- | ---: | --- | ---: | ---: | ---: |
| 10-players-run-1 | 10 / 10 | 0.502 / 0.851 | 3193 | 10020 | 2007 |
| 10-players-run-2 | 10 / 10 | 0.520 / 0.730 | 3178 | 10010 | 2005 |
| 5-players-run-1 | 5 / 5 | 0.622 / 0.692 | 1606 | 5056 | 1013 |
| 5-players-run-2 | 5 / 5 | 0.517 / 0.623 | 1586 | 5007 | 1003 |
| 5-players-run-3 | 5 / 5 | 0.523 / 0.674 | 1588 | 5011 | 1003 |

Interpretation limits:

- Engine frame/physics monitors are cached sampled values, not independent per-frame CPU observations; direct coordinator work timings are separate. Headless intervals do not measure rendered FPS or GPU work.
- Publication timing is host emission; client arrival records accepted application snapshots after validation, not UDP arrivals. Nominal-budget counts include ordinary timer jitter; >40/>50 ms counts show larger deviations.
- Process rates use the first/last 1 Hz samples inside each phase; their actual sample duration is retained in JSON. GC pauses are cumulative runtime pause time; no individual pause-event trace was collected.
- Client process rates are summed, while latency percentiles pool raw observations. CPU percentages use one logical core as 100%. Heap size updates at GC.
- Short/control runs are labeled and never establish the full 180-second steady workload. Endpoint recorder totals exclude CSV output but include startup; recorder-off controls have no raw hot-path samples.
- Approximate shared phase time adds the requested peer launch offset to its local recorder clock; process startup scheduling introduces small alignment differences.
- Frame interval tables use differences between recorder wall timestamps; JSON also retains Godot delta separately, which may be smoothed or clamped.
- Raw process.csv rtt_ms/jitter_ms columns contain SECONDS from NetworkClock despite their headers; summaries multiply by 1000. RTT is a smoothed probe estimate; jitter is smoothed absolute RTT deviation, not packet-arrival jitter.

Full per-peer distributions, over-budget counts/time/excess, process samples, lifecycle markers, endpoint totals, configuration arguments and workload outcomes are in summary.json.
