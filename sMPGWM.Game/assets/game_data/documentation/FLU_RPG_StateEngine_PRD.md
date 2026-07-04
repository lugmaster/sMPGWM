# PRD: RPG State Engine

**Version:** 6.1
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Multiplayer:** Steam Multiplayer Peer integration is out of scope  
**Genre:** 2D Space RPG  
**Network Model:** Client-Server Network Model with Server Authoritative Rewind and Client-Side Prediction /
Reconciliation
**Network ticks per second (TPS)** 30
**Execution Context:** Host-authoritative gameplay state simulation  
**Primary Output:** Compact packed delta data, optimized for full round trip generation
**Target Platform:** Desktop PC  
**Target Active Entities:** 250 living entities  
**Document Usage:** Agentic implementation workflow, technical planning, test generation, PRD review, and iterative
implementation guidance

---

## 1. Goal

Develop a deterministic, low-memory, low-allocation **RPG State Engine** for a 2D space RPG.

The engine models gameplay living entities through one common state model. The current product scope is focused on the
processing of such entities per Network Tick with a State engine, that encapsulated the whole state processing and
provides convienent methods to apply effects and calculate a delta state from a simulation step.

The system is effect-driven:

```text
Gameplay systems create ActiveEffects.
The StateEngine validates and processes ActiveEffects.
The StateEngine mutates LivingEntityStates.
The StateEngine emits compact deltas if states changed.
```

The architecture must be:

- Deterministic
- Fast
- Low-allocation
- Effect-driven
- Extensible
- Multiplayer-friendly
- Independent from rendering
- Independent from gameplay presentation logic

---

## 2. Scope

### 2.1 In Scope

This PRD covers:

- Fast 30 TPS state simulation
- Slow 15 TPS state simulation
- Stat storage and mutation
- Condition storage, hierarchy, and mutation
- Effect processing
- Active effect lifetime
- Stack policy handling
- Child effect processing
- effect removal requests
- Delta creation
- Resimulation support
- State snapshot tracking
- TRACE-only debugging logs
- Performance and allocation requirements
- Deterministic test scenarios

### 2.2 Out of Scope

The following systems may create effects, but their own implementation is out of scope:

- Skills
- Items
- Usable items
- Passives
- Talents
- Weapons
- Ship systems

Network synchronization transport itself is out of scope. However, the **StateEngine** must support host-authoritative
active effect inputs from the past, applying corrections to past ticks, and resimulating from stored state and effect
history and generate a delta from simulated tick.

---

## 3. First Use Case

The first use case is a deterministic test simulation.

Input:

```text
Existing LivingEntity to register
Applied ActiveEffects from past Networkticks
Set number of ticks to process
Run Ticks with StateEngine
Expected final LivingEntityStates
Expected delta payloads
Expected condition flags
Expected active effect changes
```

The initial implementation must be able to:

1. Register/Unregister predefined living entity.
2. Process predefined effects in deterministic order for all registered entities for one simulation step with
   resimulation past states and effects.
3. Produce compact deltas.
4. Provide a final livinEntityState for the latest networktick

The first use case is not a visual gameplay scene. It is a simulation correctness and determinism harness.

---

## 4. Tick Model

### 4.1 Godot Physics Loop

Godot physics runs at **60 physics frames per second**.

Networktick is tied to `_PhysicsProcess`.

```text
Godot physics tick: 60Hz
Fast StateEngine tick: 30 TPS
Fast StateEngine execution: every 2nd physics frame
```

The slow StateEngine simulation executes every forth physics frame:

```text
Godot physics tick: 60Hz
Slow StateEngine tick: 15 TPS
Slow StateEngine execution: every 4th physics frame
``````

Note that this is only for clarification. `StateEngine` gets called and does not use `_PhysicsProcess` or `_Process`
itself.

### 4.2 NetworkTick

The engine uses a host-synchronized **NetworkTick** value.

```text
NetworkTick type: long
NetworkTick rate: 30 TPS
```

Unless explicitly stated otherwise, **NetworkTick** always means the fast 30 TPS StateEngine tick.

All runtime effect timing is stored using long `NetworkTick` values.

This includes:

- Effect application tick
- Effect duration
- Effect execution interval
- Effect expiry
- Regeneration interval
- Snapshot retention (Snapshot retention defines how many past simulation ticks are kept in memory for rollback and
  resimulation. Each retained tick must provide enough LivingEntityState and applied ActiveEffect history to reconstruct
  the current state from a corrected past tick.)
- Resimulation windows (The resimulation window is the number of simulation ticks replayed after applying a retained
  past state. It is calculated as current NetworkTick minus the corrected delta NetworkTick. The StateEngine must retain
  enough snapshots and applied ActiveEffect history to replay this window deterministically.)
- Active effect history

### 4.3 Slow Simulation

The StateEngine also supports a slow simulation lane at **15 TPS**.

The slow simulation does not use a separate authoritative time base. It runs on every second fast `NetworkTick`:

```text
Fast StateEngine: 30 TPS
Slow StateEngine: 15 TPS
Slow tick condition: NetworkTick % 2 == 0
```

The slow lane is intended for lower-frequency systems, such as survival/resource maintenance, non-combat background
state, permanent/passive effects that do not need to execute every fast tick, and non-urgent active-effect capacity
maintenance. Examples include:

- Oxygen consumption/generation
- Food consumption/generation
- Water consumption/generation
- Permanent effects (e.g. provided from items or passives)

Permanent effects belong to both simulation lanes.
The important part is, that added or subtracted stat bonus or malus resulting from permanent modifiers must be applied
every fast lane Tick.
But the recalculation of that bonus or malus should only be recalculated if
Permanent effects where added, removed, or dirtied by their source state.

### 4.4 Durations

Durations outside the state engine are always provided in milli-seconds.

Rules:

```text
Minimum Effect duration: 100 ms
Runtime representation: long NetworkTick values
```

The runtime must not use wall-clock duration values for simulation decisions. Runtime only knows NetworkTicks. But
EffectDefinition must use ms.

---

## 5. Core Runtime Model

The required runtime model is:

```text
EffectDefinition = immutable static data
ActiveEffect = small pooled runtime struct
LivingEntityState = mutable fixed-size arrays
Delta = pooled binary/buffer writer with PackedByteArray public boundary
```

The system must avoid allocation in the hot tick loop.

Preferred internal layout:

```text
float[] BaseValues
float[] MaxValues
float[] CurrentValues
ulong ConditionFlags
BitSet DirtyStats
ActiveEffect[] ActiveEffects
int ActiveEffectCount
```

Per-tick scratch buffers are reused:

```text
PendingEffectsThisTick
StatModificationRecords
FlatMaxModifiers
PercentMaxModifiers
FlatCurrentModifiers
PercentCurrentModifiers
DirtyStatIndices
ConditionAddRequests
ConditionRemoveRequests
EffectRemovalRequests
DeltaWriterBuffer
```

---

## 6. Core Architecture

### 6.1 High-Level Flow

```text
Skill / Item / Weapon / Environment / Passive / Ship System
        ↓
Creates one or more ActiveEffects.
        ↓
StateEngine.ProcessLivingEntities processes Current ActiveEffects and provided ActiveEffects from a current NetworkSync for all registered living entities.
        ↓
StateEngine validates, stacks, blocks, executes.
        ↓
Effects produce stat modification records and condition/effect requests
        ↓
StateEngine resolves affected stats once per tick
        ↓
StateEngine infers derived conditions
        ↓
StateEngine emits compact delta if dirty
```

### 6.2 Core Rule

No gameplay system modifies `LivingEntityState` directly.

Only the **StateEngine** may mutate it:

- Stats
- Conditions
- Active effects
- Derived state
- Delta state

Gameplay systems may create `ActiveEffect` instances, but those effects are requests. The StateEngine decides whether
they are valid and how they modify state.

---

## 7. Entity Model

### 7.1 Player Controller Composition

A `PlayerController` or `AgentController` are `LivingEntities` and they own a `LivingEntityState` by composition.
Therefor `StageEngine` does not need to care which origin it has. Every registered LivingEntity is processed. The
StateEngine handles everything internal.

The StateEngine does not model direct control ownership logic. It only processes simulation steps and provide updated
`LivingEntitieStates` for LivingEntity.

### 7.2 LivingEntity

A `LivingEntity` represents an entity that can be affected by RPG state.

Future-usage examples:

- AI-controlled ship
- Enemy
- NPC
- Creature
- Friendly entity
- Neutral entity
- Hostile entity

A `LivingEntity` owns or references a `LivingEntityState`.

The `LivingEntityState` itself must not contain complex gameplay mutation logic.


---

## 8. LivingEntityState

A `LivingEntityState` represents the complete simulation state of a living entity for the current `NetworkTick`.

It contains:

```text
EntityId
NetworkTick
BaseValues[]
MaxValues[]
CurrentValues[]
ConditionFlags
ActiveEffects[]
ActiveEffectCount
```

Responsibilities:

- Store all entity stats. Retrieveable for other Systems.
- Store active conditions. Retrieveable for other Systems.
- Store currently active effects. Retrieveable for other Systems.

Preferred internal layout:

```text
float[] BaseValues
float[] MaxValues
float[] CurrentValues
ulong ConditionFlags
ActiveEffect[] ActiveEffects
```

The `StatType` enum values must map directly to array indices.

Example:

```csharp
CurrentValues[(int)StatType.Health]
MaxValues[(int)StatType.Health]
BaseValues[(int)StatType.Health]
```

This avoids dictionary lookups and object allocation in the hot path.

---

## 9. Stats

Stats describe numeric properties of a `LivingEntity`.

Stats contain no gameplay logic.

Stats are modified only through `ActiveEffect` processing inside the `StateEngine`.

Each stat has three values:

```text
BaseValue
MaxValue
CurrentValue
```

### 9.1 Stat Value Definitions

| Value          | Description                                                                                                      |
|----------------|------------------------------------------------------------------------------------------------------------------|
| `BaseValue`    | The natural/default value of the stat without active modifiers. Runtime mutation of `BaseValue` is out of scope. |
| `MaxValue`     | The current maximum value the stat can reach, including active modifiers.                                        |
| `CurrentValue` | The current value of the stat, usually clamped between `0` and `MaxValue`.                                       |

### 9.2 BaseValue Mutation

Changing `BaseValue` at runtime is out of scope for this PRD.

For this PRD, `BaseValues[]` are provided as valid and unchangeable when registering an `LivingEntity` to `StateEngine`

### 9.3 Complete Stat List

The following table is the current stat list for this PRD.

| Stat                      |           Range | Simulation Lane | Description                                                                                                                   |
|---------------------------|----------------:|-----------------|-------------------------------------------------------------------------------------------------------------------------------|
| `Health`                  | `[0, maxFloat]` | Fast 30 TPS     | If at zero, death is inferred.                                                                                                |
| `EnergyStorage`           | `[0, maxFloat]` | Fast 30 TPS     | Used for certain skills or afterburning.                                                                                      |
| `Shield`                  | `[0, maxFloat]` | Fast 30 TPS     | Absorbs incoming damage. Applied before armor.                                                                                |
| `Armor`                   | `[0, maxFloat]` | Fast 30 TPS     | Determines the percentage of applied damage that is reduced.                                                                  |
| `Speed`                   | `[0, maxFloat]` | Fast 30 TPS     | Determines how fast the player can go. Effective movement use is out of scope for this PRD.                                   |
| `Acceleration`            | `[0, maxFloat]` | Fast 30 TPS     | Determines how fast the player reaches speed. Effective movement use is out of scope for this PRD.                            |
| `AfterBurnerSpeed`        | `[0, maxFloat]` | Fast 30 TPS     | Determines how fast the player can go with afterburner. Effective movement use is out of scope for this PRD.                  |
| `AfterBurnerAcceleration` | `[0, maxFloat]` | Fast 30 TPS     | Determines how fast the player reaches max afterburner speed. Effective movement use is out of scope for this PRD.            |
| `CriticalHitChance`       | `[0, maxFloat]` | Fast 30 TPS     | Determines percentage chance to make a critical hit. Effective combat roll use is out of scope for this PRD.                  |
| `CriticalHitBonusDamage`  | `[0, maxFloat]` | Fast 30 TPS     | Determines how much bonus damage is applied on a critical hit. Effective combat roll use is out of scope for this PRD.        |
| `AttackSpeed`             | `[0, maxFloat]` | Fast 30 TPS     | Reduces time between attacks in percent. Effective attack scheduling is out of scope for this PRD.                            |
| `CoolDownReducation`      | `[0, maxFloat]` | Fast 30 TPS     | Flat amount applied to `Health` each regeneration tick.                                                                       |
| `HealthRegeneration`      | `[0, maxFloat]` | Fast 30 TPS     | Flat amount applied to `Health` each regeneration tick.                                                                       |
| `EnergyProduction`        | `[0, maxFloat]` | Fast 30 TPS     | Flat amount applied to `EnergyStorage` each regeneration tick.                                                                |
| `EnergyConsumption`       | `[0, maxFloat]` | Fast 30 TPS     | Flat amount subtracted from `EnergyStorage` each regeneration tick.                                                           |
| `ShieldRegeneration`      | `[0, maxFloat]` | Fast 30 TPS     | Flat amount applied to `Shield` each regeneration tick.                                                                       |
| `ShieldRechargeDuration`  | `[0, maxFloat]` | Fast 30 TPS     | Time in seconds the shield needs before restoration after reaching zero.                                                      |
| `OxygenConsumption`       | `[0, maxFloat]` | Slow 15 TPS     | Flat amount subtracted from `OxygenStorage` each slow regeneration tick. Effective survival use is out of scope for this PRD. |
| `OxygenGeneration`        | `[0, maxFloat]` | Slow 15 TPS     | Flat amount applied to `OxygenStorage` each slow regeneration tick. Effective survival use is out of scope for this PRD.      |
| `FoodConsumption`         | `[0, maxFloat]` | Slow 15 TPS     | Flat amount subtracted from `FoodStorage` each slow regeneration tick. Effective survival use is out of scope for this PRD.   |
| `FoodGeneration`          | `[0, maxFloat]` | Slow 15 TPS     | Flat amount applied to `FoodStorage` each slow regeneration tick. Effective survival use is out of scope for this PRD.        |
| `WaterConsumption`        | `[0, maxFloat]` | Slow 15 TPS     | Flat amount subtracted from `WaterStorage` each slow regeneration tick. Effective survival use is out of scope for this PRD.  |
| `WaterGeneration`         | `[0, maxFloat]` | Slow 15 TPS     | Flat amount applied to `WaterStorage` each slow regeneration tick. Effective survival use is out of scope for this PRD.       |
| `OxygenStorage`           | `[0, maxFloat]` | Slow 15 TPS     | Determines how much oxygen is left. Effective survival use is out of scope for this PRD.                                      |
| `FoodStorage`             | `[0, maxFloat]` | Slow 15 TPS     | Determines how much food is left. Effective survival use is out of scope for this PRD.                                        |
| `WaterStorage`            | `[0, maxFloat]` | Slow 15 TPS     | Determines how much water is left. Effective survival use is out of scope for this PRD.                                       |

### 9.4 Stat Dirty Tracking

Stats must support dirty tracking.

Dirty tracking should not be implemented as one object-level boolean per stat object in the hot path.

Preferred approach:

```text
BitSet DirtyStats
```

Dirty stats are used to determine whether delta data must be emitted.

---

## 10. Stat Modification Model

Effects do not directly mutate stat values.

Instead, executed effects produce `StatModificationRecord` entries in scratch buffers.

The StateEngine resolves all affected stats once per tick in a deterministic order.

### 10.1 StatModificationRecord

Suggested structure:

```text
StatModificationRecord:
- SourceRuntimeEffectId
- EffectDefinitionId
- TargetStat
- TargetValueKind
- Manipulation
- Calculation
- Value
- StackCount
- AppliedNetworkTick
- ExecutionNetworkTick
- CausalSequence
```

### 10.2 TargetValueKind

Each stat-mutating effect targets exactly one value kind:

| Value     | Description                   |
|-----------|-------------------------------|
| `Current` | Modifies only `CurrentValue`. |
| `Max`     | Modifies only `MaxValue`.     |

`CurrentAndMax` is intentionally removed from this PRD.

If a gameplay concept needs to modify both current and max values, it must be represented as a parent effect with child
effects, one targeting `Current` and one targeting `Max`.

### 10.3 Manipulation

| Value    | Description                 |
|----------|-----------------------------|
| `Raise`  | Increases the target value. |
| `Reduce` | Decreases the target value. |

### 10.4 Calculation

| Value        | Description                                                      |
|--------------|------------------------------------------------------------------|
| `Flat`       | Applies a fixed value directly.                                  |
| `Percentage` | Adds to the additive percentage modifier for the targeted value. |

A single effect target cannot modify flat and percentage values at the same time.

### 10.5 Additive Percentage Rule

Percentage modifiers are explicitly additive.

Example:

```text
Base value: 100
Modifier A: +20%
Modifier B: +30%

Total percentage modifier: +50%
Final value: 100 * 1.5 = 150
```

The engine must not multiply percentage modifiers together

---

## 11. Stat Calculation Order

For each affected stat, the StateEngine resolves stat changes in this deterministic order:

1. Apply all max flat modifiers.
2. Apply additive all max percentage modifiers.
3. Resolve max/current compensation using the previous tick's max value.
4. Apply all current flat modifiers.
5. Apply additive all current percentage modifiers.
6. Clamp current value to valid stat bounds.
7. Apply regeneration if the relevant regeneration interval is met.
8. Clamp again.

### 11.1 Max Value Compensation

If `MaxValue` is raised `CurrentValue` is raised by the same absolute amount, but cannot exceed `MaxValue`.

```text
MaxValue previous: 100
MaxValue new:      120
CurrentValue old:   40
CurrentValue new:   60
```

If `MaxValue` is reduced and `CurrentValue` is higher than the new `MaxValue`, `CurrentValue` is clamped to the new
`MaxValue`.

```text
MaxValue previous: 100
MaxValue new:       70
CurrentValue old:   90
CurrentValue new:   70
```

### 11.2 Recalculation Instead of Inverse Effects

Temporary max/current modifiers must be recalculated from active effects.

The engine must not rely on inverse effects to undo expired modifiers.

Recommended model:

```text
BaseValue
+ active flat modifiers
+ active additive percentage modifiers
= resolved MaxValue / CurrentValue
```

This avoids accumulated drift and supports resimulation.

---

## 12. Regeneration

Regeneration tick rate is defined as a global StateEngine setting.

The setting is provided in ms but internally used in TPS.

```text
RegenerationIntervall - setable in the conifg
GlobalRegenerationIntervalTicks - calculates once on startup from RegenerationIntervall
```

Regeneration is applied only when its interval condition is met. Based on stat this can be slow or fast lane

Regeneration should be represented internally as stat modification records or an equivalent deterministic scratch-buffer
operation.

---

## 13. Conditions

`LivingEntityCondition` is the single condition vocabulary used by the StateEngine.

A condition can be used in different roles:

- Current entity state, such as `Dead`, `Invisible`, `Poisened`, or `EnergyDepleted`

Conditions influence:

- Whether an effect can execute or is blocked
- How active effects are matched for cleanse, dispel, removal like skills

Runtime active conditions are stored as bit flags.

Preferred backing type:

```text
ulong
```

The hierarchy metadata is not stored in every entity. It is static metadata owned by the condition registry.

### 13.1 Condition List

Initial `LivingEntityCondition` values:

```text
None
Dead
Immobilized
Invulnerable
Untargetable
EnergyDepleted
UnDetectable
Invisible
Poisoned
Burning
Infected
```

`Alive` is intentionally not a condition.

If the entity is not `Dead`, it is considered alive enough for StateEngine processing. Clean State is `None`.

Lifecycle states such as `Spawned`, `Initialized`, `wait_for_revive`, and `ready_for_revive` are intentionally removed
from this PRD.

Assumption:

```text
When StateEngine is called, the input state is valid to process.
```

### 13.4 Condition Operations

Conditions are not changed directly by effects.

An effect may request that a condition should be added or removed.

The StateEngine validates the request and applies it if valid.

Suggested runtime request buffers:

```text
ConditionAddRequest[]
ConditionRemoveRequest[]
```

### 13.6 Dead Default Blocking Rule

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

This default is safer because designers must intentionally mark revive, resurrection, corpse, or death-related effects
as triggerable while dead.

## 14. Effect-Driven Gameplay

Gameplay is entirely driven by effects.

Gameplay systems do not modify `LivingEntityState` directly. Instead, they produce `ActiveEffect` instances based on
immutable `EffectDefinition` data.

Examples:

- Skills trigger effects.
- Items apply permanent effects.
- Passives apply permanent effects.
- Weapons apply effects.

The StateEngine is solely responsible for determining:

- Whether an effect may execute based on the state its processing and the states condition
- How it modifies stats
- How it requests condition changes
- Whether it triggers child effects
- Whether it stacks
- Whether it is blocked
- Whether it expires
- Whether it remains active
- Whether it is consumed
- Whether it removes other active effects

---

## 15. EffectDefinition

An **EffectDefinition** is immutable static data.

It contains an effect configuration.

It must not contain mutable runtime state.

Runtime state is stored in `ActiveEffect`.

Effect definitions are stored for now in an `EffectDefinitionRegistry`.

Suggested initial structure:

```text
scripts/
  state_engine/
    definitions/
      EffectDefinition.cs
      EffectDefinitionRegistry.cs
      EffectValidator.cs
      ...
    runtime/
      ActiveEffect.cs
      ...
```

### 15.1 EffectDefinition Fields

Suggested fields:

```text
EffectDefinition:
- EffectDefinitionId
- StackGroupId
- Name
- Description
- EffectClassifications
- Trigger
- Duration
- SimulationLane
- Target
- ConditionsAddRequest
- ConditionsRemoveRequest
- ClassificationsRemoveRequest
- ClassificationsAddRequest  
- EffectRemovalRule
- BlockedByCondition
- BlockedByClassification
- CanTriggerWhileDead
- BlockPolicy
- StackScope
- StackPolicy
- StackBehavior
- MaxStacks
- MaxStackOverflowPolicy
- Visibility
- ChildEffectDefinitionIds
- IsEffectContainer
```

### 15.2 EffectDefinition Validity Rule

An effect must contain at least one of the following:

```text
Target
ConditionAddRequest
ConditionRemoveRequest
ClassificationsRemoveRequest
ClassificationsAddRequest 

OR

ChildEffectDefinitionIds AND isEffectContainer == true (See Single Target Rule)
```

Effects with none of these are invalid and must fail validation. EffectDefinitions are validated only once at start up
and are only valid if all childs are also valid.

### 15.3 Single Target Rule

An `EffectDefinition` may have at most one stat target.

If a gameplay concept must mutate two stats, it must use child effects.

Example:

```text
EnergyDrainAndDamage
→ DrainEnergy child effect
→ DealHealthDamage child effect
→ IsEffectContainer: true
```

This keeps each effect small, explicit, reusable, and easier to validate.

### 15.4 Effect Classification

Effects use `EffectClassifications`, which are flags attached to the `ActiveEffect` for identifying and matching. E. g.
remove all `ActiveEffects` that apply `EffectClassification: Poison`, or remove all debuffs.

Initial classifications include:

```text
Poison
Fire
Bleed
Buff
Debuff
ReduceOverTime
RaiseOverTime
```

Classifications do not execute logic by themselves.

They are metadata used by the StateEngine when an effect removal rule requests removal by condition.

A classification is not applied to the target entity. It is soley used by the engine for identification.
Only conditions can be requested to be applied by state engine.

Example:

```text
PoisonDamageOverTime:
- Classifications: Poison, ReduceOverTime, Debuff
- ConditionAddRequest: Poisoned
- TargetStat: Health
- Manipulation: Reduce
- Duration: 
```

### 15.5 StackGroupId

`StackGroupId` allows multiple different `EffectDefinition` entries to share one stacking identity.

Rules:

```text
If StackGroupId is set:
    use StackGroupId for duplicate stack identity.
If StackGroupId is empty or zero:
    fallback to EffectDefinitionId.
Then apply StackScope fields.
```

This allows designers to make multiple definitions behave as the same stack family when required, while still keeping a
safe default for ordinary effects.

## 16. Effect Target

Each effect may target zero or one stat.

### 16.1 EffectTarget Fields

```text
EffectTarget:
- TargetStat
- TargetValueKind
- Manipulation
- Calculation
- Value
```

### 16.2 Effect Target Examples

Flat current damage:

```text
TargetStat: Health
TargetValueKind: Current
Manipulation: Reduce
Calculation: Flat
Value: 25
```

Max shield buff:

```text
TargetStat: Shield
TargetValueKind: Max
Manipulation: Raise
Calculation: Flat
Value: 50
```

Percentage max armor buff:

```text
TargetStat: Armor
TargetValueKind: Max
Manipulation: Raise
Calculation: Percentage
Value: 0.15
```

---

## 17. ActiveEffect

An `ActiveEffect` is runtime effect state.

It references immutable `EffectDefinition` data by ID.

It should be implemented as a small pooled struct.

### 17.1 ActiveEffect Fields

```text
ActiveEffect:
- RuntimeEffectId
- EffectDefinitionId
- SourceId
- OriginId
- TargetEntityId
- AppliedNetworkTick
- EffectNetworkTick
- LastExecutedNetworkTick
- ExpireNetworkTick
- StackCount
- ParentRuntimeEffectId
- RuntimeFlags
```

### 17.2 NetworkTick on ActiveEffect

Each `ActiveEffect` carries a runtime `EffectNetworkTick`.

This is the tick for which the effect was applied.

### 17.3 ActiveEffect Storage

The engine uses one flat `ActiveEffect[]` buffer

```text
ActiveEffect[] ActiveEffects
int ActiveEffectCount
```

Child effects are not stored as nested object collections. They are resolved on the first application and stored as
seperat ActiveEffects.

Each child effect stores:

```text
ParentActiveEffectId
ChildDepth
```

This keeps iteration fast, avoids recursive traversal, avoids child collection allocation, and allows child effect
processing to use the same deterministic queue rules as normal effects.

---

## 18. Effect Sources

Effect source information is used for causality, stacking, removal, and debug output.

### 18.1 SourceType

```text
Skills
Items
UsableItems
Talents
Weapons
ShipSystems
Environment
OtherLivingEntities
```

### 18.2 SourceId

`SourceId` identifies the concrete source that produced the effect.

Examples:

```text
Skill definition id
Item definition id
Weapon definition id
Ship system id
Environmental hazard id
```

### 18.3 OriginId

`OriginId` identifies the owner or originator behind the source.

Examples:

```text
Player id
Enemy id
Environmental system id
```

### 18.4 TargetEntityId

`TargetEntityId` identifies the entity to which the effect should be applied.

The StateEngine does not perform target acquisition.

It assumes that every effect that is processed was already a valid hit when processing it.

---

## 19. Effect Trigger

The trigger defines when the effect is executed by the StateEngine.

| Trigger                                               | Description                                                                                    |
|-------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `Immediate`                                           | Executed immediately when processed by the engine. Default.                                    |
| `OverTime(applicationIntervallInMs)`                  | Executed repeatedly for the given duration every x ms while active. Internal handled with TPS. |
| `OnExpire`                                            | Executed once when the effect duration expires.                                                |
| `OnRemoval`                                           | Executed once when the effect gets removed.                                                    |
| `OnConditionsPresent(conditions, duration)`           | Executed while or when a required condition is present.                                        |
| `OnConditionsMissing(conditions, duration)`           | Executed while or when a required condition is missing.                                        |
| `OnClassificationsPresent(Classifications, duration)` | Executed while or when a required classification is present.                                   |
| `OnClassificationsMissing(Classifications, duration)` | Executed while or when a required classification is missing.                                   |
| `OnStatReduced(duration, Stat, TargetValueKind)`      | Executed when the tracked stat is actually reduced by a valid stat modification record.        |
| `OnStatRaised(duration, Stat, TargetValueKind)`       | Executed when the tracked stat is actually raised by a valid stat modification record.         |
| `OnStatValueBelow(tresholdPercent, Stat)`             | Executed when the tracked stats current is below or equal tresholdPercent                      |
| `OnStatValueAbove(tresholdPercent, Stat)`             | Executed when the tracked stats current is above or equal tresholdPercent                      |

All trigger timing uses `NetworkTick` values at runtime.

### 19.1 OnStatRaised / OnStatReduced Rule

`OnStatRaised` and `OnStatReduced` trigger from actual validated stat modification records, not from the final
consolidated outcome.

Example:

```text
Effect A reduces Health by 20.
Effect B raises Health by 20.
Final Health is unchanged.
```

In this case:

```text
OnStatReduced(Health) must trigger because Health was actually reduced.
OnStatRaised(Health) must trigger because Health was actually raised.
```

### 19.2 OnStatValueBelow / OnStatValueAbove Rule

`OnStatValueBelow` and `OnStatValueAbove` trigger from final stat modification not from records

Example1:

```text
Health: 100
OnStatValueBelow: 80% of Health
Effect A reduces Health by 20.
Effect B raises Health by 20.
Final Health is unchanged.
```

In this case:

```text
OnStatValueBelow(80, Health) must not trigger because Health was finally not reduced.
```

Example2:

```text
Health: 100
OnStatValueBelow: 80% of Health
Effect A reduces Health by 40.
Effect B raises Health by 20.
Final Health is unchanged.
```

In this case:

```text
OnStatValueBelow(80, Health) must trigger because Health is finally at 80 which is 80% of 100.
```

---

## 20. Effect Duration

The duration defines how long an effect remains active.

| Duration        | Description                                                              |
|-----------------|--------------------------------------------------------------------------|
| `Once`          | Applied once, then consumed and removed. Default.                        |
| `Temporary(ms)` | Remains active for a fixed number of ms. Internal handled as TPS         |
| `Permanent`     | Remains active until explicitly removed by its source or another effect. |

Temporary effects expire when:

```text
CurrentNetworkTick >= ExpireNetworkTick
```

Permanent effects must not be removed automatically by duration logic.

Duration is in seconds and converted to runtime ticks during effect creation or content import.

---

## 21. Blocking

Effects may be blocked by conditions.

```text
ActiveEffect: ApplyDamage (Target: Health)
BlockedByConditions:
- Invincible
Then ApplyDamage is blocked. See how to deal with Blocking in BlockPolicy
```

Effects my be blocked by classifications from other active effects already applied

```text
BlockedByClassifications:
- ImmunToPoison
- ActiveEffect1 adds Classification: ImmunToPoison
- ActiveEffect2 has BlockedByClassification: ImmunToPoison
Then ActiveEffect 2 is blocked. See how to deal with Blocking in BlockPolicy 
```

If the target has any blocking condition or classification, the effect cannot execute for that tick.

`Dead` is special:

```text
Dead blocks by default unless CanTriggerWhileDead = true.
```

Example:

```text
Effect: NormalHeal
CanTriggerWhileDead: false
```

If the target is `Dead`, the effect does not execute.

A revive effect should use:

```text
CanTriggerWhileDead: true
ConditionRemoveRequest: Dead
TargetStat: Health
Manipulation: Raise
```

---

## 22. BlockPolicy

The `BlockPolicy` defines what happens when an effect cannot execute.

| Policy          | Description                                                                                          |
|-----------------|------------------------------------------------------------------------------------------------------|
| `ConsumeOnFail` | The effect is consumed even if execution was blocked. Default.                                       |
| `Retry`         | The effect remains active and retries execution when it is due next time (depending on its trigger). |

If an effect is blocked and its policy is `Retry`, it remains active until:

- It successfully executes,
- Its duration expires,
- It is removed by another system,
- Its source removes it.

---

## 23. Effect Removal

### 23.1 EffectRemovalRequest

Active effects may request that the StateEngine remove other active effects.

Effects do not remove other effects directly.

Instead, an `ActiveEffect` provides one or more removal requests. The StateEngine validates and applies it
deterministically. The Effect has to trigger, to iniate the removal requests. The Effect can request to remove
activeEffects identified by the following:

```text
EffectRemovalRequest:
- EffectDefinitionIds
- SourceIds
- OriginIds
- Classifications(classifications)
- Conditions(classification)
- EffectsTargetingStats(stats)
- StackGroupIds(stackGroupIds)
  
```

If the RemovalRequest can identify any achtive effects of the provided EffectRemovalRequest Fields, than
EffectRemovalPolicy is from found effect is evaluated. If evaluated to true, the effect will be removed. If the request
is poplulated with multiple fields (like EffectDefinitionIds, OriginIds) both must match. But inside OriginIds only one
must match. Example: find all EffectDefinitionIds from Definition StackableSuperBuff from a given originId (e.g.
Player).

```text
EffectRemovalRequest:
- EffectDefinitionIds: "StackableSuperBuff"
- SourceIds: "123456"
Request removal of all StackableSuperBuff ActiveEffects from source 123456  
```

### 23.2 EffectRemovalPolicy

If an activeEffect requests removal of other activeEffects, the following policies decide if it is allowed or not to
remove the effects.

```text
EffectRemovalPolicy:
- AnySource (default)
- ByEffectDefinitionId
- BySourceId
- ByOriginId
- ByClassifications(classifications)
- ByConditions(conditions)
Additional Infos:
- MaxRemoveCount
- RemoveOldestFirst
  
```

This default allows easy removal in most cases. Effects that want to be removed stricter, have to define a tighter
EffectRemovalPolicy

### 23.2 Removal Validation

An Effect gets triggerd, the state engine evaluates its removal request and provides identified effects based on the
removal request.

If `EffectRemovalPolicy` is ByEffectDefinitionId than the removal requester effect must have this EffectDefinitionId to
allow the state engine to remove the for removal targeted effect.

If the removed Effect has the the trigger OnRemoval (see triggers), than it must at least trigger once before consumed.

If `EffectRemovalPolicy` is ByClassifications(classifications), then EffectRemovalPolicy allows removal if one of the
provided classifications for `EffectRemovalPolicy` ByClassifications(classifications) must match with only one
classification from EffectRemovalRequest.classifications. Analog for Conditions.

Examples:

```text
CleansePoison:
- Classifications: Buff
- EffectRemovalRequest: Classifications(Poison)
  
Identified other Effects:
- EffectRemovalPolicy: Classifications(Buff)
OR 
- EffectRemovalPolicy: AnySource
    
Removes all Effects having classification poison
```

```text
FreeMovement:
- EffectRemovalRequest: Conditions(Paralyzed, Stunned, Entangled)
- MaxRemoveCount: 3
- RemoveOldestFirst: true
  
Upon Effect trigger, all effects containing one of (Paralyzed, Stunned, Entangled) are requested to be removed if EffectRemovalPolicy allows it. If 3 could be succesfully removed or no more left, than the removal request is finished.
```

### 23.4 Removal Ordering

When multiple active effects match a removal request, the StateEngine must remove them in deterministic order.

Default ordering:

```text
1. RemoveOldestFirst flag
2. AppliedNetworkTick
3. InsertionSequence
4. RuntimeEffectId
```

If `RemoveOldestFirst = false`, the first ordering field is reversed, but the tie-breakers remain deterministic.

### 23.5 Removal Validity

A removal request may fail if:

- The target effect does not have a matching rule.

Failure must be deterministic

## 24. Visibility

Visibility defines whether an effect is shown to the player.

| Value      | Description                        |
|------------|------------------------------------|
| `Hud`      | Shown in the HUD and skill menu.   |
| `Menu`     | Shown only in the skill menu.      |
| `Internal` | Hidden from HUD and menu. Default. |

---

## 25. Stacking

Stacking is split into four concepts:

1. `StackScope`
2. `StackPolicy`
3. `StackBehavior`
4. `MaxStackOverflowPolicy`

This keeps duplicate detection separate from how accepted stacks behave.

### 25.1 Stack Identity Key

Duplicate stack detection uses the recommended identity model:

```text
Stack identity = StackBaseId + StackScope fields
```

Where:

```text
if StackGroupId is set:
    StackBaseId = StackGroupId
else:
    StackBaseId = EffectDefinitionId
```

Then the selected `StackScope` adds runtime identity fields.

Suggested implementation boundary:

```csharp
StackIdentityKey BuildStackIdentityKey(ActiveEffect effect, EffectDefinition definition)
```

This lets designers decide whether different effect definitions belong to the same stack family by assigning a shared
`StackGroupId`.

### 25.2 StackScope

`StackScope` defines which runtime source fields are used when checking for duplicate effects.

| Scope                 | Description                                                                                                        |
|-----------------------|--------------------------------------------------------------------------------------------------------------------|
| `SameSource`          | Effects with the same `SourceId` are considered duplicates within the selected stack base identity.                |
| `SameSourceAndOrigin` | Effects with the same `SourceId` and `OriginId` are considered duplicates within the selected stack base identity. |

### 25.3 StackPolicy

`StackPolicy` defines whether duplicate effects are accepted.

| Policy    | Description                                                |
|-----------|------------------------------------------------------------|
| `Stack`   | The effect stacks according to `StackBehavior`.            |
| `Replace` | The existing effect is replaced by the new one.            |
| `Discard` | The new effect is discarded if a duplicate already exists. |

### 25.4 StackBehavior

`StackBehavior` defines how accepted stacks behave.

| Behavior                     | Description                                                    | Recommended representation                  |
|------------------------------|----------------------------------------------------------------|---------------------------------------------|
| `IndependentStacks(default)` | Each stack has its own duration and tick timer.                | Separate `ActiveEffect` entry per stack.    |
| `RefreshDuration`            | Value is unchanged; duration is refreshed.                     | One `ActiveEffect` entry.                   |
| `AddStackRefreshDuration`    | Stack count increases and duration is refreshed.               | One `ActiveEffect` entry with `StackCount`. |
| `AddStackNoRefresh`          | Stack count increases but existing duration remains unchanged. | One `ActiveEffect` entry with `StackCount`. |

`IndependentStacks` is the preferred default for stackable damage-over-time effects because it is deterministic, easy to
compute, and does not require per-stack timers inside one object.

### 25.5 MaxStackOverflowPolicy

When `MaxStacks` is reached, the engine uses `MaxStackOverflowPolicy`.

Enum:

```text
MaxStackOverflowPolicy:
- RejectNewest
- ReplaceOldest
- RefreshOldest
```

Default:

```text
ReplaceOldest
```

Oldest is determined by:

```text
1. AppliedNetworkTick
2. InsertionSequence
3. RuntimeEffectId
```

### 25.6 Stacking Examples

#### Poison Damage Over Time

```text
StackPolicy: Stack
StackBehavior: IndependentStacks
MaxStacks: designer-defined
MaxStackOverflowPolicy: ReplaceOldest
```

Result:

```text
Each poison application exists as a separate ActiveEffect.
Each has its own duration.
Each has its own tick timer.
Each may expire independently.
When MaxStacks is reached, the oldest poison stack is replaced.
```

This is deterministic and easy to compute.

#### Simple Shield Buff

```text
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

#### Rage Buff

```text
StackPolicy: Stack
StackBehavior: AddStackRefreshDuration
MaxStacks: 5
MaxStackOverflowPolicy: ReplaceOldest
```

Result:

```text
Each reapplication increases StackCount.
Duration is refreshed.
The stat value may scale by StackCount.
When MaxStacks is reached, the oldest accepted stack contribution is replaced or refreshed according to implementation representation.
```

#### Permanent Passive

```text
StackPolicy: Discard
StackBehavior: RefreshDuration not applicable
Duration: Permanent
ClassificationConditions: Permanent
```

Result:

```text
The first passive application is accepted.
Duplicate applications are discarded.
Permanent passive effects may belong to the fast or slow simulation lane depending on gameplay impact.
Permanent passive modifiers recalculate only when added, removed, or dirtied by source state.
Permanent periodic effects execute every slow tick or by their configured interval.
```

## 26. Child Effects

Effects may trigger child effects.

Child effects are required because they allow complex effects to be structured as smaller reusable pieces.

An effect may use child effects to:

- Apply multiple stat changes while keeping each child single-target.
- Separate condition application from stat mutation.
- Structure complex effects into deterministic sub-effects.
- Reuse smaller effect definitions.

Example:

```text
EmergencyRepair
→ RestoreHealth child effect
→ RestoreShield child effect
```

Example:

```text
Revive
→ RestoreHealth child effect
→ RemoveDeadCondition child effect
→ ApplyTemporaryInvulnerability child effect
```

### 26.1 No Target Acquisition

The StateEngine does not perform area targeting or target acquisition.

If an explosion affects several entities, another system must determine affected entities and gather the applied
effects. StateEngine is always called with valid already preselected Effects.

### 26.2 Child Effect Storage

Child effects are represented as `ActiveEffect` entries.

They are queued in the same tick buffer and processed deterministically.

Each child stores:

```text
ParentRuntimeEffectId
ChildDepth
```

### 26.3 Child Depth Fail-Safe

Hard fail-safe limit:

```text
MaxChildEffectDepth = 5
```

If this limit is exceeded:

1. The child effect chain is aborted.
2. The event is logged only when TRACE/debug diagnostics are active.
3. The simulation continues safely.

Designers are responsible for assembling valid effect chains. The engine must protect the host simulation from runaway
graphs.

### 26.4 Per-Tick Effect Budget

The engine must define:

```text
MaxEffectsProcessedPerEntityPerTick
```

If this budget is exceeded, deterministic overflow behavior is:

```text
Reject additional effects for this tick.
Log overflow only when TRACE/debug diagnostics are active.
Continue simulation.
```

---

## 27. Deterministic Effect Processing Order

Effects are processed in deterministic order.

When `ProcessLivingEntities` is called:

- All passed effects need to be sorted by networktick. Participating Players mave send data with different NetworkTicks
  depending on der RTT.
- The StateEngine must process all of them ordered by NetworkTick for the right targeted livingentity.
- The Process function needs to run every NetworkTick and must be work with scratch buffers and group processing, to
  avoid any perofmance or allocation overhead.

Processing order:

1. Order incoming effects by networktick.
2. Apply incoming effects for each networktick.

For each Tick:

1. Add accepted effects to the entity effect buffer.
2. Resolve child effects and place it in order as active effects depending on their parent effects.
3. Process active effects in deterministic order.
4. Resimulate all ticks between the provided effect tick and the current tick if required.
5. Discard stored states older than the provided tick after reconciliation.

Stable ordering keys:

```text
AppliedNetworkTick
InsertionSequence
ParentRuntimeEffectId
ChildSequence
RuntimeEffectId
```

Recommended priority:

1. Existing active effects by stable insertion sequence.
2. New incoming effects by passed buffer order.
3. Child effects by parent execution order.

---

## 28. StateEngine

The `StateEngine` is responsible for processing incoming effects and producing the next valid `LivingEntityState`.

The public API remains static.

### 28.1 StateEngine Responsibilities

The StateEngine must:

- Store living entity state history
- Store effect history
- Track which entity has which active effects.
- Keep living entity condition up to date.
- Process active effects.
- Resimulate past ticks.
- Calculate new `LivingEntityState` values.
- Generate compact deltas.
- Use a runtime-fixed ruleset during processing.
- Avoid allocations in the hot path.

### 28.2 Public API

The API should remain small and explicit.

#### Register Living Entity

```csharp
public static void RegisterLivingEntity(LivingEntityState initialState, livingEntityId)
```

Adds a `LivingEntityState` to the engine and starts tracking it.

#### Process Living Entities

```csharp
public static PackedByteArray ProcessLivingEntity(
    ActiveEffect[] activeEffects)

activeEffects: includes all Effects from host and agents since the last tick as well as all effects from networking players. 

OutPut: A Combines delta for all registered living entities.
```

Rules:

```text
Every effect must have EffectNetworkTick == networkTick.
Every effect must have TargetEntityId == entityId.
```

The returned `PackedByteArray` is the compact delta.

If no state changed, the returned delta may be empty.

---

## 29. StateEngine Tick Lifecycle

Per host state tick:

1. Take registered mutable entity states.
2. Clear per-tick scratch buffers.
3. Add incoming `ActiveEffect` instances to entity effect buffer - ordered by networkTick
4. Resolve stack policies without allocation.
5. Evaluate blocking conditions.
6. Evaluate blocking classifications.
7. Queue child effects into same tick buffer
8. Consume due effects in deterministic order.
9. Expire or retain active effects using deterministic removal.
10. Recalculate derived max/current stat changes:
    1. Max flat modifiers
    2. Max percentage modifiers
    3. Max/current compensation
    4. Current flat modifiers
    5. Current percentage modifiers
    6. Clamp
11. Apply regeneration.
12. Infer conditions, for example `Health <= 0 => Dead`.
13. Mark dirty stats, conditions, and effects.
14. Emit delta only if state is dirty.

### 29.1 Removal Strategy

Because deterministic order matters, compact-in-place removal is used.

```text
Use compact-in-place removal.
```

---

## 30. LivingEntityStateTracker

The **LivingEntityStateTracker** stores state history and effect history
It is used for:

- Resimulating from corrected ticks in StateEngine
- Deleting obsolete state and effect history

### 30.1 Responsibilities

The tracker must:

- Provide all neccessary data for a given NetworkTick, so that StateEngine can resimulate to current tick.

### 30.2 Snapshot and Effect History Retention

For any received delta tick, the receiver must still have enough data to resimulate from that tick to the current tick:

This implementation must expose snapshot and effect-history retention in `StateEngineConfig`. The default value is 45
ticks.

### 30.3 Snapshot Storage Model

Snapshots should use fast implementations and depend on arrays rather than objects. A Snapshot must provide everything
for a given networktick and applied effects for that tick, to resimulate all registered states up to current tick.

### 30.4 Target Scale

Initial target scale:

```text
Max living entities active at once: 250
Average active (non permanent) effects per entity: 35
High-load player active effects: 70 or more
Max incoming effects per entity per tick: 30
Max child effects per entity per tick: 3
MaxChildEffectDepth: 5
Target platform: desktop PC
```

This scales are for information, do not entforce those limits - except as statet before MaxChildEffectDepth to prevent a
complete disaster.

The uneven distribution is important: the engine must not assume active effects are evenly distributed across entities.

### 30.5 Current Implementation Scope

The first implementation is host-side focused.

The engine must process active effects delivered from other players, including:

```text
EffectNetworkTick
TargetEntityId
SourceId
OriginId
```

Client prediction or correction is not required for the first implementation, but the data model must not block later
delta application from host to client and resimulation.

---

## 31. Delta Format

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

### 32.1 Proposed Round-Trip Solution

Generation:

```text
StateEngine detects dirty state.
DeltaWriter rents or reuses a byte buffer.
DeltaWriter writes deterministic binary payload.
StateEngine returns PackedByteArray at the Godot-facing boundary.
```

The engine should avoid converting the delta into `Godot.Collections.Array` during normal processing.

### 32.2 Delta Header

Suggested delta header:

```text
DeltaHeader:
- DeltaVersion
- EntityId
- NetworkTick
- SimulationLane
- PayloadFlags
- DirtyMaskStats
- DirtyMaskConditions
- DirtyMaskEffects
- FinalRecordCount
```

### 32.3 Final State Payload

The delta must always include final authoritative changed state.

Suggested final state payload:

```text
ChangedStats:
- StatIndex
- CurrentValue
- MaxValue
- BaseValue only if ever supported later

ChangedConditions:
- ulong ConditionFlags

ChangedEffects:
- Added ActiveEffect runtime ids
- Removed ActiveEffect runtime ids
- Updated stack count
- Updated remaining ticks
```

### 32.5 Delta Versioning

Delta format must be versioned.

```text
DeltaVersion: ushort or byte
```

Versioning is required because future stat/effect changes may alter payload layout.

### 32.6 Payload Flags

Suggested payload flags:

```text
HasFinalStats
HasFinalConditions
HasFinalEffects
CompressedPayload
```

The first implementation does not need compression, but the flag should be reserved.

---

## 34. Logging

Logging is intended for debugging and balancing.

Use Logging only when TRACE is enabled.

However, logging must never allocate when TRACE is disabled.

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

## 35. StateEngineConfig

The first implementation must expose core runtime limits in `StateEngineConfig` instead of hardcoding them.

Suggested initial fields:

```text
StateTicksPerSecond = 30
SlowSimulationDivisor = 2
RegenerationInterval = 500ms
MaxChildEffectDepth = 5
SnapshotRetentionTicks = 45
StatComparisonPrecisionDecimals = 3
```

## 36. Performance and Allocation Requirements

The StateEngine runs at 30 TPS for fast state and 15 TPS for slow state.

The hot tick loop must avoid avoidable heap allocation.

### 36.2 Avoid in Hot Path

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

### 36.3 Scratch Buffers

Per-tick scratch buffers should be:

- Preallocated
- Reused
- Cleared without deallocation
- Sized by configuration
- Protected by deterministic overflow behavior
- used whereever possible

Examples:

```text
PendingEffectsThisTick
StatModificationRecords
FlatCurrentModifiers
FlatMaxModifiers
PercentCurrentModifiers
PercentMaxModifiers
DirtyStatIndices
ConditionRequestBuffers
EffectRemovalRequestBuffers
DeltaWriterBuffer
```

---

## 37. Determinism Requirements

The StateEngine must produce identical results for identical inputs.

To achieve this:

- Use long `NetworkTick` values.
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

If random behavior is required later, it must be based on deterministic seeds derived from:

```text
NetworkTick
EntityId
RuntimeEffectId
SourceId
OriginId
```

---

## 38. Validation Requirements

The PRD requires an `EffectValidator`.

`EffectValidator` checks all effect definitions at startup before normal simulation begins.

### 38.1 EffectValidator Responsibilities

The validator must check:

- Effect has at least one of target, condition request, classification request, or child effect.
- `MaxStacks < 1` for stackable effects.
- Child effect graph can exceed `MaxChildEffectDepth`.
- Effect references itself in any child depth, starting from any child. Endless loops must not happen.
- Effect graph contains cycles. Endless loops must not happen.
- Duration below 0.1 seconds before conversion.

### 38.2 Validation Timing

Validation occurs at StateEngine startup - not while game is already running. Logger.Error is used for invalid
EffectDefinitions.

### 38.3 Validation Output

The validator should produce a deterministic report:

```text
EffectDefinitionId
ValidationViolation
```

## 39. Example Effect Scenarios

### 39.1 Normal Heal

```text
Effect:
- TargetStat: Health
- TargetValueKind: Current
- Manipulation: Raise
- Calculation: Flat
- Duration: Once
- CanTriggerWhileDead: false
- BlockedByClassification: Incurable
- Amount: 35.0
- Trigger: Immediate
```

Result:

```text
Raises current Health once.
Fails if target is Dead or Unhealable.
Consumed after execution.
```

### 39.2 Revive

```text
Parent Effect:
- CanTriggerWhileDead: true
- Child: RestoreHealth
- Child: RemoveDeadCondition
```

RestoreHealth child:

```text
TargetStat: Health
TargetValueKind: Current
Manipulation: Raise
Calculation: Flat
Duration: Once
CanTriggerWhileDead: true
Trigger: Immediate
```

RemoveDeadCondition child:

```text
ConditionRemoveRequest: Dead
Duration: Once
CanTriggerWhileDead: true
 Trigger: Immediate
```

Result:

```text
Can trigger while Dead.
Restores Health.
Requests removal of Dead.
If Health > 0 at end-of-tick, Dead remains removed.
```

### 39.3 Shield Booster

```text
Effect:
- TargetStat: Shield
- TargetValueKind: Max
- Manipulation: Raise
- Calculation: Flat
- Duration: Temporary
- StackPolicy: Replace
- StackBehavior: RefreshDuration
```

Result:

```text
Raises Max Shield temporarily.
If Max Shield increases, Current Shield increases by the same absolute amount.
When expired, Max Shield is recalculated and Current Shield is clamped if needed.
```

### 39.4 Poison Damage Over Time

```text
Effect:
- ClassificationConditions: Poison
- ConditionAddRequest: Poison optional
- TargetStat: Health
- TargetValueKind: Current
- Manipulation: Reduce
- Calculation: Flat
- Trigger: OverTime(duration, everyNTicks)
- StackPolicy: Stack
- StackBehavior: IndependentStacks
- MaxStacks: designer-defined
```

Result:

```text
Each poison application is its own ActiveEffect.
Each stack keeps its own duration and tick interval.
When MaxStacks is reached, the provisional behavior is ReplaceOldest.
```

### 39.5 EMP

```text
Effect:
- ConditionAddRequest: EnergyDepleted
- Duration: Temporary
- Visibility: Hud
```

Result:

```text
Requests EnergyDepleted while active.
Energy-based gameplay systems may check this condition.
```

### 39.6 Cleanse

```text
Effect:
- EffectRemovalRule: remove effects matching condition Poison or matching EffectDefinitionId Poison
- Duration: Once
```

Result:

```text
The effect requests removal.
StateEngine deterministically removes matching valid active effects.
```

### Effects open for implementation/discussion how to

#### Retaliation

Everytime damage is applied, the target entity reflects a percent of the applied damage back

#### Lifeleach

Everytime damage is applied, the target entity heals a percent of applied damage.

#### CooldownReducer

Everytime a target is healed, all active cooldowns get reduced

---

## 40. Implementation Milestones

Milestone 1 is done when deterministic test scenarios can process a fixed set of `SimulationSteps` for given registered
`LivingEntityStates` and provided `ActiveEffect` inputs for given SimulationSteps, that produce expected final state and
delta data, pass rollback/resimulation tests, and show no avoidable hot-path allocations under the selected test scale.

Required implementation pieces:

```text
LivingEntityState
Stats arrays
Condition flags
EffectDefinition registry
EffectValidator startup check
ActiveEffect flat buffer
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

Run test with given numbers and simluate for 50 Steps. Log final Time. 
```

### 40.2 Milestone 2 — Effect Complexity

Required:

```text
EffectDefinitions
EffectDefinitionRegistry
StateEngine
ActiveEffects
StateHistory
EffectHistory
```

## 41. Testing and Acceptance Criteria

The PRD requires the following test categories.

### 41.1 Deterministic Replay Tests

Given identical initial state and identical ordered effects, the engine must produce identical results across repeated
runs.

### 41.2 Stacking Tests

Must cover:

```text
IndependentStacks
RefreshDuration
AddStackRefreshDuration
AddStackNoRefresh
MaxStacks ReplaceOldest
Discard
Replace
```

### 41.3 Death/Revive Tests

Must cover:

```text
Health <= 0 adds Dead
Dead blocks normal effects by default
Revive can trigger while Dead
Revive removes Dead only if Health > 0
Dead is re-applied if Health remains <= 0
```

### 41.4 Delta Round-Trip Tests

Must cover:

```text
Generate delta
Serialize to PackedByteArray
Read delta
Apply final state
Read causal records
Compare corrected result
```

### 41.5 Rollback/Resimulation Tests

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

### 41.6 No-Allocation Hot-Path Tests

Must verify that the tick loop avoids avoidable heap allocation during normal processing.

### 41.7 Child-Effect Overflow Tests

Must cover:

```text
MaxChildEffectDepth = 5
Overflow rejected deterministically
TRACE log only when enabled
Simulation continues safely
```

### 41.8 Condition Rebuild Tests

Must cover:

```text
Effect-granted conditions are rebuilt deterministically
Expired effects do not leave stale conditions
Multiple active effects granting same condition do not remove it too early
```

---
