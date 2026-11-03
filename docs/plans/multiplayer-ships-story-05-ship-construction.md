# Story 5 — Instantiate ship entities from ShipData

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: refinement complete; implementation pending.

*As a participant, I receive a ship whose appearance, collision geometry, and base stats come from its registered definition.*

## Agreed structure

Replace the placeholder physical setup with a reusable configured ship scene:

```text
MpController
└── ShipController : CharacterBody2D
    ├── CollisionPolygon2D
    └── ShipVisuals
        └── Sprite2D
```

The current placeholder has a Fighter sprite and circular collider. The new ship uses the texture and polygon defined in [Ships.yml](../../sMPGWM.Game/assets/game_data/Ships.yml). Its participant relationship follows Story 4.

## Implementation plan

1. Create the reusable ship scene with `ShipController` as its physical root, a `CollisionPolygon2D`, and `ShipVisuals` containing the sprite.
2. Add `ShipFactory` accepting registered `ShipData`, a unique ship entity ID, and initial state. Consumers resolve type data through `ShipRegistry`. The definition name identifies a ship type; the entity ID identifies a particular ship instance.
3. Apply the configured texture and ordered collider points. Use a centered sprite with matching local coordinates so the image and collider align. Copy collider data into node-owned mutable storage without exposing the registry's configuration to mutation.
4. Share immutable `ShipData` across instances and create a separate mutable `ShipState` per ship. Initialize position, rotation, and velocity from the supplied initial state. Expose movement stats and light/heavy weapon-slot capacities as configuration.
5. Complete configuration before processing is enabled. Only publish/assign a successfully initialized ship. `MpManager` attaches it beneath the participant through Story 4's lifecycle operations, within the gameplay world's rendering viewport. Story 7 enables local movement only for the locally controlled ship.
6. Define explicit world and ship collision layers. Configure ships to collide with world obstacles and other ships. Story 7 supplies local collision response; cross-peer collision consistency is deferred to the separate synchronization plan.
   For the one-host/two-client baseline, construct all three ships on every instance. Remote ships keep their render and collision representation at the approved spawn transform, with movement updates disabled. Local collision response must not move these remote representations. Story 7 activates movement only on the locally bound ship.
7. On construction failure, clean up all unfinished nodes and abort gameplay via `GameFlowManager.Instance.QuitToStartMenuBlocking(reason)`. Provide an actionable reason identifying the ship type, entity ID where available, and failed construction step or resource. Route the error once through the construction caller or existing error boundary; do not continue gameplay or silently fall back to another ship.

## Failure and cleanup contract

Construct the replacement before changing the participant's current association. If construction fails, the existing ship and association remain intact during failure handling. After the unfinished ship is cleaned up, call `QuitToStartMenuBlocking` with the reason; normal session teardown then removes the existing ship and participant. Preserving the previous ship is an initialization guarantee, not permission to continue the session after an error.

## Acceptance criteria

- Fighter appearance and collision geometry match its registered definition, including all ordered collider points.
- Three ship instances on each instance share immutable `fighter` configuration while maintaining independent entity IDs, mutable state, and node-owned collision data.
- Supplied initial position, rotation, and velocity are applied before processing starts.
- Successfully constructed ships become children of their assigned `MpController` without recreating local input or UI.
- Collision layers and masks support ship/world and ship/ship collisions.
- Construction failure removes all partial nodes, leaves the previous association unchanged until session teardown, and calls `GameFlowManager.QuitToStartMenuBlocking` with an actionable reason.
- Weapon-slot capacities are available as data; weapon behavior is not required.

## Validation

Use a Godot scene integration test to construct three ships beneath three participants, verify configured values and state isolation, and remove them cleanly. Inspect the Fighter collider over its sprite. Verify collision-layer configuration and use controlled construction failures to check partial-node cleanup, preservation of the old association before teardown, and the blocking return-to-menu error path. Repeat multiplayer integration with one host and two clients, verifying three rendered ship entities on each instance. Local movement/collision response is validated in Story 7, including that the two remote representations stay stationary.

## Scope boundary

This story constructs and initializes the ship body. Story 6 supplies network-approved entity IDs and spawn state. Story 7 integrates `MovementPhysics` and local physics updates; Synchronization is covered by its separate five-story plan. Original Story 10 is deferred movement-effects work, outside current scope. Weapon behavior and a player-facing ship-switch feature remain outside this story.

Dependencies: [Story 2](multiplayer-ships-story-02-ship-registry.md) and [Story 4](multiplayer-ships-story-04-participant-and-ship.md).
