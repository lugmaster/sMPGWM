# Story 1 — Load typed ship definitions through the existing loading pipeline

Epic: [YAML-defined multiplayer ships with local player control](multiplayer-ships-w-local-player-control.md).

Status: implementation added; automated checks run; user playtest concluded successfully with one host and one client on 2026-11-03. See the summary for tested scope; three-player validation was not reported.

*As a content author, I can define ship types in YAML and receive actionable errors before gameplay starts when definitions are invalid.*

## Agreed behavior

The existing YAML `name` is the ship's stable ID. Preserve its exact value and use case-sensitive comparison. The packaged name is `fighter`, matching the startup default agreed in Story 6. Do not add an `id` field, automatically lowercase names, or introduce a separate display-name field. Renaming a ship changes its identity.

Preserve the existing YAML structure and field names, including the movement-stat names. This story produces typed definitions; Story 2 converts them into validated runtime `ShipData` in the central `ShipRegistry`.

## Implementation plan

1. Introduce `ShipDefinitionsDocument`, `ShipDefinition`, and nested stats and weapon-slot definitions matching [Ships.yml](../../sMPGWM.Game/assets/game_data/Ships.yml). Use explicit serialization mappings for YAML names such as `texture_path`, `collider_points`, and `ship_stats`. Retain collider coordinates as numeric pairs until Story 2 converts them to Godot geometry.
2. Add YAML ship paths to `FilePaths`: packaged `res://assets/game_data/Ships.yml`, schema `res://assets/game_data/ShipsSchema.yml`, and user `user://config/ships/Ships.yml`. Preserve the existing schema requirements, including a nonblank `name`.
3. Add a ship-loading layer using `FileLoader` for preparation and schema validation, followed by the generic `DefinitionDeserializer` for typed conversion. Keep ship-specific behavior outside the generic deserializer.
4. Preserve existing default-copy behavior: copy the packaged definition when the user file is missing, and replace it when `ReloadConfigOnStart` is enabled. Otherwise load the existing user file. Invalid user definitions use the existing load-failure flow rather than silently falling back to defaults.
5. Ensure typed ship definitions load before compatibility data is finalized and before gameplay begins. Route failures through the existing blocking loading-error flow, identifying the source file and schema where applicable.
6. Adjust `GameConfig.EnsureCompatibilityData()` to include the actual loaded ship YAML and schema captured by `FileLoader`. Its existing eight-file inventory check must accommodate these two additional inputs and still detect incomplete data. Include this adjustment here so Story 1 remains functional on its own. Registry/checksum lifetime consistency remains in Story 2.

## Acceptance criteria

- The packaged Fighter loads into typed objects with its name, texture path, all 20 ordered collider points, six movement stats, and light/heavy weapon-slot counts preserved.
- The packaged ship identity is exactly `fighter`, matching `PlayerStartupData`'s default; no additional YAML identity field is required.
- A missing user file is populated from the packaged default. An existing user file is retained unless forced reload is enabled.
- Invalid YAML, missing required fields, wrong types, unknown fields, and deserialization failures produce useful errors identifying the offending source.
- A ship-loading failure prevents entry into gameplay.
- Compatibility generation succeeds with the complete inventory, includes the loaded ship definition and schema, and still rejects an incomplete inventory.
- Existing item loading remains functional.

## Validation

The multiplayer baseline is one host and two clients. Each instance loads its own typed definitions before joining; a single `fighter` definition supports all three participants' ships. Do not require a separate YAML entry per player. Spawning those instances is covered in Stories 5–6; this story's focused parsing tests remain independent of networking.

- Focused tests for the packaged definition, explicit field mappings, missing fields, wrong types, unknown fields, and conversion failures.
- Verify default copying, existing-user-file selection, and forced reload through the existing file-loading test facilities or a Godot integration check.
- Extend compatibility coverage for the added ship inputs and incomplete inventories; verify changing the loaded ship YAML changes the checksum.
- Run relevant existing item-loading tests and verify the blocking load-error path.

## Scope boundary

Duplicate-name detection, finite-number checks, usable collision geometry, texture availability, movement-stat semantics, immutable `ShipData`, and `ShipRegistry` construction belong to Story 2. Ship spawning and movement belong to later stories.

Dependencies: none.
