# Synchronization Story 3 — Local prediction, reconciliation, and correction smoothing

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md). Replaces part of original epic Story 8.

Status: refinement documented; implementation pending. Replay integration and numerical/visual thresholds require implementation checks and playtests.

*As a client player, my ship responds immediately while correcting meaningful differences from the host's authoritative result.*

## Prediction and history

- Predict the locally controlled ship at 60 Hz using the same movement rules, configuration, and tick-addressed inputs as the host.
- Keep bounded input history and resulting end-of-tick predicted-state history. Network transmission does not delay local prediction.
- `PlayerController` retains input policy and binding; a separate coordination layer owns history and reconciliation. Remote ships never read local input.
- The host's own ship uses its authoritative simulation directly and must not be predicted a second time.

## Reconciliation algorithm

1. Validate snapshot session, ship/control identity, and freshness. For host tick T, compare with the saved client state after the same tick T, not with the client's current future state.
2. Compare position distance, velocity-vector difference, and shortest angular difference using separate small numerical tolerances. Compare other movement-affecting values according to their semantics; a meaningful discrete state change cannot be ignored just because position error is small.
3. If differences are negligible, accept the confirmation without restoring/replaying. Still retain authoritative state, advance the finalized boundary, and prune obsolete history.
4. If differences are meaningful, restore the complete authoritative movement state at T and replay saved inputs for T+1 through the client's current prediction tick, updating predicted history.
5. Compare absolute predicted-versus-authoritative error on each received confirmation. Do not hide accumulated drift by comparing only error changes.
6. If required history is unavailable, use Story 5's fresh-baseline recovery. Never invent missing historical input or replay an unbounded backlog.

## Presentation correction

Simulation reconciliation and visual snapping use separate thresholds. Apply meaningful simulation correction through restore/replay immediately. Preserve a visual offset and smooth it toward the corrected simulation for small present-time corrections; snap for explicit teleports, replacement, or sufficiently large displacement.

Choose position/velocity/angle tolerances using world scale, movement speeds, and serialization precision. Configure smoothing duration and snap distance separately. The persistent camera from local ships Story 8 (formerly Story 9) must follow the locally presented position to avoid camera/ship jitter.

## Required implementation investigation

Original Story 7 uses `CharacterBody2D.MoveAndSlide()` in physics updates. Verify how the movement path can execute multiple fixed historical steps safely within Godot's collision/physics lifecycle before coding a replay loop. Audit all mutable movement state and isolate the step contract; merely calling the existing callback repeatedly is not an established replay solution.

Static scenery and moving ships have different replay requirements. Interpolated remote display positions belong to an older timeline and cannot be treated as accurate historical/prediction colliders. Keep custom collision response and advanced interaction prediction deferred as agreed; document the initial approximation and evaluate it in playtests. Do not claim collision-perfect reconciliation or silently expand into full-world rollback.

## Acceptance and validation

- Local input produces movement without waiting for a snapshot or input packet send.
- A snapshot for T is compared with predicted T even when the client has advanced several ticks further.
- Negligible differences skip restore/replay while still retiring confirmed history.
- Velocity, rotation, and meaningful movement-state differences can trigger correction independently of positional error.
- Restoring T and replaying later inputs does not double-apply confirmed movement or duplicate non-movement side effects.
- Simulation correction is distinct from visual smoothing; teleports/replacements do not interpolate through the world.
- Histories stay bounded and missing history causes controlled recovery.

Use focused movement/history checks plus H/A/B playtests under delay/loss. Observe normal flight, braking, afterburner transitions, wall contact, and ship contact. Record correction frequency/magnitude and remaining collision artifacts rather than treating thresholds as a way to suppress meaningful errors.

Dependencies: synchronization Stories 1–2; original movement from Story 7. Persistent camera implementation remains local ships Story 8 (formerly Story 9).
