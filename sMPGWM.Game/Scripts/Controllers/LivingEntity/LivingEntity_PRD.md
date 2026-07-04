# LivingEntity / RPG State Engine PRD

## Status

This document reflects the current implementation in `sMPGWM.Game/Scripts/Controllers/LivingEntity`.

The old `StatModifier`, `TemporaryStatModifier`, `RecurringStatModifier`, `LivingEntityEffect`,
`StatBreakdown`, and Godot `Array` delta model have been removed from the implementation.

The current implementation is an effect-driven RPG state engine:

```text
Gameplay code creates ActiveEffect values.
StateEngine validates and processes those effects.
StateEngine mutates LivingEntityState.
StateEngine emits compact packed binary delta payloads when state changes.
UI pulls UiDto data only when needed.
```

## Goals

The living-entity package should provide deterministic, host-authoritative simulation for RPG state on ships and future living entities.

Primary requirements:

- Use integer `NetworkTick` timing.
- Keep `LivingEntityState` array-backed and compact.
- Process all gameplay mutation through `StateEngine`.
- Keep effect definitions immutable.
- Keep active effects flat and fixed-capacity.
- Emit compact binary deltas.
- Support rollback/resimulation.
- Keep UI data pull-based through `UiDto`.
- Keep causal records separate from normal UI DTOs.

## Non-Goals

This package does not own:

- Movement physics.
- Rendering or presentation logic.
- Target acquisition.
- Steam transport.
- Skill, item, weapon, talent, or ship-system authoring.
- Final content-definition loading from external files.

Those systems may create `ActiveEffect` values, but state mutation remains owned by `StateEngine`.

## Current Entry Points

### `StateEngine`

File: `StateEngine.cs`

`StateEngine` is the static simulation entry point.

Public API:

```csharp
public static StateEngineConfig Config { get; }
public static void Configure(StateEngineConfig config)
public static void Reset()
public static void AddLivingEntity(LivingEntityState state)
public static PackedByteArray ProcessLivingEntity(
    long entityId,
    long networkTick,
    ActiveEffect[] activeEffects,
    int activeEffectCount)
public static UiDto GetUpdateForUiLoop(long entityId)
public static void ApplyDelta(long entityId, PackedByteArray delta)
public static bool StatesEqualForReconciliation(
    LivingEntityState left,
    LivingEntityState right)
```

Rules:

- `ProcessLivingEntity` expects ordered effects for one entity and one network tick.
- Effects with mismatched `TargetEntityId` or `EffectNetworkTick` are ignored.
- Missing or invalid effect definitions are rejected deterministically.
- The returned `PackedByteArray` is empty when no state changed.
- `GetUpdateForUiLoop` is pull-based and should not be called automatically every tick.

### `LivingEntityService`

File: `LivingEntityService.cs`

Godot singleton facade around `LivingEntityServiceCore`.

It exists for scene/autoload usage and delegates state work to `StateEngine`.

### `LivingEntityServiceCore`

File: `LivingEntityServiceCore.cs`

Non-node facade used by tests or plain C# code.

It stores peer-id to `LivingEntityRuntime` mappings and forwards:

- entity registration
- effect processing
- UI DTO requests
- authoritative delta application
- condition-changed event forwarding

### `LivingEntityRuntime`

File: `LivingEntityRuntime.cs`

Small facade around one `LivingEntityState`.

It does not own complex mutation logic. It exposes:

- `State`
- `Condition`
- `ConditionChanged`
- `IsAlive`
- `IsTargetable`
- `GetStatByType`

## Object Graph

The graph below shows which objects directly reference which other objects in the current implementation.

```text
LivingEntityService (Godot singleton facade)
  |
  +-- LivingEntityServiceCore _core
        |
        +-- Dictionary<int, LivingEntityRuntime> _runtimes
        |     |
        |     +-- LivingEntityRuntime
        |           |
        |           +-- LivingEntityState State
        |
        +-- calls StateEngine static API


StateEngine (static)
  |
  +-- StateEngineConfig _config
  |
  +-- Dictionary<long, LivingEntityStateTracker> Trackers
  |     |
  |     +-- LivingEntityStateTracker
  |           |
  |           +-- LivingEntityState CurrentState
  |           |
  |           +-- LivingEntityState[] _snapshots
  |           |
  |           +-- ActiveEffect[][] _effectHistory
  |
  +-- DeltaWriter _deltaWriter
  |
  +-- scratch buffers
        |
        +-- ActiveEffect[] _pendingChildEffects
        +-- StatModificationRecord[] _statModificationRecords
        +-- CausalRecord[] _causalRecords
        +-- float[] _oldBaseValues
        +-- float[] _oldMaxValues
        +-- float[] _oldCurrentValues
        +-- int[] _matchedRemovalIndices


LivingEntityState
  |
  +-- float[] BaseValues
  +-- float[] MaxValues
  +-- float[] CurrentValues
  |
  +-- ActiveEffect[] ActiveEffects
  |
  +-- condition fields
        |
        +-- ulong ExplicitConditionFlags
        +-- ulong DerivedConditionFlags
        +-- ulong ConditionFlags


ActiveEffect
  |
  +-- int EffectDefinitionId
  |
  +-- runtime ids and timing
        |
        +-- RuntimeEffectId
        +-- SourceId
        +-- OriginId
        +-- TargetEntityId
        +-- AppliedNetworkTick
        +-- EffectNetworkTick
        +-- LastExecutedNetworkTick
        +-- ExpireNetworkTick
        +-- ParentRuntimeEffectId
        +-- InsertionSequence
        +-- ChildSequence

Note:
ActiveEffect does not hold an object reference to EffectDefinition.
It stores EffectDefinitionId and StateEngine resolves the definition through EffectDefinitionRegistry.


EffectDefinitionRegistry (static)
  |
  +-- Dictionary<int, EffectDefinition> Definitions
  +-- HashSet<int> InvalidDefinitionIds


EffectDefinition
  |
  +-- EffectTarget? Target
  +-- EffectTrigger Trigger
  +-- EffectDuration Duration
  +-- EffectRemovalRule? EffectRemovalRule
  +-- int[] ChildEffectDefinitionIds
  |
  +-- condition masks
        |
        +-- ClassificationConditions
        +-- ConditionAddRequest
        +-- ConditionRemoveRequest
        +-- BlockedBy


ConditionRegistry (static)
  |
  +-- ConditionDefinition[] Definitions
  +-- ulong[] ParentByBit
  +-- ulong[] AncestorClosureByBit
  +-- ulong[] DescendantClosureByBit


DeltaWriter
  |
  +-- reads LivingEntityState
  +-- reads CausalRecord[]
  +-- writes PackedByteArray


DeltaReader
  |
  +-- reads PackedByteArray
  +-- creates StateDelta
        |
        +-- ChangedStatPayload[] ChangedStats
        +-- ActiveEffect[] ActiveEffects
        +-- CausalRecord[] CausalRecords


StateEngine.GetUpdateForUiLoop(entityId)
  |
  +-- reads LivingEntityState
  +-- resolves EffectDefinition through EffectDefinitionRegistry
  +-- creates UiDto
        |
        +-- UiStatDto[] Stats
        +-- UiEffectDto[] VisibleEffects
```

## Runtime Data Model

### `LivingEntityState`

File: `LivingEntityState.cs`

Stores complete mutable simulation state for one living entity.

Core storage:

```csharp
public float[] BaseValues { get; }
public float[] MaxValues { get; }
public float[] CurrentValues { get; }
public ActiveEffect[] ActiveEffects { get; }
public int ActiveEffectCount { get; internal set; }
public ulong ConditionFlags { get; }
```

Important details:

- `StatType` maps directly to array indices.
- Active effect capacity is fixed at state creation.
- The state stores explicit, derived, and effective condition flags.
- Dirty tracking is stored as `DirtyStats`, `DirtyConditions`, and `DirtyEffects`.
- Convenience properties such as `Health`, `Shield`, and `EnergyStorage` return `Stat` snapshots over the arrays.

### `Stat`

File: `Stat.cs`

`Stat` is now an immutable snapshot:

```csharp
public float BaseValue { get; }
public float CurrentValue { get; }
public float MaxValue { get; }
```

It no longer owns modifiers or tick logic.

### `ActiveEffect`

File: `Runtime/ActiveEffect.cs`

`ActiveEffect` is a flat runtime struct.

It stores runtime identity and timing:

```csharp
RuntimeEffectId
EffectDefinitionId
SourceId
OriginId
TargetEntityId
AppliedNetworkTick
EffectNetworkTick
LastExecutedNetworkTick
ExpireNetworkTick
StackCount
ParentRuntimeEffectId
ChildDepth
RuntimeFlags
InsertionSequence
ChildSequence
```

It does not reference child collections and does not reference `EffectDefinition` directly.

### `EffectDefinition`

File: `Definitions/EffectDefinition.cs`

Immutable static data that describes how an active effect behaves.

Important fields:

```csharp
EffectDefinitionId
StackGroupId
Name
Description
SourceType
ClassificationConditions
Trigger
Duration
SimulationLane
Target
ConditionAddRequest
ConditionRemoveRequest
EffectRemovalRule
BlockedBy
CanTriggerWhileDead
BlockPolicy
StackScope
StackPolicy
StackBehavior
MaxStacks
MaxStackOverflowPolicy
Visibility
ChildEffectDefinitionIds
```

Validity rule:

An effect definition must contain at least one of:

- `Target`
- `ConditionAddRequest`
- `ConditionRemoveRequest`
- `EffectRemovalRule`
- `ChildEffectDefinitionIds`

## Tick Model

The implementation uses `NetworkTick` as an integer `long`.

Rules:

- Fast simulation is 30 TPS.
- Slow simulation is derived from fast ticks.
- Slow lane condition is `networkTick % SlowSimulationDivisor == 0`.
- Runtime durations and intervals use integer ticks.
- No wall-clock time is used by the state engine.

Default config:

```csharp
StateTicksPerSecond = 30
SlowSimulationDivisor = 2
GlobalRegenerationIntervalTicks = 30
GlobalSlowRegenerationIntervalTicks = 15
MaxChildEffectDepth = 5
MaxChildEffectsPerEntityPerTick = 3
MaxIncomingEffectsPerEntityPerTick = 20
SnapshotRetentionTicks = 45
MinSnapshotRetentionTicks = 30
MaxSnapshotRetentionTicks = 60
StatComparisonPrecisionDecimals = 3
```

## Effect Processing Flow

Current processing order:

```text
1. Load tracker/current state.
2. Clear scratch buffers.
3. Capture previous stat/condition values.
4. Store incoming effect history for rollback.
5. Validate incoming effect tick/entity/definition.
6. Accept incoming effects into the flat active-effect buffer.
7. Apply stack policy and overflow behavior.
8. Process active effects in deterministic buffer order.
9. Queue child effects into the bounded per-tick child buffer.
10. Process stat-triggered effects.
11. Compact removed effects in place.
12. Resolve stats.
13. Apply regeneration if interval is due.
14. Rebuild effect-granted and inferred conditions.
15. Mark dirty state by comparing to previous values.
16. Store snapshot.
17. Emit packed delta if dirty.
```

## Stat Resolution

Stats use array-backed values:

```text
BaseValues[(int)StatType.Health]
MaxValues[(int)StatType.Health]
CurrentValues[(int)StatType.Health]
```

Resolution behavior:

- Temporary max modifiers are recalculated from active effects.
- Current-value modifications come from executed stat modification records.
- Percentage modifiers are additive, not multiplicative.
- If max increases, current increases by the same absolute delta.
- If max decreases and current exceeds max, current is clamped.
- Current is clamped after current modifiers and after regeneration.

## Conditions

Conditions use `LivingEntityCondition`, a single flag enum for:

- runtime entity state
- active-effect classification
- blocking
- effect-removal matching
- UI grouping
- validation

The implementation stores:

```text
ExplicitConditionFlags
DerivedConditionFlags
ConditionFlags = explicit + derived + ancestor closure
```

The condition hierarchy is static in `ConditionRegistry`.

Examples:

```text
Poison
  -> DamageOverTime
      -> Debuff

EnergyDepleted
  -> Energy

Immobilized
  -> Movement
```

If an effect grants `Poison`, the effective condition flags also include `DamageOverTime` and `Debuff`.

Derived conditions currently include:

- `Health <= 0` adds `Dead`.
- `EnergyStorage <= 0` adds `EnergyDepleted`.

## Death and Revive

`Dead` blocks effects by default.

An effect can execute while dead only when:

```csharp
CanTriggerWhileDead = true
```

Revive-style behavior should be modeled as a parent effect with child effects:

```text
Revive
  -> RestoreHealth
  -> RemoveDead
```

End-of-tick inference re-adds `Dead` if health remains `<= 0`.

## Stacking

Stack identity:

```text
StackBaseId = StackGroupId != 0 ? StackGroupId : EffectDefinitionId
StackScope adds SourceId or SourceId + OriginId
```

Supported concepts:

- `EffectStackPolicy.Stack`
- `EffectStackPolicy.Replace`
- `EffectStackPolicy.Discard`
- `EffectStackBehavior.IndependentStacks`
- `EffectStackBehavior.RefreshDuration`
- `EffectStackBehavior.AddStackRefreshDuration`
- `EffectStackBehavior.AddStackNoRefresh`
- `MaxStackOverflowPolicy.RejectNewest`
- `MaxStackOverflowPolicy.ReplaceOldest`
- `MaxStackOverflowPolicy.RefreshOldest`
- `MaxStackOverflowPolicy.ReplaceWeakest`

Current note:

`ReplaceWeakest` is only valid when `HasDeterministicStrengthComparison` is true. Otherwise validation reports an error.

## Child Effects

Child effects are queued as normal `ActiveEffect` structs.

They store:

```text
ParentRuntimeEffectId
ChildDepth
ChildSequence
```

Limits:

```csharp
MaxChildEffectDepth = 5
MaxChildEffectsPerEntityPerTick = 3
```

Overflow behavior:

- Reject extra child effects deterministically.
- Add causal records when causal records are enabled.
- Continue simulation.

## Deltas

### `PackedByteArray`

File: `Runtime/PackedByteArray.cs`

This project uses a local `PackedByteArray` wrapper because the current Godot C# binding in this project does not expose a CLR `PackedByteArray` type.

The wrapper contains packed bytes and exposes:

```csharp
Count
IsEmpty
ToArray()
Empty
```

### `DeltaWriter`

File: `Runtime/DeltaWriter.cs`

Writes compact little-endian binary payloads.

The payload can include:

- header
- changed stats
- final effective condition flags
- final active effects
- causal records

### `DeltaReader`

File: `Runtime/DeltaReader.cs`

Reads packed bytes into `StateDelta`.

### `StateDelta`

File: `Runtime/StateDelta.cs`

Structured decoded delta data:

```csharp
DeltaVersion
EntityId
NetworkTick
SimulationLane
PayloadFlags
DirtyStats
ConditionFlags
ChangedStats
ActiveEffects
CausalRecords
```

## Causal Records

Causal records explain why state changed.

Examples:

- effect executed
- effect blocked
- effect expired
- effect removed
- stack added
- stack refreshed
- child effect queued
- child effect rejected
- condition add accepted
- condition remove accepted
- stat modification record
- capacity overflow

Causal records are not part of normal `UiDto`.

Use them for:

- deterministic tests
- replay
- debug inspection
- explaining state changes

Do not use them as the normal HUD/menu state model.

## UI DTO

File: `UiDto.cs`

`UiDto` is the only current UI inspection DTO.

It contains:

```csharp
EntityId
NetworkTick
UiStatDto[] Stats
LivingEntityCondition Conditions
UiEffectDto[] VisibleEffects
```

`UiStatDto` contains:

```csharp
StatType
CurrentValue
MaxValue
```

`UiEffectDto` contains display-ready effect state for visible effects:

```csharp
RuntimeEffectId
EffectDefinitionId
SourceId
OriginId
Name
Description
StackCount
RemainingTicks
TriggerType
Calculation
AffectedStat
ClassificationConditions
GrantedConditions
IsBlocked
Duration
TickInterval
Manipulation
ValuePerTick
```

Visibility rules:

- `EffectVisibility.Hud` appears in `VisibleEffects`.
- `EffectVisibility.Menu` appears in `VisibleEffects`.
- `EffectVisibility.Internal` does not appear in normal `UiDto`.

## Validation

File: `Definitions/EffectValidator.cs`

Validation checks:

- empty effect definitions
- invalid max stack counts
- `ReplaceWeakest` without deterministic strength comparison
- permanent effects with `OnExpire`
- missing child effect references
- child depth overflow
- child cycles
- unknown condition references

Invalid definitions are marked in `EffectDefinitionRegistry` and rejected at runtime.

## Snapshot and Resimulation

File: `LivingEntityStateTracker.cs`

The tracker stores:

```csharp
LivingEntityState CurrentState
LivingEntityState[] _snapshots
ActiveEffect[][] _effectHistory
```

Use cases:

- apply authoritative host deltas
- find a past tick snapshot
- replay stored effects from the corrected tick
- discard obsolete history

Reconciliation comparison:

- stats compare at `StatComparisonPrecisionDecimals`
- conditions compare by exact effective condition flags

## Stat List

`StatType` maps directly to array indices.

Current values:

```text
Health
EnergyStorage
Shield
Armor
Speed
Acceleration
OxygenStorage
FoodStorage
WaterStorage
CriticalHitChance
CriticalHitBonusDamage
AttackSpeed
HealthRegeneration
EnergyProduction
EnergyConsumption
ShieldRegeneration
ShieldRechargeCooldown
OxygenConsumption
OxygenGeneration
FoodConsumption
FoodGeneration
WaterConsumption
WaterGeneration
```

Compatibility aliases:

```text
Energy = EnergyStorage
EnergyRegeneration = EnergyProduction
```

## Condition List

Current `LivingEntityCondition` flags:

```text
None
Dead
Immobilized
Invulnerable
Untargetable
EnergyDepleted
Invisible
Poison
Burn
Bleed
Buff
Debuff
DamageOverTime
HealOverTime
Movement
Energy
Shield
Survival
Permanent
Internal
```

## Test Coverage

Current tests cover:

- temporary max modifier recalculation
- max/current compensation
- additive percentage modifiers
- regeneration interval behavior
- factory id and capacity behavior
- death and revive blocking rules
- poison ancestor condition closure
- cleanse/removal matching
- independent stack replacement
- child effect overflow
- delta round trip
- rollback correction and effect-history replay
- deterministic replay

Current command:

```text
dotnet test
```

## Known Implementation Notes

Current differences from the ideal full PRD:

- `PackedByteArray` is a local wrapper around `byte[]`, not a Godot native CLR type.
- Content definitions are registered in memory through `EffectDefinitionRegistry`.
- `StateEngine` is static and uses static scratch buffers, so parallel simulation calls are not currently supported.
- TRACE logging is not implemented yet.
- No-allocation hot-path tests are not implemented yet.
- `ReplaceWeakest` validation exists, but actual strength comparison is not implemented.
- `StatSynchronizer` and `MpControllerConditionSystem` remain placeholders.

## Review Checklist

When reviewing changes in this package, verify:

- `ActiveEffect` stores ids, not object references to definitions.
- `LivingEntityState` stays array-backed.
- New runtime timing uses `NetworkTick`, not seconds.
- New mutation paths go through `StateEngine`.
- New UI state is exposed through `UiDto`, not causal records.
- Condition matching uses `LivingEntityCondition`, not a separate tag enum.
- New effect-removal behavior is deterministic.
- Child effects remain flat and bounded.
- Delta changes remain versioned and binary.
