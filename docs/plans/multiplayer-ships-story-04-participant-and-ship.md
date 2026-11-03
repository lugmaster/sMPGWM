# Story 4 — Separate participant identity from the ship body

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: refinement complete; implementation pending.

*As a participant, I retain my identity when my controlled ship changes.*

## Agreed structure

Ship changes are an intended capability. Keep participant identity separate from the physical ship, while composing the currently controlled ship beneath its participant:

```text
MpManager
└── MpController                 participant Node
    └── ShipController           CharacterBody2D, locally driven for own participant
        ├── CollisionPolygon2D
        └── ShipVisuals

PlayerController                separate local input, local peer authority
└── InputController
```

Every game instance has one `MpController` per participant, including peer 1, but only one local `PlayerController`. Place the participant/ship subtree in the scene and viewport that render the gameplay world.

`ControllingPeerId` identifies the participant used to bind local control. The local player's recursive authority assignment remains confined to its separate subtree. Under the revised first milestone, `PlayerController` drives its own participant's ship locally; remote ships do not consume local input. The future network simulation authority model is covered by the separate synchronization plan.

## Implementation plan

1. Change `MpController` from `CharacterBody2D` to a participant `Node`. Assign `ControllingPeerId` during creation, before entering the scene tree, and keep it stable for that participant's lifetime.
2. Add a nullable `ControlledShip` reference and a `ControlledShipChanged` event carrying the previous and new ship. Reassigning the same ship is a no-op. A completed replacement emits one change event with a consistent reference and parent relationship.
3. Keep `MpManager` responsible for participant registration, creation, removal, and local-participant notifications. Preserve idempotent spawning and repeated cleanup.
4. Bind `PlayerController` only to the participant matching the actual local peer ID. Unbind it before that participant is removed. Remote creation and ship changes must not retarget local controls.
5. Coordinate ship assignment and removal through explicit lifecycle operations. A controlled ship is parented to its participant. Switching control does not implicitly destroy the old ship: reparent a retained ship into the gameplay world or its next participant while preserving its world transform, and attach the new ship beneath its new participant. Do not leave a retained ship beneath a participant scheduled for deletion. A ship may be controlled by at most one participant.
6. Make removal explicit: clear the controlled-ship reference and notify listeners before freeing the ship. On participant removal, unbind the local player if applicable, clear/notify the association, and remove the participant and its still-owned ship subtree. Already transferred ships survive. Repeated removal is harmless.
7. Establish the minimal `ShipController` type/reference contract needed here; Story 5 supplies configured construction and the reusable scene. Story 7 supplies local simulation, and Story 11 exercises replacement end to end. A player-facing ship-switch feature is not required in this story.

## Movement responsibility

`ShipController` owns the `CharacterBody2D`. In Story 7, `PlayerController` passes local input to its controlled ship, which uses it during local `_PhysicsProcess()` updates with `MovementPhysics` before calling `MoveAndSlide()`. Input processing does not wait for a network round trip. `MpController` neither reads local input nor moves a body itself. Movement synchronization is separate follow-up work in the separate synchronization plan.

This story does not change `MovementPhysics` or implement the movement protocol.

## Acceptance criteria

- With one host and two clients, every instance represents all three participants exactly once, with the correct peer IDs and one child ship per participant after spawning completes.
- Only the matching local participant binds to `PlayerController`.
- Duplicate spawn records preserve the existing participant and startup data.
- A participant can exist without a ship; its current ship, when present, is its child.
- Ship replacement preserves participant identity and local controls, updates parenting and the reference consistently, and emits one change event. Reassigning the same ship emits none.
- A retained or transferred ship preserves its world transform and survives removal of its former participant.
- Explicit ship removal clears references and notifies listeners before destruction.
- Participant removal cleans up its remaining ship subtree without stale local bindings. Repeated cleanup is safe.
- Parenting or switching updates which ship receives local control without enabling local-player movement on remote participants' ships.

## Validation

Use one host and two clients as the baseline. Assert three `MpController` nodes and three child ships on every instance, with exactly one local binding. Duplicate records for either client leave the third participant unchanged. Disconnect either client and verify removal of only that participant/ship on the remaining instances; the other client retains its binding. Exercise local replacement with all three participants represented. With Story 7 integrated, remote ships remain stationary while the local participant's ship moves.

Extend `PlayerSpawningTests` for peer-ID assignment, local-only binding, nullable ship assignment, replacement events, parenting, and repeated removal. Use a minimal ship body to verify transform preservation and that transferred ships survive former-participant cleanup. Verify only the locally bound participant's ship receives local input across assignment and transfer. Full scene construction follows in Story 5; networked transfer is deferred to synchronization work.

Dependencies: [Story 3](multiplayer-ships-story-03-local-player-controller.md).
