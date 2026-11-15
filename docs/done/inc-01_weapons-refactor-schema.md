# Weapons | Refactor Schema

Status: done (2026-11-15).\
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 1 of 7  
Dependencies: None; independent of story 2.

## Outcome

Use typed subtype properties with explicit required `Mass` for every item, including resources and consumables. The weapon catalogue contains Vulcan Mk1 and Laser Mk1 as light weapons and the standard/advanced medium and heavy mock weapons. The complete catalogue loads through the schema, semantic parser, registry, item creation, and inventory path. All eleven item documents use version `3.0.0`, with one accepted format and no parallel valid legacy path. Firing remains deferred.

## Scope

- Replace `Values` dictionaries with immutable typed item subtypes; remove the split `Weapon` metadata object. Shared `ItemDefinition` fields include required finite nonnegative `Mass`; weapon categories share `WeaponItemDefinition`. Follow epic section 4.2 bounds and nonstackable rules.
- Require `Archetype`, `DeliveryType`, `Supply`, `PresentationId`, `DamageSource`, `Damage`, `AttackSpeed`, `Range`, and `EnergyConsumption` on every weapon. Optional `SpreadDegrees` resolves to `0` when omitted.
- Accept `DeliveryType: HitScan` or `Projectile` and `Supply.Mode: Ammunition` or `Energy` as independent discriminators. This is definition support only; projectile execution remains excluded. Automatic firing remains fixed behavior of the later playable slice, without an authored `FireMode` field.
- Ammunition requires `AmmoCapacity` and `ReloadDuration`; energy requires `EnergyPerShot`. Reject fields belonging to the other supply mode, legacy wrappers, and old `MagazinSize`/`MagazineCapacity` names. Parse supply into typed variants rather than a bag of optional fields.
- Update all item schemas, remove category `Values` lists, and update the parser, registry preparation, stat/tooltip consumers, starter references, and fixtures together. Preserve existing numeric baselines and stat aggregation behavior. This does not add inventory/cargo mass to ship calculations.
- Give resource/consumable items explicit authored mass, without a parser default. Every light/medium/heavy weapon uses explicit typed properties and one required `Supply` object containing `Mode` and only that mode's fields. Preserve category capacity costs. The light weapon IDs are `vulcan_mk1` and `laser_mk1`; medium/heavy mocks use their existing IDs. Do not infer mechanics from category/archetype.
- Remove legacy schema branches, successful old-format parsing, field aliases, fallback loaders, dictionary definition consumers, and compatibility adapters. Old-format detection may remain only for actionable rejection. No category, mock item, or positive fixture is exempt from the new contract.
- Migrate production data, positive test fixtures, examples, and consumers together. Keep old-format fixtures only for rejection tests. Later stories use the new typed model exclusively; gameplay deferrals do not defer any part of this migration.
- Advance all eleven item document headers consistently to `3.0.0`. Give actionable old-format errors without overwriting user-authored data.
- Carry `PresentationId` as metadata only. Runtime profile preparation and resource validation belong to story 4.

## Acceptance

- Vulcan has damage 1.8, attack speed 15, range 800, spread 6, continuous energy demand 1, magazine capacity 60, reload duration 1500 ms, and mass 1.
- Laser has damage 9, attack speed 3, range 1200, spread 0 from omission, continuous energy demand 2, energy per shot 4, and mass 1. Metadata matches the examples exactly.
- All item categories load using flat typed properties. Missing `Mass` fails for every category, including consumables/resources; nonfinite or negative mass fails. Unknown fields and obsolete wrappers fail with actionable diagnostics.
- Validate and load the entire shipped catalogue. Vulcan Mk1, Laser Mk1, and the medium/heavy mocks resolve to the typed weapon definition. Cover every weapon through item creation, inventory, equipment, stat calculation, and tooltip presentation.
- Old-format and mixed old/new documents fail even with a current version header. Verify that legacy wrappers, capacity aliases, and category `Values` lists cannot succeed through an alternative schema or parser. Existing user-authored old data fails with a migration diagnostic, without falling back to packaged defaults.
- Missing required weapon properties and wrong-supply fields fail. Both delivery discriminators parse independently of supply mode; unknown discriminators fail. Omitted spread resolves to zero; explicit out-of-range spread fails.
- Registry-created inventory items retain these definitions and values through existing inventory operations. Invalid combinations, bounds, references to damage sources, and version mismatches fail clearly.
- Existing unrelated inventory/category behavior remains valid. No weapon gains firing, reload execution, per-shot energy spending, or damage behavior in this story.

## Excluded

Muzzle geometry, groups, firing, networking additions, presentation integration, effects, audio, UI redesign, and runtime supply state. No interaction with visuals in this story.

## Reference and verification

The shipped item YAML, `ItemDefinitionParser`, `WeaponItemDefinition`, and typed supply definitions implement this contract. Coverage is recorded in `ItemDefinitionSchemaTests`, `ItemCatalogueLoadingTests`, `ItemRegistryTests`, and the existing inventory/equipment/presentation tests. Later stories own firing, runtime supply state, synchronization, and damage.
