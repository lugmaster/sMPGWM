# Synchronization Story 4 — Remote ship interpolation

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md). Replaces part of original epic Story 8.

Status: refinement documented; implementation pending. Buffer and recovery settings require playtesting.

*As a participant, I see remote ships move smoothly despite snapshots arriving less frequently and less regularly than rendered frames.*

## Presentation contract

- Clients buffer accepted full snapshots by completed host simulation tick.
- All remote ships use one shared remote presentation timeline. The local ship remains on its prediction/presentation path from Story 3.
- Start with two snapshot intervals, approximately 67 ms, of buffering behind the estimated incoming snapshot timeline. Transit delay is additional; this is not a fixed 67 ms total delay behind the current host.
- Express the presentation cursor in host simulation time, including fractional ticks, and define its offset once. Do not add latency twice or reset the cursor to every irregular arrival.
- Interpolate position and shortest-path rotation between the snapshots bracketing presentation time. Applied presentation flags are discrete state, not numeric values to blend; use a consistent sample-boundary policy when effects are integrated.
- Keep authoritative state and interpolated presentation separate. Presentation transforms must not overwrite authoritative host movement or local prediction history.

## Delivery behavior

| Situation | Agreed response |
|---|---|
| Snapshots bracket presentation time | Interpolate between them. |
| Intermediate snapshot missing | Interpolate across available surrounding snapshots. |
| Cursor reaches newest available state | Hold at that state; no indefinite extrapolation. |
| Normal delivery resumes | Recover buffering smoothly. |
| Substantial interruption | Reset/rebaseline the buffer using lifecycle recovery. |
| Teleport, replacement, or discontinuity | Snap/reset; do not interpolate across it. |

Ignore stale/duplicate state without rewinding presentation. Bound buffers and discard samples no longer needed. Startup may hold the initial baseline until enough data exists for interpolation.

The host already simulates remote participants authoritatively. Its rendering may use ordinary physics presentation smoothing; it must not feed its own snapshots back into authoritative ship movement.

## Acceptance and validation

- H/A/B each see every remote ship moving; local input never controls a remote ship.
- Remote presentation remains smooth between ordinary 30 Hz arrivals at different render rates.
- All remote ships are sampled at the same presentation time.
- A lost intermediate packet can be bridged when surrounding snapshots exist.
- Buffer exhaustion holds the newest state rather than extrapolating indefinitely.
- Resumed traffic recovers without replaying obsolete visual history; replacements and teleports reset immediately.
- Queues remain bounded through reordering, duplication, stalls, and teardown.

Use controlled snapshot sequences for interpolation/wraparound/buffer checks, then three-player visual tests with jitter and loss. Tune smoothness against visible latency. Short bounded extrapolation is a possible future response to playtest findings, not part of the initial agreed behavior.

Dependencies: synchronization Stories 1–2 and shared state contracts with Story 3. Actual thruster/effect implementation remains original Story 10. Advanced collision prediction remains deferred.
