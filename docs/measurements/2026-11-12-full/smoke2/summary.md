# Local performance measurements

Nearest-rank percentiles; milliseconds unless stated. Each steady phase excludes its first 30 seconds. Raw CSVs remain adjacent to each peer.

Host results:

| Dataset / run | Phase | Step p50 / p95 / p99 / max | Steps >16.67 ms | Publication p99 / max | Intervals >40 / >50 ms | Allocation MB/s | GC 0/1/2; pause ms | CPU % of one core |
| --- | --- | --- | ---: | --- | --- | ---: | --- | ---: |
| 5-players-run-1 | startup_smoke (short) | 0.285 / 0.743 / 1.135 / 22.158 | 1 | 35.472 / 56.275 | 2 / 1 | 2.367 | 3/3/1; 32.966 | 27.702 |

Client samples pooled within each run/phase; process rates are summed across clients:

| Dataset / run | Phase | Step p99 / max | Frame interval p99 / max | Arrival interval p99 / max | Allocation MB/s | Maximum RTT p99 |
| --- | --- | --- | --- | --- | ---: | ---: |
| 5-players-run-1 | startup_smoke | 0.279 / 12.610 | 6.944 / 150.000 | 41.318 / 62.130 | 8.635 | 0.008 |

Startup and workload outcomes:

| Dataset / run | Peers complete / expected | Loading → active min / max seconds | Accepted abilities | Transfer attempts | Item rejections |
| --- | ---: | --- | ---: | ---: | ---: |
| 5-players-run-1 | 5 / 5 | 0.578 / 0.687 | 82 | 401 | 82 |

Interpretation limits:

- Engine frame/physics monitors can describe the preceding engine frame; they are distinct from coordinator work duration. Headless intervals do not measure rendered FPS or GPU work.
- Publication timing is host emission; client arrival includes transport and scheduling. Nominal-budget counts include ordinary timer jitter; >40/>50 ms counts show larger deviations.
- Process rates use the first/last 1 Hz samples inside each phase; their actual sample duration is retained in JSON. GC pauses are cumulative runtime pause time; no individual pause-event trace was collected.
- Client process rates are summed, while latency percentiles pool raw observations. CPU percentages use one logical core as 100%. Heap size updates at GC.
- Short/control runs are labeled and never establish the full 180-second steady workload. Endpoint recorder totals exclude CSV output but include startup; recorder-off controls have no raw hot-path samples.
- Approximate shared phase time adds the requested peer launch offset to its local recorder clock; process startup scheduling introduces small alignment differences.

Full per-peer distributions, over-budget counts/time/excess, process samples, lifecycle markers, endpoint totals, configuration arguments and workload outcomes are in summary.json.
