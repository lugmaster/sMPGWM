# Ships | Weapon Fixture Points | Refactor Schema

Status: done (2026-11-15).\
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 2 of 7  
Dependencies: None; can proceed alongside story 1.

## Outcome

Add required authored `weapon_fixture_points` to `Ships.yml`, its schema, and compiled ship definitions/data (epic sections 4.3 and 4.4).

## Scope

- Author fighter offsets as `[1, -15]` and `[1, 15]`, relative to the ship origin; local +X is forward.
- Require at least one finite two-component vector per ship and store the points in immutable compiled ship data, exposed through `ShipRuntime.WeaponFixturePoints`.
- Use ship data version `3.0.0` consistently across the schema, loader, fixtures, and validation. Compatibility input capture includes the ship data and schema.
- Reject definitions missing `weapon_fixture_points` with a field-specific validation error; do not silently assign fighter geometry to other ships.

## Acceptance

- Packaged fighter loads and exposes both exact offsets through the normal ship-data path and final runtime.
- Missing or empty points, malformed pairs, nonfinite components, and coordinates outside the Godot vector range fail validation. One, two, or more valid pairs are accepted.
- Existing ship loading and construction continue to work after fixtures migrate.

## Excluded

Firing, alternating barrel state, coordinate-transform execution, scene anchors, visual changes, and weapon behavior. Story 4 consumes this geometry.

## Reference and verification

Implementation verification passed 126 targeted tests, including the headless Godot integration run. Coverage includes `ShipDefinitionTests`, `ShipRegistryTests`, `ShipFactoryTests`, and `RestoredWorldTests` for authored order, validation, immutable storage, host/replica construction, and restoration. Story 4 owns coordinate transforms and firing behavior.
