# Story 8 — Follow the local ship with a persistent camera

Epic: [YAML-defined multiplayer ships with local player control](backlog/multiplayer-ships-w-local-player-control.md).
Plan: [Multiplayer ships — Stories 1–8](multiplayer-ships-story-summary.md).

Status: implementation added; automated checks run; user playtest concluded successfully with one host and one client on 2026-11-03. See the summary for tested scope; three-player validation was not reported.

*As a player, my top-down camera follows my currently controlled ship without changing its angle.*

## Agreed behavior

- Exactly one local gameplay `Camera2D` exists per running gameplay instance, owned by the local `PlayerController` setup and placed in the viewport rendering the gameplay world.
- Parent the camera to the controlled ship at local position zero so transforms provide following. Preserve the camera across ship lifetimes by reparenting on replacement/removal. Keep the view angle fixed using Camera2D's default ignore-rotation behavior.
- Follow the controlled ship's displayed world position, including presentation correction when synchronization is integrated.
- Use Godot's built-in camera position smoothing with a fixed smoothing speed of `5f`. Do not implement a separate custom camera smoothing algorithm.
- Retarget the same camera through `ControlledShipChanged`. Remote participant creation and movement never retarget it.
- Snap on initial assignment, ship replacement, and teleport, resetting camera smoothing so it does not travel across the world from the old target.
- Clear the target before removal. Hold the last camera view while no ship is assigned.
- Dispose bindings and camera state on session shutdown. A new session starts fresh.

## Implementation plan

1. Add camera/follow setup under the local-player architecture from Story 3. `GamePlayManager` coordinates session assembly; the local player owns its camera target and subscriptions.
2. Bind explicitly to the current ship and subscribe to control changes before spawns can be missed. Rebinding the same ship is harmless.
3. Follow through camera parenting, without a per-frame position update or process-priority overrides. Let `Camera2D` apply position smoothing.
4. Set smoothing speed directly to `5f` in camera initialization; no configuration parameter. No mouse look-ahead, camera-angle changes, or custom cinematic behavior is required.
5. Handle initial assignment, replacement, teleport, target loss, and teardown explicitly. Holding the view after removal must not leave an invalid target or continue drifting toward a deleted target.
6. Verify pointer-to-world aiming uses the actual gameplay viewport/camera transform while smoothing is active.

## Acceptance and validation

- H/A/B each have one active local gameplay camera following only their own ship.
- Turning the ship never changes the camera angle.
- Ordinary movement uses native position smoothing; follow speed is fixed at `5f`.
- Remote joins and movement do not change the local target.
- Initial assignment, replacement, and teleport snap without a long smoothing traversal.
- Removal holds the view safely; restart leaves no stale targets or duplicate subscriptions.
- Pointer aiming remains correct while the camera moves.
- When synchronization is available, the camera follows the displayed ship rather than jumping immediately to a corrected physics position.

Validate through three-player visual playtests covering movement, turning, aiming, replacement, teleport, removal, and restart. Review decision: use a fixed smoothing speed of `5f`.

Dependencies: Stories 3–5 and 7. Synchronization is not required to implement local following; integrate with its presentation correction when available. Movement effects and broader lifecycle verification remain deferred, while cleanup required by this camera story remains in scope.
