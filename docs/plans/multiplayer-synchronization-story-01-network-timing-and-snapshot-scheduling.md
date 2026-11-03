# Synchronization Story 1 — Network timing and snapshot scheduling

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md). Replaces part of original epic Story 8.

Status: refinement documented; implementation pending. Timing margins remain configurable and require measurement.

*As a participant, my game shares a meaningful simulation timeline with the host while world snapshots are published at a predictable cadence.*

## Timing contract

- The host advances an authoritative simulation tick at 60 physics steps per second, each with a fixed `1/60` second timestep.
- After every second completed step, capture and publish a snapshot at nominal 30 Hz. Complete movement for all ships before capturing any published world state.
- Snapshot tick means the completed simulation tick, not packet number. Example: simulate 100, 101, 102, 103; publish states for 101 and 103.
- Physics callbacks can run late or in batches under load. Do not assume equally spaced packet arrivals or drive simulation from packet arrival or render FPS.
- A snapshot publisher is host-only. Clients still run local input/prediction scheduling and maintain a host-time estimate.

## Implementation plan

1. Establish explicit ordering between input acquisition, movement, completed-tick state capture, and publication. A callback every second physics frame is insufficient unless it runs after all authoritative movement for that step.
2. Keep scheduling separate from movement and message serialization. Integrate lifecycle under the existing gameplay/network managers without moving these responsibilities into `PlayerController`.
3. Establish an initial host tick reference and latency estimate during joining, before predicted movement is enabled. Use monotonic timing rather than wall-clock synchronization.
4. Track smoothed RTT and arrival variation. Use host feedback on how early/late targeted commands arrive to tune the client's prediction lead.
5. Account for outbound travel, input batching, and ordinary jitter in that lead. RTT/2 is an estimate, not an exact one-way measurement.
6. Keep three concepts distinct: estimated current host tick, local prediction tick ahead of it, and buffered remote presentation time behind it.
7. Adjust scheduling gradually during normal operation without changing the movement timestep or speed according to ping. Bound catch-up work; large stalls use the explicit resynchronization path from Story 5.
8. Expose diagnostics for host tick, client prediction tick, RTT/jitter estimate, input lead, late-input count, and snapshot-buffer occupancy. Reset scheduling state on session teardown.

## Acceptance criteria

- With 60 completed host physics steps, normal scheduling produces 30 snapshot publications, each representing a complete world step.
- Rendering rate does not change simulation timestep or publication cadence relative to physics steps.
- Only the host publishes authoritative world snapshots; clients continue local prediction between arrivals.
- Clients share host tick numbering and target future host steps rather than numbering inputs from an unrelated local origin.
- Host feedback can adjust lead without abrupt routine clock jumps or ping-dependent movement speed.
- Session restart does not reuse clock estimates or tick state.

## Validation

Check scheduling/order with a controlled tick driver and multiple ships. Exercise H/A/B with different latency and jitter, observing input arrival age and client lead. Simulate a host/client stall to ensure bounded recovery rather than unlimited catch-up. Exact clock-filter parameters and recovery thresholds must be recorded when implemented.

Dependencies: original Stories 6–7 and their required milestone playtest. This scheduling foundation can be verified with synthetic state before full ship replication exists.
