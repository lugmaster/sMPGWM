# Synchronization Story 2 — Client input delivery and authoritative host movement

Plan: [Multiplayer synchronization](multiplayer-synchronization-story-summary.md). Replaces part of original epic Story 8.

Status: refinement documented; implementation pending. Input redundancy and fallback durations are initial tuning values.

*As a player, my input controls my assigned ship on the host, and every participant receives the host's resulting movement state.*

## Input contract

- Generate one command per 60 Hz physics step from `PlayerController`'s current permitted input. Include movement, aim availability/angle, braking, afterburner, and automatic-braking preference as required by the actual movement model.
- Address each command to a host simulation tick and the current ship/control generation, within the current session generation.
- Send every second physics step at 30 Hz: two new commands plus, initially, four preceding commands for loss recovery. Preserve both new commands individually; never average them or simulate both as one host step.
- Continue sending neutral input. UI blocking, focus loss, and ship changes clear held requests as in original Story 3.
- Use unreliable movement delivery with application-level tick validation and bounded redundancy. Discrete future actions require their own semantics and are outside this movement fallback policy.

## Host processing

1. Derive participant identity from the actual RPC sender; validate ownership, session/control generation, field values, and allowed target-tick range. Keep primitive payloads and update protocol compatibility through the existing network path.
2. Buffer valid future commands within configured limits. Ignore identical duplicates; do not let conflicting duplicates overwrite an accepted command. Define diagnostics/rejection for malformed or conflicting traffic.
3. For each physics step, consume the command for that step and simulate every ship exactly once. Snapshot publication follows completion of all ships.
4. When input is missing, briefly reuse the last valid continuous input. Start with a configurable six-tick/100 ms limit, then clear active thrust, manual braking, afterburner, and aiming. Preserve automatic-braking preference; do not directly zero velocity.
5. Discard commands for completed ticks. The host never waits for a missing participant command or rewinds the world for late movement input.
6. Record fallback and arrival timing for diagnostics/client clock feedback. Receipt is different from application: a queued future command is not finalized.
7. Use the same host validation/movement path for the host player with local delivery. Godot `CallLocal` may serve this role. Input RPCs target the host; they are not broadcast to other clients. Do not add artificial network delay or duplicate host simulation.

## Snapshot contract

Publish full movement snapshots initially, not deltas. Each contains session generation, completed host simulation tick, all player ship movement records, and the timing feedback required by recipients. A host-local snapshot RPC handler may run, but must not reapply authoritative movement or reconcile the host against itself.

Each ship record contains:

- Ship entity ID and the control generation needed to distinguish ownership changes.
- World position, rotation, and linear velocity.
- All additional mutable state actually used by movement. Audit the implementation: angular velocity, energy, or cooldowns belong only if those systems exist and influence subsequent steps.
- Applied presentation state for thrust, turning, and afterburner; requested-but-unavailable actions must not appear active.

Immutable configuration remains in `ShipRegistry`. Snapshot tick is the finalized simulation boundary, including fallback steps; a last-received input sequence alone is not an adequate reconciliation acknowledgement.

## Collision scope

Keep `CharacterBody2D`, polygon colliders, and existing collision-based movement. Basic host blocking/sliding remains subject to playtesting. Custom crash response and advanced collision prediction are deferred; this story does not promise symmetric momentum exchange or collision-perfect client prediction.

## Acceptance and validation

- H/A/B each supply only their own input; the host simulates all three and publishes all three resulting states.
- Batching preserves press/release transitions across individual physics steps.
- Loss can be recovered by redundancy before the command deadline; duplicates never cause extra movement.
- Missing input triggers bounded hold then neutral requests; late input does not change finalized steps.
- Snapshot records all refer to the same completed tick and contain sufficient audited movement state.
- Malformed, stale, wrong-owner, and excessively future commands cannot control another ship or grow buffers without bounds.
- Verify focus loss, UI blocking, input release under loss, host-local RPC delivery, and moving-ship collisions in three-player tests.

Dependencies: synchronization Story 1; original Stories 3–7. Client responsiveness is completed by synchronization Story 3.
