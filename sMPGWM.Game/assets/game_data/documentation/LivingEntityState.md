# LivingEntityState

**Source PRD:** `FLU_RPG_StateEgine_PRD`  
**Source Version:** 6.2.3  
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Genre:** 2D Space RPG  
**Network Model:** Host-authoritative or standalone server simulation with rewind/resimulation support  
**Network Tick Rate:** 30 TPS  
**Primary Concern:** Data-oriented authoritative RPG simulation state for living entities

---

## Table of Contents

- [1 Purpose](#1-purpose)
- [2 Core Definition](#2-core-definition)
- [3 Design Goals](#3-design-goals)
- [4 Non-Goals](#4-non-goals)
- [5 Runtime Architecture Context](#5-runtime-architecture-context)
- [6 Ownership and Mutation Authority](#6-ownership-and-mutation-authority)
- [7 State Identity and Tick Semantics](#7-state-identity-and-tick-semantics)
- [8 Required State Sections](#8-required-state-sections)
- [9 Stat State](#9-stat-state)
- [10 Passive State Relationship](#10-passive-state-relationship)
- [11 Active Effect State Relationship](#11-active-effect-state-relationship)
- [12 Condition State](#12-condition-state)
- [13 Derived State](#13-derived-state)
- [14 Read Access Model](#14-read-access-model)
- [15 Snapshot and History Model](#15-snapshot-and-history-model)
- [16 Resimulation Model](#16-resimulation-model)
- [17 Delta-Relevant State](#17-delta-relevant-state)
- [18 Registration Requirements](#18-registration-requirements)
- [19 Determinism Requirements](#19-determinism-requirements)
- [20 Performance and Allocation Requirements](#20-performance-and-allocation-requirements)
- [21 Validation Requirements](#21-validation-requirements)
- [22 Testing and Acceptance Criteria](#22-testing-and-acceptance-criteria)
- [23 Minimal Clarifying Snippets](#23-minimal-clarifying-snippets)

---

## 1 Purpose

`LivingEntityState` defines the authoritative RPG simulation state representation for one living entity.

It bundles all state required by the `StateEngine` to process stats, passive results, active effects, conditions, derived state, dirty state, snapshots, resimulation, and delta generation for a given `NetworkTick`.

The document extracts and consolidates the `LivingEntityState`-relevant requirements from `FLU_RPG_StateEgine_PRD` v6.2.3 into a focused module specification.

---

## 2 Core Definition

A `LivingEntityState` is the data-oriented resolved simulation state of one living entity at one authoritative `NetworkTick`.

It represents:

- stat values after base, passive, and active resolution,
- currently accepted active effects,
- visible condition flags,
- StateEngine-owned condition contributor ownership,
- shield cooldown state,
- dirty state needed for deterministic processing and delta generation,
- metadata needed for rollback/resimulation and snapshot comparison.

`LivingEntityState` is not the living entity itself. It is not a controller, AI brain, UI model, movement model, skill model, item model, or networking transport object.

A `LivingEntity` owns or references a `LivingEntityState`, but the `LivingEntityState` must remain a data-oriented simulation record.

---

## 3 Design Goals

`LivingEntityState` must support:

- host-authoritative 30 TPS simulation,
- optional standalone-server simulation,
- slow-lane 15 TPS simulation for selected stats/effects,
- deterministic processing and replay,
- late-arriving past effect inputs,
- rollback and resimulation from retained states,
- compact authoritative delta generation,
- low memory overhead,
- low or zero avoidable hot-path allocation,
- array-indexed stat access,
- flat active-effect storage,
- bit-flag condition storage,
- dirty tracking for stats, conditions, and effects,
- safe read access by non-mutating systems,
- future client prediction/reconciliation support without requiring it in the first implementation.

The preferred design is data-oriented, compact, cache-friendly, and optimized for bulk processing rather than deep object-oriented encapsulation.

---

## 4 Non-Goals

`LivingEntityState` must not contain or perform:

- gameplay action interpretation,
- target acquisition,
- skill execution logic,
- item logic,
- weapon hit acquisition,
- AI decision making,
- player/controller ownership logic,
- UI or presentation logic,
- network transport/submission logic,
- complex gameplay mutation logic,
- per-stat heap object hierarchies,
- passive source ownership graphs,
- direct wall-clock timing decisions.

Those responsibilities belong to surrounding systems such as gameplay action resolvers, passive source systems, controllers, network transport, presentation, or the `StateEngine` itself.

---

## 5 Runtime Architecture Context

### 5.1 LivingEntity

A `LivingEntity` represents an entity that can be affected by RPG state.

Examples:

- player-controlled ship,
- AI-controlled ship,
- enemy,
- NPC,
- creature,
- friendly entity,
- neutral entity,
- hostile entity.

A `PlayerController` or `AgentController` may own or reference a `LivingEntity` by composition. The `StateEngine` must not depend on the controller, AI, network peer, or gameplay owner that produced the entity.

Every registered `LivingEntity` is processed through the same deterministic state model.

### 5.2 StateEngine

The `StateEngine` is the only system that mutates `LivingEntityState` runtime gameplay state.

The `StateEngine` processes normalized input frames, validates and applies active effects, processes dirty passive results, resolves stats, updates conditions, handles active-effect lifetime, writes dirty metadata, stores snapshots, and prepares delta-relevant state.

### 5.3 StateEngineInputFrame

The `StateEngine` should receive normalized per-tick input instead of raw player or agent actions.

External systems gather host, network, environment, and living-entity-agent action inputs. Those systems validate and resolve actions into effect inputs before the `StateEngine` processes them.

A conceptual `StateEngineInputFrame` contains:

- `NetworkTick`,
- incoming normalized `ActiveEffect` inputs,
- passive dirty events or passive source version changes,
- optional correction/resimulation metadata,
- deterministic ordering metadata where needed.

The `StateEngine` may create deterministic internal effects only as consequences of accepted effects or documented state rules, such as child effects, lifecycle effects, or future derived reaction effects.

### 5.4 Passive Modifier Pipeline

Passive sources are external persistent state.

Examples:

- items,
- talents,
- level-ups,
- ship hull configuration,
- permanent upgrade state,
- other persistent source state.

The passive modifier pipeline resolves passive source changes into stat-layer outputs. `LivingEntityState` stores the resolved result in `EffectiveMaxValues[]` and dirty/version metadata, not the full passive source ownership graph.

### 5.5 LivingEntityStateTracker

`LivingEntityStateTracker` stores retained state history and effect history for rollback/resimulation.

It must provide enough data for a given `NetworkTick` so the `StateEngine` can restore a past state and resimulate to the current tick.

---

## 6 Ownership and Mutation Authority

No gameplay system may modify `LivingEntityState` directly.

Only the `StateEngine` may mutate:

- stats,
- conditions,
- condition contributor ownership,
- active effects,
- shield cooldown state,
- derived state,
- dirty state,
- snapshot state,
- delta-relevant state.

Gameplay systems may create `ActiveEffect` requests or passive source updates. These are inputs, not direct mutations.

The `StateEngine` decides whether an input is valid, whether it can execute, how it stacks, whether it is blocked, how it affects state, and whether the resulting state becomes dirty.

---

## 7 State Identity and Tick Semantics

### 7.1 Required Identity Fields

A `LivingEntityState` must contain:

```text
EntityId
NetworkTick
```

`EntityId` identifies the living entity.

`NetworkTick` identifies the authoritative simulation tick represented by the state.

### 7.2 NetworkTick Rules

The runtime uses a host-synchronized `NetworkTick` value.

```text
NetworkTick type: long
NetworkTick rate: 30 TPS
```

Unless explicitly stated otherwise, `NetworkTick` means the fast 30 TPS StateEngine tick.

All runtime timing decisions use tick values, not wall-clock time.

This includes:

- effect application tick,
- effect duration,
- effect execution interval,
- effect expiry,
- regeneration interval,
- shield recharge cooldown expiry,
- snapshot retention,
- resimulation windows,
- active-effect history.

### 7.3 Slow-Lane Semantics

The slow simulation lane runs at 15 TPS by executing on every second fast `NetworkTick`:

```text
isSlowTick = NetworkTick % 2 == 0
```

Slow-lane processing is intended for lower-frequency state maintenance such as survival resources and slow-lane effects.

`LivingEntityState` does not store a separate slow time base. Slow-lane state is represented through the same `NetworkTick` timeline.

---

## 8 Required State Sections

A `LivingEntityState` must contain the following sections.

```text
EntityId
NetworkTick
BaseValues[]
EffectiveMaxValues[]
CurrentMaxValues[]
CurrentValues[]
ConditionFlags
ConditionContributorRecords
ActiveEffects[]
ActiveEffectCount
ShieldCooldownUntilNetworkTick
DirtyStats
PassiveDirtyStats
ActiveDirtyStats
DirtyConditionState
DirtyEffectState
PassiveVersionMetadata
SnapshotMetadata
```

The exact internal representation may vary, but the implementation must preserve the semantics and performance intent of these sections.

### 8.1 Required Internal Layout Direction

Preferred hot-path layout:

```text
float[] BaseValues
float[] EffectiveMaxValues
float[] CurrentMaxValues
float[] CurrentValues
ulong ConditionFlags
ActiveEffect[] ActiveEffects
int ActiveEffectCount
BitSet DirtyStats
BitSet PassiveDirtyStats
BitSet ActiveDirtyStats
```

The implementation should avoid per-stat heap objects and runtime dictionary lookup for stat mutation.

### 8.2 State Sections by Responsibility

| Section | Purpose | Mutated By | Snapshot Required | Delta Relevant |
|---|---|---|---:|---:|
| Identity | `EntityId`, `NetworkTick` | StateEngine | yes | yes |
| Stat layers | Base, effective max, current max, current | StateEngine | yes | yes |
| Passive metadata | Dirty/version tracking for passive results | StateEngine/passive pipeline boundary | yes | sometimes |
| Active effects | Currently accepted active runtime effects | StateEngine | yes | yes |
| Conditions | Aggregated flags and contributor records | StateEngine | yes | yes |
| Derived state | Shield cooldown, StateEngine-owned derived conditions | StateEngine | yes | yes |
| Dirty metadata | Tracks changed stats/conditions/effects | StateEngine | yes, when needed for replay | yes |
| Snapshot metadata | History/ring-buffer support | StateEngineTracker | yes | no |

---

## 9 Stat State

Stats describe numeric properties of a living entity.

Stats contain no gameplay decision logic. They do not infer conditions, stop systems, select targets, apply damage rules, or execute gameplay behavior.

Only the `StateEngine` may mutate runtime stat values.

### 9.1 Stat Array Contract

`StatType` enum values must map directly to stat array indices.

All stat arrays must use the same index mapping.

```text
index = (int)StatType
```

The implementation must validate at startup that every `StatType` maps to a valid array index.

### 9.2 Runtime Value Layers

Each stat uses deterministic value layers.

| Value Layer | Meaning |
|---|---|
| `BaseValue` | Natural/default value provided when the entity is registered. Runtime mutation is out of scope. |
| `EffectiveMaxValue` | Passive-adjusted value calculated from `BaseValue` and passive modifiers. |
| `CurrentMaxValue` | Active runtime capacity limit for pool stats. |
| `CurrentValue` | Current runtime value. For pool stats, this is the actual amount. For flow and attribute stats, this is the active-resolved runtime value. |

### 9.3 BaseValues

`BaseValues[]` are initialized before or during registration.

Defaults:

| Stat | Default BaseValue |
|---|---:|
| `Health` | `100` |
| `Shield` | `100` |
| `EnergyStorage` | `0` |
| `OxygenStorage` | `0` |
| `FoodStorage` | `0` |
| `WaterStorage` | `0` |
| All other stats | Provided by ship, entity, item, talent, or content definition. |

After registration, `BaseValues[]` are treated as stable runtime input values.

### 9.4 EffectiveMaxValues

`EffectiveMaxValues[]` store resolved passive stat values.

Passive resolution order:

```text
BaseValue
+ passive flat modifiers
then additive passive percentage modifiers
= EffectiveMaxValue
```

If no passive modifier exists for a stat:

```text
EffectiveMaxValue = BaseValue
```

Passive modifiers do not target `CurrentValue` or `CurrentMaxValue`.

### 9.5 CurrentMaxValues

`CurrentMaxValues[]` store active runtime capacity for pool stats.

Pool capacity resolution:

```text
EffectiveMaxValue
+ active CurrentMaxValue flat modifiers
then additive active CurrentMaxValue percentage modifiers
= CurrentMaxValue
```

`CurrentMaxValue` is valid only for `PoolStat` capacity modifiers.

Flow and attribute stats must not target `CurrentMaxValue`.

### 9.6 CurrentValues

`CurrentValues[]` store the active-resolved current value for each stat.

For pool stats, `CurrentValue` is the actual amount in the pool.

For flow and attribute stats, `CurrentValue` is the effective runtime value after active modifiers.

Active modifier resolution for non-pool `CurrentValue`:

```text
EffectiveMaxValue
+ active flat CurrentValue modifiers
then additive active percentage CurrentValue modifiers
= CurrentValue
```

Pool `CurrentValue` changes are transactional.

Examples:

- damage reduces `Health.CurrentValue`,
- healing raises `Health.CurrentValue`,
- shield damage reduces `Shield.CurrentValue`,
- shield boosts raise `Shield.CurrentValue`,
- energy deficit reduces `EnergyStorage.CurrentValue`,
- energy surplus raises `EnergyStorage.CurrentValue`,
- survival consumption reduces survival storage,
- survival production raises survival storage.

After transactions:

```text
Pool CurrentValue = clamp(CurrentValue, 0, CurrentMaxValue)
```

### 9.7 Stat Categories

Stats are grouped by behavior.

```text
PoolStat
FlowStat
AttributeStat
```

#### PoolStat

Pool stats use:

```text
BaseValue
EffectiveMaxValue
CurrentMaxValue
CurrentValue
```

Pool stats:

| Stat | Lane | Description |
|---|---|---|
| `Health` | Fast 30 TPS | Entity health pool. |
| `Shield` | Fast 30 TPS | Shield pool with cooldown behavior. |
| `EnergyStorage` | Fast 30 TPS | Stored surplus energy. |
| `OxygenStorage` | Slow 15 TPS | Stored oxygen survival resource. |
| `FoodStorage` | Slow 15 TPS | Stored food survival resource. |
| `WaterStorage` | Slow 15 TPS | Stored water survival resource. |

#### FlowStat

Flow stats use:

```text
BaseValue
EffectiveMaxValue
CurrentValue
```

Flow stats do not have a meaningful `CurrentMaxValue`.

Flow stats:

| Stat | Lane | Description |
|---|---|---|
| `HealthRegeneration` | Fast 30 TPS | Health restoration rate. |
| `ShieldRegeneration` | Fast 30 TPS | Shield restoration rate. |
| `EnergyProduction` | Fast 30 TPS | Energy production rate. |
| `EnergyConsumption` | Fast 30 TPS | Energy consumption rate. |
| `OxygenProduction` | Slow 15 TPS | Oxygen production rate. |
| `OxygenConsumption` | Slow 15 TPS | Oxygen consumption rate. |
| `FoodProduction` | Slow 15 TPS | Food production rate. |
| `FoodConsumption` | Slow 15 TPS | Food consumption rate. |
| `WaterProduction` | Slow 15 TPS | Water production rate. |
| `WaterConsumption` | Slow 15 TPS | Water consumption rate. |

#### AttributeStat

Attribute stats use:

```text
BaseValue
EffectiveMaxValue
CurrentValue
```

Attribute stats do not have a meaningful `CurrentMaxValue`.

Attribute stats include:

| Stat | Lane | Description |
|---|---|---|
| `Armor` | Fast 30 TPS | Damage reduction input. |
| `Speed` | Fast 30 TPS | Maximum normal movement speed. |
| `Acceleration` | Fast 30 TPS | Normal acceleration. |
| `AfterburnerSpeed` | Fast 30 TPS | Maximum afterburner movement speed. |
| `AfterburnerAcceleration` | Fast 30 TPS | Afterburner acceleration. |
| `AttackSpeed` | Fast 30 TPS | Attack frequency modifier input. |
| `CriticalHitChance` | Fast 30 TPS | Critical hit chance input. |
| `CriticalHitBonusDamage` | Fast 30 TPS | Bonus damage applied on critical hits. |
| `BonusCriticalHitChance` | Fast 30 TPS | Additional critical hit chance modifier input. |
| `CooldownReduction` | Fast 30 TPS | Cooldown reduction input. |
| `LifeLeech` | Fast 30 TPS | Life leech input. |
| `RetaliationDamage` | Fast 30 TPS | Retaliation damage input. |
| `ShieldRechargeDuration` | Fast 30 TPS | Shield cooldown duration in milliseconds before Shield can regenerate again. |
| `BonusDamage<DamageKind>` | Fast 30 TPS | Damage-kind-specific bonus damage placeholder. |
| `Resistance<DamageKind>` | Fast 30 TPS | Damage-kind-specific resistance placeholder. |

`BonusDamage<DamageKind>` and `Resistance<DamageKind>` must be represented as concrete enum entries so they still map directly to array indices.

Example concrete entries:

```text
BonusDamageKinetic
BonusDamageThermal
BonusDamageExplosive
ResistanceKinetic
ResistanceThermal
ResistanceExplosive
```

### 9.8 Capacity Compensation

When `CurrentMaxValue` changes, the `StateEngine` applies deterministic capacity compensation.

If `CurrentMaxValue` increases, `CurrentValue` increases by the same absolute capacity delta, but never above the new `CurrentMaxValue`.

If `CurrentMaxValue` decreases and `CurrentValue` is greater than the new `CurrentMaxValue`, `CurrentValue` is clamped to the new `CurrentMaxValue`.

Capacity compensation is not heal, damage, drain, or restoration. It is part of capacity resolution.

### 9.9 Regeneration and Net Flow

Regeneration, production, and consumption stats are authored as values per millisecond.

Canonical unit:

```text
RegenerationValuePerMs
```

The runtime converts configured millisecond intervals into deterministic tick values at startup.

Runtime simulation uses ticks only.

Application amount:

```text
AmountPerApplication = RegenerationValuePerMs * RegenerationApplicationInMsRuntime
```

Health, Shield, and Energy use the fast lane.

Oxygen, Food, and Water use the slow lane.

Energy participates in fast-lane processing every fast `NetworkTick`.

### 9.10 Dirty Tracking

`LivingEntityState` must support stat dirty tracking.

Preferred fields:

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

Dirty stats are used for scoped stat resolution and delta generation.

### 9.11 Stat Bounds

Unless explicitly stated otherwise, stat values are clamped to:

```text
[0, maxFloat]
```

Pool `CurrentValue` is additionally clamped to:

```text
[0, CurrentMaxValue]
```

Gameplay-specific caps should be represented as stat metadata or external `StateEngine` rules, not hardcoded into stat objects.

### 9.12 Canonical Stat Naming

Runtime stat enum names must use canonical spelling.

Canonical names include:

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

Deprecated spellings must not be used in new runtime content:

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

---

## 10 Passive State Relationship

Passives are permanent or persistent stat sources, but they are not active runtime effects.

`LivingEntityState` stores passive results, not full passive source ownership.

Required state behavior:

- `EffectiveMaxValues[]` store resolved passive-adjusted values.
- `PassiveDirtyStats` marks stats whose passive layer must be recalculated.
- Passive recalculation happens only when a passive source is added, removed, replaced, or dirtied by its source state.
- Passives must not be recalculated every tick.
- Passive modifiers write into `EffectiveMaxValue` only.
- Passive source objects and source ownership stay outside `LivingEntityState`.

Recommended metadata:

```text
PassiveInputVersion
PassiveResolvedVersion
PassiveDirtyStats
```

The implementation may use equivalent version or revision metadata to determine whether the resolved passive state inside `LivingEntityState` is current for the processed tick.

During resimulation, passive source changes must be available as deterministic input/history events or already represented in the retained state snapshot.

---

## 11 Active Effect State Relationship

`LivingEntityState` stores currently accepted active runtime effects.

Preferred storage:

```text
ActiveEffect[] ActiveEffects
int ActiveEffectCount
```

Active effects are runtime records. They reference immutable `EffectDefinition` data by ID and store runtime-only execution state.

Active effects should be compact pooled structs or equivalent data-oriented records.

Canonical active-effect fields relevant to state storage include:

| Field | Meaning |
|---|---|
| `EffectRuntimeId` | Unique runtime ID for this active effect instance. |
| `EffectDefinitionId` | Immutable definition lookup key. |
| `SourceId` | Concrete source that produced the effect. |
| `OriginId` | Owner or originator behind the source. |
| `TargetEntityId` | Entity receiving the effect. |
| `AppliedNetworkTick` | Tick when the effect was accepted/applied. |
| `LastExecutedNetworkTick` | Last tick when execution produced work. |
| `LastTriggeredNetworkTick` | Last tick when trigger evaluation succeeded. |
| `ExpiresNetworkTick` | Expiration tick for temporary effects. |
| `StackCount` | Current stack count where stack-count representation is used. |
| `AppliedTickCount` | Number of due ticks applied by accumulate-while-active behavior. |
| `AccumulatedFlatModifier` | Runtime flat accumulator. |
| `AccumulatedPercentageModifier` | Runtime percentage accumulator. |
| `ParentEffectRuntimeId` | Parent runtime effect ID for child effects. |
| `ChildDepth` | Depth in the child effect graph. |
| `HasFiredOnFirstApplication` | Lifecycle guard. |
| `HasFiredOnFirstExecution` | Lifecycle guard. |
| `RuntimeFlags` | Implementation-specific runtime flags. |

Duplicate `EffectRuntimeId` values are invalid and must be rejected before stacking logic.

`EffectRuntimeId` is also used as a deterministic tie-breaker for removal and overflow ordering.

### 11.1 Flat Child Effect Storage

Child effects are stored as separate active-effect entries.

They are not stored as nested runtime collections.

Each child effect stores:

```text
ParentEffectRuntimeId
ChildDepth
```

This keeps iteration fast, avoids recursive traversal, avoids child collection allocation, supports deterministic ordering, and supports rollback/resimulation.

### 11.2 Active Effect Classifications

Effect classifications are metadata attached to accepted `ActiveEffect` instances.

Classifications are not stored as conditions on `LivingEntityState`.

Classifications disappear automatically when the corresponding active effect expires or is removed.

---

## 12 Condition State

`LivingEntityCondition` is the condition vocabulary used by the `StateEngine`.

Conditions can represent:

- current entity state,
- effect blocking input,
- effect trigger input,
- effect-removal matching input,
- UI/debug state where explicitly exposed.

Stats never infer conditions by themselves.

The `StateEngine` may derive or remove conditions after stat resolution as deterministic state rules.

### 12.1 ConditionFlags

Runtime active conditions are stored as bit flags.

Preferred backing type:

```text
ulong ConditionFlags
```

`LivingEntityState` exposes only whether a condition is present.

### 12.2 Condition Contributor Ownership

Condition contributor ownership is tracked internally by the `StateEngine` where effect-granted conditions can have multiple contributors.

Example:

```text
Condition: Poisoned
Contributors:
- EffectRuntimeId A
- EffectRuntimeId B
```

Removing one contributor removes only that contribution unless a request uses `RemoveAllContributions`.

A condition remains visible while at least one contributor still owns it.

Condition hierarchy metadata is static registry data and must not be stored redundantly in every `LivingEntityState`.

### 12.3 Initial Condition List

Initial `LivingEntityCondition` values:

```text
None
Dead
Immobilized
Invulnerable
Untargetable
EnergyDepleted
Undetectable
Invisible
Poisoned
PoisonExposure
Burning
Infected
Revived
ShieldCooldown
```

`Alive` is intentionally not a condition.

If an entity is not `Dead`, it is considered alive enough for `StateEngine` processing.

Clean state is `None`.

Lifecycle states such as `Spawned`, `Initialized`, `wait_for_revive`, and `ready_for_revive` are out of scope.

### 12.4 Condition Mutation

Effects do not change conditions directly.

Effects may request condition additions or removals through condition requests.

The `StateEngine` validates and applies those requests if valid.

Suggested runtime request buffers:

```text
ConditionAddRequest[]
ConditionRemoveRequest[]
ConditionContributorRecords[]
```

---

## 13 Derived State

Derived state belongs to the `StateEngine`, not to stat objects.

`LivingEntityState` stores the result of derived state where required.

### 13.1 Dead

Initial derived rule:

| Rule | Timing | Result |
|---|---|---|
| `Health.CurrentValue <= 0` | After stat resolution | Add or keep `Dead`. |
| `Health.CurrentValue > 0` and revive/condition removal permits it | After stat resolution | Remove `Dead`. |

`Dead` blocks effects by default.

Effects that can trigger while dead must explicitly opt in through their effect definition.

### 13.2 Shield Cooldown

`LivingEntityState` stores:

```text
ShieldCooldownUntilNetworkTick
```

When `Shield.CurrentValue` reaches `0`, the shield enters cooldown.

While shield cooldown is active:

- `Shield.CurrentValue` remains `0`,
- shield regeneration does not restore Shield,
- shield boosts do not restore Shield,
- damage taken during cooldown does not reset the cooldown,
- active capacity modifiers may still change `Shield.CurrentMaxValue`,
- `Shield.CurrentValue` remains clamped to `0` until cooldown ends.

When shield cooldown ends:

- Shield becomes eligible for restoration again,
- `Shield.CurrentValue` remains `0` at the moment cooldown ends,
- Shield regeneration may then restore Shield through normal regeneration application,
- shield boosts may then raise `Shield.CurrentValue` again.

Derived condition rules:

| Rule | Timing | Result |
|---|---|---|
| `Shield.CurrentValue <= 0` and shield cooldown is not already active | After shield transaction resolution | Start shield cooldown and add `ShieldCooldown`. |
| Shield cooldown expiry reached | At due tick | Remove `ShieldCooldown`; Shield may be restored later. |

`ShieldRechargeDuration` is an attribute stat authored in milliseconds and converted into deterministic tick values for runtime use.

### 13.3 EnergyDepleted

Energy depletion must not be inferred by the stat object itself.

If `EnergyStorage.CurrentValue == 0` and energy consumption still exceeds production, dependent systems or documented `StateEngine` rules may use that information.

The stat model itself must not infer `EnergyDepleted` automatically unless the rule is explicitly owned by the `StateEngine`.

---

## 14 Read Access Model

The hot path may use direct array access inside the `StateEngine`.

External systems should read through read-only views or query helpers rather than mutating raw arrays.

Recommended model:

| Reader | Access |
|---|---|
| `StateEngine` hot path | Direct internal array access. |
| Gameplay systems | Read-only state view or query API. |
| UI/debug systems | Read-only view, snapshot view, or debug inspection path. |
| Tests | Controlled direct inspection may be allowed. |

This preserves data-oriented efficiency while preventing accidental mutation outside the `StateEngine`.

A read-only view may expose:

- current stat values,
- condition presence,
- active-effect summaries,
- current `NetworkTick`,
- dirty-state summaries where useful for tests/debugging.

The read model must not force per-stat heap objects or dictionary lookups into the hot path.

---

## 15 Snapshot and History Model

### 15.1 Current State and State Frames

`LivingEntityState` represents resolved simulation state at a specific `NetworkTick`.

The latest state is the current mutable state.

A retained historical copy is a state frame.

Recommended terminology:

```text
LivingEntityState      = current mutable state or generic state contract
LivingEntityStateFrame = retained snapshot copy for one entity at one NetworkTick
LivingEntityStateHistory = entity-centric retained frame ring
```

### 15.2 Snapshot Purpose

Snapshots are required for:

- restoring a past tick when late effects arrive,
- resimulating from corrected ticks,
- deleting obsolete history,
- generating authoritative deltas after rollback/resimulation,
- comparing predicted and corrected states later,
- deterministic replay tests.

### 15.3 Snapshot Retention

The implementation must expose snapshot and effect-history retention through configuration.

Default:

```text
SnapshotRetentionTicks = 45
```

The first implementation should prefer full entity-state snapshots per retained tick for correctness and fast restore.

Later optimization may store full snapshots only for changed entities or share unchanged frame references, but the initial model must remain deterministic and easy to validate.

### 15.4 Snapshot Fields

A state frame must provide everything required for a given `NetworkTick` and applied effects for that tick to resimulate up to the current tick.

Required snapshot fields:

```text
EntityId
NetworkTick
BaseValues[]
EffectiveMaxValues[]
CurrentMaxValues[]
CurrentValues[]
ConditionFlags
ConditionContributorRecords
ActiveEffects[]
ActiveEffectCount
ShieldCooldownUntilNetworkTick
Dirty state metadata required for replay
Passive version or dirty metadata required for replay
```

Snapshots should use arrays and compact records, not per-stat objects.

### 15.5 History Lookup

The history system should support both:

```text
EntityId + NetworkTick -> LivingEntityStateFrame
NetworkTick -> changed or relevant entities for that tick
```

Entity-centric storage is preferred for first implementation:

```text
EntityId -> fixed-size ring buffer of LivingEntityStateFrame
```

Tick index metadata may be maintained to quickly find changed entities or replay ranges for a corrected tick.

### 15.6 Effect History

State history alone is insufficient for deterministic resimulation.

The tracker must also retain:

```text
Applied ActiveEffects for the tick
Accepted/rejected effect history where needed for deterministic replay
Delta comparison metadata
```

Accepted/rejected decision history is required where replay could otherwise diverge due to duplicate IDs, stacking decisions, blocking, or removal ordering.

Raw gameplay actions may remain outside the `StateEngine`; the StateEngine history should retain normalized effect inputs and deterministic processing decisions.

---

## 16 Resimulation Model

The host-authoritative or standalone server runs at 30 TPS.

Before the `StateEngine` processes a tick:

1. Host/server gathers arrived action inputs from external systems, players, environment, and living-entity agents.
2. Upstream systems validate or authorize actions as needed.
3. Actions are converted into normalized active-effect inputs and passive dirty events.
4. The resulting `StateEngineInputFrame` is passed to the `StateEngine`.

The `StateEngine` then:

1. Sorts incoming effects by `AppliedNetworkTick` and deterministic tie-breakers.
2. Groups effects by `TargetEntityId`.
3. Detects whether any accepted effect belongs to a retained past tick.
4. Restores the retained snapshot for the earliest affected tick when resimulation is required.
5. Applies accepted incoming effects for the relevant tick.
6. Replays retained effect history and state rules forward to the current tick.
7. Produces the new current authoritative `LivingEntityState`.
8. Marks dirty stats, conditions, and effects.
9. Stores new state snapshots and effect history.
10. Prepares delta-relevant state for delta generation.

The resimulation window is:

```text
current NetworkTick - corrected delta NetworkTick
```

The `StateEngine` must retain enough snapshots and applied effect history to replay this window deterministically.

### 16.1 Deterministic Effect Ordering

Incoming effects may be provided for the current tick or past retained ticks.

Processing order:

1. Order incoming effects by `AppliedNetworkTick`.
2. Group effects by `TargetEntityId`.
3. For each affected tick, restore the retained snapshot if resimulation is required.
4. Apply accepted incoming effects for that tick.
5. Resimulate forward to the current tick.
6. Discard stored states older than the configured retention window.

Per-tick effect ordering:

1. Existing active effects by stable insertion sequence.
2. New incoming effects by passed buffer order after sorting by tick.
3. Child effects by parent execution order and child list order.
4. Removal requests by documented removal ordering rules.

Stable ordering keys:

```text
AppliedNetworkTick
InsertionSequence
ParentRuntimeEffectId
ChildSequence
EffectRuntimeId
```

### 16.2 Tick Lifecycle State Updates

Per host state tick, the `StateEngine` must update `LivingEntityState` through a deterministic lifecycle:

1. Select current `NetworkTick`.
2. Determine whether slow-lane processing is due.
3. Take registered mutable entity states.
4. Clear per-tick scratch buffers without deallocation.
5. Apply dirty passive source changes and resolve affected `EffectiveMaxValue` values.
6. Add incoming active effects to entity effect buffers in deterministic order.
7. Resolve stack policies without allocation.
8. Fire `OnFirstApplication` condition requests for accepted active effects.
9. Evaluate blocking conditions and classifications.
10. Evaluate triggers and due intervals.
11. Execute due active effects in deterministic order.
12. Fire `OnFirstExecution` condition requests after each first successful execution.
13. Queue child effects into the same tick buffer in deterministic list order.
14. Collect stat transactions, active modifier contributions, condition requests, and effect-removal requests.
15. Resolve active modifiers using the stat model.
16. Apply regeneration and net-flow application when due.
17. Update shield cooldown state.
18. Derive StateEngine-owned conditions such as `Dead` after stat resolution.
19. Expire or retain active effects using deterministic removal.
20. Fire `OnExpire`, `OnRemove`, and `OnExpireOrRemove` condition requests as applicable.
21. Remove expired active-effect classifications automatically.
22. Mark dirty stats, conditions, and effects.
23. Emit or prepare delta data only if state is dirty.
24. Store snapshot and applied-effect history for rollback/resimulation.

---

## 17 Delta-Relevant State

Delta transport/submission is out of scope for this document, but `LivingEntityState` must expose enough dirty metadata for delta creation.

Delta generation should be able to detect and write:

- changed stats,
- changed condition flags,
- added active effects,
- removed active effects,
- updated active effect stack counts,
- updated active effect expiry or remaining ticks,
- accumulated modifier changes where required,
- passive layer changes where required.

Suggested dirty masks:

```text
DirtyStats
DirtyConditionState
DirtyEffectState
PassiveDirtyStats
ActiveDirtyStats
```

The delta payload must include final authoritative changed state, not only operations.

Suggested final state payload sections:

```text
ChangedStats
ChangedConditions
ChangedEffects
```

`LivingEntityState` must not depend on `Godot.Collections.Array` internally for delta representation.

Preferred public boundary for delta remains `PackedByteArray`; internal delta writing should use pooled byte buffers or equivalent deterministic binary layout.

---

## 18 Registration Requirements

When a living entity is registered:

```text
BaseValues[] must be initialized.
EffectiveMaxValues[] may be initialized or derived from BaseValues[] during registration.
CurrentMaxValues[] may be initialized or derived from EffectiveMaxValues[] during registration.
CurrentValues[] must be clamped to [0, CurrentMaxValue] for PoolStats.
StatType enum order must match all stat arrays.
ConditionFlags must be initialized to a valid condition state.
ActiveEffects[] must be initialized to an empty or valid active effect buffer.
ActiveEffectCount must match the active buffer contents.
Dirty bitsets must be initialized consistently.
NetworkTick must be initialized to the registration tick or initial simulation tick.
```

Registration starts StateEngine tracking for the entity.

---

## 19 Determinism Requirements

`LivingEntityState` processing must produce identical results for identical inputs.

Requirements:

- Use `long NetworkTick` values.
- Avoid wall-clock time in simulation decisions.
- Convert authored milliseconds to ticks once before runtime simulation.
- Avoid unordered collection iteration in simulation.
- Use stable insertion order for active effects.
- Use deterministic child effect order.
- Use deterministic overflow behavior.
- Use deterministic removal ordering.
- Use additive percentage modifiers.
- Resolve stats in a documented order.
- Compare corrected states using the configured precision rule.
- Compare conditions by exact flag match.
- Avoid random values unless seeded deterministically.

If random behavior is required later, seeds must be derived from deterministic inputs such as:

```text
NetworkTick
EntityId
EffectRuntimeId
SourceId
OriginId
```

---

## 20 Performance and Allocation Requirements

`LivingEntityState` must remain deterministic, low-allocation, and array-oriented.

The hot path must avoid:

```text
per-stat heap objects
LINQ in tick resolution
runtime dictionary lookup per stat mutation
reflection
boxing enum values
temporary collection allocation per tick
string logging when TRACE is disabled
per-tick Godot.Collections.Array allocation
nested ActiveEffect child collections
Queue<ActiveEffect> in the hot path
```

Per-tick scratch buffers should be:

- preallocated,
- reused,
- cleared without deallocation,
- sized by configuration,
- protected by deterministic overflow behavior,
- used wherever possible.

Relevant scratch buffers include:

```text
PendingEffectsThisTick
StatModificationRecords
ActiveFlatCurrentModifiers
ActivePercentCurrentModifiers
ActiveFlatCurrentMaxModifiers
ActivePercentCurrentMaxModifiers
DirtyStatIndices
ConditionAddRequests
ConditionRemoveRequests
ConditionContributorRecords
EffectRemovalRequests
ChildEffectQueue
DeltaWriterBuffer
PassiveFlatModifiers
PassivePercentModifiers
CurrentValueTransactions
DirtyPoolStats
DirtyFlowStats
DirtyAttributeStats
```

### 20.1 Target Scale

Initial target scale:

```text
Max living entities active at once: 250
Average active effects per entity: 35
High-load player active effects: 70 or more
Max incoming effects per entity per tick: 30
Max child effects per entity per tick: 3
MaxChildEffectDepth: 5
Target platform: desktop PC
```

The implementation must not assume active effects are evenly distributed across entities.

---

## 21 Validation Requirements

Validation must fail fast before normal gameplay simulation when state metadata, stat metadata, or effect metadata is invalid.

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
- slow-lane stats are only applied during the slow lane or explicitly resolved by deterministic rules,
- percentage values use decimal fractions,
- all percentage modifiers are additive,
- stat names use canonical spelling,
- active-effect buffers do not contain duplicate runtime IDs,
- active-effect target IDs match registered living entities,
- active-effect definitions exist in the registry,
- condition flags contain only valid condition bits,
- condition contributor records reference valid conditions and runtime effect IDs,
- snapshot frames contain enough state to restore and resimulate.

---

## 22 Testing and Acceptance Criteria

### 22.1 Simulation Correctness

Tests must verify that predefined registered living entities and predefined active-effect inputs produce expected final `LivingEntityState` values after a fixed number of ticks.

Expected outputs include:

- final stat values,
- condition flags,
- active-effect additions/removals,
- dirty stat masks,
- delta-relevant changed state.

### 22.2 Deterministic Replay

Given identical initial states, effect inputs, passive dirty events, and tick ranges, repeated runs must produce identical final state.

### 22.3 Rollback and Resimulation

Tests must verify that a past effect applied within the retained window can restore the correct snapshot, resimulate to the current tick, and produce the same final state as a run that had the effect at the correct time originally.

Default test window:

```text
45 rollback/resimulation ticks
```

Cap test:

```text
60 rollback/resimulation ticks
```

### 22.4 Dirty Tracking and Delta Round Trip

Tests must verify that changed stats, conditions, and effects are marked dirty and that unchanged state does not produce unnecessary delta payload.

Delta round-trip tests should verify that applying the final authoritative changed state produces equivalent state on the receiving side.

### 22.5 Condition Rebuild

Tests must verify contributor-owned conditions.

Example:

```text
Poisoned contributor A removed -> Poisoned remains if contributor B still exists.
Poisoned contributor B removed -> Poisoned disappears when no contributors remain.
RemoveAllContributions -> Poisoned disappears and all contributor records are cleared.
```

### 22.6 Death and Revive

Tests must verify:

- `Health.CurrentValue <= 0` adds or keeps `Dead`,
- normal heal cannot trigger while `Dead` unless explicitly allowed,
- revive can trigger while `Dead` when configured,
- `Dead` is removed only when permitted and Health is positive,
- `Dead` is re-applied if Health remains `<= 0` after stat resolution.

### 22.7 Shield Cooldown

Tests must verify:

- Shield reaching `0` starts cooldown,
- `ShieldCooldown` condition is added,
- Shield regeneration and boosts do not restore Shield during cooldown,
- damage during cooldown does not reset cooldown,
- cooldown expiry removes `ShieldCooldown`,
- Shield remains `0` until later regeneration or boost applies.

### 22.8 No-Allocation Hot Path

Tests must verify no avoidable hot-path allocations under selected stress scale.

Milestone 1 stress scale:

```text
150 active living entities
35 average active effects per entity
At least one player-like entity with 70 active effects
20 incoming effects per entity per tick stress case
3 child effects per entity per tick stress case
5 max child effect depth overflow test
45 rollback/resimulation ticks default test
60 rollback/resimulation ticks cap test
50 simulated steps
```

### 22.9 Stat Layer Tests

Tests must verify:

- passive flat and additive percentage resolution,
- active flat and additive percentage resolution,
- pool capacity compensation,
- pool current-value transactions,
- flow and attribute active value resolution,
- stat bounds and clamping,
- slow-lane processing rules,
- regeneration and net-flow application.

---

## 23 Minimal Clarifying Snippets

The snippets below clarify data shape only. They are not a full implementation plan.

### 23.1 Array-Indexed Stat Access

```csharp
var index = (int)StatType.Health;
var currentHealth = state.CurrentValues[index];
var currentMaxHealth = state.CurrentMaxValues[index];
var effectiveMaxHealth = state.EffectiveMaxValues[index];
```

### 23.2 Conceptual Input Boundary

```csharp
public readonly struct StateEngineInputFrame
{
    public readonly long NetworkTick;
    public readonly ReadOnlyMemory<ActiveEffect> IncomingEffects;
    public readonly ReadOnlyMemory<PassiveDirtyEvent> PassiveDirtyEvents;
    public readonly CorrectionInfo Correction;
}
```

### 23.3 Conceptual State/History Terminology

```text
LivingEntityState
  Mutable current state for one entity at one NetworkTick.

LivingEntityStateFrame
  Retained copy of LivingEntityState for rollback/resimulation.

LivingEntityStateHistory
  Entity-centric ring buffer of retained LivingEntityStateFrame records.
```

---

## Final Contract

`LivingEntityState` is the authoritative, data-oriented, resolved simulation state for one living entity at one `NetworkTick`.

It must remain efficient, deterministic, snapshot-friendly, and StateEngine-owned.

It stores resolved stat layers, condition flags, condition contributors, active effects, shield cooldown state, dirty metadata, and snapshot-relevant metadata.

It does not own gameplay behavior, raw action interpretation, passive source graphs, UI logic, controller logic, or network transport.

