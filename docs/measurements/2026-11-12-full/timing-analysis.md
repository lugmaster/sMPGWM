# Timing analysis: first two five-player Release repetitions

Provisional analysis of the retained raw data, without changing the measured implementation. The full matrix and diagnostic comparisons are separate. Supporting extracted observations are in [timing-analysis-data.json](release-matrix/timing-analysis-data.json); full measurements are in the corresponding peer CSV files.

## Host emission irregularity

The burst measurement window is 450–630 seconds. Both runs keep every publication tick increment at two, including the publication immediately after each long interval. There is no missing host publication tick in those inspected sequences.

| Observation | Five-player run 1 | Five-player run 2 |
| --- | ---: | ---: |
| Host publication intervals above 40 ms | 302 | 12 |
| Those immediately followed by an interval below 26.667 ms | 18 | 11 |
| Those whose interval plus next interval totals 66.667 ms within 2 ms | 14 | 11 |
| Publication-step median work for the long intervals | 0.473 ms | 18.300 ms |
| Overall burst publication frequency | 30.000 Hz | 29.994 Hz |
| Burst host Gen1 collections / cumulative GC pause | 20 / 383.492 ms | 19 / 363.549 ms |

Run 2 largely shows a long interval followed by a short interval, consistent with resuming the fixed-step schedule after a pause. Run 1 has a different tail: 292 of its 302 long intervals occur between 560 and 630 seconds, at approximately 4–5 per second. Most are not immediately paired with a sufficiently short following interval. The unchanged two-tick increments and average frequency imply compensation over a longer sequence, but they do not establish smooth publication timing.

The 18 host coordinator work overruns in run 1 each fall in a one-second process sample bucket containing one Gen1 collection. Bucket pause increments of approximately 17–24.673 ms account for nearly all the corresponding 17–24.848 ms coordinator duration. This strongly associates those work overruns with GC. It does **not** account for the much larger number of long publication intervals late in the run. Most of those publication steps have sub-millisecond coordinator work.

The existing samples separate coordinator execution time from delayed emission, but they cannot attribute the remaining delay to a specific engine task, OS scheduling decision, timer behavior, or peer interaction. A scheduler/engine trace around the late burst interval is the appropriate follow-up if the pattern persists. Increasing simulation throughput or removing readiness guards would not be an evidence-based remedy.

## The 293 ms client snapshot gap

The maximum recorded client interval in run 1 is 293.057 ms on `client-3`, at local recorder time 559.588443 seconds. The preceding accepted snapshot is at 559.295386 seconds; host ticks advance from 33666 to 33684. The next accepted snapshot arrives after another 94.430 ms, advancing to tick 33690.

The client continues processing frames throughout the gap. The largest nearby frame gap is approximately 44 ms. The surrounding process sample records one Gen1 collection and 28.792 ms of cumulative GC pause, and the largest nearby coordinator step is 28.979 ms. These observations cannot explain a 293 ms application snapshot gap as a single stopped client frame or GC pause.

The recorder's `Arrival` call is after `SynchronizationCoordinator.Receive` validates the snapshot and accepts its session/tick/configuration state. Therefore `arrivals.csv` records **accepted application snapshots**, not UDP packet arrivals. During equipment bursts, configuration revision checks can reject snapshots; unreliable transport delivery and recovery can also affect the sequence. This measurement does not record rejected-message reasons or transport packets, so the particular reason for the eight intervening publication ticks not appearing in this client's accepted sequence is unproven. Do not label that gap packet loss or network latency from this evidence alone.

No loading or construction marker occurs near the gap. The fixture's last preceding lifecycle marker is the burst phase at 415.997723 seconds; it is still active and ultimately reports success. Recovery transitions were not independently marked, so their absence cannot be established from the marker file.

## Engine monitor limitation

Godot's sampled `TimeProcess`/`TimePhysicsProcess` values are visibly cached: around the client gap, the same 3.819 ms / 0.341 ms values repeat for many frames before changing together. Their raw columns and distributions are retained as sampled engine monitors. They are not independent per-frame CPU measurements and cannot identify the work responsible for a specific wall-time interval. Use the recorder's direct coordinator timings and adjacent `time_s` differences for event timing; process CPU totals measure aggregate CPU use.

The summary separately retains Godot `delta`, which can be smoothed or clamped, and wall frame intervals computed from adjacent recorder timestamps. Client RTT and jitter columns in the original CSV contain seconds despite their `_ms` headers; the summarizer converts them to milliseconds. Those values are smoothed RTT probes and RTT deviation, not packet-arrival jitter.
