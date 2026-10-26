version: 6.2.2
## 9. Stats

Stats describe numeric properties of a `LivingEntity`.

Stats contain no gameplay decision logic. They do not infer conditions, stop systems, select targets, apply damage rules, or execute gameplay behavior. The `StateEngine` is responsible for reading stat values and applying deterministic state rules.

Only the `StateEngine` may mutate stat values at runtime.

Gameplay systems may provide passive stat sources or create `ActiveEffect` requests, but they must not modify `LivingEntityState` directly.

---

### 9.1 Goals

The stat model must support:

- deterministic 30 TPS fast simulation,
- deterministic 15 TPS slow simulation,
- passive stat bonuses that are recalculated only when their source changes,
- active effects that are processed every tick,
- pool resources such as Health, Shield, EnergyStorage, FoodStorage, WaterStorage, and OxygenStorage,
- flow values such as Production, Consumption, and Regeneration,
- attribute values such as Speed, Armor, AttackSpeed, CriticalHitChance, and Resistance,
- compact dirty tracking and delta emission,
- array-indexed hot-path access without dictionary lookup or per-stat heap objects.

---

### 9.2 Runtime Value Layers

Each stat uses deterministic value layers.

Not every stat category uses every layer, but the value semantics are fixed.

| Value Layer | Description |
|---|---|
| `BaseValue` | The natural/default value of the stat. It is provided when a `LivingEntity` is registered. Runtime mutation of `BaseValue` is out of scope. |
| `EffectiveMaxValue` | The passive-adjusted value. It is calculated from `BaseValue` plus permanent passive modifiers from items, talents, level-ups, ship configuration, or other passive sources. |
| `CurrentMaxValue` | The active runtime capacity limit for pool stats. It is calculated from `EffectiveMaxValue` plus active capacity modifiers. |
| `CurrentValue` | The current runtime value. For pool stats, this is the actual amount in the pool. For flow and attribute stats, this is the effective runtime value after active modifiers. |

Preferred internal layout:

```text
float[] BaseValues
float[] EffectiveMaxValues
float[] CurrentMaxValues
float[] CurrentValues
BitSet DirtyStats
BitSet PassiveDirtyStats
BitSet ActiveDirtyStats
```

`StatType` enum values must map directly to array indices.

Example:

```csharp
var index = (int)StatType.Health;
var currentHealth = state.CurrentValues[index];
var currentMaxHealth = state.CurrentMaxValues[index];
var effectiveMaxHealth = state.EffectiveMaxValues[index];
```

This avoids dictionary lookups and object allocation in the hot path.

---

### 9.3 Base Values

`BaseValue` is immutable during runtime for this PRD.

Initial defaults:

| Stat | Default BaseValue |
|---|---:|
| `Health` | `100` |
| `Shield` | `100` |
| `EnergyStorage` | `0` |
| `OxygenStorage` | `0` |
| `FoodStorage` | `0` |
| `WaterStorage` | `0` |
| All other stats | Provided by ship, entity, item, talent, or content definition. |

The defaults may be overridden by entity configuration before registration.

After registration, `BaseValues[]` are treated as valid, stable input values.

---

### 9.4 Passive Modifiers

Passives are permanent stat sources.

Examples:

- items,
- talents,
- level-ups,
- ship hull configuration,
- permanent upgrade state,
- other persistent source state.

Passives are not `ActiveEffect`s.

Passives must be processed by a separate passive modifier pipeline. They are recalculated only when a passive source is added, removed, replaced, or dirtied by its source state.

Passives must not be recalculated every tick.

Passive modifiers write into `EffectiveMaxValue`.

Passive resolution order:

```text
BaseValue
+ passive flat modifiers
then additive passive percentage modifiers
= EffectiveMaxValue
```

Percentage modifiers are additive with each other.

Example:

```text
BaseValue: 100
Passive flat modifier: +20
Passive percentage modifiers: +10% and +15%
Total passive percentage: +25%
EffectiveMaxValue: (100 + 20) * 1.25 = 150
```

If no passive modifier exists for a stat:

```text
EffectiveMaxValue = BaseValue
```

---

### 9.5 Active Effects

Active effects are runtime effects with duration, trigger, stacking, blocking, and removal behavior.

Active effects must be processed every fast `NetworkTick` unless their simulation lane or trigger explicitly makes them due only on a slow tick or interval.

Processing active effects every tick does not mean every stat is fully recalculated every tick. A stat is resolved when it is dirty, due, or required by a stat-processing rule.

Active effects do not modify `BaseValue`.

Active effects do not write passive state.

Active effects may target:

| Target Value Kind | Valid Use |
|---|---|
| `CurrentValue` | Pool transactions such as damage, heal, drain, restoration, production application, consumption application, or effective runtime changes to flow and attribute stats. |
| `CurrentMaxValue` | Runtime capacity changes for pool stats. |

`EffectiveMaxValue` is not directly targeted by active effects. It is produced by the passive modifier pipeline.

Temporary active modifiers must be recalculated from the currently active effect set. The engine must not rely on inverse effects to undo expired modifiers.

When an active effect expires or is removed, its contribution disappears from the active modifier set and the affected stat snaps back to the value implied by the remaining passive and active modifiers.

---

### 9.6 Stat Categories

Stats are grouped by behavior, not by visual presentation.

The initial categories are:

```text
PoolStat
FlowStat
AttributeStat
```

Each stat category defines which value layers are meaningful and how active effects interact with those layers.

---

## 9.7 PoolStat

A `PoolStat` has an actual amount and a runtime capacity.

Pool stats use all four value layers:

```text
BaseValue
EffectiveMaxValue
CurrentMaxValue
CurrentValue
```

Pool resolution:

```text
BaseValue
+ passive modifiers
= EffectiveMaxValue

EffectiveMaxValue
+ active CurrentMaxValue modifiers
= CurrentMaxValue

CurrentValue is clamped to [0, CurrentMaxValue]
```

Valid active effect targets for pool stats:

| Target | Meaning |
|---|---|
| `CurrentValue` | Changes the actual amount in the pool. Examples: damage, heal, drain, restoration, regeneration application. |
| `CurrentMaxValue` | Changes the runtime capacity of the pool. Examples: temporary max Health buff, temporary Shield capacity debuff, EnergyStorage capacity buff. |

Pool stats:

| Stat | Lane | Description |
|---|---|---|
| `Health` | Fast 30 TPS | Entity health pool. The stat does not infer death by itself. |
| `Shield` | Fast 30 TPS | Shield pool. Can enter a recharge cooldown when depleted. |
| `EnergyStorage` | Fast 30 TPS | Stored surplus energy that can be spent by skills or other systems. |
| `OxygenStorage` | Slow 15 TPS | Stored oxygen survival resource. |
| `FoodStorage` | Slow 15 TPS | Stored food survival resource. |
| `WaterStorage` | Slow 15 TPS | Stored water survival resource. |

### 9.7.1 Pool Capacity Resolution

Active `CurrentMaxValue` modifiers are resolved deterministically:

```text
EffectiveMaxValue
+ active CurrentMaxValue flat modifiers
then additive active CurrentMaxValue percentage modifiers
= CurrentMaxValue
```

Percentage modifiers are additive with each other.

Example:

```text
EffectiveMaxValue: 100
Active flat capacity modifier: +20
Active percentage capacity modifiers: +10% and +15%
Total active percentage: +25%
CurrentMaxValue: (100 + 20) * 1.25 = 150
```

### 9.7.2 Pool CurrentValue Resolution

`CurrentValue` changes are transactional.

Examples:

- damage reduces `Health.CurrentValue`,
- healing raises `Health.CurrentValue`,
- shield damage reduces `Shield.CurrentValue`,
- shield boosts raise `Shield.CurrentValue`,
- energy deficit reduces `EnergyStorage.CurrentValue`,
- energy surplus raises `EnergyStorage.CurrentValue`,
- survival consumption reduces survival storage,
- survival production raises survival storage.

After all valid current transactions for the tick are resolved:

```text
CurrentValue = clamp(CurrentValue, 0, CurrentMaxValue)
```

Percentage-based pool `CurrentValue` transactions are resolved against `CurrentMaxValue` unless a future effect schema introduces an explicit percentage basis.

Example:

```text
CurrentMaxValue: 200
CurrentValue: 150
Incoming CurrentValue reduction, Percentage, 0.10
Transaction amount: 200 * 0.10 = 20
CurrentValue after transaction: 130
```

### 9.7.3 Capacity Compensation

When `CurrentMaxValue` changes, the engine applies deterministic capacity compensation.

If `CurrentMaxValue` increases, `CurrentValue` is increased by the same absolute capacity delta, but never above the new `CurrentMaxValue`.

Example:

```text
Previous CurrentMaxValue: 100
New CurrentMaxValue:      120
Previous CurrentValue:     40
New CurrentValue:          60
```

If `CurrentMaxValue` decreases and `CurrentValue` is higher than the new `CurrentMaxValue`, `CurrentValue` is clamped to the new `CurrentMaxValue`.

Example:

```text
Previous CurrentMaxValue: 100
New CurrentMaxValue:       70
Previous CurrentValue:     90
New CurrentValue:          70
```

Capacity compensation is part of capacity resolution. It is not a heal, damage, drain, or restoration effect by itself.

---

## 9.8 Health

`Health` is a fast-lane pool stat.

Value chain:

```text
Health.BaseValue
+ Health passive modifiers
= Health.EffectiveMaxValue

Health.EffectiveMaxValue
+ active Health.CurrentMaxValue modifiers
= Health.CurrentMaxValue

Health.CurrentValue is clamped to [0, Health.CurrentMaxValue]
```

Health starts with a default `BaseValue` of `100` unless overridden by entity configuration.

`Health.CurrentValue` can be raised by healing and regeneration.

`Health.CurrentValue` can be reduced by damage or drain effects.

`HealthRegeneration` is not modeled as a direct Health effect. It is a separate `FlowStat` so passives and active effects can target `HealthRegeneration` itself.

`Health` does not infer `Dead`. If death or revival conditions are needed, the `StateEngine` derives and applies those conditions after stat resolution.

---

## 9.9 Shield

`Shield` is a fast-lane pool stat.

Value chain:

```text
Shield.BaseValue
+ Shield passive modifiers
= Shield.EffectiveMaxValue

Shield.EffectiveMaxValue
+ active Shield.CurrentMaxValue modifiers
= Shield.CurrentMaxValue

Shield.CurrentValue is clamped to [0, Shield.CurrentMaxValue]
```

Shield starts with a default `BaseValue` of `100` unless overridden by entity configuration.

`Shield.CurrentValue` can be raised by shield boosts and shield regeneration.

`Shield.CurrentValue` can be reduced by damage or drain effects.

`ShieldRegeneration` is not modeled as a direct Shield effect. It is a separate `FlowStat` so passives and active effects can target `ShieldRegeneration` itself.

### 9.9.1 Shield Depletion

When `Shield.CurrentValue` reaches `0`, the shield enters cooldown.

While shield cooldown is active:

- `Shield.CurrentValue` remains `0`,
- shield regeneration does not restore Shield,
- shield boosts do not restore Shield,
- damage taken during cooldown does not reset the cooldown,
- active capacity modifiers may still change `Shield.CurrentMaxValue`, but `Shield.CurrentValue` remains clamped to `0` until cooldown ends.

When shield cooldown ends:

- Shield becomes eligible for restoration again,
- `Shield.CurrentValue` remains `0` at the moment cooldown ends,
- `ShieldRegeneration` may then restore Shield through normal regeneration application,
- shield boosts may then raise `Shield.CurrentValue` again.

Shield cooldown duration is controlled by `ShieldRechargeDuration`.

`ShieldRechargeDuration` is an `AttributeStat` representing milliseconds. When used by the runtime, it is converted to deterministic `NetworkTick` values.

---

## 9.10 Energy

Energy consists of three stats:

```text
EnergyStorage
EnergyProduction
EnergyConsumption
```

`EnergyStorage` is a `PoolStat`.

`EnergyProduction` and `EnergyConsumption` are `FlowStat`s.

Energy is a fast-lane system and must participate in fast 30 TPS processing.

### 9.10.1 EnergyStorage

`EnergyStorage` represents stored surplus energy that may be used by skills, afterburner use, ship systems, or other gameplay systems.

Value chain:

```text
EnergyStorage.BaseValue
+ EnergyStorage passive modifiers
= EnergyStorage.EffectiveMaxValue

EnergyStorage.EffectiveMaxValue
+ active EnergyStorage.CurrentMaxValue modifiers
= EnergyStorage.CurrentMaxValue

EnergyStorage.CurrentValue is clamped to [0, EnergyStorage.CurrentMaxValue]
```

Default:

```text
EnergyStorage.BaseValue = 0
```

Energy storage capacity can be raised by passives or by active capacity effects.

`EnergyStorage.CurrentValue` can be raised by surplus production and direct restoration effects.

`EnergyStorage.CurrentValue` can be reduced by production deficit, skill costs, drains, or other valid current transactions.

### 9.10.2 EnergyProduction and EnergyConsumption

`EnergyProduction` describes how much energy is produced over time.

`EnergyConsumption` describes how much energy is consumed over time.

Both are authored as rate stats:

```text
RegenerationValuePerMs
```

The runtime converts these authored values into deterministic tick/application amounts.

```text
AmountPerApplication = RegenerationValuePerMs * RegenerationApplicationInMsRuntime
```

`RegenerationApplicationInMs` is provided globally by `StateEngineConfig` and converted once into `NetworkTick` values during startup.

Runtime simulation must not use wall-clock time for these decisions.

### 9.10.3 Energy Net Flow

Energy net flow is resolved from current effective flow values:

```text
EnergyNetValuePerMs = EnergyProduction.CurrentValue - EnergyConsumption.CurrentValue
```

If the net value is positive:

```text
EnergyStorage.CurrentValue += EnergyNetValuePerMs * RegenerationApplicationInMsRuntime
```

If the net value is negative:

```text
EnergyStorage.CurrentValue -= abs(EnergyNetValuePerMs) * RegenerationApplicationInMsRuntime
```

After applying net flow:

```text
EnergyStorage.CurrentValue = clamp(EnergyStorage.CurrentValue, 0, EnergyStorage.CurrentMaxValue)
```

If `EnergyStorage.CurrentValue` is `0` and energy consumption still exceeds production, dependent systems may stop working. That behavior is outside this Stats PRD and must be handled by the relevant gameplay systems or by `StateEngine` rules outside the stat definition itself.

A stat must not infer `EnergyDepleted` or any other condition by itself.

---

## 9.11 Survival Resources

Survival resources use the same Storage, Production, and Consumption model as Energy.

The survival resource groups are:

```text
OxygenStorage
OxygenProduction
OxygenConsumption

FoodStorage
FoodProduction
FoodConsumption

WaterStorage
WaterProduction
WaterConsumption
```

Storage stats are `PoolStat`s.

Production and Consumption stats are `FlowStat`s.

Survival stats use the slow 15 TPS simulation lane and do not need to be calculated or processed every fast tick.

### 9.11.1 Survival Net Flow

For each survival resource:

```text
NetValuePerMs = Production.CurrentValue - Consumption.CurrentValue
```

If the net value is positive, the corresponding storage stat is raised.

If the net value is negative, the corresponding storage stat is reduced.

The amount applied is based on the global regeneration application interval:

```text
AmountPerApplication = NetValuePerMs * RegenerationApplicationInMsRuntime
```

After application:

```text
Storage.CurrentValue = clamp(Storage.CurrentValue, 0, Storage.CurrentMaxValue)
```

Survival stats must not infer conditions by themselves.

---

## 9.12 FlowStat

A `FlowStat` describes a rate used by another stat process.

Flow stats do not have a pool amount.

Flow stats use these value layers:

```text
BaseValue
EffectiveMaxValue
CurrentValue
```

`CurrentMaxValue` is not meaningful for flow stats and should not be targeted.

Flow resolution:

```text
BaseValue
+ passive modifiers
= EffectiveMaxValue

EffectiveMaxValue
+ active CurrentValue modifiers
= CurrentValue
```

When an active modifier expires or is removed, `CurrentValue` is recalculated from the remaining active modifiers and snaps back toward `EffectiveMaxValue`.

Flow stats:

| Stat | Lane | Description |
|---|---|---|
| `HealthRegeneration` | Fast 30 TPS | Health restoration rate, authored as `RegenerationValuePerMs`. |
| `ShieldRegeneration` | Fast 30 TPS | Shield restoration rate, authored as `RegenerationValuePerMs`. |
| `EnergyProduction` | Fast 30 TPS | Energy production rate, authored as `RegenerationValuePerMs`. |
| `EnergyConsumption` | Fast 30 TPS | Energy consumption rate, authored as `RegenerationValuePerMs`. |
| `OxygenProduction` | Slow 15 TPS | Oxygen production rate, authored as `RegenerationValuePerMs`. |
| `OxygenConsumption` | Slow 15 TPS | Oxygen consumption rate, authored as `RegenerationValuePerMs`. |
| `FoodProduction` | Slow 15 TPS | Food production rate, authored as `RegenerationValuePerMs`. |
| `FoodConsumption` | Slow 15 TPS | Food consumption rate, authored as `RegenerationValuePerMs`. |
| `WaterProduction` | Slow 15 TPS | Water production rate, authored as `RegenerationValuePerMs`. |
| `WaterConsumption` | Slow 15 TPS | Water consumption rate, authored as `RegenerationValuePerMs`. |

---

## 9.13 AttributeStat

An `AttributeStat` describes an effective runtime property.

Attribute stats do not have a pool amount.

Attribute stats use these value layers:

```text
BaseValue
EffectiveMaxValue
CurrentValue
```

`CurrentMaxValue` is not meaningful for attribute stats and should not be targeted.

Attribute resolution:

```text
BaseValue
+ passive modifiers
= EffectiveMaxValue

EffectiveMaxValue
+ active CurrentValue modifiers
= CurrentValue
```

When an active modifier expires or is removed, `CurrentValue` is recalculated from the remaining active modifiers and snaps back toward `EffectiveMaxValue`.

Attribute stats participate in fast 30 TPS processing unless explicitly assigned to the slow lane by stat metadata.

Attribute stats include:

| Stat | Lane | Description |
|---|---|---|
| `Armor` | Fast 30 TPS | Damage reduction input. Effective damage use is outside this Stats section. |
| `Speed` | Fast 30 TPS | Maximum normal movement speed. Effective movement use is outside this Stats section. |
| `Acceleration` | Fast 30 TPS | Normal acceleration. Effective movement use is outside this Stats section. |
| `AfterburnerSpeed` | Fast 30 TPS | Maximum afterburner movement speed. Effective movement use is outside this Stats section. |
| `AfterburnerAcceleration` | Fast 30 TPS | Afterburner acceleration. Effective movement use is outside this Stats section. |
| `AttackSpeed` | Fast 30 TPS | Attack frequency modifier input. Effective attack scheduling is outside this Stats section. |
| `CriticalHitChance` | Fast 30 TPS | Critical hit chance input. Effective combat roll use is outside this Stats section. |
| `CriticalHitBonusDamage` | Fast 30 TPS | Bonus damage applied on critical hits. Effective combat use is outside this Stats section. |
| `BonusCriticalHitChance` | Fast 30 TPS | Additional critical hit chance modifier input. |
| `CooldownReduction` | Fast 30 TPS | Cooldown reduction input. Effective cooldown system use is outside this Stats section. |
| `LifeLeech` | Fast 30 TPS | Life leech input. Effective healing behavior is outside this Stats section. |
| `RetaliationDamage` | Fast 30 TPS | Retaliation damage input. Effective retaliation behavior is outside this Stats section. |
| `ShieldRechargeDuration` | Fast 30 TPS | Shield cooldown duration in milliseconds before shield can regenerate again. |

Damage-kind-specific stats must be represented as concrete stat enum entries so they can still map directly to array indices.

Examples:

```text
BonusDamageKinetic
BonusDamageThermal
BonusDamageExplosive
ResistanceKinetic
ResistanceThermal
ResistanceExplosive
```

The concrete damage-kind stat list is defined by the supported `DamageKind` vocabulary.

---

## 9.14 Regeneration and Application Interval

Regeneration, production, and consumption stats are authored as values per millisecond.

Canonical unit:

```text
RegenerationValuePerMs
```

The global application interval is provided by configuration:

```text
StateEngineConfig.RegenerationApplicationInMs
```

At startup, the runtime converts the configured interval into deterministic `NetworkTick` values.

```text
GlobalRegenerationApplicationTicks = ConvertMsToTicks(RegenerationApplicationInMs)
```

Runtime decisions use ticks only.

No wall-clock time may be used for simulation decisions.

Application amount:

```text
AmountPerApplication = RegenerationValuePerMs * RegenerationApplicationInMsRuntime
```

`RegenerationApplicationInMsRuntime` is the deterministic millisecond duration represented by the converted tick interval.

Regeneration application must be deterministic and lane-aware:

- Health, Shield, and Energy use the fast lane.
- Oxygen, Food, and Water use the slow lane.
- Slow-lane resources are not required to process every fast tick.
- Energy participates in fast-lane processing every fast `NetworkTick`.

Regeneration being represented as a stat does not mean regeneration is an effect. Regeneration stats are separate flow stats so passives and active effects can target them.

---

## 9.15 Active Modifier Resolution

For `CurrentMaxValue` and non-pool `CurrentValue`, active modifiers are resolved in deterministic order:

```text
EffectiveMaxValue
+ active flat modifiers
then additive active percentage modifiers
= resolved active value
```

For pool capacity:

```text
resolved active value = CurrentMaxValue
```

For flow and attribute stats:

```text
resolved active value = CurrentValue
```

Percentage modifiers are additive.

Example:

```text
EffectiveMaxValue: 100
Active flat modifier: +10
Active percentage modifiers: +20% and +30%
Total active percentage: +50%
Resolved value: (100 + 10) * 1.5 = 165
```

The engine must not multiply percentage modifiers together.

---

## 9.16 Tick Resolution Order

For each entity and simulation tick, the StateEngine resolves stats deterministically.

Recommended order:

1. Process incoming active effects and active effect lifetime rules.
2. Recalculate dirty passive stat values into `EffectiveMaxValue`.
3. Collect active stat modifiers from active effects that are valid for the current tick.
4. Resolve `CurrentMaxValue` for dirty or affected pool stats.
5. Apply pool capacity compensation.
6. Resolve active `CurrentValue` for dirty or affected flow and attribute stats.
7. Apply transactional pool `CurrentValue` modifications.
8. Apply due regeneration, production, and consumption operations.
9. Clamp all affected pool `CurrentValue`s to `[0, CurrentMaxValue]`.
10. Mark dirty stats for delta generation.
11. Allow the StateEngine to infer derived state outside the stat model.

Stats themselves do not infer derived state.

Examples of derived state that must not be inferred by the stat object itself:

```text
Health <= 0 => Dead
EnergyStorage == 0 and deficit remains => EnergyDepleted
Shield.CurrentValue == 0 => Shield cooldown state
```

Those rules belong to the `StateEngine` or to gameplay systems outside this Stats section.

---

## 9.17 Dirty Tracking

Stats must support dirty tracking.

Dirty tracking should not be implemented as one object-level boolean per stat object in the hot path.

Preferred approach:

```text
BitSet DirtyStats
BitSet PassiveDirtyStats
BitSet ActiveDirtyStats
```

A stat is dirty if any public runtime layer changed:

```text
EffectiveMaxValue
CurrentMaxValue
CurrentValue
```

`BaseValue` is immutable and should not normally become dirty after registration.

Dirty stats are used to determine whether delta data must be emitted.

---

## 9.18 Stat Bounds

Unless explicitly stated otherwise, stat values are clamped to:

```text
[0, maxFloat]
```

Pool `CurrentValue` is additionally clamped to:

```text
[0, CurrentMaxValue]
```

The stat model does not apply gameplay-specific caps unless those caps are represented as stat metadata or external StateEngine rules.

Examples:

- `CriticalHitChance` may exceed `1.0` unless a combat system or stat metadata clamps it.
- `CooldownReduction` may exceed normal design limits unless a cooldown system or stat metadata clamps it.
- `ResistanceKinetic` may exceed normal design limits unless a damage system or stat metadata clamps it.

---

## 9.19 Stat List

The initial stat list is:

| Stat | Category | Lane | Value Layers |
|---|---|---|---|
| `Health` | PoolStat | Fast 30 TPS | Base, EffectiveMax, CurrentMax, Current |
| `Shield` | PoolStat | Fast 30 TPS | Base, EffectiveMax, CurrentMax, Current |
| `EnergyStorage` | PoolStat | Fast 30 TPS | Base, EffectiveMax, CurrentMax, Current |
| `OxygenStorage` | PoolStat | Slow 15 TPS | Base, EffectiveMax, CurrentMax, Current |
| `FoodStorage` | PoolStat | Slow 15 TPS | Base, EffectiveMax, CurrentMax, Current |
| `WaterStorage` | PoolStat | Slow 15 TPS | Base, EffectiveMax, CurrentMax, Current |
| `HealthRegeneration` | FlowStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `ShieldRegeneration` | FlowStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `EnergyProduction` | FlowStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `EnergyConsumption` | FlowStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `OxygenProduction` | FlowStat | Slow 15 TPS | Base, EffectiveMax, Current |
| `OxygenConsumption` | FlowStat | Slow 15 TPS | Base, EffectiveMax, Current |
| `FoodProduction` | FlowStat | Slow 15 TPS | Base, EffectiveMax, Current |
| `FoodConsumption` | FlowStat | Slow 15 TPS | Base, EffectiveMax, Current |
| `WaterProduction` | FlowStat | Slow 15 TPS | Base, EffectiveMax, Current |
| `WaterConsumption` | FlowStat | Slow 15 TPS | Base, EffectiveMax, Current |
| `Armor` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `Speed` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `Acceleration` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `AfterburnerSpeed` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `AfterburnerAcceleration` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `AttackSpeed` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `CriticalHitChance` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `CriticalHitBonusDamage` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `BonusCriticalHitChance` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `CooldownReduction` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `LifeLeech` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `RetaliationDamage` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `ShieldRechargeDuration` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `BonusDamage<DamageKind>` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |
| `Resistance<DamageKind>` | AttributeStat | Fast 30 TPS | Base, EffectiveMax, Current |

`BonusDamage<DamageKind>` and `Resistance<DamageKind>` are placeholders for concrete enum values generated from the supported `DamageKind` list.

Example concrete enum values:

```text
BonusDamageKinetic
BonusDamageThermal
BonusDamageExplosive
ResistanceKinetic
ResistanceThermal
ResistanceExplosive
```

---

## 9.20 Naming Rules

Stat enum names must use corrected canonical spelling.

Canonical names:

```text
AfterburnerSpeed
AfterburnerAcceleration
CooldownReduction
CriticalHitChance
CriticalHitBonusDamage
BonusCriticalHitChance
LifeLeech
RetaliationDamage
OxygenProduction
FoodProduction
WaterProduction
```

Deprecated or invalid spellings must not be used in new content:

```text
AfterBurnerSpeed
AfterBurnerAcceleration
CoolDownReducation
CoolDownReduction
BonusCriticalHitChange
Lifeleech
OxygenGeneration
FoodGeneration
WaterGeneration
```

Runtime stat enum names must use canonical spelling.

---

## 9.21 Validation Requirements

The stat registry must validate stat metadata at startup.

Validation must check:

- every `StatType` maps to a valid array index,
- every stat has exactly one category,
- every stat has exactly one simulation lane,
- pool stats support `CurrentMaxValue`,
- flow and attribute stats do not target `CurrentMaxValue`,
- active effects do not target `BaseValue`,
- active effects do not target `EffectiveMaxValue`,
- passive modifiers do not target `CurrentValue`,
- passive modifiers do not target `CurrentMaxValue`,
- regeneration, production, and consumption stats use `RegenerationValuePerMs`,
- slow-lane stats are only applied during the slow lane or when explicitly resolved by deterministic StateEngine rules,
- percentage values use decimal fractions,
- all percentage modifiers are additive,
- stat names use canonical spelling.

Invalid stat metadata or invalid stat targets must fail startup validation.

---

## 9.22 Implementation Notes

The stat system must remain deterministic, low-allocation, and array-oriented.

The hot path must avoid:

```text
per-stat heap objects
LINQ in tick resolution
runtime dictionary lookup per stat mutation
reflection
boxing enum values
temporary collection allocation per tick
string logging when TRACE is disabled
```

Scratch buffers should be reused:

```text
PassiveFlatModifiers
PassivePercentModifiers
ActiveFlatModifiers
ActivePercentModifiers
CurrentValueTransactions
DirtyStatIndices
DirtyPoolStats
DirtyFlowStats
DirtyAttributeStats
```

The engine should resolve only dirty, affected, or due stats.

Passives are recalculated only when their source changes.

Active effects are processed every tick, but stat recalculation is scoped to the affected stats.
