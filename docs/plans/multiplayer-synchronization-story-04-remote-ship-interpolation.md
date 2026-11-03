# Synchronization Story 4 — Remote ship interpolation

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md).

Status: implemented. User playtest confirms synchronized movement with three ENet players and two Steam players. See the summary for evidence and remaining specialized checks.

As a participant, I see remote ships move smoothly between snapshots.

## Implementation

1. Add session-owned bounded snapshot buffers and a presentation sampler. Resolve validated entity/control identity through MpManager; movement packets cannot instantiate unknown ships.
2. Separate authoritative samples, collision/body placement, and ShipVisuals. Clients never use local input to move remote ships. Coordinate collision proxies/layers with Story 3; display transforms cannot become replay state.
3. Use one fractional host-tick presentation cursor for all remote ships. Start with two snapshot intervals (about 67 ms) beyond estimated transit delay. Compute the offset once without counting latency twice.
4. Advance the cursor monotonically rather than resetting it on irregular arrivals. Interpolate global position and shortest-path rotation, converting to parent-local visual coordinates at presentation.
5. Sample discrete applied flags from the lower bracket until reaching the upper sample time. Effects implementation remains deferred.
6. Hold the initial baseline until buffered. Bridge missing intermediate packets using available surrounding states. On underrun hold the newest sample; no indefinite extrapolation.
7. Restore ordinary buffering gradually without rewinding presentation. Large interruption invokes Story 5 recovery. Entity/control/discontinuity changes reset affected buffers and snap.
8. Bound samples, ages, pending unknown-entity state, and per-frame work. Ignore stale/duplicate state without rollback. Clear references on despawn/teardown.

Host rendering uses authoritative movement without applying its snapshots back to simulation. Ordinary host physics smoothing must preserve authority and camera behavior.

## Acceptance and validation

Test interpolation, angle wrap, common cursor, parent transforms, missing samples, reordering, duplicate traffic, underrun, and discontinuity reset with synthetic sequences. Run H/A/B at differing render rates under jitter/loss.

All remote ships share one presentation time and move smoothly under normal 30 Hz delivery. Local prediction stays independent. Recovery cannot replay obsolete visual history or retain despawned entities. Measure occupancy, underruns, and visible delay in Story 5.

Dependencies: Stories 1–2 and Story 3 presentation/collision separation.
