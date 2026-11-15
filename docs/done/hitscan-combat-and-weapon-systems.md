# Hitscan combat and weapon systems - epic

Date: 2026-11-14  
Status: in progress; stories 1 and 2 are done. Stories 3–7 remain planned.\
Updated: 2026-11-15\
Primary data entry point: [light_weapon.yml](../../sMPGWM.Game/assets/game_data/items/light_weapon.yml)

## Story map and scope precedence

This document is the parent epic and retained end-state design reference, not one implementation story. The seven linked stories define delivery boundaries and acceptance. Their explicit deferrals take precedence over end-state requirements below: data-only stories do not require firing or presentation, and supply/damage are intentionally absent from early playable slices. Existing technical findings must be rechecked before implementation.

Story 1 is complete: every item uses the typed item contract, including the light weapons and medium/heavy mocks. Legacy weapon handling is complete, with no valid legacy schema or parser path. Story 2 is complete: authored `weapon_fixture_points` load through immutable ship data into `ShipRuntime.WeaponFixturePoints`. Later stories consume these definitions and own gameplay execution and presentation.

| Order | Story | Status | Dependencies |
| --- | --- | --- | --- |
| 1 | [Weapons: Refactor Schema](inc-01_weapons-refactor-schema.md) | Done | None; independent of story 2. |
| 2 | [Ships: Weapon Fixture Points and Schema](inc-02_ships-muzzle-offsets-refactor-schema.md) | Done | None; independent of story 1. |
| 3 | [Weapons: Equipment Groups](inc-03_weapons-equipment-groups.md) | Planned | Story 1. |
| 4 | [Weapons: Firing Visuals](inc-04_weapons-firing-visuals.md) | Planned | Stories 1, 2, and 3. |
| 5 | [Weapons: Syncing Firing Action over Host](inc-05_weapons-sync-firing-action-over-host.md) | Planned | Story 4; uses group authority from story 3. |
| 6 | [Weapons: EnergyPerShot and Reloading](inc-06_weapons-energy-per-shot-and-reloading.md) | Planned | Story 5. |
| 7 | [Weapons: Damage](inc-07_weapons-damage.md) | Planned | Story 6. |

Stories 1 and 2 are independent. Story 3 builds grouping on story 1; story 4 combines all three into local firing. Stories 5?7 progressively add host synchronization, synchronized supply mechanics, and synchronized damage/death. Each story owns lifecycle, validation, and recovery for the state it introduces; full-epic validation is completed in story 7.

Story 3 implements one `labeled_button_pair` per equipped weapon with its name and independent primary/secondary toggles, loadout group initialization, restoration, and host-authoritative group synchronization. Input mapping is outside story 3. Supply UI belongs to story 6.

## Contents

- [Decisions and research](#2-decision-register)
- [Existing architecture](#3-existing-architecture-and-required-integration-points)
- [Definitions and migration](#4-definitions-and-schema)
- [System ownership](#5-ownership-and-systems)
- [Weapon state and supply](#6-weapon-state-groups-and-supply)
- [Simulation and collision](#7-simulation-and-collision-ordering)
- [Damage and death](#8-damage-shields-hull-and-death)
- [Network contracts](#9-network-contracts)
- [Presentation and UI](#10-pooled-presentation-and-ui)
- [Performance](#11-capacity-and-performance-requirements)
- [Implementation phases](#12-implementation-sequence-and-acceptance-gates)
- [Tests](#13-test-matrix)
- [Future projectiles](#14-future-projectile-boundary)
- [Handover checklist](#15-handover-checklist)

## 1. Objective and scope

Implement only `laser_mk1` and `vulcan_mk1` as host-authoritative automatic hitscan weapons, ammunition magazines, discrete energy costs, shield/health damage with hull mitigation, two fire groups, synchronized combat state, and pooled presentation. Preserve 60 physics steps/second and publish networking every second step at 30 Hz. Support five typical players and ten maximum, including all nine permitted weapon slots per ship.

Heavy/medium weapon execution and additional playable weapon variants are outside this slice. The item-format migration covers all categories, including medium/heavy definitions, resources, and consumables. Use the two specified muzzle-flash, tracer, and weapon-impact profiles below, with independent generic shield-hit feedback as specified in [story 4](inc-04_weapons-firing-visuals.md). Additional shield-break effects remain outside scope. Existing out-of-scope weapon data does not authorize implementing its firing behavior.

The design leaves a delivery boundary for future projectiles. Projectile motion, projectile replication, historical hit rewind, speculative local shooting, ammunition inventory items, new AI, loot-on-death, despawn/respawn, and a general effect-execution framework are outside this slice. Existing projectile and damage-effect definitions are not evidence that those executors already work.

This document authorizes no implementation by itself. It records the agreed requirements and concrete implementation defaults for review and handover. No unresolved product question blocks this first slice. Defaults identified below are design choices, not claims that the user explicitly specified them or that all reference games behave identically.

## 2. Decision register

### 2.1 Confirmed by the user

| Topic | Decision |
| --- | --- |
| Combat scope limit | Laser Mk1 and Vulcan Mk1 are the playable combat slice. This limit does not exclude any existing weapon from the schema and definition migration. |
| Impact presentation | Every contact plays one impact selected by the weapon's `PresentationId`, including shield-only hits. An active shield independently adds one generic shield arc; shield break/overflow never duplicates the weapon impact. See [story 4](inc-04_weapons-firing-visuals.md). |
| Initial weapons | `vulcan_mk1`: automatic physical SMG; `laser_mk1`: automatic laser pulses. Both use `DeliveryType: HitScan`. |
| Item shape | Shared fields and typed subtype properties are authored at item level; remove the generic `Values` bag and the split `Weapon` metadata object. |
| Item mass | `Mass` is explicitly required on every item, including consumables and resources. |
| Migration completion | Complete. The catalogue contains `vulcan_mk1`, `laser_mk1`, and the standard/advanced medium/heavy mocks, all using one typed definition path. No parallel valid legacy path remains. |
| Weapon composition | Required `DeliveryType` (`HitScan` or `Projectile`) and `Supply.Mode` (`Ammunition` or `Energy`) are independent. The required `Supply` object contains ammunition capacity/reload duration or energy per shot for its selected mode. |
| Weapon spread | `SpreadDegrees` is optional and defaults to `0`. |
| Defense order | Shields absorb first. Excess damage reaches health immediately, after hull mitigation. |
| Hull | Percentage mitigation with diminishing increases in reduction per added armor point, using the positive League-style armor curve. |
| Ammunition reserve | Unlimited. Magazine capacity and reload time still apply. |
| Weapon selection | Separate fire groups. |
| Ship weapon fixture points | `weapon_fixture_points` contains at least one finite `[x, y]` pair, exposed as `WeaponFixturePoints` in C#. The fighter uses `(1, -15)` and `(1, 15)`, relative to the ship origin. |
| Barrel behavior | Alternate muzzles; one damaging shot and one supply cost per accepted shot. |
| Aim | Straight forward with the ship, not independent cursor aiming. |
| Future projectiles | Architectural extension point only; not implemented now. |

### 2.2 Adopted implementation defaults

| Topic | Default and rationale |
| --- | --- |
| Groups | Two independent groups: primary and secondary. Each equipped weapon can belong to either, both, or neither, independently of archetype. Group membership leaves slot capacity and equipment rules unchanged. Both triggers may be held together. |
| Group storage | Ship loadout configuration stores both membership flags against equipped weapon identity and slot binding. Unequip clears membership; fixture removal clears all weapon memberships through the existing unequip behavior. Equipment and membership changes update the configuration revision transactionally. |
| Initial membership | Fresh sessions apply the selected `DevelopmentLoadout` or `StartLoadout` group definition to the final equipped weapons. Restored sessions use captured membership. Weapons equipped, replaced, or re-equipped during play default to primary enabled and secondary disabled. |
| Input defaults | Existing `Action` becomes the displayed "Fire Group 1" action (LMB); add `FireGroup2` (F) and `ReloadWeapons` (R). Preserve `AlternateAction`/RMB ship steering. All remain configurable through the established binding UI. |
| Reload command | Reload all equipped ammunition weapons needing ammunition; ignore full magazines, energy weapons, and already reloading weapons. |
| Automatic reload | Starts immediately after the final round is spent; an empty equipped weapon also starts reloading when its group is held. |
| Manual reload | Full duration, including partial magazines; no round loss or refill until completion. |
| Unequip | Cancel reload, preserve rounds, cooldown, and shot sequence. Re-equipping never refills a weapon. |
| Power | Both weapon types require existing powered-system availability. Laser additionally requires the full stored-energy cost. Preserve energy-empty initialization. |
| Shield availability | Use existing `ShieldProtectionAvailable`, including power and downtime, rather than positive stored charge alone. |
| Damage types | Physical and laser initially use the same shield/hull effectiveness. Hull mitigates both. No new type-specific multipliers in this slice. |
| Friendly fire | Off. Allied ship bodies do not intercept these hitscan rays. World blockers and hostile bodies do. This is a co-op accessibility choice, not an inferred universal game rule. |
| Death | One authoritative transition; stop and retain the ship as an inert wreck. No respawn/despawn. Wrecks block rays as world obstacles but take no more damage. |
| Simultaneous attacks | Admit all eligible shots before applying damage, allowing same-tick trades. Resolve accepted damage in stable order. |
| Visual latency | Host-confirmed effects initially; no client damage prediction and no lag compensation. |
| Shot-event delivery | Unreliable, bounded batches; no blanket retransmission of cosmetic events initially. Persistent combat state remains reliable and independently recoverable. |

### 2.3 Reference-game research and its limits

- **StarCraft II:** weapon timing, gameplay effects, and presentation have distinct responsibilities in its editor. This supports separate weapon admission, damage resolution, and local presentation in this project. See the [original Blizzard Data Module tutorial, mirrored](https://s2editor-guides.readthedocs.io/Classic_Tutorials/03_Data_module/2/) and [Blizzard-sponsored Actors guide](https://s2editor-guides.readthedocs.io/New_Tutorials/04_Data_Editor/060_Actors/). The [guide index](https://s2editor-guides.readthedocs.io/) identifies the provenance. Adopt the separation, not StarCraft's entire data graph or multiplayer architecture.
- **Diablo IV:** Blizzard describes independently rebindable skill controls and separating movement from attacking. This supports preserving ship steering while adding fire-group actions. See [Blizzard's February 2020 development update](https://news.blizzard.com/en-us/article/23308274/diablo-iv-quarterly-updatefebruary-2020).
- **League of Legends:** positive armor follows `100 / (100 + Armor)` for the damage multiplier; effective health grows linearly despite diminishing percentage reduction. See the [armor reference](https://wiki.leagueoflegends.com/en-us/Armor). Riot also describes Lucian's double-shot as an explicit passive mechanic, demonstrating why visual weapon count must not silently determine attack count: [Lucian](https://www.leagueoflegends.com/en-us/champions/lucian/).

These references informed design defaults. They do not establish how this game's barrels or ship aim should work; those two questions were returned to the user and are now resolved above. No claim about a reference game's private network protocol is used in this plan.

## 3. Existing architecture and required integration points

Paths below are relative to `sMPGWM.Game/Scripts/` unless noted otherwise.

| Existing source | Finding / required integration |
| --- | --- |
| `NetworkSystem/SynchronizationCoordinator.cs` | Owns the active physics ordering, input queues, movement prediction, and 30 Hz publication. Do not add a second tick owner through `NetworkTicker`. |
| `GamePlaySystem/GameplaySimulation.cs` | Currently processes transfers, configuration commits, abilities, resources, then movement. Add bounded combat phases here. |
| `ShipSystem/ShipRuntime.cs` | Owns resources, power state, downtime, conditions, revisions, and restoration. Add atomic resource spending and authoritative damage/death application. |
| `ShipSystem/ShipLoadout.cs` | Prepare/commit equipment changes transactionally. Prepare weapon bindings in the same transaction; publish only after successful installation. |
| `StatSystem/EquipmentStatCalculator.cs` | Equipped weapon `EnergyConsumption` already contributes continuous demand. Keep this meaning. `Armor` already represents hull protection. |
| `ItemSystem/ItemEntity.cs`, `ItemStore.cs` | Items have identity, shared definitions, and counts; no ammunition state. Add explicit per-item weapon-state ownership without using stack count as ammunition. |
| `ItemSystem/ItemDefinitionParser.cs` | Parses typed item properties and the selected `Supply` shape. Rejects unknown fields, obsolete wrappers, and wrong-mode supply fields. |
| `ItemSystem/ItemCatalogueLoader.cs`, `GameplayDataSystem/GameplayCatalogueLoader.cs` | Load eleven item documents through the shared typed pipeline during startup and require matching versions; `ItemRegistry` exposes the prepared dataset. |
| `FileLoadingSystem/YamlParser.cs`, `FileLoader.cs` | Do not assume cross-file schema references work automatically. Existing user definitions can outlive packaged-default changes. |
| `CombatSystem/`, `ProjectileSystem/`, `AbilitySystem/AbilityPreparer.cs` | Definitions exist; hitscan, projectile simulation, and general damage-effect execution do not. Do not route firing through an unsupported ability plan. |
| `MovementSystem/CommandQueue.cs`, `MovementWire.cs` | Bounded tick input, six-command redundancy, expiry after more than six missing ticks, and strict payload limits. Extend these intentionally. |
| `NetworkSystem/NetworkManager.Gameplay.cs`, `GameplaySnapshotCodec.cs` | Reliable full-world gameplay states currently publish when any ship changes. Resources already replicate; ammunition and shot events do not. |
| `NetworkSystem/Compatibility.cs` | Inspected protocol is `11`; new wire layouts require a protocol bump. |
| `ShipSystem/ShipDefinition.cs`, `ShipData.cs`, `ShipRuntime.cs` | Load required `weapon_fixture_points`, validate at least one finite coordinate pair, and expose immutable `WeaponFixturePoints` through the runtime. |
| `ShipSystem/ShipPresentation.cs` | Visual transforms are separate from authoritative transforms. Effects must use the appropriate timeline. |
| `InputSystem/`, `PlayerControllerSystem/` | LMB input exists but is not passed to shooting. RMB aim currently controls ship rotation; preserve that semantic. |

The checked-in `light_weapon.yml` contains Vulcan Mk1 and Laser Mk1. The medium/heavy catalogues contain their standard and advanced mock weapons in the same typed format. All eleven item documents use version `3.0.0`.

## 4. Definitions and schema

### 4.1 Target weapon examples

Use item document version `3.0.0`, explicit subtype properties, required `Mass` on every item, and a typed `Supply` object on weapons. All category schemas/content and all eleven item document headers follow this contract. YAML field spelling is `Archetype`; discriminator values use `HitScan`, `Projectile`, `Ammunition`, and `Energy` consistently.

```yaml
Version: 3.0.0
Items:
  - Id: vulcan_mk1
    DisplayName: Vulcan Mk1
    CategoryId: light_weapon
    Description: Fast-firing physical SMG with a reloadable magazine.
    Mass: 1
    Archetype: SMG
    DeliveryType: HitScan
    PresentationId: smg_standard
    DamageSource: physical
    Damage: 1.8
    AttackSpeed: 15
    Range: 800
    SpreadDegrees: 6
    EnergyConsumption: 1
    Supply:
      Mode: Ammunition
      AmmoCapacity: 60
      ReloadDuration: 1500

  - Id: laser_mk1
    DisplayName: Laser Mk1
    CategoryId: light_weapon
    Description: Precise laser firing repeated energy-powered pulses.
    Mass: 1
    Archetype: Laser
    DeliveryType: HitScan
    PresentationId: laser_standard
    DamageSource: laser
    Damage: 9
    AttackSpeed: 3
    Range: 1200
    EnergyConsumption: 2
    Supply:
      Mode: Energy
      EnergyPerShot: 4
```

### 4.2 Composition and validation

- Every shipped weapon uses the same authored contract and typed definition path. Light weapon IDs are `vulcan_mk1` and `laser_mk1`; standard/advanced medium and heavy mocks retain their existing IDs. Preserve category identities and capacity costs. The two named weapons define the playable combat slice. Do not create categories for archetype/delivery/supply combinations.
- Use immutable typed subtype definitions: a shared `ItemDefinition` owns identity, display metadata, stacking, abilities, behaviour grants, and required `Mass`; `WeaponItemDefinition` adds weapon properties. Battery, engine, and other subtypes likewise expose their explicit properties instead of a dictionary. Category metadata selects the supported subtype; light/medium/heavy categories share the weapon subtype. No redundant per-item `Type` selector is needed.
- Author subtype fields at item level. Remove both `Values` and the separate `Weapon` object. Keep `Archetype` as a validated descriptive identifier, not an execution switch; delivery and supply are independent typed discriminators. Compile typed authored baselines into `ResolvedWeaponStats` during preparation or loadout/stat changes. Do not read strings/dictionaries per shot.
- Every item requires `Id`, `DisplayName`, `CategoryId`, and finite nonnegative `Mass`, including resources and consumables. Missing mass is an error, not an implicit zero. Mass describes one item unit; this schema change does not introduce new cargo-weight gameplay.
- Every weapon additionally requires `Archetype`, `DeliveryType`, `Supply`, `PresentationId`, `DamageSource`, `Damage`, `AttackSpeed`, `Range`, and `EnergyConsumption`. Validate identifiers as nonempty and resolve references at the appropriate preparation stage.
- `SpreadDegrees` is optional; omission resolves to `0` in the typed definition. Explicit invalid values still fail validation.
- `Supply.Mode: Ammunition`: require `AmmoCapacity` and `ReloadDuration` inside `Supply`; reject `EnergyPerShot`.
- `Supply.Mode: Energy`: require `EnergyPerShot` inside `Supply`; reject `AmmoCapacity` and `ReloadDuration`.
- Parse the required `Supply` object into immutable typed supply variants (`AmmunitionSupplyDefinition` or `EnergySupplyDefinition`). Its `Mode` selects the variant and its allowed fields.
- The definition contract accepts `DeliveryType: HitScan` or `Projectile` and either supply mode, independently of archetype. Projectile execution remains deferred: definition acceptance does not authorize binding an unsupported delivery to the hitscan executor. Keep an explicit capability check at combat binding. Automatic firing is fixed behavior for this slice; do not author a redundant `FireMode` field.
- Require weapons to be nonstackable. Individual magazine/cadence state must not become ambiguous through merging/splitting weapon stacks.
- Validate damage-source and presentation-profile references during catalogue preparation, before compatibility/readiness succeeds.
- Remove the comma-separated category `Values` lists and their parser-driven field enumeration. Update every item schema, the generic `ItemSchema.yml`, subtype parsing, equipment/stat consumers, and tooltip readers together. Schemas validate required/allowed fields, supply conditionals, and bounds; typed preparation enforces semantic invariants and references. Reject unknown fields and obsolete wrappers. Use local schema definitions/conditionals where practical; shared external `$ref` files require explicit resolver and checksum work.
- Remove legacy success paths from loading, parsing, registry preparation, and definition consumers by the end of story 1. Do not retain a legacy dictionary model, old-format schema branch, field alias, fallback loader, or adapter that accepts old input. Category-specific schemas may remain, but every category enforces the new format. Old-format detection exists only to reject input with an actionable error.

| Value | Contract |
| --- | --- |
| `Damage` | Finite, nonnegative; zero-damage shots still consume supply. |
| `AttackSpeed` | Finite, `0 < value <= 60` logical shots/second. At most one shot per weapon per physics tick. |
| `Range` | Finite, positive, representable in the existing world coordinate model. Validate computed endpoints as finite. |
| `SpreadDegrees` | Optional, defaults to `0`; finite, `0..180`, full cone width; `6` means uniform angular deviation in `[-3°, +3°]`. |
| `EnergyConsumption` | Finite, nonnegative units/second while equipped. |
| `EnergyPerShot` | Finite and positive for energy supply. Full stored-energy payment is required. |
| `AmmoCapacity` | Required for ammunition only; integer, `1..65535`; use a compatible count representation. |
| `ReloadDuration` | Positive finite milliseconds, converted once using `ceil(ms * 60 / 1000)` with a minimum of one tick and checked deadline bounds. |
| `Mass` | Explicitly required on every item; finite and nonnegative. |

Validate compiled/resolved values as well as source values. Bound identifiers and arrays using existing catalogue/network conventions. A higher future rate limit requires revisiting shot queues, wire budgets, and presentation capacity.

### 4.3 Weapon fixture points and presentation profiles

Require `weapon_fixture_points` in `ShipsSchema.yml`, `ShipDefinition`, and `ShipData`, exposed through `ShipRuntime.WeaponFixturePoints`; for `fighter`:

```yaml
weapon_fixture_points:
  - [1, -15]
  - [1, 15]
```

Require at least one finite two-component vector representable by Godot `Vector2`. Store the points in immutable compiled ship data and share them with each ship runtime through construction and restoration. These are offsets from the ship origin; ship local `+X` is forward.

Each weapon item alternates its own muzzle index, starting at index 0. Advance only on an accepted shot, including a miss. Failed admission/reload attempts do not advance it. Weapons firing together may use the same anchor; anchors do not limit weapon capacity. Preserve shot/barrel sequence on transfers and restoration.

Presentation profiles reference these existing scenes:

| Profile | Muzzle | Tracer | Impact (all hits) |
| --- | --- | --- | --- |
| `smg_standard` | `scenes/vfx/muzzle_flashes/muzzle_smg_standard/muzzle_smg_standard.tscn` | `scenes/vfx/tracers/tracer_smg_standard/tracer_smg_standard.tscn` | `scenes/vfx/impacts/metal_impact_smg_standard/metal_impact_smg_standard.tscn` |
| `laser_standard` | `scenes/vfx/muzzle_flashes/muzzle_laser_standard/muzzle_laser_standard.tscn` | `scenes/vfx/tracers/tracer_laser_standard/tracer_laser_standard.tscn` | `scenes/vfx/impacts/metal_impact_laser_standard/metal_impact_laser_standard.tscn` |

Audio paths at inspection are `assets/audio/sound_effects/shooting_smg_standard/shooting_smg_standard.wav` and `assets/audio/sound_effects/shooting_laser_standard/shooting_laser_standard.wav`. Resolve the existing audio; this work does not rename it.

Use the listed impact scene for every contact, including shield-only, hull, world, and wreck hits. Weapon presentation selects it from `PresentationId` without inspecting shield state. Independently, the target/shield supplies the authoritative shield-contact outcome for one generic pooled shield arc, without inspecting the weapon profile. A shield-breaking or overflow hit produces one weapon impact plus one arc when protection was active immediately before that hit's damage resolution; it never produces a second weapon impact. Misses produce neither impact nor arc. [Story 4](inc-04_weapons-firing-visuals.md) defines the drawing, lifetime, pooling, and the read-only shield-availability behavior before story 7 introduces damage. Profile IDs/resources are prepared before session activation and included in compatibility inputs.

### 4.4 Migration behavior

Migrate all item documents and schemas to flat typed properties and universal required `Mass`, including explicit authored mass for resources and consumables. Preserve existing numeric baselines where present. Update stat and tooltip consumers of the removed dictionary without changing their aggregation rules. The schema migration does not add inventory/cargo mass to ship calculations.

The weapon catalogue contains Laser Mk1 and Vulcan Mk1 as light weapons and the standard/advanced medium and heavy mock weapons. Category capacity costs are preserved. Starter references, fixtures, and consumers use these catalogue entries.

All existing weapons need valid explicit weapon metadata and one supply mode: their old mixed energy/magazine fields cannot be copied unchanged. Author missing required fields deliberately; do not infer supply or delivery from archetype/category or silently provide parser defaults. Record the chosen migration values for review rather than treating unspecified metadata as agreed. Migrate production data, positive test fixtures, examples, and all definition consumers together. Legacy-shaped fixtures may remain only as rejection tests. No migrated item uses a legacy execution or definition adapter.

Never silently replace a user's authored `user://` data. Detect the old item format (`Values`, `Weapon`, `MagazinSize`, `MagazineCapacity`, or missing required mass) with an actionable item/file/field error and an explicit migration example. Reloading packaged defaults remains an explicit existing development/user workflow. A general save-file migration system is outside scope.

Old user-authored data must be migrated before loading succeeds. Do not fall back to accepting it or silently load packaged defaults in its place. All shipped definitions must pass the typed contract, and old-format documents must fail even when supplied under the current version header. Version checks alone do not establish removal of the legacy path.

Ship data uses version `3.0.0` with required `weapon_fixture_points`. The schema and loader require at least one finite `[x, y]` pair. The packaged fighter, ship fixtures, immutable compiled data, and final runtime use this contract. User-authored ships must supply their own points; missing fields fail validation. Preserve the explicit default-reload workflow.

Merge newly introduced keybinding defaults into saved user settings without overwriting existing bindings. Current binding validation requires matching action sets; simply adding shipped actions otherwise breaks existing settings.

## 5. Ownership and systems

```text
Scripts/
  ItemSystem/
    ItemDefinition.cs
    WeaponItemDefinition.cs
    ...individual typed item subtype files
  WeaponSystem/
    AmmunitionSupplyDefinition.cs
    EnergySupplyDefinition.cs
    ResolvedWeaponStats.cs
    WeaponRuntimeState.cs
    WeaponStateStore.cs
    ShipWeaponController.cs
    WeaponSystem.cs
    ...individual enum, command, and result files
  CombatSystem/
    HitscanResolver.cs
    DamageSystem.cs
    DamageRequest.cs
    DamageResult.cs
  CollisionSystem/
    HitscanQuery.cs
  VfxSystem/
    WeaponPresentationProfile.cs
    ShotPresentationController.cs
    ...reusable muzzle, tracer, weapon-impact, generic shield-arc, and audio pools
  NetworkSystem/
    ...weapon-state codec and shot-event codec
```

Names describe intended responsibilities; avoid scaffolding unused strategies or interfaces. Use one file per public class/enum/record and matching namespaces. Follow the repository's no-LINQ and allocation-conscious conventions.

| Owner | Responsibility |
| --- | --- |
| Session `WeaponStateStore` | Authoritative per-item state keyed by `ItemEntityId`; integrate item creation/deletion/transfer and capture/restore. No static process-global ownership. |
| `ShipWeaponController` | Prepared fixed-size equipped bindings, group membership, stable slot order, and weapon-configuration revision. |
| `WeaponSystem` | Admission, cadence, reload, all-or-nothing supply payment, shot sequence, and hitscan submission for the two scoped weapons. |
| `HitscanResolver` / collision adapter | Host physics query, nearest valid intersection, and collider-to-entity mapping. |
| `DamageSystem` + `ShipRuntime` | Shared shield/armor/health calculation and one coherent authoritative mutation/death transition. |
| `ShotPresentationController` | Local effects from accepted results; never owns damage or authoritative resources. |
| Session composition/lifecycle | Construct, prepare, bind, restore, activate, and dispose all new components using the existing readiness and `SessionScope` conventions. |

Expose weapon availability through the existing ship component-presence lifecycle if weapons are detachable as a system. Add a `Weapons` component flag and update its serialized presence validation consistently; do not make removal equivalent to destroying item state. Detached/disabled weapon systems cannot fire or reload, while cooldown time continues to pass.

Definitions are immutable. Weapon state is mutable authoritative item state. Equipment bindings are a projection of item placement. Presentation is disposable. Do not put loaded rounds in item definitions, item stack counts, or scene nodes.

## 6. Weapon state, groups, and supply

Store at least item identity, state revision, rounds, next-fire deadline including fractional phase, reload start/completion ticks, and monotonically increasing accepted-shot sequence. Barrel index can be derived from the sequence modulo two. Preserve sequence continuity for deterministic spread and effect deduplication; select a defined stable PRNG/hash algorithm, never process-dependent `GetHashCode()`.

Group configuration stores primary and secondary membership for each equipped weapon identity and slot binding, with a monotonically increasing weapon-configuration revision. Installing/removing/reassigning a weapon changes that revision. Tick input carries the revision it was sampled against; stale input must not shoot a new slot occupant. Reassignment requests include item identity and expected revision, and are reliable, bounded, and idempotent.

Both `DevelopmentLoadout` and `StartLoadout` define membership for the weapons they equip. Apply the selected definition after equipment initialization, resolving each assignment to an equipped instance. Inventory-only weapons and empty slots cannot be group members. Capture and restore membership with equipment bindings and configuration revision; restored membership takes precedence over startup definitions and defaults.

`StartLoadout` equips Vulcan Mk1 in `weapon_1` and assigns it to primary; secondary is empty. `DevelopmentLoadout` equips Vulcan Mk1 in `weapon_1`, Laser Mk1 in `weapon_2`, and the standard heavy weapon in `weapon_3`; Vulcan Mk1 and the heavy weapon belong to primary, and Laser Mk1 belongs to secondary. A weapon omitted from a group definition is outside that group. Every equipped weapon supports either, both, or neither group. Join/rejoin and restoration preserve empty groups even when all weapon slots are occupied.

### 6.1 Admission and cadence

An accepted shot requires a ready owning peer, matching session/control/loadout binding, a live ship, attached/available weapon system, powered systems, at least one held assigned group, elapsed cooldown, no reload, and sufficient supply. A miss is still an accepted shot and pays the same cost.

The first eligible trigger fires immediately. At `15/s`, accepted ticks are `T, T+4, T+8, ...`; at `3/s`, `T, T+20, T+40, ...`. Keep fractional scheduling state for rates not dividing 60. Define continuous-fire cadence separately from a resumed trigger: preserve the accumulated phase while continuously eligible, but reset stale scheduling debt to the current tick after idle/starvation/reload. At most one shot per item per tick; no catch-up bursts.

Prepare and validate a shot before payment; commit its supply, cadence, sequence, and result exactly once. Query/allocation errors must not create half-applied gameplay. Cosmetic queue capacity must never determine admission.

### 6.2 Ammunition

- New item: full magazine. Restored/transferred item: retain recorded rounds.
- One accepted shot consumes one round. Emptying the magazine begins reload immediately at that shot's tick.
- Reload completes at `startTick + 90` for the supplied SMG; completion may permit a shot on the same tick if its group is held and power is available.
- Precise boundary example: first shot at tick 0, 60th at tick 236, automatic reload completes at tick 326. Do not accidentally wait an additional firing interval before starting reload.
- Manual reload of a partial magazine preserves its rounds until completion, then fills to capacity. Repeated requests do not extend/restart an active reload. No ammunition inventory transaction exists.
- Unequip, death, weapon-system removal, or loss of powered-system availability cancels reload without filling. When power returns, an empty held weapon can start again; partial magazines require another manual request. UI focus loss or release of all groups assigned to a weapon stops its firing but does not cancel an otherwise valid reload.
- Item identity, rounds, and unexpired cooldown survive transfers. New slot occupants cannot inherit the outgoing item's reload. Never restart all item states on equipment refresh.

### 6.3 Energy

Preserve continuous equipped demand through `EquipmentStatCalculator`. The SMG contributes 1 unit/second; the laser contributes 2. A continuously firing laser adds 12 units/second through accepted shots (three shots at four units each).

Add `ShipRuntime.TrySpendEnergy` as a production operation. Require the full cost and mutate once; refresh power conditions, resource revisions, and movement availability/configuration if affected. No partial payment, shot event, or barrel advancement on failure. Insufficient shot charge is a weapon availability reason, not automatically the existing ship-wide `energyDepleted` condition.

Preserve existing new-ship energy initialization at zero. Regeneration earlier in the same tick is available to fire. Resolve competing weapons in ascending slot order, including across groups. Re-evaluate power and affordability after each payment. Both groups being held never doubles a weapon's admission.

## 7. Simulation and collision ordering

Run only on the authoritative physics path:

1. Consume validated tick input. Apply queued equipment transfers/group changes and invalidate stale bindings.
2. Commit runtime configuration and process existing ability commands in their established order.
3. Advance live-ship power/shield resources once. Apply accepted reload requests and complete eligible reload deadlines.
4. Move all controlled live ships; update their authoritative transforms before any shot queries.
5. In stable ship-identity and slot order, check eligibility/affordability, choose the prospective sequence/spread/muzzle, and prepare the collision query/result. Recheck affordability and commit supply, cadence, sequence, and accepted result exactly once after successful preparation. Store bounded accepted results. A ship alive at the start of this phase remains eligible for its already admitted attacks. If payment causes power loss, cancel that ship's active reloads immediately in this tick.
6. Apply damage results in the same stable shot order; process death once. Already admitted shots still resolve, permitting trades. Shots geometrically intercepted by a target that dies earlier in this phase do not retarget through it.
7. Finalize hit/miss presentation results, the independent shield-contact result, and authoritative death state. Preserve collision hit/miss independently of damage amounts; for shield contact, use the target's `ShieldProtectionAvailable` captured at each hit's turn in the stable damage order before that hit mutates shields. Active protection adds generic shield feedback without requiring positive absorbed damage or changing the weapon impact. Refresh each affected controller's cached movement configuration after combat energy/death changes; changing runtime revisions alone does not update the movement snapshot cache. Finalize revisions/change notifications and completed tick. At even ticks publish current resources, weapon state, and cosmetic shot batches.

A shot's origin is `ship.GlobalTransform * selectedLocalOffset`; its base direction is the authoritative ship's normalized `+X` axis after movement. Rotate this direction by host-selected spread and cast for `Range` measured from that muzzle. No mouse world position, client-supplied muzzle, client target, or damage value determines the hit.

Use body collisions for ships, with shields resolved by resources. World structures/asteroids block. Exclude the shooter and live allied ship bodies using reused exclusion storage/collider mappings. All wrecks, including allied wrecks, remain blockers. There is no penetration or bounce. Define starts-inside-world-obstacle handling as an immediate blocked shot and verify it in-engine; never let an origin embedded in a wall bypass that wall.

Reuse query parameters and masks. Do not promise allocation-free Godot physics queries: `IntersectRay` returns a dictionary. Query only inside the physics callback and measure actual allocations. See [Godot ray-casting documentation](https://docs.godotengine.org/en/stable/tutorials/physics/ray-casting.html).

## 8. Damage, shields, hull, and death

For finite nonnegative incoming damage `D`, active shield amount `S`, and resolved hull `A = max(0, Stats.Armor)`:

```text
absorbed = ShieldProtectionAvailable ? min(S, D) : 0
remaining = D - absorbed
healthDamage = remaining * (100 / (100 + A))
newShield = S - absorbed
newHealth = max(0, HealthCurrent - healthDamage)
```

Clamp reported applied health damage to available health; retain requested/mitigated values separately only if needed. Compute the multiplier first to avoid unnecessary intermediate overflow. Armor never reduces shield damage. No rounding to whole damage points.

| Armor | Reduction | Health damage from 20 incoming after 5 active shield |
| ---: | ---: | ---: |
| 0 | 0% | 15 |
| 50 | 33.33% | 10 |
| 100 | 50% | 7.5 |
| 200 | 66.67% | 5 |

Reuse shield depletion/downtime handling. Do not restart downtime on hits that find the shield already empty. Positive charge on an unpowered or downtime-disabled shield is not protection under the existing model; health then receives the full mitigated hit while stored shield charge remains unchanged.

`DamageSources.yml` currently describes physical/plasma bonuses without implementing numeric coefficients. Update descriptions to accurately reflect this slice, or clearly label future balance behavior. Do not silently invent coefficients. Later coefficients must calculate overflow as unspent base damage before applying the health multiplier, not reuse already shield-scaled damage.

Damage application must synchronize resource changes, `dead`, and revisions. On the first health transition to zero: set `dead`, clear held actions and `AfterburnerActive`, cancel reloads, stop resource regeneration, disable further weapon/ability admission and movement, and zero velocity. Merely disabling the movement motor is insufficient: current `ShipController.Simulate` still integrates existing velocity. Make the disabled movement configuration visible to prediction/replication. Retain the entity and inert collider; further shots may show world/wreck impacts but must not reissue death or retarget through the wreck.

Death is not a new respawn or loot system. Restoration must reject inconsistent zero-health/live and positive-health/dead combinations for ships governed by this model, and must not initialize restored resources again.

Enforce the same invariant across authoritative health mutation, confirmation, and restoration of initialized combat-capable ships. Current `SetHealthCurrent` permits zero health without death and subsequent refill; update that API and its tests intentionally rather than leaving a public bypass. Do not provide implicit revival in this slice. Low-level resource-pool tests, uninitialized/resource-only fixtures, and disposed runtimes are not living combat ships and need explicitly bounded treatment rather than a global ban on zero-capacity resources.

## 9. Network contracts

### 9.1 Input and reliable actions

Extend the existing tick input with a two-bit held-fire mask and weapon-configuration revision. Fixed-forward weapons require no new aiming payload; existing steering aim remains movement input. Keep session/entity/control-generation validation and the current 30 Hz unreliable command stream with six recent commands.

Recompute command/snapshot bounds from the new codec layout: the current command ceiling is 256 bytes and movement snapshot ceiling is 1,400 bytes. Add exact maximum-size tests. Do not append shot events to movement snapshots.

Release held fire on focus/UI blocking and send released tick input. The host's existing expiry after more than six missing input ticks must include fire bits. Clear immediately on host-observed death, disconnect, unbind, and validated recovery; delayed packets cannot resurrect an old binding. On baseline/recovery/control transfer, require a fresh released trigger before held fire is armed again.

Reload and group-assignment requests are reliable and idempotent, with bounded request history and queues. Carry session, ship identity, control generation, request sequence, expected weapon-configuration revision, and affected item identities. Apply inside the simulation. Return a stable outcome on duplicate requests. Never execute them directly in RPC callbacks.

### 9.2 Persistent state

- Continue reliable authoritative health/shield/energy/conditions replication. First optimize full-world dirty publication to changed-entity rows for ready peers, retaining complete baselines and complete snapshots for non-ready joiners. Current joining clients reject partial gameplay rows; do not apply the ready-peer optimization to them unchanged.
- Add a compact owner weapon-state stream with absolute revisioned values for item identity, slot/group binding, rounds, reload start/deadline, cadence phase/deadline, and accepted-shot sequence. Ammo changes must not increment inventory/container revisions or resend item definitions.
- Synchronize host-approved primary/secondary weapon membership and unequip/fixture-removal cleanup to the owner and other players. Include complete group configuration in initial, join/rejoin, and recovery baselines.
- Replicate remote presentation identity through immutable profile IDs and shot events; remote clients do not need private inventory or exact ammo for every player.
- Validate all values before applying a state batch. Reject stale revisions and impossible item/binding relationships. Coalesce to the latest state per publication; clients do not reconstruct ammo/health by counting cosmetic events.
- Join order must install item identities before dependent owner weapon state. Require topology, movement, gameplay, items, weapon configuration, and weapon baseline before enabling controls/readiness.
- Recovery includes fresh weapon state/configuration, not only movement. A validated baseline's completed host tick is the global cosmetic cutoff: discard shot events at or before that tick, including those from remote shooters. Owner-only item state cannot establish remote per-item sequence floors. Keep normal per-item deduplication for events after the cutoff; never replay past shots on a baseline.
- Restored world capture includes the whole per-item weapon store, including unequipped surviving weapons, group configuration, sequence/seed state, and deadlines in the preserved tick domain. Validate before committing restoration. Transport sessions, pending requests, held inputs, and visual queues are not restored.

Initially use bounded reliable absolute updates for correctness. Measure backlog/bytes; splitting immutable ship configuration from dynamic resources is a follow-up only if measurements justify it. A changed-row optimization is not permission to omit final health/death or weapon convergence.

### 9.3 Shot events

Each accepted shot produces a compact event containing session/tick, `ItemEntityId`, item shot sequence, firing entity/control generation, profile ID, muzzle index, authoritative origin/end point, impact normal, a hit/miss result, and a shield-contact boolean. The deduplication key is `(session, ItemEntityId, shotSequence)`: two identical equipped weapons may both emit sequence 1. The target/shield supplies the shield-contact result at authoritative hit time: story 5 carries the read-only collision-time availability sample from story 4; story 7 captures protection availability at each hit's turn in the stable damage order before that hit mutates shields, independently of absorbed-damage amount. Clients render that result without rechecking current shield state. Weapon presentation consumes the hit result and profile; generic shield presentation consumes the independent shield-contact result without a weapon profile. Damage amounts remain authoritative gameplay state. Preserve item identity and profile data in the event so it can display even if the firing weapon has since been unequipped. Encode all fields through the predefined positional array/scalar schema and include the added boolean in codec ceilings and compatibility checks.

Batch at 30 Hz using separate unreliable packets with a proposed 1,200-byte application-payload ceiling. Derive events-per-packet from the final codec, split batches, and test both ENet and GodotSteam MultiplayerPeer rather than assuming channel/MTU behavior. Use compact profile indices from the prepared compatible catalogue where useful; never serialize full definitions per shot.

Initial policy: no cosmetic retransmission. Deduplicate with a bounded out-of-order window so packet reordering does not incorrectly discard every earlier event. Drop expired events; buffer briefly and boundedly for missing topology. Do not let unknown or malicious event payloads grow memory. Damage and persistent state are independent of this stream.

Schedule remote effects against `NetworkClock.PresentationTick`. Local confirmed effects use current local visual anchors; their hit endpoint remains host-authoritative. Age late effects or skip them when their lifetime has passed. Clear queues on session change and recovery. Do not replay combat from movement prediction/reconciliation.

No rewind is provided: shots intersect the current host world. Describe the resulting client latency in release/testing notes without presenting movement prediction as hit compensation.

### 9.4 Compatibility

Bump inspected protocol `11` to the next available version when wire formats change. Include new weapon/profile/muzzle schema and data in captured compatibility inputs. If shared schema files are introduced, explicitly capture them too. Version errors must identify the incompatible file/item rather than partially enabling a session.

## 10. Pooled presentation and UI

Preload profiles and instantiate bounded pools during session loading. Pools belong to the session, not static global registries. Reuse muzzle flashes, short moving tracer segments, the two specified weapon-impact effects, a shared generic shield-arc pool, and a bounded audio voice pool. [Story 4](inc-04_weapons-firing-visuals.md) records the extracted historical capacities, timing, and revised layering contract. Connect completion signals once where used and reset all animation, transform, color, visibility, lifetime, and ownership state on reuse.

An accepted hit applies damage immediately once story 7 introduces damage. Tracers are presentation only: they can travel from the recorded origin toward the endpoint during a short profile-defined lifetime, but collision/damage never waits for them. Muzzle flashes follow the displayed ship; tracers, weapon impacts, and shield arcs retain the recorded world path/contact. Every hit uses exactly one instance of the weapon's specified impact at the contact point, including shield-only and overflow hits. An authoritative active-shield contact adds one generic arc at that same point, independently of the weapon profile. Each layer acquires from its own pool; exhaustion of either layer does not suppress the other or affect gameplay.

Use expiry/animation completion and an array-backed active list instead of creating timers/tweens or closures per shot. Pool exhaustion drops a cosmetic effect or replaces the oldest low-priority instance, never suppresses damage. Bound/attenuate simultaneous audio voices instead of starting unlimited sounds.

During session initialization, remove the placeholder `Weapons/VBoxContainer/LabeledButtonPair` from `equipment_container.tscn`. Populate that container in slot order with one `labeled_button_pair.tscn` instance per equipped weapon: weapon name, `Primary` toggle, and `Secondary` toggle. Render initialized or restored authoritative membership, including separate entries for identical weapons. Keep the slot grid and capacity display intact; remove entries on unequip and prevent stale or duplicate entries across sessions. Disable an entry's toggles while its request is pending; refresh authoritative state and show a concise reason on rejection.

Show per-weapon rounds/reload state with clear energy-starvation feedback. Reload progress derives from authoritative start/deadline ticks and the synchronized clock; UI cannot complete a reload. Preserve pointer/UI filtering for both fire groups, including keyboard-triggered fire while editing equipment.

## 11. Capacity and performance requirements

The fixture supports nine light weapons. Ten ships firing nine 15/s SMGs can produce 1,350 accepted shots/second: 45 events per 30 Hz publication on average, with synchronized bursts of 90. Alternating muzzles does not multiply these counts.

At the adopted maximum of 60/s per weapon, ten ships can accept 90 shots per physics tick and 180 between publications. A 256-entry result/event staging buffer covers that configured population. If future AI increases the shooter count, capacity must be recalculated from the maximum authoritative shooters; do not silently use the ten-player limit for an unbounded NPC world.

Every accepted shot must resolve damage even if outgoing cosmetic storage is exhausted. Use separate bounded gameplay resolution storage with enforced admission/population limits; drop only the presentation copy if needed. Clear or flush staging at each publication even when no remote peers are present.

Pool capacities should use `ceil(max concurrent rate * effect lifetime) + synchronized burst allowance`, separated by effect/profile and capped by a documented visual budget. Local effects can receive priority over distant effects. Do not infer pool size from a single equipped weapon.

Measure five and ten players, one and nine weapons each, mixed supply, synchronized firing, misses and dense hits, shield overflow/death, reload cycles, impaired networks, joining, and restored sessions. Include a 60/s synthetic definition for the declared operational ceiling. Capture physics p50/p95/p99, 30 Hz publication intervals, allocations/GC pauses, bytes/second, reliable backlog, and pool high-water/drop counts.

Reuse fixed arrays and prepared lookups. Some physics/RPC allocations remain acceptable if measured timing stays stable. Do not force garbage collection. Use the existing [performance handoff](multiplayer-performance-handoff.md) and [measurement methodology](../measurements/2026-11-12-full/methodology.md); record baseline versus changed results and actual transport coverage. Do not claim Steam performance from an ENet-only run.

## 12. Implementation sequence and acceptance gates

Implement the seven stories in the [story map](#story-map-and-scope-precedence). A story is complete when its acceptance criteria and relevant regressions pass.

Keep session cleanup and readiness working after each increment. Story 4 is local-host firing; story 5 enables multiplayer firing only after its baselines and profiles are ready. Story 6 adds supply state and its synchronization together. Story 7 adds damage/death and completes integrated performance validation. Do not pull later supply/damage requirements into schema or visual stories.

Stories 1 and 2 are done. The complete shipped item catalogue, legacy weapon handling, and ship fixture-point lifecycle are covered by those stories. Subsequent stories consume the typed item definitions and `ShipRuntime.WeaponFixturePoints`.

## 13. Test matrix

| Area | Required cases |
| --- | --- |
| Data | Flat subtype fields and rejected legacy wrappers; required mass on every category, including resources/consumables; omitted spread resolves to zero; supply-dependent required/forbidden fields; nonstackable weapons; finite bounds; old capacity names; item-version mismatch; missing damage/profile reference; malformed muzzle vectors; projectile definitions accepted but unsupported combat binding rejected; preserved user keybindings. |
| Migration | Entire shipped catalogue validates and loads through the typed path; Vulcan Mk1, Laser Mk1, and standard/advanced medium/heavy mocks work through item creation, inventory, equipment, stats, and tooltips; old-format and mixed old/new documents fail under both old and current version headers; no successful legacy loader, alias, dictionary consumer, or compatibility adapter remains. |
| Timing | First shot immediate; SMG ticks 0/4/8; laser 0/20/40; fractional-rate long-run cadence; one-shot/tick bound; no idle/starvation catch-up; trigger release/repress cannot bypass cooldown. |
| Ammunition | Exactly one round per shot; final shot at 236/reload complete at 326; partial manual reload; duplicate request; cancellation; unequip/re-equip/transfer preserve rounds and cooldown; restored reload resumes without refill initialization. |
| Energy | Exactly four charged units admits one laser shot; insufficient charge pays nothing; same-tick generation usable; initial energy zero; steady demand remains separate; stable competing-slot payment and power transitions. |
| Geometry | Offsets rotated/translated correctly; alternate only on accepted shots; range starts at selected muzzle; fixed-forward despite cursor location; shooter/ally exclusions; nearest hostile/world blocker; origin inside obstacle; miss still pays. |
| Damage | Shield-only, exact break, overflow; 20 damage/5 shield/100 armor gives 7.5 health loss; armor 0/50/100/200; inactive charged shield; downtime does not restart while empty; both damage sources obey hull rule. |
| Death | One death result; health clamps; movement truly stops with prior velocity; no regeneration/firing/reload; wreck blocks; same-tick trades; later admitted hits do not retarget through newly dead victim. |
| Groups | All four membership combinations; independent per-weapon toggles; placeholder cleanup; both loadout definitions initialize equipped weapons; saved membership survives restoration; unequip/fixture removal clears membership and entries; host changes converge on all players; stale requests rejected; each weapon fires once when both groups are held; UI/focus/recovery clear input. |
| Networking | Malformed/oversize packets; wrong peer/session/generation/item; duplicate reload/group requests; delayed release and input expiry; reordered weapon state; complete baseline after items; recovery while reloading; no historical effects on join. |
| Presentation | Events deduplicate without losing valid reordered shots; remote timeline alignment; specified muzzle/tracer/impact scenes for both weapons; every contact uses one profile-selected weapon impact; active-shield contacts independently add one generic arc; exact break/overflow never duplicate either layer; unavailable shields and later same-tick hits after depletion add no arc; clients use the recorded shield outcome despite newer shield state; misses have neither impact nor arc; missing profile rejected at preparation; independent bounded pool exhaustion; pool reset/reuse and session disposal. |
| Performance | Ten players × nine weapons, synchronized bursts, 60/s supported ceiling, sustained impaired network, allocations/timing/backlog/pool evidence. |

Extend existing `CommandQueueTests`, movement synchronization tests, `HealthSnapshotTests`, `ResourceSnapshotTests`, `JoinProtocolTests`, weapon-fixture replication tests, restoration tests, and resource/equipment tests where relevant. Use real Godot integration tests for query geometry and presentation lifecycle; pure tests cover scheduling and damage arithmetic.

All code introduced solely for test support must follow `AGENTS.md`: use `TEST-SUPPORT-CLASS` for a test-only class or `TEST-SUPPORT` above a test-support method, constructor, or isolated statement, with a unique ID and each consuming fully qualified test class listed once (or script path). Do not add unmarked application hooks or manual-verification helpers. Production seams such as energy spending are production behavior, not test-only accessors.

## 14. Future projectile boundary

Keep one accepted shot contract and one damage application contract. `DeliveryType: Projectile` is represented in the definition model, but execution is deferred. A future schema extension can add a `ProjectileId` reference to the existing projectile catalogue; the executor can create a host-owned projectile at the chosen muzzle and submit the same `DamageRequest` when it impacts. It must independently define movement, lifetime, collision, replication, and visual interpolation.

No projectile entities, generic strategy registry, rewind history, or unused projectile pooling is required now. Archetype must not dictate delivery; a future energy projectile and a physical hitscan weapon remain valid compositions once their combinations are implemented.

## 15. Handover checklist

- [ ] Re-read repository instructions and inspect intervening changes; preserve unrelated staged/untracked work.
- [ ] Confirm current item/protocol versions before applying the planned increments.
- [x] Complete story 1: typed item definitions for the full shipped catalogue and completed legacy weapon handling, with no parallel valid legacy paths.
- [x] Complete story 2: required `weapon_fixture_points`, at least one finite coordinate pair, immutable ship data, and `ShipRuntime.WeaponFixturePoints` through construction and restoration.
- [ ] Keep playable combat implementation limited to Laser Mk1 and Vulcan Mk1 hitscan, their specified weapon effects, and the independent generic shield arc from story 4; this limitation does not exempt other weapons from migration. Exclude additional shield-break effects.
- [ ] Implement agreed requirements and adopted defaults; do not reopen settled barrel/aim/overflow/reserve questions.
- [ ] Keep one authoritative simulation owner and no combat side effects in prediction replay.
- [ ] Include per-item state, readiness, recovery, restoration, and disposal in the implementation, not as later fixes.
- [ ] Verify geometry and real resource convergence, not only codec roundtrips.
- [ ] Record performance evidence and any untested transport/visual limitations.
- [ ] Update this document's implementation status only with completed work and evidence.

The epic and seven linked Markdown stories are the planning artifacts. Stories 1 and 2 are implemented; the remaining stories define the outstanding gameplay work.
