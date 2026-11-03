# Synchronization Story 5 — Synchronization lifecycle and network-condition verification

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md). Replaces part of original epic Story 8.

Status: refinement documented; implementation pending. The final network test envelope and quality thresholds must be established and recorded during validation.

*As a player, I can join, leave, recover from interruptions, and start a new session without stale movement or control state.*

## Fresh lifecycle contract

Joining creates a fresh participant/ship lifecycle. Persistence is deferred. A late joiner receives the current roster and current authoritative movement of existing ships; their movement is not reset to original spawn transforms.

## Implementation plan

1. Extend original Story 6's reliable spawn/readiness sequence with a complete movement baseline captured at one completed host tick. Establish entity creation, local binding, timing estimate, and baseline before acknowledging movement readiness/enabling prediction.
2. While loading, retain only bounded pending movement state. Apply state only after its matching entity exists. Track freshness so an older baseline or duplicate spawn never overwrites a newer applied state.
3. Keep existing players running when another participant joins. Do not clear their history or reinitialize their ships for the new participant.
4. On replacement/control transfer, clear held input and invalidate the old control generation's commands/history. Establish the new binding and baseline explicitly. This defines lifecycle support, not a player-facing ship-switching feature.
5. On disconnect, clear the participant's host input queue, timing feedback, prediction references, and remote snapshot buffers alongside existing entity cleanup.
6. On host loss, use the existing session-exit flow. Host migration is out of scope.
7. On restart, create a fresh session generation and reset tick counters, estimates, buffers, histories, and subscriptions. Reject old-generation packets.
8. When a stall exceeds available history/recovery limits, discard obsolete prediction, obtain a current baseline, and re-establish timing before resuming. Never submit seconds of queued movement for later host execution.
9. Preserve construction/join-failure cleanup from the original plans. Synchronization setup failure must not leave a partly active local ship or leaked ticker/subscriptions.

## Verification matrix

Use host H plus clients A and B for every multiplayer scenario. Repeat with client roles/join order exchanged.

| Scenario | Required observation |
|---|---|
| Ordinary three-player movement | All ships move on all instances; each participant supplies only its own input. |
| Late join while H/A move | B receives current state; H/A do not reset. |
| Delay and jitter | Prediction remains responsive; timing feedback and buffer diagnostics explain behavior. |
| Loss, duplication, and reordering | No duplicate movement, stale rollback, unbounded queues, or cross-owner control. |
| Lost input release | Redundancy or bounded fallback clears active requests; velocity follows normal movement rules. |
| Focus loss/blocking UI | Local held requests clear and the host receives neutral input. |
| Client/host stall | Catch-up is bounded; missing history takes the explicit resynchronization path. |
| Ship replacement/control transfer | Old commands and snapshots cannot affect the new control generation. |
| Disconnect and restart | Correct entities disappear; all synchronization state is fresh in the next session. |
| Walls and moving-ship contact | Record basic blocking/sliding and correction artifacts; use findings to scope later collision work. |

## Measurements and acceptance

Record the actual player count, tested RTT range, jitter, random and burst loss, stall durations, and machine performance. Select concrete test values before running the implementation tests; no specific maximum latency/loss guarantee was agreed in refinement.

Measure input-arrival age, late/missing-input counts, prediction lead, snapshot-buffer occupancy, correction frequency/magnitude, and history/queue bounds. Record the final numerical tolerances, smoothing/snap settings, fallback timeout, redundancy, and interpolation buffer.

Acceptance requires correct ownership and lifecycle, fixed simulation steps, 30 Hz normal publication, bounded recovery, no stale-state application, and successful three-player hands-on evaluation. Numerical networking checks alone do not establish acceptable visual quality. Record remaining visual/collision limitations and resolve blockers identified by playtests.

## Relationship to original Story 11

This story owns synchronization-specific initialization, cleanup, recovery, and adverse-network tests. Original Story 11 is out of current scope and retained as a future outline for broader integration/regression work. Reuse existing lifecycle mechanisms and harnesses; do not defer cleanup, failure handling, or tests required by the active stories.

Dependencies: synchronization Stories 1–4 and original Story 6's join/cleanup protocol. Local ships Story 8 supplies the agreed camera integration when available. Original Story 10 effects are deferred; no effects implementation is required by this story.
