# Synchronization Story 2 — Client input delivery and authoritative host movement

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md).

Status: planned. Implementation and measured network quality remain to be verified.

As a player, my requests control my assigned ship on the host.

## Implementation

1. Expose permitted ShipInputState from PlayerController instead of directly simulating there. Preserve InputController sampling, PlayerInputPolicy, pointer filtering, focus/UI release, and automatic-braking preference.
2. Separate ShipController simulation eligibility from LocallyControlled. The coordinator steps all host ships; clients step only their local predicted ship.
3. Generate commands every physics step, including neutral input. Carry session, entity, control generation, target host tick, Direction, AimAngle, HasAim, Afterburner, Brake, and AutomaticBraking. Send unreliable batches every second step with two new commands and four previous commands; preserve per-step transitions.
4. Route RPCs explicitly to host peer 1. Derive ownership from the authenticated sender and MpController binding. Host-local input uses the same validator/queue with trusted local identity and no artificial delay; CallLocal must not cause duplicate movement.
5. Validate readiness, identity, finite values, direction bounds, payload shape/count, traffic limits, and allowed tick window. Ignore identical duplicates and reject conflicting accepted-tick commands. Bound each peer queue.
6. Consume the current tick once. Never wait for a peer or rewind completed ticks. Hold last valid continuous requests for at most six ticks after their last application, then clear thrust, brake, afterburner, and aim while preserving automatic braking. Do not directly zero velocity.
7. Publish full snapshots after every ship completes the same tick. Include global position/rotation, velocity, session/entity/control/discontinuity identity, applied thrust/turn/afterburner state, and arrival feedback. Receipt of future input is not finalization; completed snapshot tick is the reconciliation boundary.
8. Standardize capture/restore on global coordinates: current ShipState captures local transforms whereas Teleport accepts global values. Test transformed parents and reassignment.
9. Audit movement state: current movement depends on transform, velocity, input, and immutable Data. Preserve automatic-braking policy where required for fallback/replay. Do not invent energy, cooldown, or angular-velocity systems. Applied presentation flags reflect performed movement.
10. Add strict authority/freshness decoding and payload/record bounds. Measure full snapshots at supported capacity; define atomic chunk assembly if required. Update Compatibility through the existing protocol path.

## Acceptance and validation

Test malformed/foreign/stale commands, conflicting duplicates, deadlines, fallback, queue limits, and press/release within one batch. Run H/A/B: each controls only its own ship; host simulates all three once per tick; every client receives all states. Verify UI/focus release under loss and host-local delivery.

Keep CharacterBody2D/polygon blocking and sliding; test host collisions without promising symmetric momentum exchange. Client responsiveness is completed in Story 3.

Dependencies: Story 1 and current PlayerController, MpManager, ShipController, MovementPhysics, and Compatibility.
