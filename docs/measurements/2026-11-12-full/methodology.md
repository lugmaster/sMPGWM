# Local performance measurement method

This pass measures the current implementation and proposes cleanup. Remote Steam and the other correctness/visual verification work were closed or deferred by the user for this pass. No new production optimization is included in the baseline.

## Main matrix

`python tools/run_performance_matrix.py --output docs/measurements/2026-11-12-full/release-matrix`

Five participants, then ten, three independent process launches per scale. Every launch runs three phases: idle (0–210 seconds), movement and supported shield ability requests (210–420), then movement/ability requests plus equipment transfers (420–630). Each phase has a 30-second warm-up and a 180-second measured window. Host shutdown has a three-second grace period after clients finish. Normal cooldown, energy, ownership and revision checks remain enabled. This is the currently available ability workload, not a simulation of future weapons or enemies.

All peers are headless ENet processes on one machine; scenarios do not run concurrently with other benchmark groups. This measures simulation, serialization, engine CPU work and local transport scheduling, not rendered GPU performance or remote network quality. The staged project is built in Release; output is copied into the engine's Debug lookup directory. Every peer records the loaded assembly configuration and JIT optimizer flag to verify that Release actually ran. The engine command explicitly uses `--max-fps 60`; the production project currently lacks an explicit cap. Physics remains 60 Hz and network publication every second step.

Production-like runs change only the staged configuration to Info logging and development=false. Exact configuration, definitions, hashes, initial tracked patch, staged project location and commands are retained with each dataset. User data and dynamic ports are isolated. The staged C# source includes bounded instrumentation and a workload node; production source is not instrumented in the working tree.

## Measurements and interpretation

- `steps.csv`: synchronization coordinator wall-clock work duration and current-thread managed allocation, tick and role. This includes the entire host simulation/publication method, but excludes other nodes and transport polling outside it. Publication/non-publication step distributions are reported separately for the host.
- `publications.csv`: host publication invocation timestamps, completed ticks and movement payload sizes. These are publication opportunities, not a packet capture proving NIC emission.
- `arrivals.csv`: accepted movement snapshot application timestamps/ticks after decode/validation. These are not raw datagram arrivals or transport retransmission counters.
- `frames.csv`: raw monotonic timestamps, Godot delta and engine process/physics monitors. Summaries derive actual wall intervals from adjacent timestamps; Godot delta is separate. Godot's monitors are cached sampled values that repeat across frames; they are not a full per-frame CPU event trace. The coordinator stopwatch is the direct per-step work measurement.
- `process.csv`: one-second process CPU, working set, allocation, GC collection counts and cumulative managed pause time. Heap/committed values reflect the most recent collection. **The frozen recorder's `rtt_ms`/`jitter_ms` columns actually contain seconds** from NetworkClock; the summarizer multiplies them by 1,000. They are smoothed application clock estimates, not packet-level distributions.
- `markers.csv`: loading, first active, phase, preparation, construction, binding and ship/loadout construction boundaries. Resource instance timing includes Godot cache behavior; it does not establish disk-read counts.
- `workload.json`: success, observed participant count, actual travelled distance, accepted/rejected ability requests, transfer attempts, revision changes, restored identity result and compatibility fingerprints.

Raw samples are retained. Percentiles use nearest rank. Overruns include count, total sample time above budget and excess time; nominal-interval comparisons are supplemented by 40/50 ms publication thresholds so small scheduling noise is visible without being equated to a missed simulation budget. No forced GC is used.

The recorder preallocates bounded arrays (including about 14 MiB of raw sample storage per process). Working-set figures include these arrays. Control runs disable recorder hot paths but retain its static storage and the same workload driver. They estimate recurring observer overhead, not a recorder-free startup footprint. Endpoint aggregate totals include workload JSON serialization immediately before recorder flush; steady windows end before that output and exclude it. Cumulative GC pause measurements and one-second sampling do not identify every individual pause's precise cause. Source allocation counts in `allocation-audit.md` are candidates for optimization, not profiler-attributed byte totals.

Peer clocks start during each process's initialization. The scheduled launch offset aligns nominal phase selection, not a precision synchronized cross-process clock. Loading durations use same-process markers. Late-join correlation should use the host trace and wall-clock logs together.

## Reproduction

The runner also accepts `--scenario active|idle|burst|restored`, `--players`, `--repeats`, `--seconds`, `--latejoin`, `--configuration Debug`, `--diagnostic`, `--uninstrumented`, and `--impairment moderate|stress`. The last two profiles use a seeded UDP proxy; their exact parameters and counters are saved per run. They are transport datagram shaping, not changes to game RPC handlers. Regenerate summaries with `python tools/summarize_performance.py docs/measurements/2026-11-12-full` after runs complete. The opt-in NUnit entrypoint is `sMPGWM.Tests.PerformanceProcessTests.LocalPerformanceMatrix`.
