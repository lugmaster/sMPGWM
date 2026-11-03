# Synchronization Story 3 — Local prediction, reconciliation, and correction smoothing

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md).

Status: implementation integrated; automated ENet evidence recorded in the summary. Final acceptance remains open for the documented transport, collision-quality, and hands-on checks.

As a client player, my ship responds immediately and converges to host movement.

## Replay feasibility gate

Prove a fixed historical-step collision contract before implementing replay. ShipController.Simulate calls MoveAndSlide; repeatedly calling it does not establish safe historical simulation. Use a Godot integration experiment with multiple steps in one physics callback, wall contact, restored collision state, and side effects. Select a safe explicit-step collision adapter if necessary while retaining CharacterBody2D/polygon geometry.

Document moving-ship collision approximation and separate client collision placement from delayed ShipVisuals. Remote presentation is not historical collider state. Full-world rollback is outside scope.

## Implementation

1. Predict from each tick command immediately using the host's movement rules and ShipRegistry configuration. Keep bounded input and resulting end-of-tick state histories. Host-local ships use authority once without prediction replay.
2. Validate snapshot identity/freshness. Compare authoritative T with saved predicted T, never with the current future state.
3. Compare absolute position, velocity, shortest angular difference, and movement-affecting discrete state using independent tolerances. Do not hide accumulated drift with incremental-error comparisons.
4. If agreement is negligible, advance finalization and prune history without replay. Otherwise restore complete authoritative T and replay saved commands T+1 through the current prediction tick, replacing history.
5. Bound replay work. Missing history, an unusable clock, or authority ahead of available prediction invokes Story 5 baseline recovery; never fabricate historical input.
6. Separate ordinary state restoration from Teleport notifications. Replay must not resample input, send RPCs, repeat effects, or reset the camera per historical step.
7. Correct simulation immediately; preserve and decay a small visual offset under ShipVisuals. Configure numerical tolerances separately from smoothing duration and snap distance. Snap/reset on discontinuity, replacement, or excessive correction.
8. Adapt PlayerController's existing persistent camera to follow the locally presented transform using ShipVisuals/DisplayedPosition. Account for Camera2D smoothing to avoid compounded lag. Preserve unbind view retention, reassignment, and teleport reset.

## Acceptance and validation

Input responds before packet send/receipt. Same-tick comparisons and replay neither omit nor double confirmed movement. Velocity/angle/discrete errors can trigger correction independently of position. Confirmations prune history even without replay. Histories/work stay bounded and missing history recovers explicitly.

Add focused history/reconciliation checks and extend camera/spawning integration tests. Exercise H/A/B flight, braking, afterburner, walls, and ship contact under latency/loss. Record replay feasibility, cost, correction frequency/magnitude, tolerances, camera quality, and collision limitations.

Dependencies: Stories 1–2 and the implemented persistent camera.
