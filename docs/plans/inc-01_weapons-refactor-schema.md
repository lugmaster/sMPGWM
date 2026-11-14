# Weapons | Refactor Schema

Status: planned; no implementation authorized by this document.  
Epic: [Hitscan combat and weapon systems](hitscan-combat-and-weapon-systems.md)  
Story: 1 of 7  
Dependencies: None; independent of story 2.

## Outcome

Convert the existing light-weapon YAML to the Vulcan Mk1 and Laser Mk1 examples in epic ?4.1. Both weapons must load through schema validation, semantic parsing, the item registry, item creation, and inventory with their new values. They remain nonfunctional.

## Scope

- Add immutable typed weapon metadata and conditional value validation; retain authored numeric baselines in `Values`. Follow epic ?4.2 bounds, supported discriminators, and nonstackable rules.
- Ammunition requires `MagazineCapacity` and `ReloadDuration`; energy requires `EnergyPerShot`; reject fields belonging to the other supply mode and the old `MagazinSize` format.
- Update generic/light-weapon schemas, category value handling, parser, registry preparation, starter references, and fixtures together. Preserve medium/heavy content and behavior except shared version headers.
- Advance all eleven item document headers consistently (planned `3.0.0`; inspect current versions first). Give actionable old-format errors without overwriting user-authored data.
- Carry `PresentationId` as metadata only. Runtime profile preparation and resource validation belong to story 4.

## Acceptance

- Vulcan has damage 1.8, attack speed 15, range 800, spread 6, continuous energy demand 1, magazine capacity 60, reload duration 1500 ms, and mass 1.
- Laser has damage 9, attack speed 3, range 1200, spread 0, continuous energy demand 2, energy per shot 4, and mass 1. Metadata matches the examples exactly.
- Registry-created inventory items retain these definitions and values through existing inventory operations. Invalid combinations, bounds, references to damage sources, and version mismatches fail clearly.
- Existing unrelated inventory/category behavior remains valid. Neither weapon fires, reloads, spends per-shot energy, or deals damage.

## Excluded

Muzzle geometry, groups, firing, networking additions, presentation integration, effects, audio, UI redesign, and runtime supply state. No interaction with visuals in this story.

## Reference and verification

The epic retains detailed end-state contracts. Implement only the scope above; later stories own their deferred behavior. Recheck current code and versions before implementation. Add focused verification for this story and preserve existing regressions. Follow repository test-support markers; record completed evidence and limitations here.
