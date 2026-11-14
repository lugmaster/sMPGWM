# Shield collider and glow implementation handoff

Date: 2026-11-14

Status: PNG baking and ship-data support complete; collider switching, glow, and multiplayer integration remain.

Baseline: working tree based on `798613dcaa697e73a0ea785a713f72b906085652`. The completed baking work includes new and modified files that are not yet committed. Preserve those files and the existing friendly-fire work when continuing.

Finish the shield increment by using each ship's baked circle or ellipse as its physical collider while shield protection is active. When protection is unavailable, use the existing hull polygon. A confirmed hit on the shield briefly illuminates the whole shield outline with a shader.

## Agreed scope

- Shape fitting is an offline authoring task. The Python baker reads the ship PNG's alpha channel and writes `shield_geometry` into `Ships.yml`. The game must not scan pixels or fit shapes at runtime.
- The active shield shape replaces the hull for both physical collision and hitscan detection. It is not merely a visual boundary or a second weapon-only area.
- Shield-down ships and wrecks use the existing authored `collider_points` polygon.
- The visual requirement from the reference image is only that the shield boundary glows when hit. Use a brief whole-outline pulse. Do not add localized flares, impact-coordinate tracking, ripples, beam effects, or a permanent visible bubble.
- Preserve host authority, existing shield/hull damage arithmetic, friendly-fire rules, 60 Hz simulation, and 30 Hz network publication.
- This remaining work does not add collision damage, new shield balance rules, a settings UI, or new projectile/area-damage behavior.

The implementation defaults below make the remaining work concrete. They do not expand the visual requirement beyond the whole-outline glow.

## Completed foundation

| Area | Existing implementation |
| --- | --- |
| Offline baker | `tools/shield_geometry/bake_shield_geometry.py`, with pinned dependencies in the adjacent `requirements.txt`. |
| Fitting | Nontransparent pixel cell corners, convex hull reduction, numerical minimum enclosing ellipse, and enclosing-circle comparison. Default retained alpha is at least 1. Center offset and rotation are fitted. |
| Safe output | Geometry values use one decimal place. Radii increase in 0.1-pixel steps when rounding would break containment, checked after serialization and float32 conversion. Writes are atomic and preserve unrelated YAML fields, comments, BOM, and newline style. |
| Reuse | `--write` bakes, `--check` checks freshness without writing, `--ship` selects a ship, and `--ships`/`--project-root` support other catalogues. |
| Ship definitions | `ShieldGeometryDefinition`, `ShieldGeometry`, and `ShieldGeometryShape`; optional `ShipDefinition.ShieldGeometry` and immutable `ShipData.ShieldGeometry`. |
| Validation | `ShipsSchema.yml` and `ShipRegistry` validate circle/ellipse fields, finite coordinates, positive float-compatible radii, and major/minor ordering. |
| Baked fighter | `sMPGWM.Game/assets/game_data/Ships.yml` contains its ellipse. Existing hull and muzzle geometry remain unchanged. |

Current fighter data, in centered sprite-local pixels:

```yaml
shield_geometry:
  shape: ellipse
  center: [-5.0, 0.0]
  radii: [33.5, 32.1]
  rotation_degrees: 90.0
```

`radii` are semi-major and semi-minor axes. Rotation is clockwise in Godot's downward-positive Y coordinates. Circle entries use `radius` and omit `radii` and `rotation_degrees`. The immutable runtime data normalizes either shape to two radii.

The fighter needs a minor radius of 32.1 after rounding its center: 32.0 would leave two nose-pixel corners outside. Keep the baker's containment check when regenerating rounded values.

The baker fits the PNG only. It warns if authored hull vertices extend outside that result; it does not silently enlarge the shield to include them. Resolve such warnings during asset authoring before enabling shield geometry for that ship.

Completed validation: five Python test methods, 420 tests from the normal .NET command including the Godot integration wrapper, a current fighter bake check, and the gameplay-data validator. This establishes the baking/data foundation; it does not establish switching, shader rendering, or network convergence.

## Remaining implementation sequence

### 1 Build reusable physics shapes from the baked data

Extend `ShipFactory`, `ShipController`, and `scenes/game/game_objects/ship.tscn` to prepare a shield collider beside the existing hull polygon. Both belong to the same `CharacterBody2D` and therefore the same body RID. Keep the existing ship collision layer and mask. Do not use the legacy shield/energy-weapon layer pairing: the envelope must block physical movement and all current hitscan weapons.

Use `CircleShape2D` for circles. For ellipses, build a conservative `ConvexPolygonShape2D` approximation from the baked axes, with unit node scale, and apply the baked local center and rotation. Generate/cache this geometry once per definition or session preparation; constructing a physics resource from baked dimensions is not runtime fitting. Reuse immutable shapes between compatible ships.

Start with 32 vertices and verify the maximum outward approximation error against a declared pixel tolerance, such as 0.5 ship-local pixels. Increase the segment count during preparation for larger modded ships when necessary, with a validated upper bound. A polygon sampled directly on the ellipse is inscribed and can cut inside it; use a circumscribed construction. Both the physics shape and shader must use the same baked dimensions, and their difference must remain within the declared tolerance. [Godot convex-shape documentation](https://docs.godotengine.org/en/4.7/classes/class_convexpolygonshape2d.html)

Exactly one collider is effective whenever movement or ray queries run. Reuse the hull polygon unchanged for shield-down and wreck states. Existing shooter/allied-body RID exclusion must continue to work without a separate shield-body filter.

**Missing-data policy:** geometry is currently optional so the baking-only increment remains compatible with older definitions. For the full increment, the recommended default is to require baked geometry for playable ship definitions, with a clear migration error identifying the ship and baker command. Update the ship format/schema and fixtures consistently if making it mandatory. Do not silently fit a missing shape at runtime or quietly keep an active shield on hull geometry.

### 2 Make collider switching part of authoritative simulation

Use `Runtime.IsAlive && Runtime.ShieldProtectionAvailable` as the desired shield-collider state. The existing protection property accounts for generator/component presence, power, positive charge, and downtime; shield charge alone is insufficient. The life check is separate.

The current simulation advances configuration, abilities, and resources before movement, then resolves all accepted weapon rays before applying their damage. Preserve that ordering:

1. After resource advancement and weapon commands, synchronize collider modes for every ship before moving the first ship.
2. Keep those modes fixed throughout movement and the complete accepted-shot resolution batch.
3. Apply all accepted damage as today.
4. Synchronize final collider modes before final movement/state capture, for subsequent simulation work.

Do not switch shapes directly from a runtime change notification inside weapon resolution. Energy spending can change power during that loop; damage can break shields during the following damage loop. An immediate switch would make later rays depend on iteration order. Such notifications may mark the desired state dirty for the coordinator to apply.

A shot already accepted against the shield envelope remains accepted if another shot in the same batch depletes the shield. Keep the current shield-first and mitigated-hull-overflow arithmetic; do not recast accepted shots against the hull or discard them retroactively.

Prove the switch timing with a small Godot integration test before completing the larger integration. Godot documents deferred changes to collision-shape `disabled` state; `SetDeferred` does not mean the next query already sees the new shape. Select a physics-safe coordinator mechanism and verify the actual effective shape at both boundaries. If a transition must take effect at the next safe step, represent that effective step consistently in authoritative state and prediction rather than allowing a silent one-frame discrepancy. [Godot collision-shape documentation](https://docs.godotengine.org/en/4.7/classes/class_collisionshape2d.html#class-collisionshape2d-property-disabled)

Cover spawn, regeneration, shield depletion, power loss/recovery, downtime expiry, generator removal/replacement, death, restoration, and replicated state installation. Restored ships must have the correct collider before their first movement/query; transient glow is not saved.

Shield recovery can expand the body into a nearby wall or ship. Use a bounded, tested collision-recovery policy consistent with the existing movement motor. No ramming damage or new impulses are part of this increment. Test stationary recovery as well as moving recovery; do not rely on nonzero input to resolve every overlap.

### 3 Keep prediction and selection consistent

`LocalMovementPrediction` replays `ShipController.Simulate`, including `MoveAndCollide`. Add the effective hull/shield collider mode to authoritative movement configuration or an equivalently revisioned movement contract. Advance the configuration revision on effective mode changes, even when movement speed and enabled state remain unchanged. Install the mode before restoring/replaying movement, and use the existing configuration-change reconciliation barrier to avoid replaying historical movement with the wrong shape.

Update `MovementConfiguration`, `MovementWire`, `MovementStateAdapter`, relevant snapshots/restoration validation, and client application paths together. Recalculate fixed payload sizes and retain the ten-player packet bounds. Prevent older reliable resource updates from overriding a newer effective collider mode. Remote bodies must also use the authoritative mode when local predicted movement collides with them.

`ShipTargetPicker` currently requires an enabled node named `CollisionPolygon2D`. Disabling the hull would therefore make shielded ships unselectable. Preserve hull-based click selection independently of whether the hull physics shape is enabled. This retains the existing selection footprint and relationship rules; broadening click selection to the envelope is not required.

### 4 Identify shield contacts for presentation on every peer

The host already obtains the struck ship from `HitscanQuery`. No target identifier is needed to detect a hit locally. Other peers need the struck ship's identity to pulse the correct outline; the existing `AcceptedShot.EntityId` identifies the shooter only.

Extend the existing shot notification with the struck entity ID, using an explicit empty value for misses/world contacts. Keep the current item/sequence deduplication. This is a notification of which shield was hit, not a targeting or lock-on mechanic. Whole-outline glow needs no new local impact coordinates or per-hit shader position.

Record a distinct shield-contact flag at ray resolution from the frozen collider mode. A proposed `ShieldContact = 16` bit can coexist with the existing `Shield` flag, which continues to mean that shield damage was actually absorbed. They differ when an earlier accepted shot breaks the shield or a weapon energy cost removes protection before damage application. Trigger the outline from contact, without changing damage accounting.

For a fixed-width target GUID, `ShotEventCodec.EventBytes` increases from 75 to 91 and its 1200-byte budget fits 13 events instead of 15. Update encode/decode, batching, maximum-count tests, empty-target rules, and the current `flags > 15` validation. Reject inconsistent or unknown fields/bits. Keep cosmetic delivery on the existing shot channel; resource/death authority remains on the reliable gameplay path.

Process hit notifications on the existing presentation timeline. If a target has not arrived yet, use bounded waiting or skip the cosmetic; never infer it by rerunning a ray against client-interpolated bodies. Ignore removed targets and stale events safely. A missed cosmetic packet must not change damage or collider state.

Update the gameplay compatibility marker, currently `8:configurable-friendly-fire`, for the new semantics and wire layouts. Include the shader and any scene/material dependencies in `WeaponPresentationCompatibility`. Baked geometry already belongs to the ship-data compatibility inputs; runtime pixel scanning or PNG-derived shape checksums are unnecessary.

### 5 Replace the small shield arc with a whole-outline shader

Create one reusable shield visual per ship under `ShipVisuals`, so it follows the displayed ship through interpolation and rotation. Drive its ellipse/circle boundary from `ShipData.ShieldGeometry`, using the same center, radii, and orientation as physics. Give the draw bounds enough padding for the soft outer glow.

Use a simple unshaded `canvas_item` shader with an analytic boundary band and soft halo. Start with a blue-white color and a short pulse around the existing 0.15-second duration. Keep the outline invisible at rest. Use a local shader halo so the feature works without making global HDR/bloom a prerequisite; any later bloom tuning is separate. [Godot canvas shader reference](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html)

On each confirmed shield contact, refresh a bounded intensity/fade on that ship's existing visual. Multiple hits in one frame coalesce into the same pulse; they must not create nodes, materials, or growing hit lists. Share the shader resource and isolate each ship's pulse parameters through instance parameters or a material created once for that ship. Hitting one ship must never illuminate another.

Let the last confirmed pulse finish its short fade when a shield breaks, even if the resource update arrives first. Collision availability must not wait for that visual fade. Reset the visual on removal, world replacement, and session cleanup; joining/restoring ships do not replay old pulses.

Presentation follows the contacted surface. Shield-envelope contacts illuminate the outline, including hits with hull overflow; do not place metal sparks in empty space on the enlarged shield boundary. Ordinary hull, world, and wreck contacts continue to use the existing weapon-specific impact effects. No second ray or invented hull contact point is needed.

Remove the `ShieldFlash` arc and its dedicated 128-entry effect pool once the new visual is wired. Adjust the shared effect arrays, pool indices, and tests in `ShotPresentationController` together. Preserve muzzle flashes, tracers, metal impacts, audio, and shot deduplication.

## Integration map

Paths are relative to `sMPGWM.Game/` unless noted.

| Area | Starting points |
| --- | --- |
| Baked data | `Scripts/ShipSystem/ShieldGeometry*.cs`, `ShipData.cs`, `ShipRegistry.cs`, `assets/game_data/Ships.yml`, `ShipsSchema.yml` |
| Body construction | `Scripts/ShipSystem/ShipFactory.cs`, `ShipController.cs`, `scenes/game/game_objects/ship.tscn` |
| Authority and resources | `Scripts/GamePlaySystem/GameplaySimulation.cs`, `Scripts/ShipSystem/ShipRuntime.cs` |
| Rays and damage | `Scripts/CollisionSystem/HitscanQuery.cs`, `Scripts/WeaponSystem/WeaponSystem.cs`, `AcceptedShot.cs`, `ShotResultFlags.cs` |
| Movement and restore | `Scripts/MovementSystem/MovementConfiguration.cs`, `MovementWire.cs`, `MovementStateAdapter.cs`, `LocalMovementPrediction.cs`; `Scripts/WorldSystem/GameWorld.Restoration.cs` |
| Selection | `Scripts/AbilitySystem/ShipTargetPicker.cs` |
| Presentation | `Scripts/VfxSystem/ShotPresentationController.cs`, `ShieldFlash.cs`, `WeaponPresentationCompatibility.cs`; `Scripts/ShipSystem/ShipPresentation.cs` |
| Shot transport | `Scripts/NetworkSystem/ShotEventCodec.cs`, its send/receive callers, and `Scripts/GameConfigSystem/GameConfig.cs` |

## Acceptance and measurement

| Area | Required evidence |
| --- | --- |
| Baked geometry | Circle and offset/rotated ellipse produce valid cached shapes. Physics approximation contains the analytic envelope within its declared tolerance. Missing/invalid geometry fails clearly under the chosen full-increment migration policy. |
| Physical switching | Exactly one collider is effective at query/movement boundaries. A grazing path outside the hull but inside the shield blocks only while the envelope is active. Test walls, ship contact, stationary expansion, regeneration, power, downtime, equipment changes, and death. |
| Hitscan | Vulcan and Laser both hit the active envelope. Shooter exclusion and friendly-fire modes remain correct. Wrecks use hull geometry and remain blockers. |
| Damage ordering | Shield-only, exact break, overflow, same-batch multiple hits, simultaneous kills, and power loss during shot admission preserve accepted-shot ordering and existing damage arithmetic. |
| Selection | A shielded ship remains clickable through the existing hull selection footprint. |
| Networking | Shot identity/contact flags round-trip with correct bounds; malformed, duplicate, late, and reordered events are handled. Old incompatible builds are rejected. Both peers converge on resources and collider mode. |
| Prediction | Collider transitions near obstacles reconcile without replaying against the wrong shape. Older resource packets cannot revert the mode. Fresh, joined, recontrolled, and restored ships install it before simulation. |
| Rendering | Real rendered-client checks show the whole outline aligned with the displayed ship, including rotation, offset, zoom, rapid hits, shield break, and two different ships. No old arc, floating shield-contact metal sparks, cross-ship material bleed, or stuck glow. |
| Lifecycle | No per-hit shape/node/material creation. Removal, restoration, and session teardown reset pulse state and release owned resources. |

Keep 60 Hz physics and 30 Hz publication stable at five and ten players. Measure sustained actual shield hits and repeated shield transitions, with a bounded workload that keeps targets alive long enough to exercise the feature. Record physics/coordinator p50/p95/p99, publication intervals, allocation/GC impact, shot packet sizes, and rendered CPU/GPU frame timing. Headless checks cannot establish shader appearance or GPU cost; local networking cannot establish Steam transport behavior.

Existing useful tests include `HitscanGeometryTests`, combat/resource tests, movement prediction and wire tests, restored-session tests, shot codec/presentation tests, and `FileLoader/ShieldGeometryDefinitionTests`. New test-only declarations and executable code must carry unique `TEST-SUPPORT` IDs and fully qualified test method names under `AGENTS.md`. Isolate verification settings and fixtures from real user data.

Run from the repository root with the baker requirements installed in the chosen Python environment:

```powershell
python -B tools/shield_geometry/bake_shield_geometry.py --check
python -B -m unittest tools.shield_geometry.test_bake_shield_geometry -v
python -B tools/validate_gameplay_data.py
dotnet test sMPGWM.Tests/sMPGWM.Tests.csproj --no-restore
```

## Completion checklist

- [x] Reusable PNG baking tool and fighter geometry.
- [x] Optional baked-data schema, loader validation, immutable data, and foundation tests.
- [ ] Full-increment missing-geometry policy and any required data migration.
- [ ] Cached shield shape and verified switching at authoritative physics boundaries.
- [ ] Prediction, replication, restoration, and hull-based click selection integrated.
- [ ] Struck-entity notification and shield-contact semantics implemented with updated wire bounds and compatibility.
- [ ] Per-ship whole-outline shader pulse implemented; old arc pool removed.
- [ ] Acceptance tests, rendered multiplayer checks, and five/ten-player measurements recorded.
- [ ] Update the collision/presentation decisions in [the hitscan handoff](hitscan-combat-and-weapon-systems.md) after implementation, and retain [friendly-fire behavior](friendly-fire-handoff.md).

The separate friendly-fire handoff still lists its own uncompleted acceptance checks. Do not treat the 420-test baseline or this baking increment as completion of those checks.
