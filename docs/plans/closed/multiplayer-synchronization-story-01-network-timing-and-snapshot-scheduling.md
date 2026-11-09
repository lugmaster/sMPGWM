# Synchronization Story 1 — Network timing and snapshot scheduling

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md).

Status: implemented. User playtest confirms synchronized movement with three ENet players and two Steam players. See the summary for evidence and remaining specialized checks.

As a participant, I share the host timeline and receive movement and input snapshots from completed simulation steps.

## Implementation

1. Add a session-owned synchronization coordinator through GameSessionFactory/SessionScope, with role and readiness supplied by NetworkManager. Disable processing synchronously during teardown before deferred node deletion.
2. Establish explicit 60 Hz physics configuration/validation and session-local tick numbering. Audit NetworkTicker consumers before replacing its static timing role; HostDelta is not proof that all ships have completed movement.
3. Own ordering explicitly: acquire permitted input, select commands, simulate eligible ships, capture movement and inputs after the completed step, publish every second step. Remove direct simulation scheduling from PlayerController when connecting the pipeline; ships retain no independent physics tick. Establish this sequence using existing movement behavior in Story 1; PlayerController retains input/UI policy, binding, and camera ownership. Story 2 adds remote command delivery and authoritative host movement for client-owned ships.
4. Define baseline tick zero, step 1 producing state 1, and publications at ticks 2, 4, 6. Never combine two movement steps into one doubled delta.
5. Define shared envelopes for session, entity/control generation, discontinuity revision, and completed tick. Specify integer limits/overflow behavior and bounded payload formats. Limit snapshot payloads to movement and inputs for now. Keep system-specific payloads separate from the shared envelope and scheduling so additional systems can extend snapshots later without redesigning timing or publication.
6. Initialize client host-time estimates from the reliable join baseline. Measure RTT with monotonic timestamps and bounded outstanding probes; smooth jitter and incorporate Story 2 input-arrival feedback.
7. Distinguish current host estimate, future prediction tick, and delayed presentation cursor. Lead includes outbound transit, batching, and jitter; RTT/2 is an estimate. Slew routine corrections without changing movement delta. Bound catch-up work and route excessive stalls to Story 5.
8. Expose host/prediction tick, RTT/jitter, lead, arrival age, publication count, and recovery diagnostics. Reset every timing resource on failure/restart.

## Acceptance and validation

A controlled 60-step run publishes exactly 30 multi-ship movement and input snapshots. Each snapshot represents a completed simulation step; it does not claim to capture complete world state. Render-rate changes do not alter fixed-step movement or publication cadence. Only the host publishes authority; host-local handlers never simulate twice. Client commands target host ticks. Timing estimates, lead, probes, and catch-up remain bounded.

Add controlled ordering/timing tests, including teardown and partial startup. Exercise latency/jitter and stalls with H/A/B in Story 5. Choose explicit lead, probe, and recovery bounds before enabling prediction.

Dependencies: implemented session/spawn foundation. Story 2 supplies real arrival feedback.
