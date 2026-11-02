# Story 7 — Control the local ship through PlayerController

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: refinement in progress; implementation pending.

*As a player, my input controls only my assigned ship.*

## Implementation plan

- Process local input each frame through `InputController` and `PlayerController`. Pass the current values directly to the bound participant's `ControlledShip`; no network round trip or host command queue is involved.
- If a `ShipInputState` value is useful, keep it a local data snapshot containing movement direction, aim angle/availability, afterburner, braking, and automatic braking. Network sequences and payload contracts belong to deferred Story 8.
- Specify movement coordinates explicitly: forward/backward and strafe relative to the ship.
- Convert pointer position to world-space aiming through the gameplay camera/viewport. Persistent camera following in Story 9 is not required for this local-control milestone.
- Have the locally controlled `ShipController` apply the current input during `_PhysicsProcess()`, use `MovementPhysics` for rotation and velocity, and call `MoveAndSlide()` locally. Explicitly order input updates before their consumption.
- Define mass/inertia and braking integration with the existing movement calculations during this story's refinement.
- Clear active input and aiming when control becomes unavailable or the controlled ship changes. Preserve the automatic-braking preference.
- Keep menu actions local.
- Reserve discrete actions such as firing for later work.

## Acceptance criteria

- `PlayerController` supplies local input to the ship explicitly; the ship need not read `InputController.Instance` or receive a network command.
- Input affects only the locally assigned ship, which remains a child of its `MpController`.
- Pointer/UI filtering remains effective.
- Focus loss and blocking UI release active movement requests.
- Input captured for an old ship cannot control its replacement.
- With one host and two clients, every instance contains all three ships. Each instance can immediately move its own ship locally; its two remote representations remain at their approved spawn transforms, including during local collisions. No remote movement synchronization is expected yet.

## Validation

Use local input-to-movement tests and UI/focus smoke tests. All multiplayer scenarios use at least one host and two clients. Exercise control on all three instances independently and verify that only the locally assigned ship moves. Check that remote joins, disconnects, and local ship changes do not redirect held input or enable movement on remote ships.

## Remaining refinement

- Settle mass/inertia and manual/automatic braking behavior using the existing movement calculations.
- Settle pointer-driven aiming while hovering interactive UI; freezing aim while preserving keyboard movement was proposed but has not been agreed.

## Milestone

Completing Stories 1–7 reaches the first major delivery milestone and requires the [user playtest described in the epic](multiplayer-ships-w-local-player-control.md) before proceeding to follow-up work. Movement synchronization, persistent camera following, and movement effects are separate follow-ups.

Dependencies: [Story 3](multiplayer-ships-story-03-local-player-controller.md), [Story 4](multiplayer-ships-story-04-participant-and-ship.md), and [Story 6](multiplayer-ships-story-06-spawn-protocol.md).
