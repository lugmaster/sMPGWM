# PRD: RPG State Engine

**Version:** 6.2.3  
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Multiplayer:** Steam Multiplayer Peer integration is out of scope  
**Genre:** 2D Space RPG  
**Network Model:** Client-Server Network Model with server-authoritative rewind and client-side prediction / reconciliation support  
**Network Ticks per Second:** 30 TPS  
**Execution Context:** Host-authoritative gameplay state simulation  
**Primary Output:** Compact packed delta data, optimized for full round-trip generation and application  
**Target Platform:** Desktop PC  
**Target Active Entities:** 250 living entities  
**Document Usage:** Agentic implementation workflow, technical planning, test generation, PRD review, and iterative implementation guidance


---

## Table of Contents

- [1 Goal](#1-goal)
- [2 Scope](#2-scope)
    - [2.1 In Scope](#21-in-scope)
    - [2.2 Out of Scope](#22-out-of-scope)
- [3 First Use Case](#3-first-use-case)
- [4 Tick Model](#4-tick-model)
    - [4.1 Godot Physics Loop](#41-godot-physics-loop)
    - [4.2 NetworkTick](#42-networktick)
    - [4.3 Slow Simulation](#43-slow-simulation)
    - [4.4 Durations](#44-durations)
- [5 Core Runtime Model](#5-core-runtime-model)
- [6 Core Architecture](#6-core-architecture)
    - [6.1 High-Level Flow](#61-high-level-flow)
    - [6.2 Core Rule](#62-core-rule)
- [7 Entity Model](#7-entity-model)
    - [7.1 Player Controller Composition](#71-player-controller-composition)
    - [7.2 LivingEntity](#72-livingentity)
- [8 LivingEntityState](#8-livingentitystate)
- [9 Stats](#9-stats)
    - [9.1 Goals](#91-goals)
    - [9.2 Runtime Value Layers](#92-runtime-value-layers)
    - [9.3 Base Values](#93-base-values)
    - [9.4 Passive Modifiers](#94-passive-modifiers)
    - [9.5 Active Effects](#95-active-effects)
    - [9.6 Stat Categories](#96-stat-categories)
    - [9.7 PoolStat](#97-poolstat)
        - [9.7.1 Pool Capacity Resolution](#971-pool-capacity-resolution)
        - [9.7.2 Pool CurrentValue Resolution](#972-pool-currentvalue-resolution)
        - [9.7.3 Capacity Compensation](#973-capacity-compensation)
    - [9.8 Health](#98-health)
    - [9.9 Shield](#99-shield)
        - [9.9.1 Shield Depletion](#991-shield-depletion)
    - [9.10 Energy](#910-energy)
        - [9.10.1 EnergyStorage](#9101-energystorage)
        - [9.10.2 EnergyProduction and EnergyConsumption](#9102-energyproduction-and-energyconsumption)
        - [9.10.3 Energy Net Flow](#9103-energy-net-flow)
    - [9.11 Survival Resources](#911-survival-resources)
        - [9.11.1 Survival Net Flow](#9111-survival-net-flow)
    - [9.12 FlowStat](#912-flowstat)
    - [9.13 AttributeStat](#913-attributestat)
    - [9.14 Regeneration and Application Interval](#914-regeneration-and-application-interval)
    - [9.15 Active Modifier Resolution](#915-active-modifier-resolution)
    - [9.16 Tick Resolution Order](#916-tick-resolution-order)
    - [9.17 Dirty Tracking](#917-dirty-tracking)
    - [9.18 Stat Bounds](#918-stat-bounds)
    - [9.19 Stat List](#919-stat-list)
    - [9.20 Naming Rules](#920-naming-rules)
    - [9.21 Validation Requirements](#921-validation-requirements)
    - [9.22 Implementation Notes](#922-implementation-notes)
- [10 Conditions](#10-conditions)
    - [10.1 Condition List](#101-condition-list)
    - [10.2 Condition Operations](#102-condition-operations)
    - [10.3 Dead Default Blocking Rule](#103-dead-default-blocking-rule)
    - [10.4 Derived Conditions](#104-derived-conditions)
- [11 Effect Driven Gameplay](#11-effect-driven-gameplay)
- [12 EffectDefinition](#12-effectdefinition)
    - [12.1 EffectDefinition Fields](#121-effectdefinition-fields)
    - [12.2 EffectDefinition Validity Rules](#122-effectdefinition-validity-rules)
    - [12.3 Single Target Rule](#123-single-target-rule)
    - [12.4 Effect Classification](#124-effect-classification)
    - [12.5 Effect Target](#125-effect-target)
        - [12.5.1 EffectTarget Fields](#1251-effecttarget-fields)
        - [12.5.2 EffectTarget Examples](#1252-effecttarget-examples)
    - [12.6 EffectTrigger](#126-effecttrigger)
        - [12.6.1 Trigger Conditions](#1261-trigger-conditions)
        - [12.6.2 OnStatRaised / OnStatReduced Rule](#1262-onstatraised-onstatreduced-rule)
        - [12.6.3 OnStatValueBelow / OnStatValueAbove Rule](#1263-onstatvaluebelow-onstatvalueabove-rule)
    - [12.7 Effect Duration](#127-effect-duration)
    - [12.8 Blocking](#128-blocking)
        - [12.8.1 BlockPolicy](#1281-blockpolicy)
    - [12.9 Effect Removal](#129-effect-removal)
        - [12.9.1 EffectRemovalRequest](#1291-effectremovalrequest)
        - [12.9.2 EffectRemovalPolicy](#1292-effectremovalpolicy)
        - [12.9.3 Removal Validation](#1293-removal-validation)
        - [12.9.4 Removal Ordering](#1294-removal-ordering)
        - [12.9.5 Removal Validity](#1295-removal-validity)
    - [12.10 Visibility](#1210-visibility)
    - [12.11 Stacking](#1211-stacking)
        - [12.11.1 StackScope](#12111-stackscope)
        - [12.11.2 StackPolicy](#12112-stackpolicy)
        - [12.11.3 StackBehavior](#12113-stackbehavior)
        - [12.11.4 MaxStackOverflowPolicy](#12114-maxstackoverflowpolicy)
        - [12.11.5 StackGroupId](#12115-stackgroupid)
        - [12.11.6 StackIdentityKey](#12116-stackidentitykey)
        - [12.11.7 Stacking Examples](#12117-stacking-examples)
    - [12.12 ChildEffects](#1212-childeffects)
        - [12.12.1 Child Effect Storage](#12121-child-effect-storage)
- [13 ActiveEffect](#13-activeeffect)
    - [13.1 ActiveEffect Fields](#131-activeeffect-fields)
    - [13.2 EffectRuntimeId](#132-effectruntimeid)
    - [13.3 EffectDefinitionId](#133-effectdefinitionid)
- [14 Effect Sources](#14-effect-sources)
    - [14.1 SourceType](#141-sourcetype)
    - [14.2 SourceId](#142-sourceid)
    - [14.3 OriginId](#143-originid)
    - [14.4 TargetEntityId](#144-targetentityid)
    - [14.5 NetworkTick on ActiveEffect](#145-networktick-on-activeeffect)
    - [14.6 ActiveEffect Storage](#146-activeeffect-storage)
- [15 Deterministic Effect Processing Order](#15-deterministic-effect-processing-order)
- [16 StateEngine](#16-stateengine)
    - [16.1 StateEngine Responsibilities](#161-stateengine-responsibilities)
    - [16.2 Public API](#162-public-api)
        - [16.2.1 Register Living Entity](#1621-register-living-entity)
        - [16.2.2 Process Living Entities](#1622-process-living-entities)
- [17 StateEngine Tick Lifecycle](#17-stateengine-tick-lifecycle)
    - [17.1 Removal Strategy](#171-removal-strategy)
- [18 LivingEntityStateTracker](#18-livingentitystatetracker)
    - [18.1 Responsibilities](#181-responsibilities)
    - [18.2 Snapshot and Effect History Retention](#182-snapshot-and-effect-history-retention)
    - [18.3 Snapshot Storage Model](#183-snapshot-storage-model)
    - [18.4 Target Scale](#184-target-scale)
    - [18.5 Current Implementation Scope](#185-current-implementation-scope)
- [19 Delta Format](#19-delta-format)
    - [19.1 Proposed Round-Trip Solution](#191-proposed-round-trip-solution)
    - [19.2 Delta Header](#192-delta-header)
    - [19.3 Final State Payload](#193-final-state-payload)
    - [19.4 Delta Versioning](#194-delta-versioning)
    - [19.5 Payload Flags](#195-payload-flags)
- [20 Logging](#20-logging)
- [21 StateEngineConfig](#21-stateengineconfig)
- [22 Performance and Allocation Requirements](#22-performance-and-allocation-requirements)
    - [22.1 Avoid in Hot Path](#221-avoid-in-hot-path)
    - [22.2 Scratch Buffers](#222-scratch-buffers)
- [23 Determinism Requirements](#23-determinism-requirements)
- [24 Validation Requirements](#24-validation-requirements)
    - [24.1 EffectValidator Responsibilities](#241-effectvalidator-responsibilities)
    - [24.2 Validation Timing](#242-validation-timing)
    - [24.3 Validation Output](#243-validation-output)
- [25 Example Effect Scenarios](#25-example-effect-scenarios)
    - [25.1 Normal Heal](#251-normal-heal)
    - [25.2 Revive](#252-revive)
    - [25.3 Shield Booster](#253-shield-booster)
    - [25.4 Poison Damage Over Time](#254-poison-damage-over-time)
    - [25.5 EMP](#255-emp)
    - [25.6 Cleanse](#256-cleanse)
    - [25.7 Open Effect Patterns](#257-open-effect-patterns)
        - [25.7.1 Retaliation](#2571-retaliation)
        - [25.7.2 LifeLeech](#2572-lifeleech)
        - [25.7.3 CooldownReduction](#2573-cooldownreduction)
- [26 Implementation Milestones](#26-implementation-milestones)
    - [26.1 Milestone 2 — Effect Complexity](#261-milestone-2-effect-complexity)
- [27 Testing and Acceptance Criteria](#27-testing-and-acceptance-criteria)
    - [27.1 Deterministic Replay Tests](#271-deterministic-replay-tests)
    - [27.2 Stacking Tests](#272-stacking-tests)
    - [27.3 Death/Revive Tests](#273-deathrevive-tests)
    - [27.4 Delta Round-Trip Tests](#274-delta-round-trip-tests)
    - [27.5 Rollback/Resimulation Tests](#275-rollbackresimulation-tests)
    - [27.6 No-Allocation Hot-Path Tests](#276-no-allocation-hot-path-tests)
    - [27.7 Child-Effect Overflow Tests](#277-child-effect-overflow-tests)
    - [27.8 Condition Rebuild Tests](#278-condition-rebuild-tests)
    - [27.9 Stat Layer Tests](#279-stat-layer-tests)

---

## 1 Goal

Develop a deterministic, low-memory, low-allocation **RPG State Engine** for a 2D space RPG.

The engine models gameplay living entities through one common state model. The current product scope focuses on processing those entities per `NetworkTick` through a `StateEngine` that encapsulates state processing, applies active effects, resolves stat state, owns conditions, and emits compact delta state from a simulation step.

The system is effect-driven:

```text
Gameplay systems create ActiveEffect inputs from immutable EffectDefinition data.
Passive source systems update passive modifier inputs through a separate passive pipeline.
The StateEngine validates and processes ActiveEffects.
The StateEngine mutates LivingEntityStates.
The StateEngine emits compact deltas if states changed.
```

The architecture must be:

- deterministic,
- fast,
- low-allocation,
- effect-driven,
- stat-layer aware,
- extensible,
- multiplayer-friendly,
- independent from rendering,
- independent from gameplay presentation logic.

---

## 2 Scope

### 2.1 In Scope

This PRD covers:

- fast 30 TPS state simulation,
- slow 15 TPS state simulation,
- stat storage and mutation,
- passive modifier resolution into `EffectiveMaxValue`,
- active effect processing into `CurrentValue` and `CurrentMaxValue`,
- condition storage, contributor ownership, hierarchy metadata, and mutation,
- active effect lifetime,
- stack policy handling,
- child effect processing,
- effect removal requests,
- delta creation,
- resimulation support,
- state snapshot tracking,
- TRACE-only debugging logs,
- performance and allocation requirements,
- deterministic test scenarios.

### 2.2 Out of Scope

The following systems may create active effects or passive stat sources, but their own implementation is out of scope:

- skills,
- items,
- usable items,
- passive source systems,
- talents,
- weapons,
- ship systems,
- damage targeting and hit acquisition,
- presentation/UI rendering.

Network synchronization transport itself is out of scope. However, the `StateEngine` must support host-authoritative active effect inputs from past ticks, applying corrections to past ticks, resimulating from stored state and effect history, and generating a delta from the simulated tick.

---

## 3 First Use Case

The first use case is a deterministic test simulation.

Input:

```text
Existing LivingEntity to register
Applied ActiveEffects from past NetworkTicks
Set number of ticks to process
Run ticks with StateEngine
Expected final LivingEntityStates
Expected delta payloads
Expected condition flags
Expected active effect changes
```

The initial implementation must be able to:

1. Register and unregister predefined living entities.
2. Process predefined active effects in deterministic order for all registered entities for one simulation step, including resimulation from past states and effects.
3. Resolve passive-dirty and active-dirty stats deterministically.
4. Produce compact deltas.
5. Provide the final `LivingEntityState` for the latest `NetworkTick`.

The first use case is not a visual gameplay scene. It is a simulation correctness and determinism harness.

---

## 4 Tick Model

### 4.1 Godot Physics Loop

Godot physics runs at **60 physics frames per second**.

`NetworkTick` is tied to `_PhysicsProcess` by the caller, not by the `StateEngine` itself.

```text
Godot physics tick: 60 Hz
Fast StateEngine tick: 30 TPS
Fast StateEngine execution: every 2nd physics frame
```

The slow StateEngine simulation executes every fourth physics frame:

```text
Godot physics tick: 60 Hz
Slow StateEngine tick: 15 TPS
Slow StateEngine execution: every 4th physics frame
```

This is only scheduling clarification. `StateEngine` is called by game code and does not use `_PhysicsProcess` or `_Process` internally.

### 4.2 NetworkTick

The engine uses a host-synchronized `NetworkTick` value.

```text
NetworkTick type: long
NetworkTick rate: 30 TPS
```

Unless explicitly stated otherwise, `NetworkTick` always means the fast 30 TPS StateEngine tick.

All runtime effect timing is stored using `long NetworkTick` values.

This includes:

- effect application tick,
- effect duration,
- effect execution interval,
- effect expiry,
- regeneration interval,
- shield recharge cooldown expiry,
- snapshot retention,
- resimulation windows,
- active effect history.

Snapshot retention defines how many past simulation ticks are kept in memory for rollback and resimulation. Each retained tick must provide enough `LivingEntityState` and applied `ActiveEffect` history to reconstruct current state from a corrected past tick.

The resimulation window is the number of simulation ticks replayed after applying a retained past state. It is calculated as:

```text
current NetworkTick - corrected delta NetworkTick
```

The `StateEngine` must retain enough snapshots and applied effect history to replay this window deterministically.

### 4.3 Slow Simulation

The StateEngine supports a slow simulation lane at **15 TPS**.

The slow simulation does not use a separate authoritative time base. It runs on every second fast `NetworkTick`:

```text
Fast StateEngine: 30 TPS
Slow StateEngine: 15 TPS
Slow tick condition: NetworkTick % 2 == 0
```

The slow lane is intended for lower-frequency systems such as survival/resource maintenance and non-urgent state maintenance.

Examples include:

- Oxygen production and consumption,
- Food production and consumption,
- Water production and consumption,
- slow-lane active effects configured with `EffectProcessing: Slow`.

Passive modifiers are not active effects. Passive source changes are handled by the passive modifier pipeline and are recalculated only when the passive source is added, removed, replaced, or dirtied by its source state.

### 4.4 Durations

Durations outside the runtime StateEngine are authored in milliseconds.

Rules:

```text
Minimum Effect duration: > 100 ms for Temporary effects
Minimum OverTime interval: > 100 ms
Runtime representation: long NetworkTick values
```

The runtime must not use wall-clock duration values for simulation decisions. Runtime logic uses only `NetworkTick` values. `EffectDefinition` and configuration inputs may use milliseconds, which are converted once into deterministic tick values at validation/startup.

---

## 5 Core Runtime Model

The required runtime model is:

```text
EffectDefinition = immutable static data
ActiveEffect = small pooled runtime struct
PassiveModifierSource = external persistent source state
LivingEntityState = mutable fixed-size arrays
Delta = pooled binary/buffer writer with PackedByteArray public boundary
```

The system must avoid allocation in the hot tick loop.

Preferred internal layout:

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

Per-tick scratch buffers are reused:

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
```

---

## 6 Core Architecture

### 6.1 High-Level Flow

```text
Skill / Item Use / Weapon / Environment / Script
        ↓
Creates one or more ActiveEffect requests.
        ↓
Item / Talent / Level-Up / Ship Config / Persistent Upgrade
        ↓
Updates passive modifier source state when the passive source changes.
        ↓
StateEngine.ProcessLivingEntities processes incoming ActiveEffects and dirty passive sources.
        ↓
StateEngine validates, stacks, blocks, executes, removes, and expires active effects.
        ↓
Effects produce stat modification records, condition requests, child effects, and effect-removal requests.
        ↓
StateEngine resolves affected stats using deterministic stat-layer rules.
        ↓
StateEngine derives state-level conditions and shield cooldown state.
        ↓
StateEngine emits compact delta if dirty.
```

### 6.2 Core Rule

No gameplay system modifies `LivingEntityState` directly.

Only the `StateEngine` may mutate:

- stats,
- conditions,
- condition contributor ownership,
- active effects,
- derived state,
- delta state.

Gameplay systems may create `ActiveEffect` instances and passive modifier source updates, but those inputs are requests. The StateEngine decides whether they are valid and how they modify state.

---

## 7 Entity Model

### 7.1 Player Controller Composition

A `PlayerController` or `AgentController` may own or reference a `LivingEntity` by composition. The `StateEngine` does not care which controller, AI, network peer, or gameplay owner produced the entity.

Every registered `LivingEntity` is processed through the same deterministic state model.

The `StateEngine` does not model direct control ownership logic. It only processes simulation steps and provides updated `LivingEntityState` values for registered living entities.

### 7.2 LivingEntity

A `LivingEntity` represents an entity that can be affected by RPG state.

Future usage examples:

- AI-controlled ship,
- enemy,
- NPC,
- creature,
- friendly entity,
- neutral entity,
- hostile entity.

A `LivingEntity` owns or references a `LivingEntityState`.

The `LivingEntityState` itself must not contain complex gameplay mutation logic.

---

## 8 LivingEntityState

A `LivingEntityState` represents the complete simulation state of a living entity for the current `NetworkTick`.

It contains:

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
```

Responsibilities:

- Store all entity stats for other systems to read.
- Store visible active conditions for other systems to read.
- Store StateEngine-owned condition contributor ownership.
- Store currently active effects for StateEngine processing and inspection.
- Provide array-indexed hot-path access without per-stat heap objects.

Preferred internal layout:

```text
float[] BaseValues
float[] EffectiveMaxValues
float[] CurrentMaxValues
float[] CurrentValues
ulong ConditionFlags
ActiveEffect[] ActiveEffects
```

The `StatType` enum values must map directly to array indices.

Example:

```csharp
var statIndex = (int)StatType.Health;
var currentHealth = state.CurrentValues[statIndex];
var currentMaxHealth = state.CurrentMaxValues[statIndex];
var effectiveMaxHealth = state.EffectiveMaxValues[statIndex];
```

This avoids dictionary lookups and object allocation in the hot path.

---

## 9 Stats

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

### 9.7 PoolStat

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

#### 9.7.1 Pool Capacity Resolution

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

#### 9.7.2 Pool CurrentValue Resolution

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

#### 9.7.3 Capacity Compensation

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

### 9.8 Health

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

### 9.9 Shield

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

#### 9.9.1 Shield Depletion

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

### 9.10 Energy

Energy consists of three stats:

```text
EnergyStorage
EnergyProduction
EnergyConsumption
```

`EnergyStorage` is a `PoolStat`.

`EnergyProduction` and `EnergyConsumption` are `FlowStat`s.

Energy is a fast-lane system and must participate in fast 30 TPS processing.

#### 9.10.1 EnergyStorage

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

#### 9.10.2 EnergyProduction and EnergyConsumption

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

#### 9.10.3 Energy Net Flow

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

### 9.11 Survival Resources

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

#### 9.11.1 Survival Net Flow

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

### 9.12 FlowStat

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

### 9.13 AttributeStat

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

### 9.14 Regeneration and Application Interval

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

### 9.15 Active Modifier Resolution

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

### 9.16 Tick Resolution Order

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

### 9.17 Dirty Tracking

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

### 9.18 Stat Bounds

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

### 9.19 Stat List

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

### 9.20 Naming Rules

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

If migration from older content is required, aliases may be handled by import tooling, but runtime stat enum names must use canonical spelling.

---

### 9.21 Validation Requirements

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

### 9.22 Implementation Notes

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

---

## 10 Conditions

`LivingEntityCondition` is the single condition vocabulary used by the StateEngine.

A condition can be used in different roles:

- current entity state, such as `Dead`, `Invisible`, `Poisoned`, or `EnergyDepleted`,
- effect blocking input,
- effect trigger input,
- effect-removal matching input,
- UI/debug state where explicitly exposed.

Stats never infer conditions by themselves. The `StateEngine` may derive or remove conditions after stat resolution as a deterministic state rule.

Runtime active conditions are stored as bit flags.

Preferred backing type:

```text
ulong ConditionFlags
```

Condition contributor ownership is tracked internally by the StateEngine where effect-granted conditions can have multiple contributors:

```text
Condition: Poisoned
Contributors:
- EffectRuntimeId A
- EffectRuntimeId B
```

`LivingEntityState` exposes only whether the condition is present. Internally, the StateEngine tracks which active effects contribute each owned condition. Removing one contributor removes only that contribution unless a request uses `RemoveAllContributions`.

The hierarchy metadata is not stored in every entity. It is static metadata owned by the condition registry.

### 10.1 Condition List

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

If the entity is not `Dead`, it is considered alive enough for StateEngine processing. Clean state is `None`.

Lifecycle states such as `Spawned`, `Initialized`, `wait_for_revive`, and `ready_for_revive` are intentionally out of scope for this PRD.

Assumption:

```text
When StateEngine is called, the input state is valid to process.
```

### 10.2 Condition Operations

Conditions are not changed directly by effects.

An effect may request that a condition should be added or removed through `ConditionRequests`.

The StateEngine validates the request and applies it if valid.

Suggested runtime request buffers:

```text
ConditionAddRequest[]
ConditionRemoveRequest[]
ConditionContributorRecords[]
```

Condition lifecycle rules are defined by `ConditionLifecycleTiming` and `ConditionRemoveMode` in the effect model.

### 10.3 Dead Default Blocking Rule

`Dead` blocks effects by default.

Effects that are allowed to trigger while the entity is dead must explicitly opt in.

Suggested field:

```text
CanTriggerWhileDead: bool
```

Default:

```text
CanTriggerWhileDead = false
```

Examples:

```text
Normal heal:
CanTriggerWhileDead = false
```

```text
Revive:
CanTriggerWhileDead = true
```

This default is safer because designers must intentionally mark revive, resurrection, corpse, or death-related effects as triggerable while dead.

### 10.4 Derived Conditions

The StateEngine may derive conditions after stat resolution.

Initial derived rules:

| Rule | Timing | Result |
|---|---|---|
| `Health.CurrentValue <= 0` | After stat resolution | Add or keep `Dead`. |
| `Health.CurrentValue > 0` and revive/condition removal permits it | After stat resolution | Remove `Dead`. |
| `Shield.CurrentValue <= 0` and shield cooldown is not already active | After shield transaction resolution | Start shield cooldown and add `ShieldCooldown`. |
| Shield cooldown expiry reached | At due tick | Remove `ShieldCooldown`; shield may be restored by later regeneration or boosts. |

Derived condition rules must be deterministic and must not depend on wall-clock time.

---

## 11 Effect Driven Gameplay

Gameplay is effect-driven. Gameplay systems do not mutate `LivingEntityState` directly. They create `ActiveEffect` inputs from immutable `EffectDefinition` data, and only the `StateEngine` mutates stats, conditions, active effects, and state deltas.

Effects in this document are **active runtime effects only**. Passive stat sources from items, talents, level-ups, ship configuration, permanent upgrades, or similar long-lived source state are handled by a separate passive modifier pipeline. They are not represented as `EffectDefinition` entries or `ActiveEffect` instances.

An active effect may still last until it is explicitly removed. That lifetime uses `DurationType: UntilRemoved`; it is not passive behavior.

Canonical replacements from earlier documents:

```text
EffectType: Passive          -> separate passive modifier pipeline
DurationType: Permanent      -> DurationType: UntilRemoved
TargetValueKind: Current     -> TargetValueKind: CurrentValue
TargetValueKind: Max         -> TargetValueKind: CurrentMaxValue when targeting pool capacity
TriggerType: OverTimeConstant -> TriggerType: WhileActive
BlockPolicy: ConsumeOnFail   -> BlockPolicy: RemoveOnFail
SimulationLane               -> EffectProcessing
EffectRemovalRule            -> EffectRemovalRequest
ChildEffectDefinitionIds      -> ChildEffectDefinitions[].ChildEffectDefinitionId
ChildEffectBehaviour          -> ChildEffectDefinitions[].ChildEffectTrigger
LimitStacks                  -> removed; use MaxStacks
MaxStacksToOverflow          -> removed; use MaxStacks
RefreshOldest overflow       -> removed; use ReplaceOldest or RejectNewest
```

The StateEngine is responsible for determining whether an effect may execute and how it affects the target. An effect can describe:

- stat modification intent,
- lifecycle condition requests,
- static classification metadata,
- blocking rules,
- trigger timing,
- duration and expiration,
- stacking behavior,
- child effects,
- effect-removal requests,
- player visibility.

Definitions are data. Runtime progress, timers, stack counts, accumulators, parent-child runtime links, and deterministic ordering state belong to `ActiveEffect`.

---

## 12 EffectDefinition

An `EffectDefinition` is immutable static data. It contains the authored effect configuration and must not contain mutable runtime state.

The initial implementation may store definitions in an `EffectDefinitionRegistry` and validate them once during startup.

Suggested structure:

```text
scripts/
  state_engine/
    definitions/
      EffectDefinition.cs
      EffectDefinitionRegistry.cs
      EffectValidator.cs
    runtime/
      ActiveEffect.cs
```

A valid non-container effect definition must contain `Definition` and at least one executable payload:

```text
Target
ConditionRequests
EffectClassifications
EffectRemovalRequest
ChildEffectDefinitions
```

A valid container definition has:

```yaml
IsEffectContainer: true
ChildEffectDefinitions:
  - ChildEffectDefinitionId: effect.child.example
```

and must not define direct `Target`, direct `ConditionRequests`, direct `EffectClassifications`, or direct `EffectRemovalRequest`.

Each definition is valid only if all referenced child definitions are valid, the child graph is acyclic, and the graph does not exceed the configured maximum child depth.

### 12.1 EffectDefinition Fields

Canonical top-level fields:

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.example.identifier
    Name: Example Effect
    Description: Human-readable description.
  EffectProcessing: Fast
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Flat
    Value: 50.0
    ApplicationMode: Instant
  EffectClassifications:
    - Buff
  ConditionRequests:
    - Timing: OnFirstApplication
      ConditionsAddRequest: []
    - Timing: OnExpireOrRemove
      RemoveMode: RemoveOwnContribution
      ConditionsRemoveRequest: []
  Blocking:
    BlockPolicy: RemoveOnFail
    BlockedByCondition: []
    BlockedByClassification: []
  EffectRemovalRequest:
    EffectRuntimeIds: []
    EffectDefinitionIds: []
    SourceIds: []
    OriginIds: []
    Classifications: []
    Conditions: []
    EffectsTargetingStats: []
    StackGroupIds: []
    EffectRemovalBehaviour:
      MaxRemoveCount: 1
      RemoveOldestFirst: true
  EffectRemovalPolicy: ByAnySource
  CanTriggerWhileDead: false
  Stacking:
    StackGroupId: 123
    StackScope: ByEffectDefinitionId
    StackPolicy: Stack
    StackBehavior: IndependentStacks
    MaxStacks: 5
    MaxStackOverflowPolicy: ReplaceOldest
  Visibility: Internal
  ChildEffectDefinitions:
    - ChildEffectDefinitionId: effect.child.example
      ChildEffectTrigger: BoundToParent
  IsEffectContainer: false
```

Only `Definition` is always required at schema level, but a definition without executable payload is invalid unless it is a valid container.

Field summary:

| Field | Required | Default | Meaning |
|---|---:|---|---|
| `Definition` | yes | none | Stable identity, name, and description. |
| `EffectProcessing` | no | `Fast` | Simulation processing lane. Valid values: `Fast`, `Slow`. Replaces the old `SimulationLane` concept. |
| `Trigger` | no | `Immediate` | Defines when the effect executes or contributes. |
| `Duration` | no | `Once` | Defines lifetime. |
| `Target` | no | none | Optional single stat target. |
| `EffectClassifications` | no | none | Static classification metadata attached to the `ActiveEffect` after acceptance. |
| `ConditionRequests` | no | none | Optional lifecycle-timed condition add/remove requests. |
| `Blocking` | no | none | Optional blocking rules. |
| `EffectRemovalRequest` | no | none | Optional request to remove matching active effects. |
| `EffectRemovalPolicy` | no | `ByAnySource` | Removal permission policy for this effect when another effect tries to remove it. |
| `CanTriggerWhileDead` | no | `false` | Allows execution when the target has `Dead`. |
| `Stacking` | no | default stack behavior | Duplicate detection and stack handling. |
| `Visibility` | no | `Internal` | Player-facing visibility. |
| `ChildEffectDefinitions` | no | none | Child effect definitions and parent lifecycle trigger. |
| `IsEffectContainer` | no | `false` | Marks a definition that exists only to group child effects. |

Canonical enum values:

```text
EffectProcessing: Fast, Slow
TriggerType: Immediate, OverTime, WhileActive, OnExpire, OnRemoval
TriggerConditionType: OnConditionsPresent, OnConditionsMissing, OnClassificationsPresent, OnClassificationsMissing, OnStatReduced, OnStatRaised, OnStatValueBelow, OnStatValueAbove
ConditionOperator: Any, All
ConditionLifecycleTiming: OnFirstApplication, OnFirstExecution, OnExpire, OnRemove, OnExpireOrRemove
ConditionRemoveMode: RemoveOwnContribution, RemoveAllContributions
DurationType: Once, Temporary, UntilRemoved
TargetValueKind: CurrentValue, CurrentMaxValue
Manipulation: Raise, Reduce
Calculation: Flat, Percentage
ApplicationMode: Instant, WhileActive, AccumulateWhileActive
BlockPolicy: RemoveOnFail, Retry
EffectRemovalPolicy: ByAnySource, BySourceId, ByOriginId
StackScope: ByEffectDefinitionId, SameSource, SameSourceAndOrigin, ByStackGroupId
StackPolicy: Stack, Replace, Discard
StackBehavior: IndependentStacks, RefreshDuration, AddStackRefreshDuration, AddStackNoRefresh
MaxStackOverflowPolicy: ReplaceOldest, RejectNewest
Visibility: Internal, Hud, Menu
ChildEffectTrigger: BoundToParent, OnParentExpired, OnParentRemoved, OnParentExpiredOrRemoved
```

### 12.2 EffectDefinition Validity Rules

The validator must reject invalid or deprecated data at startup. Runtime systems should not need to defend against malformed definitions during normal simulation.

General validity rules:

- YAML must parse successfully.
- `Definition.EffectDefinitionId`, `Definition.Name`, and `Definition.Description` are required and non-null.
- A non-container effect must have at least one gameplay payload: `Target`, `ConditionRequests`, `EffectClassifications`, `EffectRemovalRequest`, or `ChildEffectDefinitions`.
- A container must set `IsEffectContainer: true`, must contain at least one child effect, and must omit direct `Target`, `ConditionRequests`, `EffectClassifications`, and `EffectRemovalRequest`.
- Each effect may define at most one `Target`.
- Referenced child effect IDs must exist.
- The child graph must be acyclic and within the configured maximum depth.
- Unknown enum values are invalid.
- Deprecated field names and enum values are invalid.

Deprecated or invalid values:

```text
EffectType: Passive
DurationType: Permanent
TriggerType: OverTimeConstant
TargetValueKind: Current
TargetValueKind: Max
ConditionAndClassificationRequests
ClassificationsAddRequest
ClassificationsRemoveRequest
SimulationLane
EffectRemovalRule
ChildEffectDefinitionIds
ChildEffectBehaviour
LimitStacks
MaxStacksToOverflow
MaxStackOverflowPolicy: RefreshOldest
```

Deprecated stat spellings that must be replaced by canonical names:

```text
AfterBurnerSpeed        -> AfterburnerSpeed
AfterBurnerAcceleration -> AfterburnerAcceleration
CoolDownReduction       -> CooldownReduction
BonusCriticalHitChange  -> BonusCriticalHitChance
Lifeleech               -> LifeLeech
OxygenGeneration        -> OxygenProduction
FoodGeneration          -> FoodProduction
WaterGeneration         -> WaterProduction
```

Stat-target validity rules:

- Active effects may target only `CurrentValue` or `CurrentMaxValue`.
- Active effects must not target `BaseValue` or `EffectiveMaxValue`.
- `CurrentMaxValue` is valid only for `PoolStat` capacity modifiers.
- `WhileActive` must not target `PoolStat.CurrentValue`.
- `AttributeStat` and `FlowStat` modifiers must use `TriggerType: WhileActive`, `TargetValueKind: CurrentValue`, and `ApplicationMode: WhileActive`.
- `Immediate`, `OnExpire`, and `OnRemoval` stat targets must use `PoolStat.CurrentValue` and `ApplicationMode: Instant`.
- `OverTime` stat targets must use either `PoolStat.CurrentValue + ApplicationMode: Instant` or `PoolStat.CurrentMaxValue + ApplicationMode: AccumulateWhileActive`.
- `Calculation: Percentage` values are decimal fractions. `0.05` means 5%.
- For active modifiers, percentage basis is the passive baseline.
- For instant `PoolStat.CurrentValue` percentage effects, percentage basis is the pool `CurrentMaxValue` at execution time.
- `AccumulateWhileActive` percentage reductions cap at `-100%` contribution.
- FlowStat `Value` is authored in the stat's canonical per-millisecond unit where applicable.

Timing and duration validity rules:

- `TriggerType: OverTime` requires `TickIntervalMs > 100`.
- Non-`OverTime` triggers should omit `TickIntervalMs` in canonical definitions.
- `DurationType: Temporary` requires `DurationMS > 100`.
- `DurationType: Once` and `DurationType: UntilRemoved` should omit `DurationMS` in canonical definitions.
- Runtime duration is converted from milliseconds to `NetworkTick` values.
- Runtime simulation must not use wall-clock duration values.

Condition and classification validity rules:

- `ConditionAndClassificationRequests`, `ClassificationsAddRequest`, and `ClassificationsRemoveRequest` are invalid in 6.2.3.
- `EffectClassifications`, if present, is a top-level list of static metadata attached to accepted `ActiveEffect` instances.
- `ConditionRequests`, if present, must contain at least one request entry.
- Each `ConditionRequest` requires `Timing` in canonical definitions and at least one non-empty `ConditionsAddRequest` or `ConditionsRemoveRequest`.
- A single `ConditionRequest` may contain both add and remove arrays.
- `ConditionsAddRequest` and `ConditionsRemoveRequest` mutate target `LivingEntityState` conditions through the StateEngine.
- Condition presence is contributor-owned internally by `EffectRuntimeId`; a condition remains visible while at least one contributor remains.
- `RemoveMode: RemoveOwnContribution` removes only the current active effect's contribution.
- `RemoveMode: RemoveAllContributions` clears the condition from the target and removes all contributor ownership records for that condition.
- If `RemoveMode` is omitted, `OnFirstApplication` and `OnFirstExecution` default to `RemoveAllContributions`; `OnExpire`, `OnRemove`, and `OnExpireOrRemove` default to `RemoveOwnContribution`.

Blocking, removal, stacking, and child validity rules:

- `BlockPolicy` must be `RemoveOnFail` or `Retry`.
- `Dead` blocks by default unless `CanTriggerWhileDead: true`.
- `EffectRemovalPolicy` must be `ByAnySource`, `BySourceId`, or `ByOriginId`.
- If `EffectRemovalRequest` is present, at least one selector list must contain at least one value.
- `StackScope: ByStackGroupId` requires `StackGroupId`.
- `MaxStacks`, when present, must be greater than zero.
- `MaxStacks` is the only canonical stack limit field.
- `MaxStackOverflowPolicy` must be `ReplaceOldest` or `RejectNewest`.
- `ChildEffectDefinitions` are processed in deterministic list order.

### 12.3 Single Target Rule

An `EffectDefinition` may have at most one stat target.

This keeps effects small, explicit, reusable, and easy to validate. If a gameplay concept must mutate multiple stats or combine unrelated behaviors, use child effects.

Example:

```text
EnergyDrainAndDamage
-> DrainEnergy child effect
-> DealHealthDamage child effect
-> IsEffectContainer: true
```

A definition may have zero stat targets when it only applies conditions, classifications, effect-removal requests, or child effects.

### 12.4 Effect Classification

Effect classifications are static metadata attached to accepted `ActiveEffect` instances. They are not target-state conditions and do not execute logic by themselves.

Classifications are used for:

- blocking against active effect metadata,
- removal request matching,
- debug and inspection identity,
- trigger conditions such as `OnClassificationsPresent`,
- UI/debug grouping such as `Poison`, `Buff`, `Debuff`, `Fire`, `Overheating`, and similar authored categories.

Classifications are authored with the top-level field:

```yaml
EffectClassifications:
  - Poison
  - Debuff
```

Rules:

- `EffectClassifications` is attached only after stack policy resolution accepts the runtime `ActiveEffect` instance.
- Discarded duplicate effects do not attach classifications.
- Replacement removes the old effect first; the new accepted replacement attaches its own classifications.
- Classifications are not added to or removed from `LivingEntityState`.
- Classifications disappear automatically when the corresponding `ActiveEffect` expires or is removed.
- `ClassificationsAddRequest` and `ClassificationsRemoveRequest` are invalid in 6.2.3.

Conditions and classifications are deliberately separate:

| Concept | Stored on | Mutates target state | Removed how |
|---|---|---:|---|
| Condition | `LivingEntityState` aggregated flag plus StateEngine contributor ownership | yes | By lifecycle condition requests, condition rules, or explicit clear-all requests. |
| Classification | `ActiveEffect` metadata | no | Automatically when the active effect disappears. |

Condition requests are authored separately with lifecycle timing:

```yaml
ConditionRequests:
  - Timing: OnFirstApplication
    ConditionsAddRequest:
      - PoisonExposure

  - Timing: OnFirstExecution
    ConditionsAddRequest:
      - Poisoned

  - Timing: OnExpireOrRemove
    RemoveMode: RemoveOwnContribution
    ConditionsRemoveRequest:
      - PoisonExposure
      - Poisoned
```

Supported lifecycle timings:

| Timing | Fires when | Main use |
|---|---|---|
| `OnFirstApplication` | After stack policy resolution accepts the runtime `ActiveEffect`. | Presence, exposure, lock-on, channel start, pending poison, blocked-but-active retry state. |
| `OnFirstExecution` | After the first successful execution for this runtime instance. | Conditions that should start only after the effect actually affects the target. |
| `OnExpire` | When the effect expires naturally by duration. | Natural-end consequences or cleanup. |
| `OnRemove` | When the effect is explicitly removed before natural expiry. | Interrupt, cleanse, dispel, cancellation, or removal-specific consequences. |
| `OnExpireOrRemove` | When the effect leaves the active effect buffer for either reason. | Deterministic cleanup of lifecycle-owned conditions. |

`OnFirstApplication` rules:

- Fires only after stack policy resolution.
- Fires only for an accepted runtime effect instance.
- Discarded duplicate effects do not fire it.
- Replacement removes the old effect first; the accepted replacement may fire its own `OnFirstApplication`.

`OnFirstExecution` rules:

- Fires at most once per `ActiveEffect` runtime instance.
- Does not fire when the effect is merely accepted.
- Does not fire on blocked execution attempts.
- Does not fire again after retry failures, refreshes, or later successful ticks.
- For `OverTime`, it fires after the first successful due tick.
- For `Immediate`, it fires during the first successful execution step if not blocked.

Condition contribution ownership:

```text
Condition: Poisoned
Contributors:
- EffectRuntimeId A
- EffectRuntimeId B
```

`LivingEntityState` exposes only whether `Poisoned` is present. Internally, the StateEngine tracks which active effects contribute the condition. Removing `EffectRuntimeId A` removes only A's contribution. `Poisoned` remains visible while B still contributes it.

Remove modes:

| RemoveMode | Meaning | Default timings |
|---|---|---|
| `RemoveOwnContribution` | Remove only this `ActiveEffect` runtime instance's contribution for the listed conditions. | `OnExpire`, `OnRemove`, `OnExpireOrRemove` |
| `RemoveAllContributions` | Clear the listed conditions from the target and remove all contributor ownership records for them. | `OnFirstApplication`, `OnFirstExecution` |

A single lifecycle request may add and remove conditions at the same timing:

```yaml
ConditionRequests:
  - Timing: OnFirstExecution
    RemoveMode: RemoveAllContributions
    ConditionsRemoveRequest:
      - Dead
    ConditionsAddRequest:
      - Revived
```

Default timing inference is allowed only as an importer/authoring convenience. Canonical 6.2.3 definitions should declare `Timing` explicitly. If `Timing` is omitted, the importer may infer:

| TriggerType | Default condition timing |
|---|---|
| `Immediate` | `OnFirstExecution` |
| `WhileActive` | `OnFirstApplication` |
| `OverTime` | `OnFirstExecution` |
| `OnExpire` | `OnExpire` |
| `OnRemoval` | `OnRemove` |

For lifetime condition ownership, add an `OnExpireOrRemove` cleanup request for conditions added by `OnFirstApplication` or `OnFirstExecution`.

Example poison exposure and poisoned state:

```yaml
EffectDefinitionId: effect.debuff.poison.damage_over_time

EffectClassifications:
  - Poison
  - Debuff

Trigger:
  TriggerType: OverTime
  TickIntervalMs: 1000

Duration:
  DurationType: Temporary
  DurationMS: 5000

Blocking:
  BlockPolicy: Retry
  BlockedByCondition:
    - Invulnerable

Target:
  TargetStat: Health
  TargetValueKind: CurrentValue
  Manipulation: Reduce
  Calculation: Flat
  Value: 5
  ApplicationMode: Instant

ConditionRequests:
  - Timing: OnFirstApplication
    ConditionsAddRequest:
      - PoisonExposure

  - Timing: OnFirstExecution
    ConditionsAddRequest:
      - Poisoned

  - Timing: OnExpireOrRemove
    RemoveMode: RemoveOwnContribution
    ConditionsRemoveRequest:
      - PoisonExposure
      - Poisoned
```

Runtime meaning:

```text
The poison effect is accepted:
- EffectClassifications Poison and Debuff are attached to the ActiveEffect.
- PoisonExposure is added immediately after acceptance.
- The effect is visible and removable even if blocked.

The poison effect is blocked with Retry:
- It remains active.
- PoisonExposure remains.
- Poisoned is not added yet.

The poison effect successfully ticks for the first time:
- Health damage is applied.
- OnFirstExecution fires.
- Poisoned is added.

The poison effect expires or is removed:
- The effect removes its own PoisonExposure and Poisoned contributions.
- The visible condition remains only if another contributor still owns it.
- Classifications disappear automatically with the ActiveEffect.
```

### 12.5 Effect Target

Each effect may target zero or one stat.

```yaml
Target:
  TargetStat: Health
  TargetValueKind: CurrentValue
  Manipulation: Raise
  Calculation: Flat
  Value: 50.0
  ApplicationMode: Instant
```

Effects use the stat value layers from the stat model:

```text
BaseValue
EffectiveMaxValue
CurrentMaxValue
CurrentValue
```

Active effects may target only:

| TargetValueKind | Valid use |
|---|---|
| `CurrentValue` | Pool transactions, or effective active runtime values for FlowStats and AttributeStats. |
| `CurrentMaxValue` | Active runtime capacity modifiers for PoolStats only. |

Active effects must not target `BaseValue` or `EffectiveMaxValue`. `EffectiveMaxValue` is produced only by the passive modifier pipeline.

Stat categories used by effect validation:

```text
PoolStat:
- Health
- Shield
- EnergyStorage
- OxygenStorage
- FoodStorage
- WaterStorage

FlowStat:
- HealthRegeneration
- ShieldRegeneration
- EnergyProduction
- EnergyConsumption
- OxygenProduction
- OxygenConsumption
- FoodProduction
- FoodConsumption
- WaterProduction
- WaterConsumption

AttributeStat:
- Armor
- Speed
- Acceleration
- AfterburnerSpeed
- AfterburnerAcceleration
- AttackSpeed
- CriticalHitChance
- CriticalHitBonusDamage
- BonusCriticalHitChance
- CooldownReduction
- LifeLeech
- RetaliationDamage
- ShieldRechargeDuration
- BonusDamageKinetic
- BonusDamageThermal
- BonusDamageExplosive
- ResistanceKinetic
- ResistanceThermal
- ResistanceExplosive
```

PoolStat rules:

- `CurrentValue` is a transactional pool change such as damage, heal, drain, restore, cost, production application, or consumption application.
- `CurrentMaxValue` is a runtime capacity modifier such as temporary max Health, Shield capacity, or EnergyStorage capacity.
- `CurrentValue` is clamped after every individual `CurrentValue` stat modification record.
- `CurrentMaxValue` is resolved from active capacity modifiers and clamped only after full capacity resolution.
- Intermediate capacity values may temporarily fall outside valid bounds during resolution because early clamping would change final deterministic outcomes.

After final capacity resolution:

```text
CurrentMaxValue = clamp(CurrentMaxValue, 0, maxFloat)
CurrentValue = clamp(CurrentValue, 0, CurrentMaxValue)
```

FlowStat rules:

- Flow stats describe rates such as production, consumption, and regeneration.
- FlowStat `Value` is authored in the stat's canonical value unit. Where applicable, that unit is per millisecond.
- FlowStats may be actively modified only with `WhileActive + CurrentValue + ApplicationMode: WhileActive`.

AttributeStat rules:

- Attribute stats describe effective combat, movement, and system values.
- AttributeStats may be actively modified only with `WhileActive + CurrentValue + ApplicationMode: WhileActive`.

`ApplicationMode` controls how a stat target contributes:

| ApplicationMode | Meaning |
|---|---|
| `Instant` | Creates a transactional stat modification record when the effect triggers or is due. |
| `WhileActive` | Contributes a recalculated active modifier while the ActiveEffect exists. The contribution disappears when the effect expires or is removed. |
| `AccumulateWhileActive` | Each due tick increases an accumulated active modifier stored in runtime state on the ActiveEffect. The accumulated contribution disappears when the effect expires or is removed. |

If `ApplicationMode` is omitted, use deterministic defaults:

| TriggerType | Stat category | TargetValueKind | Default ApplicationMode |
|---|---|---|---|
| `Immediate` | `PoolStat` | `CurrentValue` | `Instant` |
| `OverTime` | `PoolStat` | `CurrentValue` | `Instant` |
| `OnExpire` | `PoolStat` | `CurrentValue` | `Instant` |
| `OnRemoval` | `PoolStat` | `CurrentValue` | `Instant` |
| `WhileActive` | `PoolStat` | `CurrentMaxValue` | `WhileActive` |
| `OverTime` | `PoolStat` | `CurrentMaxValue` | `AccumulateWhileActive` |
| `WhileActive` | `AttributeStat` | `CurrentValue` | `WhileActive` |
| `WhileActive` | `FlowStat` | `CurrentValue` | `WhileActive` |

Valid explicit stat target combinations:

| TriggerType | Valid combination |
|---|---|
| `Immediate` | `PoolStat.CurrentValue + ApplicationMode: Instant` |
| `OverTime` | `PoolStat.CurrentValue + ApplicationMode: Instant` |
| `OverTime` | `PoolStat.CurrentMaxValue + ApplicationMode: AccumulateWhileActive` |
| `WhileActive` | `PoolStat.CurrentMaxValue + ApplicationMode: WhileActive` |
| `WhileActive` | `AttributeStat.CurrentValue + ApplicationMode: WhileActive` |
| `WhileActive` | `FlowStat.CurrentValue + ApplicationMode: WhileActive` |
| `OnExpire` | `PoolStat.CurrentValue + ApplicationMode: Instant` |
| `OnRemoval` | `PoolStat.CurrentValue + ApplicationMode: Instant` |

Invalid examples:

```text
WhileActive + PoolStat.CurrentValue
Immediate + AttributeStat.CurrentValue
Immediate + FlowStat.CurrentValue
Immediate + PoolStat.CurrentMaxValue
OnExpire + PoolStat.CurrentMaxValue
OnRemoval + PoolStat.CurrentMaxValue
Any AttributeStat/FlowStat + CurrentMaxValue
```

Percentage calculation rules:

```text
0.05 = 5%
0.50 = 50%
1.00 = 100%
```

Percentage modifiers are additive, not multiplicative.

| Target | Percentage basis |
|---|---|
| `PoolStat.CurrentMaxValue` | `EffectiveMaxValue` |
| `AttributeStat.CurrentValue` | passive/equipment-adjusted effective baseline |
| `FlowStat.CurrentValue` | passive/equipment-adjusted effective baseline |
| `PoolStat.CurrentValue` with `ApplicationMode: Instant` | pool `CurrentMaxValue` at execution time |

Pool capacity compensation order:

1. Resolve `EffectiveMaxValue` from base and passives.
2. Resolve active `CurrentMaxValue` modifiers.
3. Clamp final `CurrentMaxValue` to `[0, maxFloat]`.
4. Apply positive capacity compensation only if `CurrentMaxValue` is raised above `EffectiveMaxValue`.
5. Apply `CurrentValue` modification records in deterministic order.
6. Clamp `CurrentValue` after each `CurrentValue` modification record.
7. Final-clamp `CurrentValue` to `[0, CurrentMaxValue]`.

Positive capacity compensation is based only on the positive capacity delta gained this tick. It is not repeatedly applied every tick while a buff remains active. Removing a capacity debuff that returns `CurrentMaxValue` toward `EffectiveMaxValue` does not raise `CurrentValue`.

#### 12.5.1 EffectTarget Fields

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `TargetStat` | Stat | yes | The stat to affect. Must use canonical stat spelling. |
| `TargetValueKind` | enum | yes | `CurrentValue` or `CurrentMaxValue`. |
| `Manipulation` | enum | yes | `Raise` or `Reduce`. |
| `Calculation` | enum | yes | `Flat` or `Percentage`. |
| `Value` | float | yes | Numeric amount. Percentage values use decimal fractions. FlowStats use the stat's canonical per-ms unit where applicable. |
| `ApplicationMode` | enum | no | `Instant`, `WhileActive`, or `AccumulateWhileActive`. Inferred if omitted. |

#### 12.5.2 EffectTarget Examples

Flat current heal:

```yaml
Heal:
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Flat
    Value: 50.0
    ApplicationMode: Instant
```

Flat current damage over time:

```yaml
SuperEvilDot:
  Trigger:
    TriggerType: OverTime
    TickIntervalMs: 300.0
  Duration:
    DurationType: Temporary
    DurationMS: 2100.0
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Flat
    Value: 75.0
    ApplicationMode: Instant
```

Temporary Shield capacity buff:

```yaml
ShieldCapacityBoost:
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: Temporary
    DurationMS: 3000.0
  Target:
    TargetStat: Shield
    TargetValueKind: CurrentMaxValue
    Manipulation: Raise
    Calculation: Flat
    Value: 50.0
    ApplicationMode: WhileActive
```

Attribute debuff while active:

```yaml
CorrodingPoison:
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: Temporary
    DurationMS: 1500.0
  Target:
    TargetStat: Armor
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Percentage
    Value: 0.05
    ApplicationMode: WhileActive
```

FlowStat rate buff while active:

```yaml
EnergyProductionSurge:
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: Temporary
    DurationMS: 2500.0
  Target:
    TargetStat: EnergyProduction
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Percentage
    Value: 0.25
    ApplicationMode: WhileActive
```

Accumulating temporary capacity rot:

```yaml
MaxHealthRot:
  Trigger:
    TriggerType: OverTime
    TickIntervalMs: 1000.0
  Duration:
    DurationType: Temporary
    DurationMS: 5000.0
  Target:
    TargetStat: Health
    TargetValueKind: CurrentMaxValue
    Manipulation: Reduce
    Calculation: Percentage
    Value: 0.05
    ApplicationMode: AccumulateWhileActive
```

`AccumulateWhileActive` behavior:

```text
Tick 1: accumulated active capacity modifier = -5%
Tick 2: accumulated active capacity modifier = -10%
Tick 3: accumulated active capacity modifier = -15%
Effect removed: accumulated contribution disappears.
```

The accumulated contribution is runtime state on `ActiveEffect`, not mutable data on `EffectDefinition`.

---

### 12.6 EffectTrigger

`Trigger` defines when an effect executes or contributes to active state.

| TriggerType | Meaning |
|---|---|
| `Immediate` | Executes when processed. Usually paired with `DurationType: Once`. |
| `OverTime` | Executes when due according to `TickIntervalMs` while active. Requires `TickIntervalMs > 100` and `DurationType: Temporary` or `UntilRemoved`. |
| `WhileActive` | Contributes active modifiers, conditions, or classifications while the `ActiveEffect` exists. It does not tick and ignores `TickIntervalMs`. |
| `OnExpire` | Executes once when the effect expires naturally by duration. Requires `DurationType: Temporary`. |
| `OnRemoval` | Executes once when the effect is explicitly removed. Requires `DurationType: Temporary` or `UntilRemoved`. |

`Immediate` effects execute once when accepted and then are consumed unless they create child effects or lifecycle behavior requiring retention. Valid stat target:

```text
PoolStat.CurrentValue + ApplicationMode: Instant
```

`OverTime` effects are active for their lifetime and create stat modification records when due in the current simulation step. Valid stat targets:

```text
PoolStat.CurrentValue + ApplicationMode: Instant
PoolStat.CurrentMaxValue + ApplicationMode: AccumulateWhileActive
```

`WhileActive` replaces the old `OverTimeConstant` concept. It contributes while the `ActiveEffect` exists and does not execute repeatedly every engine tick. Use it for temporary Armor debuffs, AttackSpeed buffs, Shield capacity buffs, FlowStat rate modifiers, and condition carriers such as `Overheated`. `WhileActive` must not target `CurrentValue` of a PoolStat.

`OnExpire` and `OnRemoval` may target stats only as transactional `Instant` changes to `PoolStat.CurrentValue`.

Example delayed explosion:

```yaml
DelayedExplosion:
  Trigger:
    TriggerType: OnExpire
  Duration:
    DurationType: Temporary
    DurationMS: 2000.0
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Flat
    Value: 100.0
    ApplicationMode: Instant
```

#### 12.6.1 Trigger Conditions

Trigger conditions are optional additional gates. If present, the effect's trigger must be due and the trigger conditions must evaluate to true before the effect executes.

Supported condition types:

```text
OnConditionsPresent
OnConditionsMissing
OnClassificationsPresent
OnClassificationsMissing
OnStatReduced
OnStatRaised
OnStatValueBelow
OnStatValueAbove
```

Canonical trigger condition fields:

| Field | Used by | Meaning |
|---|---|---|
| `Type` | all | Trigger condition type. |
| `Conditions` | condition presence/missing checks | Target conditions to test. |
| `Classifications` | classification presence/missing checks | Active-effect classifications on the target to test. |
| `Operator` | condition/classification checks | `Any` or `All`; default `Any`. |
| `Stat` | stat checks | Stat to observe. |
| `TargetValueKind` | `OnStatRaised`, `OnStatReduced` | Observed value kind. |
| `ThresholdPercent` | `OnStatValueBelow`, `OnStatValueAbove` | Percentage-point threshold for final resolved stat value; `80.0` means 80%. |

Rules:

- `OnConditionsPresent` and `OnConditionsMissing` require `Conditions`.
- `OnClassificationsPresent` and `OnClassificationsMissing` require `Classifications`.
- `OnStatRaised` and `OnStatReduced` require `Stat` and `TargetValueKind`.
- `OnStatValueBelow` and `OnStatValueAbove` require `Stat` and `ThresholdPercent`. `ThresholdPercent` uses percentage points, so `80.0` means 80%.
- Condition and classification trigger checks are evaluated when the effect is in turn. They do not look ahead to effects that execute later in the same turn.

Example:

```yaml
Trigger:
  TriggerType: Immediate
  TriggerConditions:
    - Type: OnConditionsPresent
      Conditions:
        - Dead
      Operator: All
```

#### 12.6.2 OnStatRaised / OnStatReduced Rule

`OnStatRaised` and `OnStatReduced` trigger from validated stat modification records, not from the final consolidated outcome.

Example:

```text
Effect A reduces Health by 20.
Effect B raises Health by 20.
Final Health is unchanged.
```

In this case:

```text
OnStatReduced(Health, CurrentValue) triggers because Health was actually reduced by a valid record.
OnStatRaised(Health, CurrentValue) triggers because Health was actually raised by a valid record.
```

The trigger must not be based only on final delta because opposing modifications in the same turn may cancel each other out.

#### 12.6.3 OnStatValueBelow / OnStatValueAbove Rule

`OnStatValueBelow` and `OnStatValueAbove` trigger from final resolved stat values after all modifications are applied.

Example 1:

```text
Health starts at 100.
Threshold is 80%.
Effect A reduces Health by 20.
Effect B raises Health by 20.
Final Health is 100.
```

`OnStatValueBelow(Health, 80%)` must not trigger because the final resolved value is not below or equal to the threshold.

Example 2:

```text
Health starts at 100.
Threshold is 80%.
Effect A reduces Health by 40.
Effect B raises Health by 20.
Final Health is 80.
```

`OnStatValueBelow(Health, 80%)` triggers because the final resolved value is at the threshold.

---

### 12.7 Effect Duration

`Duration` defines how long an effect remains active.

| DurationType | Meaning |
|---|---|
| `Once` | Executes once, then is consumed. Default. |
| `Temporary` | Remains active for `DurationMS`, then expires. |
| `UntilRemoved` | Remains active until explicitly removed. This is active lifetime behavior, not passive behavior. |

Rules:

- `Temporary` requires `DurationMS > 100`.
- `Once` and `UntilRemoved` should omit `DurationMS` in canonical definitions.
- Runtime duration is converted from milliseconds to `NetworkTick` values.
- Runtime simulation must not use wall-clock duration values.
- Temporary effects expire when `CurrentNetworkTick >= ExpiresNetworkTick`.
- `Permanent` is invalid; use `UntilRemoved` for active effects that persist until explicit removal.

Example:

```yaml
Duration:
  DurationType: UntilRemoved
```

---

### 12.8 Blocking

Blocking is evaluated before trigger and trigger conditions. If an effect is blocked, its trigger and trigger conditions are not evaluated for that attempt.

```yaml
Blocking:
  BlockPolicy: RemoveOnFail
  BlockedByCondition:
    - Invulnerable
  BlockedByClassification:
    - CannotBeHealed
```

`BlockedByCondition` checks conditions on the target `LivingEntityState`.

`BlockedByClassification` checks classifications attached to active effects on the target.

If the target has any blocking condition or classification, the effect cannot execute for that attempt.

`Dead` blocks effects by default. Effects that must execute while dead need:

```yaml
CanTriggerWhileDead: true
```

Example revive:

```yaml
Revive:
  Trigger:
    TriggerType: Immediate
    TriggerConditions:
      - Type: OnConditionsPresent
        Conditions:
          - Dead
        Operator: All
  Duration:
    DurationType: Once
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Flat
    Value: 50.0
    ApplicationMode: Instant
  ConditionRequests:
    - Timing: OnFirstExecution
      RemoveMode: RemoveAllContributions
      ConditionsRemoveRequest:
        - Dead
  CanTriggerWhileDead: true
```

#### 12.8.1 BlockPolicy

| BlockPolicy | Meaning |
|---|---|
| `RemoveOnFail` | The effect is consumed or removed when blocked. Default. |
| `Retry` | The effect remains active and retries when due, until it succeeds, expires, or is explicitly removed. |

`RemoveOnFail` replaces the old `ConsumeOnFail` name.

If an effect is blocked and uses `Retry`, it remains active until one of these happens:

- it successfully executes,
- its duration expires,
- an `EffectRemovalRequest` removes it.

---

### 12.9 Effect Removal

Effects do not directly remove other effects. They request removal, and the StateEngine validates and applies the request deterministically.

The requester must trigger successfully before its `EffectRemovalRequest` is evaluated.

Passive modifiers are not effects and cannot be removed through `EffectRemovalRequest`.

#### 12.9.1 EffectRemovalRequest

Supported selectors:

```text
EffectRuntimeIds
EffectDefinitionIds
SourceIds
OriginIds
Classifications
Conditions
EffectsTargetingStats
StackGroupIds
```

Selector rules:

- Selector fields are combined with `AND`. The `Conditions` selector matches active effects that currently own a contribution to at least one listed condition.
- Values inside one selector list are combined with `OR`.
- If `EffectRemovalBehaviour` is omitted, all matching active effects are requested for removal.
- If `EffectRemovalBehaviour` is present, it limits and orders the removal request.

Example: remove all poison-classified active effects that the candidate effects allow to be removed by this requester.

```yaml
EffectRemovalRequest:
  Classifications:
    - Poison
```

Example: remove up to three oldest matching crowd-control effects.

```yaml
EffectRemovalRequest:
  Conditions:
    - Paralyzed
    - Stunned
    - Entangled
  EffectRemovalBehaviour:
    MaxRemoveCount: 3
    RemoveOldestFirst: true
```

Example: curse that also requests removal of active effects targeting `EnergyProduction`:

```yaml
EnergyStorageCurse:
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: UntilRemoved
  Target:
    TargetStat: EnergyStorage
    TargetValueKind: CurrentMaxValue
    Manipulation: Reduce
    Calculation: Percentage
    Value: 0.5
    ApplicationMode: WhileActive
  EffectRemovalRequest:
    EffectsTargetingStats:
      - EnergyProduction
```

#### 12.9.2 EffectRemovalPolicy

`EffectRemovalPolicy` controls whether this active effect is allowed to be removed by another effect's `EffectRemovalRequest`.

Canonical values:

| EffectRemovalPolicy | Meaning |
|---|---|
| `ByAnySource` | Any matching requester may remove this effect. Default. |
| `BySourceId` | Only a requester with the same `SourceId` may remove this effect. |
| `ByOriginId` | Only a requester with the same `OriginId` may remove this effect. |

The old names `AnySource` and misspelled `ByAnSource` are invalid. Use `ByAnySource`.

#### 12.9.3 Removal Validation

Removal validation is deterministic and happens after the requester has successfully triggered.

Validation flow:

1. Collect candidate active effects on the target using the request selectors.
2. Apply selector semantics: `AND` between selector fields, `OR` within each selector list.
3. For each candidate, evaluate the candidate's `EffectRemovalPolicy` against the requester.
4. Remove only candidates whose policy allows removal.
5. If a removed candidate has `TriggerType: OnRemoval`, execute that removal trigger once before the candidate is consumed.
6. Apply `EffectRemovalBehaviour` count and ordering rules if present.

Policy examples:

```text
Target effect policy: ByAnySource
Result: any matching requester may remove it.

Target effect policy: BySourceId
Result: only matching requester.SourceId may remove it.

Target effect policy: ByOriginId
Result: only matching requester.OriginId may remove it.
```

A removal request that matches no valid active effects is not an error. It produces no removals and must remain deterministic.

#### 12.9.4 Removal Ordering

When multiple active effects match a removal request, the StateEngine removes them in deterministic order.

Canonical ordering:

```text
1. RemoveOldestFirst
2. AppliedNetworkTick
3. InsertionSequence
4. EffectRuntimeId
```

If `RemoveOldestFirst` is `false`, the age ordering is reversed. Tie-breakers remain deterministic.

Default `EffectRemovalBehaviour` when present:

```yaml
EffectRemovalBehaviour:
  MaxRemoveCount: 1
  RemoveOldestFirst: true
```

If `EffectRemovalBehaviour` is omitted, all matching active effects are requested for removal.

#### 12.9.5 Removal Validity

A removal request may remove nothing when:

- no active effect matches the selectors,
- matching active effects have a removal policy that rejects the requester,
- selectors reference stats, conditions, classifications, sources, origins, stack groups, definitions, or runtime IDs that are not present,
- the request targets passive modifiers, which are outside the active effect model.

Failure to remove must be deterministic and must not partially depend on collection iteration order.

---

### 12.10 Visibility

`Visibility` defines whether an effect is shown to the player.

| Visibility | Meaning |
|---|---|
| `Internal` | Hidden from HUD and menus. Default. |
| `Hud` | Shown in the HUD and skill menu. |
| `Menu` | Shown only in the skill menu. |

Visibility is presentation metadata. It must not change simulation behavior.

---

### 12.11 Stacking

Stacking is split into separate concepts:

1. `StackScope` identifies duplicates.
2. `StackPolicy` decides whether duplicates are accepted.
3. `StackBehavior` decides how accepted duplicates behave.
4. `MaxStacks` limits accepted stacks.
5. `MaxStackOverflowPolicy` decides what happens when `MaxStacks` is reached.

Canonical stacking block:

```yaml
Stacking:
  StackGroupId: 123
  StackScope: ByStackGroupId
  StackPolicy: Stack
  StackBehavior: AddStackRefreshDuration
  MaxStacks: 5
  MaxStackOverflowPolicy: ReplaceOldest
```

Duplicates can only exist within the same target `LivingEntityState`. Identical `EffectRuntimeId` values are invalid and should be rejected before stacking is evaluated.

`AccumulateWhileActive` follows normal stacking first:

- If duplicate handling discards the new effect, no new accumulator is created.
- If duplicate handling refreshes the existing effect, the accumulated value is kept and only duration is refreshed.
- If duplicate handling replaces the existing effect, the old `ActiveEffect` and its accumulator are removed and a fresh accumulator starts.
- If `StackBehavior: IndependentStacks` is used, each accepted duplicate `ActiveEffect` has its own independent accumulator.

#### 12.11.1 StackScope

`StackScope` defines which identity fields are used to detect duplicate effects on the same target.

| StackScope | Meaning |
|---|---|
| `ByEffectDefinitionId` | Duplicate identity uses the same `EffectDefinitionId`. Default. |
| `SameSource` | Duplicate identity includes the same `EffectDefinitionId` and `SourceId`. |
| `SameSourceAndOrigin` | Duplicate identity includes the same `EffectDefinitionId`, `SourceId`, and `OriginId`. |
| `ByStackGroupId` | Duplicate identity uses `StackGroupId`; requires `StackGroupId`. |

#### 12.11.2 StackPolicy

`StackPolicy` defines whether duplicate effects are accepted.

| StackPolicy | Meaning |
|---|---|
| `Stack` | Accept the duplicate according to `StackBehavior`. Default. |
| `Replace` | Replace an existing duplicate with the new effect. |
| `Discard` | Discard the new duplicate. |

`Discard` is not the same as rejecting duplicate `EffectRuntimeId`; `EffectRuntimeId` uniqueness is validated before stacking.

#### 12.11.3 StackBehavior

The canonical field name is `StackBehavior`.

| StackBehavior | Meaning | Recommended representation |
|---|---|---|
| `IndependentStacks` | Each accepted stack is a separate `ActiveEffect`. Default. | Separate entry per duplicate. |
| `RefreshDuration` | Value does not stack; duration is refreshed. | One `ActiveEffect` entry. |
| `AddStackRefreshDuration` | `StackCount` increases and duration is refreshed. | One `ActiveEffect` entry with `StackCount`. |
| `AddStackNoRefresh` | `StackCount` increases and existing duration remains unchanged. | One `ActiveEffect` entry with `StackCount`. |

When a value scales by stack count, the scaling must be deterministic and based on `StackCount`, not on duplicated mutation of definition data.

#### 12.11.4 MaxStackOverflowPolicy

`MaxStacks` is the canonical stack limit field. `LimitStacks` and `MaxStacksToOverflow` are invalid.

When `MaxStacks` is reached, the engine uses `MaxStackOverflowPolicy`.

| MaxStackOverflowPolicy | Meaning |
|---|---|
| `ReplaceOldest` | Replace the oldest duplicate stack with the new accepted stack. Default. |
| `RejectNewest` | Reject the new duplicate when the stack limit is already reached. |

Oldest is determined by:

```text
1. AppliedNetworkTick
2. InsertionSequence
3. EffectRuntimeId
```

`RefreshOldest` is not canonical in 6.2.3 and must not be used.

#### 12.11.5 StackGroupId

`StackGroupId` allows multiple different `EffectDefinitionId` or source entries to share one stacking identity.

Rules:

- `StackGroupId` is required when `StackScope: ByStackGroupId`.
- Different effect definitions may share a group to make them mutually exclusive or jointly stack-limited.
- A stack group ID should be stable authored data, not runtime-generated state.

Example: the Overheating buff and Overheating Cooldown debuff share stack group `123` so they can use a shared duplicate identity.

```yaml
Stacking:
  StackGroupId: 123
  StackScope: ByStackGroupId
  StackPolicy: Discard
```

#### 12.11.6 StackIdentityKey

`StackIdentityKey` is the deterministic runtime key used to detect duplicates. It is derived from the target and the selected `StackScope`.

Recommended construction:

| StackScope | StackIdentityKey contents |
|---|---|
| `ByEffectDefinitionId` | `TargetEntityId + EffectDefinitionId` |
| `SameSource` | `TargetEntityId + EffectDefinitionId + SourceId` |
| `SameSourceAndOrigin` | `TargetEntityId + EffectDefinitionId + SourceId + OriginId` |
| `ByStackGroupId` | `TargetEntityId + StackGroupId` |

The exact binary representation is implementation-specific, but it must be deterministic, stable during rollback/resimulation, and independent of collection order.

#### 12.11.7 Stacking Examples

Poison or damage-over-time stacks:

```yaml
Stacking:
  StackScope: ByEffectDefinitionId
  StackPolicy: Stack
  StackBehavior: IndependentStacks
  MaxStacks: 5
  MaxStackOverflowPolicy: ReplaceOldest
```

Result:

```text
Each accepted poison application is a separate ActiveEffect.
Each has its own duration and tick timer.
Each may expire independently.
When MaxStacks is reached, the oldest stack is replaced.
```

Corroding Poison stack-count debuff:

```yaml
CorrodingPoison:
  Target:
    TargetStat: Armor
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Percentage
    Value: 0.05
    ApplicationMode: WhileActive
  Stacking:
    StackScope: ByEffectDefinitionId
    StackPolicy: Stack
    StackBehavior: AddStackRefreshDuration
    MaxStacks: 5
    MaxStackOverflowPolicy: ReplaceOldest
```

Result:

```text
Each application increases StackCount up to 5 and refreshes duration.
The Armor reduction is based on the active stack count.
When the effect expires or is removed, the contribution disappears.
```

Simple Shield capacity buff:

```yaml
Stacking:
  StackScope: ByEffectDefinitionId
  StackPolicy: Replace
  StackBehavior: RefreshDuration
  MaxStacks: 1
  MaxStackOverflowPolicy: ReplaceOldest
```

Result:

```text
Reapplying the buff replaces or refreshes the existing buff.
The value does not multiply.
The duration is refreshed.
```

Rage-style buff:

```yaml
Stacking:
  StackPolicy: Stack
  StackBehavior: AddStackRefreshDuration
  MaxStacks: 5
  MaxStackOverflowPolicy: ReplaceOldest
```

Result:

```text
Each accepted reapplication increases StackCount.
Duration is refreshed.
The stat value may scale by StackCount.
```

UntilRemoved active curse:

```yaml
Duration:
  DurationType: UntilRemoved
Stacking:
  StackScope: ByEffectDefinitionId
  StackPolicy: Discard
```

Result:

```text
The first active curse application is accepted.
Duplicate applications are discarded.
The effect remains active until removed by a valid EffectRemovalRequest.
```

---

### 12.12 ChildEffects

Child effects allow complex gameplay concepts to be composed from smaller single-target definitions.

Use child effects to:

- apply multiple stat changes while keeping each child single-target,
- separate condition requests from stat mutation,
- structure complex effects into deterministic sub-effects,
- reuse smaller definitions,
- trigger follow-up effects when a parent expires or is removed.

Canonical field:

```yaml
ChildEffectDefinitions:
  - ChildEffectDefinitionId: effect.buff.weapon.overheating.cooldown
    ChildEffectTrigger: OnParentExpired
```

Supported child triggers:

| ChildEffectTrigger | Meaning |
|---|---|
| `BoundToParent` | Default. Child is created when the parent triggers and expires or is removed with the parent. |
| `OnParentExpired` | Child is created when the parent naturally expires. Child is then independent. |
| `OnParentRemoved` | Child is created when the parent is explicitly removed. Child is then independent. |
| `OnParentExpiredOrRemoved` | Child is created when the parent either expires or is removed. Child is then independent. |

Child effects are processed in deterministic list order.

Example:

```yaml
Overheating:
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: Temporary
    DurationMS: 5000.0
  Target:
    TargetStat: AttackSpeed
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Percentage
    Value: 0.5
    ApplicationMode: WhileActive
  ChildEffectDefinitions:
    - ChildEffectDefinitionId: effect.buff.weapon.overheating.cooldown
      ChildEffectTrigger: OnParentExpired
```

```yaml
OverheatingCooldown:
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: Temporary
    DurationMS: 5000.0
  EffectClassifications:
    - OverheatingCooldown
    - Debuff
  ConditionRequests:
    - Timing: OnFirstApplication
      ConditionsAddRequest:
        - Overheated
    - Timing: OnExpireOrRemove
      RemoveMode: RemoveOwnContribution
      ConditionsRemoveRequest:
        - Overheated
```

Result:

```text
AttackSpeed is increased while Overheating is active.
When Overheating expires naturally, OverheatingCooldown starts.
OverheatingCooldown applies Overheated while active.
```

#### 12.12.1 Child Effect Storage

Child effects are stored as separate `ActiveEffect` entries. They are not nested runtime collections.

Each child stores:

```text
ParentEffectRuntimeId
ChildDepth
```

Child effects are queued in the same deterministic tick buffer as normal effects. This keeps iteration fast, avoids recursive traversal, avoids child collection allocation, and lets child processing use the same deterministic queue rules as normal effects.

---

## 13 ActiveEffect

An `ActiveEffect` is runtime effect state. It references immutable `EffectDefinition` data by ID and stores runtime-only execution state.

It should be implemented as a compact pooled struct or equivalent data-oriented runtime record.

The `ActiveEffect` must not copy mutable definition data. It should store IDs, counters, ticks, stack state, accumulated runtime modifiers, and parent-child runtime links.

### 13.1 ActiveEffect Fields

Canonical runtime fields:

| Field | Required | Default | Meaning |
|---|---:|---|---|
| `EffectRuntimeId` | yes | none | Unique runtime ID for this active effect instance. |
| `EffectProcessing` | no | from definition | Readonly processing lane: `Fast` or `Slow`. |
| `EffectDefinitionId` | yes | none | Immutable definition ID lookup key. |
| `SourceId` | yes | none | Concrete source that produced the effect. |
| `OriginId` | yes | none | Owner or originator behind the source. |
| `TargetEntityId` | yes | none | Entity receiving the effect. |
| `AppliedNetworkTick` | yes | none | Tick when the effect was accepted/applied. |
| `LastExecutedNetworkTick` | no | `-1` | Last tick when execution produced work. |
| `LastTriggeredNetworkTick` | no | `-1` | Last tick when trigger evaluation succeeded. |
| `ExpiresNetworkTick` | no | `null` | Expiration tick for `Temporary` effects. |
| `StackCount` | no | `1` | Current stack count for stack-count representations. |
| `AppliedTickCount` | no | `0` | Number of due ticks applied by `AccumulateWhileActive`. |
| `AccumulatedFlatModifier` | no | `0.0` | Runtime flat accumulator for `AccumulateWhileActive`. |
| `AccumulatedPercentageModifier` | no | `0.0` | Runtime percentage accumulator for `AccumulateWhileActive`; reduce contributions cap at `-1.0`. |
| `ParentEffectRuntimeId` | no | `null` | Parent runtime effect ID for child effects. |
| `ChildDepth` | no | `0` | Depth in child graph. |
| `HasFiredOnFirstApplication` | no | `false` | Runtime guard for lifecycle condition requests. |
| `HasFiredOnFirstExecution` | no | `false` | Runtime guard for first successful execution lifecycle condition requests. |
| `RuntimeFlags` | no | `null` | Implementation-specific runtime flags. |

Recommended C# shape:

```csharp
public struct ActiveEffect
{
    public EffectRuntimeId EffectRuntimeId;
    public EffectDefinitionId EffectDefinitionId;
    public SourceId SourceId;
    public OriginId OriginId;
    public EntityId TargetEntityId;

    public long AppliedNetworkTick;
    public long LastExecutedNetworkTick;
    public long LastTriggeredNetworkTick;
    public long? ExpiresNetworkTick;

    public int StackCount;
    public int AppliedTickCount;
    public float AccumulatedFlatModifier;
    public float AccumulatedPercentageModifier;

    public EffectRuntimeId? ParentEffectRuntimeId;
    public int ChildDepth;

    public bool HasFiredOnFirstApplication;
    public bool HasFiredOnFirstExecution;
    public RuntimeFlags RuntimeFlags;
}
```

The implementation may store either accumulated values or enough deterministic counters to recompute them during rollback/resimulation.

### 13.2 EffectRuntimeId

`EffectRuntimeId` is unique to each `ActiveEffect`. Duplicate runtime IDs are invalid and must be rejected before stacking logic.

This prevents double execution of the same runtime effect input.

`EffectRuntimeId` is also used as a deterministic tie-breaker for removal and overflow ordering.

### 13.3 EffectDefinitionId

Each `ActiveEffect` references one immutable `EffectDefinition` through `EffectDefinitionId`.

The `EffectDefinitionRegistry` provides fast lookup for the static configuration needed by the StateEngine when processing an active effect.

`EffectDefinitionId` participates in default duplicate detection through `StackScope: ByEffectDefinitionId`.

---

## 14 Effect Sources

Effect source information is used for causality, stacking, removal, analytics, debugging, and deterministic replay.

The canonical 6.2.3 runtime schema requires:

```text
SourceId
OriginId
TargetEntityId
AppliedNetworkTick
```

`SourceType` can be kept as external provenance metadata or derived from the `SourceId` namespace. It is not required by the canonical 6.2.3 `ActiveEffect` runtime schema unless the implementation intentionally extends it.

### 14.1 SourceType

`SourceType` describes the category of source that produced an effect.

Examples:

```text
Skill
Item
Weapon
ShipSystem
Environment
Quest
Script
```

Recommended use:

- Use `SourceType` for debug output, analytics, editor filtering, and log readability.
- Do not rely on `SourceType` for deterministic identity if `SourceId` and `OriginId` already provide the needed identity.
- If stored, keep it readonly on runtime effects.
- If not stored, derive it from `SourceId` naming conventions such as `skill.*`, `item.*`, `weapon.*`, or `environment.*`.

### 14.2 SourceId

`SourceId` identifies the concrete source that produced the effect.

Examples:

```text
Skill definition id
Item definition id
Weapon definition id
Ship system id
Environmental hazard id
```

`SourceId` is used by:

- stacking with `StackScope: SameSource`,
- removal permission with `EffectRemovalPolicy: BySourceId`,
- debugging and causality tracking,
- replay and deterministic diagnostics.

### 14.3 OriginId

`OriginId` identifies the owner or originator behind the source.

Examples:

```text
Player id
Enemy id
NPC id
Environmental system id
Trap owner id
```

`OriginId` is used by:

- stacking with `StackScope: SameSourceAndOrigin`,
- removal permission with `EffectRemovalPolicy: ByOriginId`,
- attribution such as damage owner, cleanse owner, kill credit, and debug traces.

### 14.4 TargetEntityId

`TargetEntityId` identifies the entity to which the effect should be applied.

The StateEngine does not perform target acquisition. It assumes that each effect input was produced by an upstream gameplay system that already performed targeting, hit validation, and gameplay authorization.

The StateEngine still validates whether the effect can execute against the current target state through blocking, trigger conditions, duration, stacking, and removal rules.

### 14.5 NetworkTick on ActiveEffect

Runtime timing uses network ticks, not wall-clock milliseconds.

`DurationMS` and `TickIntervalMs` are authored in milliseconds and converted into deterministic tick values during effect creation or content import.

Important runtime tick fields:

| Field | Meaning |
|---|---|
| `AppliedNetworkTick` | Tick when the active effect was accepted. |
| `LastExecutedNetworkTick` | Last tick in which the effect executed work. |
| `LastTriggeredNetworkTick` | Last tick in which trigger evaluation succeeded. |
| `ExpiresNetworkTick` | Tick at which a temporary effect expires. |

Temporary effects expire when:

```text
CurrentNetworkTick >= ExpiresNetworkTick
```

Over-time effects execute when they are due according to their tick interval and current simulation tick. The exact conversion must be deterministic and rollback-safe.

### 14.6 ActiveEffect Storage

The engine uses a flat active-effect buffer.

```csharp
ActiveEffect[] ActiveEffects;
int ActiveEffectCount;
```

Child effects are not stored as nested object collections. They are resolved into separate `ActiveEffect` entries and processed through the same deterministic queue rules as normal effects.

Each child entry stores:

```text
ParentEffectRuntimeId
ChildDepth
```

This storage model:

- keeps iteration fast,
- avoids recursive traversal,
- avoids per-effect child collection allocation,
- supports deterministic ordering,
- supports rollback/resimulation,
- allows child effects, normal effects, removal triggers, and lifecycle triggers to use one processing model.

---

## 15 Deterministic Effect Processing Order

Effects are processed in deterministic order.

When `ProcessLivingEntities` is called:

- Incoming effects may be provided for the current tick or past retained ticks.
- All incoming effects are sorted by `AppliedNetworkTick` and deterministic tie-breakers.
- The StateEngine processes effects for the correct targeted `LivingEntity`.
- If an accepted past effect changes a retained tick, the StateEngine restores the retained snapshot and resimulates to the current tick.
- Hot-path processing uses scratch buffers and group processing to avoid performance and allocation overhead.

Processing order for incoming effects:

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
4. Removal requests by removal ordering rules.

Stable ordering keys:

```text
AppliedNetworkTick
InsertionSequence
ParentRuntimeEffectId
ChildSequence
EffectRuntimeId
```

Recommended priority:

1. Existing active effects by stable insertion sequence.
2. New incoming effects by sorted passed buffer order.
3. Child effects by parent execution order.
4. Overflow and removal tie-breakers by `EffectRuntimeId`.

---

## 16 StateEngine

The `StateEngine` is responsible for processing incoming effects and producing the next valid `LivingEntityState`.

The public API remains small, explicit, and deterministic.

### 16.1 StateEngine Responsibilities

The StateEngine must:

- store living entity state history,
- store applied active effect history,
- track which entity has which active effects,
- keep living entity conditions up to date,
- track condition contributor ownership,
- process active effects,
- process dirty passive modifier changes through the passive pipeline,
- resimulate past ticks,
- calculate new `LivingEntityState` values,
- generate compact deltas,
- use a runtime-fixed ruleset during processing,
- avoid allocations in the hot path.

### 16.2 Public API

The API should remain small and explicit.

#### 16.2.1 Register Living Entity

```csharp
public static void RegisterLivingEntity(long livingEntityId, LivingEntityState initialState)
```

Adds a `LivingEntityState` to the engine and starts tracking it.

Registration rules:

```text
BaseValues[] must be initialized.
EffectiveMaxValues[] may be initialized or derived from BaseValues[] during registration.
CurrentMaxValues[] may be initialized or derived from EffectiveMaxValues[] during registration.
CurrentValues[] must be clamped to [0, CurrentMaxValue] for PoolStats.
StatType enum order must match all stat arrays.
```

#### 16.2.2 Process Living Entities

```csharp
public static PackedByteArray ProcessLivingEntities(
    long networkTick,
    ReadOnlySpan<ActiveEffect> incomingActiveEffects)
```

`incomingActiveEffects` includes all active effects from host and networked agents since the last processed tick, including valid retained past ticks that may require rollback/resimulation.

Output:

```text
Combined delta for all registered living entities.
```

Rules:

```text
Every effect must have AppliedNetworkTick <= networkTick.
Every effect must have TargetEntityId matching a registered living entity.
Every effect must reference a valid EffectDefinitionId.
Duplicate EffectRuntimeId values are rejected deterministically.
```

The returned `PackedByteArray` is the compact delta.

If no state changed, the returned delta may be empty.

---

## 17 StateEngine Tick Lifecycle

Per host state tick:

1. Select current `NetworkTick`.
2. Determine whether slow-lane processing is due:
   ```text
   isSlowTick = NetworkTick % 2 == 0
   ```
3. Take registered mutable entity states.
4. Clear per-tick scratch buffers without deallocation.
5. Apply dirty passive source changes and resolve affected `EffectiveMaxValue` values.
6. Add incoming `ActiveEffect` instances to entity effect buffers in deterministic order.
7. Resolve stack policies without allocation.
8. Fire `OnFirstApplication` condition requests for accepted active effects.
9. Evaluate blocking conditions and classifications.
10. Evaluate triggers and due intervals.
11. Execute due active effects in deterministic order.
12. Fire `OnFirstExecution` condition requests after each first successful execution.
13. Queue child effects into the same tick buffer in deterministic list order.
14. Collect stat transactions, active modifier contributions, condition requests, and effect-removal requests.
15. Resolve active modifiers using the stat model:
    1. passive baseline into `EffectiveMaxValue`,
    2. active flat `CurrentMaxValue` modifiers,
    3. active percentage `CurrentMaxValue` modifiers,
    4. pool capacity compensation,
    5. `CurrentValue` transactions in deterministic order,
    6. active FlowStat and AttributeStat `CurrentValue` modifiers,
    7. clamp according to stat category.
16. Apply regeneration and net-flow application when due.
17. Update shield cooldown state.
18. Derive StateEngine-owned conditions such as `Dead` after stat resolution.
19. Expire or retain active effects using deterministic removal.
20. Fire `OnExpire`, `OnRemove`, and `OnExpireOrRemove` condition requests as applicable.
21. Remove expired active effect classifications automatically.
22. Mark dirty stats, conditions, and effects.
23. Emit delta only if state is dirty.
24. Store snapshot and applied effect history for rollback/resimulation.

### 17.1 Removal Strategy

Because deterministic order matters, compact-in-place removal is used.

```text
Use compact-in-place removal.
Preserve stable ordering of retained active effects.
Do not allocate during removal.
```

Removal side effects, such as `OnRemove` condition requests or child effects, are queued through deterministic scratch buffers before compacting storage.

---

## 18 LivingEntityStateTracker

The `LivingEntityStateTracker` stores state history and effect history.

It is used for:

- resimulating from corrected ticks in the StateEngine,
- deleting obsolete state and effect history,
- generating authoritative deltas after rollback/resimulation,
- comparing predicted and corrected states.

### 18.1 Responsibilities

The tracker must provide all necessary data for a given `NetworkTick` so that the StateEngine can resimulate to the current tick.

Required data:

```text
LivingEntityState snapshot
Applied ActiveEffects for the tick
Accepted/rejected effect history where needed for deterministic replay
Delta comparison metadata
```

### 18.2 Snapshot and Effect History Retention

For any received delta tick, the receiver must still have enough data to resimulate from that tick to the current tick.

This implementation must expose snapshot and effect-history retention in `StateEngineConfig`.

Default value:

```text
SnapshotRetentionTicks = 45
```

### 18.3 Snapshot Storage Model

Snapshots should use fast implementations and depend on arrays rather than per-stat objects.

A snapshot must provide everything needed for a given `NetworkTick` and applied effects for that tick to resimulate all registered states up to the current tick.

Snapshot fields should include:

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
```

### 18.4 Target Scale

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

These scales are informational, except for configured hard fail-safe limits such as `MaxChildEffectDepth` and effect processing budgets.

The uneven distribution is important: the engine must not assume active effects are evenly distributed across entities.

### 18.5 Current Implementation Scope

The first implementation is host-side focused.

The engine must process active effects delivered from other players, including:

```text
AppliedNetworkTick
TargetEntityId
SourceId
OriginId
EffectRuntimeId
EffectDefinitionId
```

Client prediction or correction is not required for the first implementation, but the data model must not block later delta application from host to client and resimulation.

---

## 19 Delta Format

The StateEngine should output the most performant packed representation possible.

The whole round trip matters:

```text
Generate delta
→ send compact data
→ receive compact data
→ apply without expensive conversion
→ resimulate if needed
```

The StateEngine should not use `Godot.Collections.Array` internally as its primary delta format.

Preferred public Godot-compatible representation:

```text
PackedByteArray
```

Preferred internal implementation:

```text
Pooled byte buffer
Pooled binary writer
Deterministic binary layout
Little-endian numeric encoding
No per-field heap allocations
```

### 19.1 Proposed Round-Trip Solution

Generation:

```text
StateEngine detects dirty state.
DeltaWriter rents or reuses a byte buffer.
DeltaWriter writes deterministic binary payload.
StateEngine returns PackedByteArray at the Godot-facing boundary.
```

The engine should avoid converting the delta into `Godot.Collections.Array` during normal processing.

### 19.2 Delta Header

Suggested delta header:

```text
DeltaHeader:
- DeltaVersion
- EntityId
- NetworkTick
- StateTickKind: Fast or SlowDue
- PayloadFlags
- DirtyMaskStats
- DirtyMaskConditions
- DirtyMaskEffects
- FinalRecordCount
```

### 19.3 Final State Payload

The delta must always include final authoritative changed state.

Suggested final state payload:

```text
ChangedStats:
- StatIndex
- BaseValue only if ever supported later
- EffectiveMaxValue if changed or required by payload flags
- CurrentMaxValue if changed or required by payload flags
- CurrentValue if changed or required by payload flags

ChangedConditions:
- ulong ConditionFlags

ChangedEffects:
- Added ActiveEffect runtime ids
- Removed ActiveEffect runtime ids
- Updated StackCount
- Updated ExpiresNetworkTick or RemainingTicks
- Updated accumulated modifiers for AccumulateWhileActive when required
```

### 19.4 Delta Versioning

Delta format must be versioned.

```text
DeltaVersion: ushort or byte
```

Versioning is required because future stat/effect changes may alter payload layout.

### 19.5 Payload Flags

Suggested payload flags:

```text
HasFinalStats
HasFinalConditions
HasFinalEffects
HasPassiveLayerChanges
HasActiveLayerChanges
CompressedPayload
```

The first implementation does not need compression, but the flag should be reserved.

---

## 20 Logging

Logging is intended for debugging and balancing.

Use logging only when TRACE is enabled.

Logging must never allocate when TRACE is disabled.

Rules:

```text
Guard before string creation.
Do not allocate log DTOs in the hot path unless TRACE is active.
Prefer structured logging fields over interpolated strings.
Logging must not affect deterministic simulation.
```

Bad:

```csharp
_logger.Trace($"Entity {entityId} changed {stat} from {oldValue} to {newValue}");
```

Good:

```csharp
if (_traceEnabled)
{
    LogStatChanged(entityId, stat, oldValue, newValue, effectId, networkTick);
}
```

---

## 21 StateEngineConfig

The first implementation must expose core runtime limits in `StateEngineConfig` instead of hardcoding them.

Suggested initial fields:

```text
StateTicksPerSecond = 30
SlowSimulationDivisor = 2
RegenerationApplicationIntervalMs = 500
MaxChildEffectDepth = 5
MaxEffectsProcessedPerEntityPerTick = configured
MaxIncomingEffectsPerEntityPerTick = 30
MaxChildEffectsPerEntityPerTick = 3
SnapshotRetentionTicks = 45
StatComparisonPrecisionDecimals = 3
ShieldRechargeDuration default = content-defined stat value
```

Configuration values authored in milliseconds must be converted to deterministic `NetworkTick` values during startup or validation.

Runtime simulation must not use wall-clock time for deterministic decisions.

---

## 22 Performance and Allocation Requirements

The StateEngine runs at 30 TPS for fast state and 15 TPS for slow state.

The hot tick loop must avoid avoidable heap allocation.

### 22.1 Avoid in Hot Path

The engine should avoid:

```text
Per-stat heap objects
LINQ in the tick loop
Per-tick Godot.Collections.Array allocation
String logging in the hot path
Allocating temporary collections every tick
Nested ActiveEffect child collections
Reflection
Boxing enum values
Dictionary lookups per stat mutation
Queue<ActiveEffect> in the hot path
```

### 22.2 Scratch Buffers

Per-tick scratch buffers should be:

- preallocated,
- reused,
- cleared without deallocation,
- sized by configuration,
- protected by deterministic overflow behavior,
- used wherever possible.

Examples:

```text
PendingEffectsThisTick
StatModificationRecords
ActiveFlatCurrentModifiers
ActivePercentCurrentModifiers
ActiveFlatCurrentMaxModifiers
ActivePercentCurrentMaxModifiers
DirtyStatIndices
ConditionRequestBuffers
ConditionContributorBuffers
EffectRemovalRequestBuffers
ChildEffectQueue
DeltaWriterBuffer
```

---

## 23 Determinism Requirements

The StateEngine must produce identical results for identical inputs.

To achieve this:

- Use `long NetworkTick` values.
- Avoid unordered collection iteration in simulation.
- Avoid random values unless seeded deterministically.
- Avoid wall-clock time.
- Use stable insertion order for active effects.
- Use deterministic child effect order.
- Use deterministic overflow behavior.
- Use additive percentage modifiers.
- Resolve stats in a documented order.
- Compare corrected states using 3-decimal precision.
- Compare conditions by exact flag match.
- Sort removals and overflow replacement by documented keys.
- Convert authored milliseconds to ticks once before runtime simulation.

If random behavior is required later, it must be based on deterministic seeds derived from:

```text
NetworkTick
EntityId
EffectRuntimeId
SourceId
OriginId
```

---

## 24 Validation Requirements

The PRD requires an `EffectValidator` and stat-definition validation during startup.

`EffectValidator` checks all effect definitions before normal simulation begins.

Stat validation checks all stat names, categories, bounds, lanes, and target compatibility.

### 24.1 EffectValidator Responsibilities

The validator must check:

- YAML parses successfully.
- `Definition.EffectDefinitionId`, `Definition.Name`, and `Definition.Description` are present.
- A non-container effect has at least one gameplay payload: `Target`, `ConditionRequests`, `EffectClassifications`, `EffectRemovalRequest`, or `ChildEffectDefinitions`.
- A container uses `IsEffectContainer: true`, has children, and omits direct target, condition, classification, and removal payloads.
- Each effect defines at most one stat target.
- Referenced child effect IDs exist.
- Child effect graph is acyclic.
- Child effect graph does not exceed `MaxChildEffectDepth`.
- Deprecated field names and enum values are rejected.
- `MaxStacks`, when present, is greater than zero.
- `StackScope: ByStackGroupId` has `StackGroupId`.
- `EffectRemovalRequest`, when present, has at least one non-empty selector.
- Duration and tick interval values satisfy the 6.2.3 timing rules.
- Active effect stat targets are compatible with the Stats model.

### 24.2 Validation Timing

Validation occurs at StateEngine startup, before normal gameplay simulation.

Invalid `EffectDefinition` entries must fail fast and produce a deterministic validation report.

### 24.3 Validation Output

The validator should produce a deterministic report:

```text
EffectDefinitionId
ValidationViolation
FieldPath
InvalidValue
SuggestedReplacement when available
```

---

## 25 Example Effect Scenarios

### 25.1 Normal Heal

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.health.heal.normal
    Name: Normal Heal
    Description: Restores Health once.
  EffectProcessing: Fast
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Flat
    Value: 35.0
    ApplicationMode: Instant
  Blocking:
    BlockPolicy: RemoveOnFail
    BlockedByCondition:
      - Dead
      - Incurable
  CanTriggerWhileDead: false
  Visibility: Hud
```

Result:

```text
Raises Health.CurrentValue once.
Fails if the target is Dead or Incurable.
The effect is removed after execution or failed blocking with RemoveOnFail.
```

### 25.2 Revive

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.revive.basic
    Name: Basic Revive
    Description: Restores Health and removes Dead if Health becomes positive.
  IsEffectContainer: true
  ChildEffectDefinitions:
    - ChildEffectDefinitionId: effect.revive.restore_health
      ChildEffectTrigger: BoundToParent
    - ChildEffectDefinitionId: effect.revive.remove_dead
      ChildEffectTrigger: BoundToParent
```

Restore Health child:

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.revive.restore_health
    Name: Revive Restore Health
    Description: Restores Health during revive.
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Raise
    Calculation: Flat
    Value: 25.0
    ApplicationMode: Instant
  CanTriggerWhileDead: true
```

Remove Dead child:

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.revive.remove_dead
    Name: Revive Remove Dead
    Description: Removes Dead during revive.
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  ConditionRequests:
    - Timing: OnFirstExecution
      RemoveMode: RemoveAllContributions
      ConditionsRemoveRequest:
        - Dead
  CanTriggerWhileDead: true
```

Result:

```text
Can trigger while Dead.
Restores Health.
Requests removal of Dead.
If Health.CurrentValue remains <= 0 after stat resolution, the StateEngine re-applies Dead.
```

### 25.3 Shield Booster

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.shield.capacity_boost
    Name: Shield Capacity Booster
    Description: Temporarily raises Shield capacity.
  EffectProcessing: Fast
  Trigger:
    TriggerType: WhileActive
  Duration:
    DurationType: Temporary
    DurationMS: 5000
  Target:
    TargetStat: Shield
    TargetValueKind: CurrentMaxValue
    Manipulation: Raise
    Calculation: Flat
    Value: 50.0
    ApplicationMode: WhileActive
  Stacking:
    StackScope: ByEffectDefinitionId
    StackPolicy: Replace
    StackBehavior: RefreshDuration
    MaxStacks: 1
    MaxStackOverflowPolicy: ReplaceOldest
  Visibility: Hud
```

Result:

```text
Raises Shield.CurrentMaxValue temporarily.
If Shield.CurrentMaxValue increases, Shield.CurrentValue receives positive capacity compensation once for the capacity delta.
When the effect expires, Shield.CurrentMaxValue is recalculated and Shield.CurrentValue is clamped if needed.
```

### 25.4 Poison Damage Over Time

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.debuff.poison.damage_over_time
    Name: Poison Damage Over Time
    Description: Applies poison exposure and deals periodic Health damage.
  EffectProcessing: Fast
  EffectClassifications:
    - Poison
    - Debuff
  Trigger:
    TriggerType: OverTime
    TickIntervalMs: 1000
  Duration:
    DurationType: Temporary
    DurationMS: 5000
  Blocking:
    BlockPolicy: Retry
    BlockedByCondition:
      - Invulnerable
  Target:
    TargetStat: Health
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Flat
    Value: 5.0
    ApplicationMode: Instant
  ConditionRequests:
    - Timing: OnFirstApplication
      ConditionsAddRequest:
        - PoisonExposure
    - Timing: OnFirstExecution
      ConditionsAddRequest:
        - Poisoned
    - Timing: OnExpireOrRemove
      RemoveMode: RemoveOwnContribution
      ConditionsRemoveRequest:
        - PoisonExposure
        - Poisoned
  Stacking:
    StackGroupId: poison.damage
    StackScope: ByStackGroupId
    StackPolicy: Stack
    StackBehavior: IndependentStacks
    MaxStacks: 5
    MaxStackOverflowPolicy: ReplaceOldest
  Visibility: Hud
```

Result:

```text
Each poison application is its own ActiveEffect until MaxStacks is reached.
Each stack keeps its own duration and tick interval.
PoisonExposure is added on accepted application.
Poisoned is added only after the first successful damage tick.
When MaxStacks is reached, ReplaceOldest removes the oldest stack deterministically.
```

### 25.5 EMP

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.emp.energy_lockout
    Name: EMP Lockout
    Description: Temporarily applies EnergyDepleted to the target.
  EffectProcessing: Fast
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Temporary
    DurationMS: 3000
  EffectClassifications:
    - Debuff
    - EMP
  ConditionRequests:
    - Timing: OnFirstApplication
      ConditionsAddRequest:
        - EnergyDepleted
    - Timing: OnExpireOrRemove
      RemoveMode: RemoveOwnContribution
      ConditionsRemoveRequest:
        - EnergyDepleted
  Visibility: Hud
```

Result:

```text
Requests EnergyDepleted while active.
Energy-based gameplay systems may check this condition.
The condition contribution is removed when the effect expires or is removed.
```

### 25.6 Cleanse

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.cleanse.poison
    Name: Cleanse Poison
    Description: Removes poison-classified active effects.
  EffectProcessing: Fast
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  EffectRemovalRequest:
    Classifications:
      - Poison
    EffectRemovalBehaviour:
      MaxRemoveCount: 3
      RemoveOldestFirst: true
  EffectRemovalPolicy: ByAnySource
  Visibility: Hud
```

Result:

```text
The effect requests removal of active effects classified as Poison.
The StateEngine deterministically removes matching valid active effects.
Removed effects run OnRemove and OnExpireOrRemove condition cleanup.
```

### 25.7 Open Effect Patterns

#### 25.7.1 Retaliation

Retaliation should be implemented as a future trigger pattern rather than direct stat mutation outside the StateEngine.

Required future behavior:

```text
When damage is successfully applied to the target, create a child or reaction ActiveEffect that reduces the attacker's Health by a deterministic percentage of the applied damage.
```

The exact damage-event payload is outside this PRD.

#### 25.7.2 LifeLeech

LifeLeech should be implemented as a future trigger pattern rather than direct stat mutation outside the StateEngine.

Required future behavior:

```text
When damage is successfully applied by the source, create a child or reaction ActiveEffect that raises the source's Health by a deterministic percentage of the applied damage.
```

The exact damage-event payload is outside this PRD.

#### 25.7.3 CooldownReduction

CooldownReduction should be implemented as a future effect-removal or cooldown-state mutation pattern.

Required future behavior:

```text
When a target is healed, active cooldown effects or cooldown state owned by that target may be reduced by a deterministic amount.
```

The exact cooldown model is outside this PRD.

---

## 26 Implementation Milestones

Milestone 1 is done when deterministic test scenarios can process a fixed set of simulation steps for given registered `LivingEntityState` values and provided `ActiveEffect` inputs, produce expected final state and delta data, pass rollback/resimulation tests, and show no avoidable hot-path allocations under the selected test scale.

Required implementation pieces:

```text
LivingEntityState
Stats arrays: BaseValues, EffectiveMaxValues, CurrentMaxValues, CurrentValues
Condition flags
Condition contributor ownership
EffectDefinition registry
EffectValidator startup check
ActiveEffect flat buffer
Passive modifier pipeline
StateEngine tick loop
Basic delta writer
LivingEntityStateTracker
Applied ActiveEffect history
Apply delta
Resimulate
Deterministic test harness
```

Milestone 1 test scale:

```text
150 active living entities
35 average active effects per entity
At least one player-like entity with 70 active effects
20 incoming effects per entity per tick stress case
3 child effects per entity per tick stress case
5 max child effect depth overflow test
45 rollback/resimulation ticks default test
60 rollback/resimulation ticks cap test
```

Run the test with the given numbers and simulate for 50 steps. Log final processing time only outside the hot path or with allocation-safe instrumentation.

### 26.1 Milestone 2 — Effect Complexity

Required:

```text
EffectDefinitions
EffectDefinitionRegistry
StateEngine
ActiveEffects
StateHistory
EffectHistory
Stacking policies
Condition lifecycle ownership
Effect removal requests
Child effect graph validation
```

---

## 27 Testing and Acceptance Criteria

The PRD requires the following test categories.

### 27.1 Deterministic Replay Tests

Given identical initial state and identical ordered effects, the engine must produce identical results across repeated runs.

### 27.2 Stacking Tests

Must cover:

```text
IndependentStacks
RefreshDuration
AddStackRefreshDuration
AddStackNoRefresh
MaxStacks ReplaceOldest
MaxStacks RejectNewest
Discard
Replace
StackScope: ByEffectDefinitionId
StackScope: SameSource
StackScope: SameSourceAndOrigin
StackScope: ByStackGroupId
```

### 27.3 Death/Revive Tests

Must cover:

```text
Health.CurrentValue <= 0 adds Dead through StateEngine derived condition rules
Dead blocks normal effects by default
Revive can trigger while Dead
Revive removes Dead only if Health.CurrentValue > 0 after stat resolution
Dead is re-applied if Health.CurrentValue remains <= 0
```

### 27.4 Delta Round-Trip Tests

Must cover:

```text
Generate delta
Serialize to PackedByteArray
Read delta
Apply final state
Read causal records when included
Compare corrected result
```

### 27.5 Rollback/Resimulation Tests

Must cover:

```text
Apply delta from past tick
Find stored state
Replay applied effects
Compare to predicted current state
Use 3-decimal stat precision
Use exact condition match
Discard older snapshots
```

### 27.6 No-Allocation Hot-Path Tests

Must verify that the tick loop avoids avoidable heap allocation during normal processing.

### 27.7 Child-Effect Overflow Tests

Must cover:

```text
MaxChildEffectDepth = 5
Overflow rejected deterministically
TRACE log only when enabled
Simulation continues safely
```

### 27.8 Condition Rebuild Tests

Must cover:

```text
Effect-granted conditions are rebuilt deterministically
Expired effects do not leave stale conditions
Multiple active effects granting the same condition do not remove it too early
RemoveOwnContribution removes only the owning EffectRuntimeId contribution
RemoveAllContributions clears every contributor for the requested condition
```

### 27.9 Stat Layer Tests

Must cover:

```text
BaseValue remains stable after registration
Passive modifiers update EffectiveMaxValue only when passive source state changes
Active capacity modifiers update CurrentMaxValue
Pool CurrentValue transactions clamp after each transaction
FlowStat and AttributeStat active modifiers snap back after effect removal
Capacity compensation applies only to positive capacity gained this tick
```

---
