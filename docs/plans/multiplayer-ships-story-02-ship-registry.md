# Story 2 — Build the central ShipRegistry and include it in compatibility checks

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: implementation added; automated checks run; three-player user playtest pending.

*As a participant, I can start or join a session only when all ship definitions are usable and compatible with the other participants.*

## Agreed behavior

`ShipRegistry` is the central source for all loaded ship-type data. Every defined ship type is converted into immutable `ShipData` and registered under its exact YAML `name`, using case-sensitive comparison. The packaged ship uses `fighter`, matching the startup default agreed in Story 6. There is no separate `ShipCatalog`.

Consumers obtain ship-type data through the registry instead of loading YAML independently. Live ship instances retain their own mutable `ShipState`; the registry holds shared type configuration, not live entities.

Use the validation rules below for this story. Further validation can be added later without expanding the current scope.

## Implementation plan

1. Introduce `ShipRegistry` following the existing registry conventions. Provide lookup by ID, optional lookup, and read-only enumeration, consistent with `ItemRegistry`'s `GetById`, `TryGetById`, and `GetAll`. Here ID means the exact ship name. Unknown names must not silently resolve to a default ship.
2. Consume the typed definitions from Story 1 and convert every ship type into immutable `ShipData`: name, texture information, collider coordinates, movement stats, and weapon-slot capacities. Convert collider coordinates into Godot-compatible vectors. Protect nested collections from consumer mutation; copy them when assigning mutable node-owned data during construction.
3. Validate all entries before publishing the registry. Duplicate names, invalid numbers, unusable geometry, and unavailable textures fail loading. Errors identify the ship name, source file, and offending field where applicable. A failed load exposes no partially populated registry and prevents gameplay.
4. Preserve the existing movement values and document their meanings using the current [MovementPhysics.cs](../../sMPGWM.Game/Scripts/Movement/MovementPhysics.cs). This story does not change the movement formulas.
5. Ensure registry initialization succeeds before compatibility data is finalized. Build on Story 1's inventory adjustment and checksum the same loaded YAML and schema used to build the registry.
6. Keep the registry and its associated compatibility data fixed for the session. File edits take effect on a subsequent data load, which must rebuild both together. Live reloading is outside scope.

## Validation rules and stat meanings

- Reject duplicate names using exact, case-sensitive comparison.
- Require finite numeric values that fit their runtime numeric types. Retain schema requirements for nonnegative movement values and integer, nonnegative weapon-slot counts.
- Require `ShipMass > 0`. Allow zero speed, rotation speed, and acceleration for intentionally immobile ships.
- Allow afterburner values below normal values; do not impose balance rules.
- Require the texture to load as a usable texture resource.
- Require at least three distinct collider vertices and nonzero polygon area. Reject duplicate vertices and self-intersections. Accept valid concave outlines, including the packaged Fighter's shape, and preserve vertex order.

| YAML field | Meaning |
|---|---|
| `Speed`, `AfterBurnerSpeed` | Target movement speed in world units per second. |
| `RotationSpeed` | Rotation rate in radians per second before inertia. |
| `Acceleration`, `AfterBurnerAcceleration` | Velocity-response coefficients; current movement updates velocity toward its target using the coefficient multiplied by elapsed seconds. |
| `ShipMass` | Positive gameplay mass parameter; its effect on local movement is settled in Story 7. |

## Acceptance criteria

- The packaged Fighter is available through `ShipRegistry` under `fighter`, with its configured values preserved.
- All successfully loaded ship types are available through the registry's read-only enumeration and name-based lookup.
- Three ship instances on each game instance can share its registry's `fighter` data without sharing mutable instance state or being able to alter registry configuration. The host and two clients each load their own equivalent registry; this does not require three different ship types.
- Duplicate names, invalid numeric values, unusable polygons, and unavailable textures fail before spawning with actionable errors.
- A failed load never exposes a partially populated registry.
- Compatibility uses the same loaded definition and schema as the registry; changing ship YAML changes compatibility data.
- Peers with mismatched ship definitions cannot enter the session.
- In-session file edits cannot change the active registry or leave it inconsistent with its advertised checksum.

## Validation

Use one host and two clients as the multiplayer baseline. Verify compatible definitions permit both clients to join. In mismatch scenarios, vary which client has different data and verify that client is rejected while the host and compatible client remain unaffected. These integration checks can run once Story 6 is available; registry unit tests do not require a network session.

- Focused tests for multiple registered types, exact-name lookup, unknown names, duplicate names, and read-only data exposure.
- Boundary tests for nonfinite or unrepresentable numbers, zero movement values, positive mass, and afterburner values below normal values.
- Geometry cases covering the valid concave Fighter, duplicate vertices, zero area, and self-intersection.
- Godot resource-loading coverage for a valid texture and missing or unsuitable resources.
- Atomic-load failure coverage and compatibility tests confirming changed ship input changes the checksum and mismatched peers are rejected.

Dependencies: [Story 1](multiplayer-ships-story-01-load-typed-definitions.md).
