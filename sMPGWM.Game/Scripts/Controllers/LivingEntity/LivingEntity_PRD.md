# LivingEntity PRD

## Purpose

`LivingEntity` is the gameplay state package for living objects in the game: players, NPCs, enemies, agents, and future boss entities.

The package owns stat values, stat modifiers, living-entity conditions, state snapshots, and ordered state deltas for host-authoritative multiplayer sync.

Movement synchronization is handled by another package. This package only exposes movement-relevant stat values and conditions, such as speed, acceleration, immobilized, dead, invisible, invulnerable, and targetability.

## Core Principles

1. The host is authoritative.
2. External systems do not directly mutate `LivingEntityState`.
3. External systems submit `LivingEntityEffect` instances to `LivingEntityService`.
4. `LivingEntityService` is the only class that should react to living entity state changes.
5. `LivingEntityState` is primarily a serializable data model.
6. `LivingEntityRuntime` wraps one state and owns runtime-only condition source tracking.
7. `LivingEntityStateTracker` snapshots state and creates ordered delta arrays for sync.
8. `StatBreakdown` is for UI/debug explanation only, not gameplay decisions.
9. Conditions are source-tracked so one expired effect does not remove a condition still granted by another source.
10. Reducing `Health` to zero is a special service-owned outcome that adds `Dead` and drains shield/energy.

## Ownership Model

### Runtime Flow

```text
External system
  -> LivingEntityService.ApplyEffect(peerId, effect)
  -> LivingEntityServiceCore processes effect
  -> LivingEntityRuntime updates condition sources if needed
  -> LivingEntityState stores final stat/condition values
  -> LivingEntityService.ConditionChanged notifies subscribers
```

### Sync Flow

```text
Host service owns state
  -> GetDelta(peerId, includePermanent)
  -> send ordered Godot Array over network
  -> remote service ApplyDelta(delta)
  -> state updated
  -> runtime rebuilds condition sources from authoritative condition flags
  -> ConditionChanged emitted if condition changed
```

## Public Entry Point

### `LivingEntityService`

File: `LivingEntityService.cs`

`LivingEntityService` is the Godot-facing singleton facade.

```csharp
public sealed partial class LivingEntityService : AbstractSingleton<LivingEntityService>
```

It is not an autoload by itself. It becomes the singleton when a node instance is added to the scene tree.

It delegates implementation to `LivingEntityServiceCore`.

#### Events

```csharp
public event Action<LivingEntityConditionChanged> ConditionChanged;
```

Subscribers:
- movement system
- action system
- presentation/animation/sound systems
- UI systems that need immediate condition updates
- future sync/metrics systems

The event is emitted after service-owned changes or authoritative delta application change condition flags.

#### Methods

```csharp
public void RegisterPeer(int peerId, LivingEntityState state)
```

Registers one living entity state for one peer id. Creates a `LivingEntityRuntime` and `LivingEntityStateTracker`.

```csharp
public LivingEntityRuntime RegisterPeerWithDefaultState(int peerId)
```

Creates a default player state with `LivingEntityStateFactory`, registers it, and returns the runtime.

```csharp
public void UnregisterPeer(int peerId)
```

Removes runtime and tracker and unsubscribes runtime condition forwarding.

```csharp
public bool HasPeer(int peerId)
```

Returns whether the service knows a peer.

```csharp
public LivingEntityRuntime GetRuntime(int peerId)
```

Returns runtime for gameplay queries. Do not use this to mutate state from outside the service.

```csharp
public LivingEntityState GetState(int peerId)
```

Returns raw state for read/serialization paths.

```csharp
public StatBreakdown GetStatBreakdown(int peerId, StatType statType)
```

Returns a UI/debug snapshot explaining one stat.

```csharp
public void ApplyEffect(int peerId, LivingEntityEffect effect)
```

Main mutation entry point. External systems should call this instead of mutating state directly.

```csharp
public Array GetDelta(int peerId, bool includePermanent)
public Array GetDeltas(bool includePermanent)
```

Creates ordered delta arrays for one peer or all peers. `includePermanent` should be true on slow-sync ticks.

```csharp
public void ApplyDelta(Array delta)
public void ApplyDeltas(Array deltas)
```

Applies authoritative ordered delta arrays.

```csharp
public void TickActiveEffects(int peerId, float delta)
public void TickAllActiveEffects(float delta)
```

Ticks timed condition sources and active stat modifiers.

## Testable Core

### `LivingEntityServiceCore`

File: `LivingEntityServiceCore.cs`

This contains the same public API as `LivingEntityService`, but is not a Godot `Node`. Unit tests use this class to avoid constructing Godot native objects outside a Godot runtime.

#### Stored Data

```csharp
private readonly Dictionary<int, LivingEntityRuntime> _runtimes;
private readonly Dictionary<int, LivingEntityStateTracker> _trackers;
private readonly ILivingEntityLogger _logger;
```

Why:
- `_runtimes`: one closed living entity aggregate per peer id.
- `_trackers`: one snapshot tracker per peer id for delta creation.
- `_logger`: injected so tests can use a no-op/mock logger and runtime can use Godot logging.

#### Internal Effect Handling

```csharp
private void ApplyEffect(LivingEntityRuntime target, LivingEntityEffect effect)
```

Dispatches specific effect types:
- `ReduceEffect`
- `RaiseEffect`
- `StatModifierEffect`
- `ConditionEffect`

```csharp
private void ApplyReduce(LivingEntityRuntime target, string sourceId, StatType statType, float amount)
```

Reduces one specific stat. If `statType == StatType.Health` and health reaches zero, applies zero-health outcome.

```csharp
private void ApplyRaise(LivingEntityRuntime target, string sourceId, StatType statType, float amount)
```

Raises one specific stat, clamped by stat max.

```csharp
private void ApplyModifier(LivingEntityRuntime target, string sourceId, StatType statType, StatModifier modifier)
```

Adds a modifier to one stat.

```csharp
private void SetCondition(
    LivingEntityRuntime target,
    string effectInstanceId,
    string sourceId,
    LivingEntityCondition condition,
    bool isApplied,
    float duration = 0f)
```

Adds/removes a condition source. If `duration > 0`, creates a timed condition source.

```csharp
private void TickActiveEffects(LivingEntityRuntime target, float delta)
```

Ticks timed condition sources and all stat modifier lists on the entity.

```csharp
private void ApplyZeroHealthOutcome(LivingEntityRuntime target, string sourceId)
```

Adds `Dead` as a condition source and drains shield/energy.

## Runtime Aggregate

### `LivingEntityRuntime`

File: `LivingEntityRuntime.cs`

`LivingEntityRuntime` wraps one `LivingEntityState` and owns runtime-only condition source tracking.

#### Stored Data

```csharp
public LivingEntityState State { get; }
public LivingEntityCondition Condition => State.Condition;
private readonly Dictionary<LivingEntityCondition, HashSet<string>> _conditionSources;
private readonly List<TimedConditionSource> _timedConditionSources;
```

Why:
- `State`: serializable values that sync over network.
- `Condition`: current aggregate condition flags.
- `_conditionSources`: tracks which effect/source grants each condition.
- `_timedConditionSources`: tracks source expiration for timed conditions.

#### Events

```csharp
public event Action<LivingEntityConditionChanged> ConditionChanged;
```

`LivingEntityRuntime` emits this internally. `LivingEntityServiceCore` subscribes to every runtime and re-emits through the service-level event.

#### Public Query Methods

```csharp
public bool IsAlive()
public bool IsTargetable()
public Stat GetStatByType(StatType type)
public StatBreakdown GetStatBreakdown(StatType type)
```

Use runtime for gameplay queries:
- movement checks `Condition`
- target systems call `IsTargetable`
- action systems inspect energy/stat values

Do not use runtime to mutate state outside service code.

#### Internal Mutation Methods

```csharp
internal void SetConditionSource(LivingEntityCondition condition, string sourceId, bool isApplied)
```

Adds/removes a source for one or more condition flags. Rebuilds final condition flags and emits condition change if needed.

```csharp
internal void SetTimedConditionSource(LivingEntityCondition condition, string sourceId, float duration)
```

Adds a condition source that expires after `duration`.

```csharp
internal void TickConditionSources(float delta)
```

Expires timed sources and removes their condition grants.

```csharp
internal void RebuildConditionSourcesFromState(string sourceId)
```

Used after authoritative delta application. Recreates source tracking from synced condition flags.

```csharp
internal void EmitConditionChangedFromExternal(LivingEntityCondition oldCondition, string sourceId)
```

Used after delta application to emit service/runtime condition events if authoritative condition flags changed.

## Serializable State

### `LivingEntityState`

File: `LivingEntityState.cs`

`LivingEntityState` stores actual stat objects, condition flags, entity id, type, and serialization methods.

#### Stored Data

```csharp
public Stat Health;
public Stat HealthRegeneration;
public Stat Shield;
public Stat ShieldRegeneration;
public Stat Energy;
public Stat EnergyRegeneration;
public Stat Speed;
public Stat Acceleration;
public Stat AfterBurnerSpeed;
public Stat AfterBurnerAcceleration;
public int LivingEntityId;
public LivingEntityType LivingEntityType;
private LivingEntityCondition _condition;
```

Why:
- stats represent the model state of a living entity.
- `LivingEntityId` is the sync identity.
- player living entity ids should match peer id.
- agent ids are reserved by factory at `1_000_000+`.
- `_condition` is the final synced condition bitfield.

#### Query Properties/Methods

```csharp
public LivingEntityCondition Condition { get; init; }
public Stat GetStatByType(StatType type)
public bool IsTargetable()
public bool IsAlive()
public float GetCurrentSpeed(bool isAfterBurnerPressed)
public float GetCurrentAcceleration(bool isAfterBurnerPressed)
public StatBreakdown GetStatBreakdown(StatType type)
```

#### Internal Mutation Helpers

```csharp
internal void SetCondition(LivingEntityCondition newCondition)
internal void RemoveCondition(LivingEntityCondition conditionToRemove)
internal void AddCondition(LivingEntityCondition conditionToAdd)
internal void ReduceStatToZero(StatType type)
```

These are intentionally internal. External systems should not call them. They exist so `LivingEntityRuntime` and `LivingEntityServiceCore` can update the stored state.

#### Full State Serialization

```csharp
public Array ToVariant()
public void UpdateFullState(Array livingEntityStateVariant)
```

Full-state structure:

```text
[
  condition,
  health,
  healthRegeneration,
  shield,
  shieldRegeneration,
  energy,
  energyRegeneration,
  speed,
  acceleration,
  afterBurnerSpeed,
  afterBurnerAcceleration,
  livingEntityType,
  livingEntityId
]
```

#### Delta Serialization

```csharp
public Array CreateDeltaVariant(LivingEntityState oldState, bool includePermanent)
public void ApplyDeltaVariant(Array deltaVariant)
public static bool DeltaVariantHasChanges(Array deltaVariant)
```

Delta structure:

```text
[
  livingEntityId,
  currentDeltaSlots,
  permanentDeltaSlots
]
```

Each delta slot array is ordered by `StatType`. Empty slots mean no change.

`currentDeltaSlots` includes:
- condition
- current values
- temporary modifiers
- recurring modifiers

`permanentDeltaSlots` includes:
- base value
- permanent modifiers

## Snapshot Tracking

### `LivingEntityStateTracker`

File: `LivingEntityStateTracker.cs`

Tracks current state against a private last-state snapshot.

#### Stored Data

```csharp
public LivingEntityState CurrentState { get; }
private readonly LivingEntityState _lastState;
```

Why:
- `CurrentState` points at the live state.
- `_lastState` stores the last synced baseline.

#### Methods

```csharp
public LivingEntityStateTracker(LivingEntityState currentState)
public void ApplyFullInitialState(Array livingEntityStateVariant)
public Array CreateCombinedDelta(bool includePermanent)
```

`CreateCombinedDelta` returns one ordered delta containing current changes and, if requested, permanent changes.

## Stats

### `Stat`

File: `Stat.cs`

Stores base/current/max values and active modifiers for one stat.

#### Stored Data

```csharp
public float BaseValue { get; private set; }
public float CurrentValue { get; private set; }
public float MaxValue { get; private set; }
public float PermanentMaxValue { get; private set; }
public IReadOnlyList<StatModifier> PermanentModifiers { get; }
public IReadOnlyList<TemporaryStatModifier> TemporaryModifiers { get; }
public IReadOnlyList<RecurringStatModifier> RecurringModifiers { get; }
```

Why:
- `BaseValue`: unmodified stat basis.
- `PermanentMaxValue`: base plus permanent modifiers.
- `MaxValue`: permanent max plus temporary modifiers.
- `CurrentValue`: current usable amount, clamped to max.
- modifier lists are read-only from outside to preserve recalculation rules.

#### Constructors

```csharp
public Stat(float baseValue)
public Stat(float baseValue, float currentValue)
public Stat(Array variant)
```

#### Modifier Methods

```csharp
public void AddModifier(StatModifier modifier)
public void RemoveModifier(StatModifier modifier)
public void AddRecurringModifier(RecurringStatModifier recurringStat)
```

Modifier stack policy is applied when adding modifiers.

#### Value Methods

```csharp
public void SetBaseValue(float value)
public void Tick(float delta)
public bool ReduceAndCheckZero(float amount)
public float ReduceAndGetOverflow(float amount)
public void Reduce(float amount)
public void Raise(float amount)
public void RestoreToMax()
public void DrainToZero()
```

Max/current rule:
- if max increases, current increases by the same delta.
- if max decreases, current is only clamped if it exceeds new max.

#### Sync Methods

```csharp
public void ApplyCurrentSync(Array variant)
public void ApplyPermanentSync(Array variant)
public Array ToPermanentVariant()
public Array ToCurrentVariant()
public Array ToFullVariant()
public Stat DeepCopy()
public bool HasCurrentDelta(Stat oldStat)
public bool HasPermaDelta(Stat oldStat)
```

#### UI/Debug

```csharp
public StatBreakdown CreateBreakdown(StatType statType)
```

Creates an on-demand stat explanation snapshot. Do not call in frame loops.

## Effects

### Base Effect

```csharp
public abstract record LivingEntityEffect(string EffectInstanceId, string SourceId);
```

Fields:
- `EffectInstanceId`: unique id for this effect application.
- `SourceId`: skill/item/system source id.

### `ReduceEffect`

```csharp
public sealed record ReduceEffect(
    string EffectInstanceId,
    string SourceId,
    StatType StatType,
    float Amount)
```

Reduces one targeted stat.

### `RaiseEffect`

```csharp
public sealed record RaiseEffect(
    string EffectInstanceId,
    string SourceId,
    StatType StatType,
    float Amount)
```

Raises one targeted stat.

### `StatModifierEffect`

```csharp
public sealed record StatModifierEffect(
    string EffectInstanceId,
    string SourceId,
    StatType StatType,
    StatModifier Modifier)
```

Adds a modifier to one targeted stat.

### `ConditionEffect`

```csharp
public sealed record ConditionEffect(
    string EffectInstanceId,
    string SourceId,
    LivingEntityCondition Condition,
    bool IsApplied,
    float Duration = 0f)
```

Adds/removes a condition source. If `Duration > 0`, the condition source expires during service ticking.

## Modifiers

### `StatModifier`

File: `Modifiers/StatModifier.cs`

#### Stored Data

```csharp
public string InstanceId { get; }
public string SourceId { get; }
public ModifierSourceType SourceType { get; }
public string DisplayName { get; }
public float Value { get; }
public ModifierType Type { get; }
public ModifierStackPolicy StackPolicy { get; }
```

Why:
- `InstanceId`: unique modifier instance.
- `SourceId`: skill/item/passive/system source.
- `SourceType`: categorizes source for UI/debug.
- `DisplayName`: UI tooltip label.
- `Value`: flat or percent value.
- `Type`: flat or percent.
- `StackPolicy`: controls duplicate source behavior.

Constructors:

```csharp
public StatModifier(
    string instanceId,
    string sourceId,
    float value,
    ModifierType type,
    ModifierSourceType sourceType = ModifierSourceType.Unknown,
    string displayName = "",
    ModifierStackPolicy stackPolicy = ModifierStackPolicy.Stack)

public StatModifier(Array values)
```

Serialization:

```csharp
public virtual Array ToVariant()
```

### `TemporaryStatModifier`

Adds duration.

```csharp
public float TimeRemaining { get; private set; }
public bool IsExpired => TimeRemaining <= 0;
public virtual void Tick(float delta)
public override Array ToVariant()
```

### `RecurringStatModifier`

Adds periodic stat application.

```csharp
public Stat Target { get; }
public float TickInterval { get; }
public float TickTimer => _tickTimer;
public override void Tick(float delta)
public override Array ToVariant()
```

`RecurringStatModifier(Array values, Stat target)` requires `target` because the serialized array stores data only; the runtime `Stat` reference cannot be serialized.

## UI/Debug Snapshots

### `StatBreakdown`

File: `StatBreakdown.cs`

```csharp
public readonly struct StatBreakdown(
    StatType statType,
    float baseValue,
    float currentValue,
    float maxValue,
    StatModifierContribution[] permanentContributions,
    StatModifierContribution[] temporaryContributions,
    StatModifierContribution[] recurringContributions)
```

Properties:
- `StatType`
- `BaseValue`
- `CurrentValue`
- `MaxValue`
- `PermanentContributions`
- `TemporaryContributions`
- `RecurringContributions`

Use for:
- tooltip
- character sheet
- debug inspector

Do not use for:
- movement logic
- targeting logic
- frame-loop gameplay checks

### `StatModifierContribution`

File: `StatModifierContribution.cs`

```csharp
public readonly struct StatModifierContribution(
    string instanceId,
    string sourceId,
    ModifierSourceType sourceType,
    string displayName,
    ModifierType modifierType,
    float value,
    float? timeRemaining)
```

Represents one row in a stat breakdown tooltip/debug view.

## Factories

### `LivingEntityStateFactory`

```csharp
public static LivingEntityState CreateDefaultPlayerState(int peerId)
public static LivingEntityState CreateDefaultAgentState(int agentId)
public static int ToAgentLivingEntityId(int agentId)
```

Rules:
- player living entity id equals peer id.
- agent ids start at `1_000_000 + agentId`.

## Events And Subscriptions

### Event Source

`LivingEntityRuntime.ConditionChanged` is emitted internally when condition source tracking changes final condition flags.

`LivingEntityServiceCore` subscribes to each runtime:

```text
runtime.ConditionChanged += OnRuntimeConditionChanged
```

`LivingEntityServiceCore.ConditionChanged` re-emits the event.

`LivingEntityService.ConditionChanged` delegates to the core event.

### Event Payload

```csharp
public readonly struct LivingEntityConditionChanged(
    int livingEntityId,
    LivingEntityCondition added,
    LivingEntityCondition removed,
    LivingEntityCondition current,
    string sourceId)
```

Use this for:
- movement responsiveness
- action freeze/unfreeze
- presentation effects
- UI condition indicators

## Enums

### `LivingEntityCondition`

Flag enum:
- `None`
- `Dead`
- `Immobilized`
- `Invulnerable`
- `Untargetable`
- `EnergyDepleted`
- `Invisible`

Helpers:

```csharp
Add(...)
Remove(...)
Contains(...)
NotContains(...)
IsTargetAble()
ToFlagListString()
```

### `StatType`

Ordered stat enum. Order matters for delta arrays.

```text
Condition = 0
Health = 1
HealthRegeneration = 2
Shield = 3
ShieldRegeneration = 4
Energy = 5
EnergyRegeneration = 6
Speed = 7
Acceleration = 8
AfterBurnerSpeed = 9
AfterBurnerAcceleration = 10
```

### Modifier Enums

```csharp
ModifierType.Flat
ModifierType.Percent
```

```csharp
ModifierSourceType.Unknown
ModifierSourceType.Skill
ModifierSourceType.Item
ModifierSourceType.Passive
ModifierSourceType.System
```

```csharp
ModifierStackPolicy.Stack
ModifierStackPolicy.RefreshDuration
ModifierStackPolicy.Replace
ModifierStackPolicy.IgnoreIfActive
ModifierStackPolicy.StackAndRefresh
ModifierStackPolicy.MaxStacksRefresh
```

## Current Tests

File: `sMPGWM.Tests/StatTests.cs`

Existing test cases:

1. `PermanentModifiers_RecalculateMaxAndApplyCurrentRules`
   - verifies permanent flat/percent modifiers, max recalculation, current max-increase/current-clamp rules.

2. `TemporaryModifier_Expires_AfterTick_RecalculatesMax`
   - verifies temporary modifier expiration and max/current recalculation.

3. `MaxDecrease_OnlyClampsCurrent_WhenCurrentExceedsNewMax`
   - verifies max decrease does not reduce current unless current exceeds new max.

4. `MaxIncrease_RaisesCurrentBySameDelta`
   - verifies max increase raises current by the same delta.

5. `SetBaseValue_UsesMaxCurrentRules`
   - verifies changing base value follows max/current rules.

6. `RecurringModifier_UsesPerSecondValue`
   - verifies recurring modifier value is treated as per-second value.

7. `ModifierMetadata_IsExposedThroughBreakdown`
   - verifies instance/source/display/time metadata appears in `StatBreakdown`.

8. `StackPolicy_RefreshDuration_ReplacesSameSourceModifier`
   - verifies same-source refresh policy replaces the active temporary modifier.

9. `EffectService_ReducesTargetedStatsAndZeroHealthChangesCondition`
   - currently named with old service wording.
   - verifies service/core reduces targeted stats and zero health adds `Dead`.

10. `EffectService_RemovesTimedCondition_WhenConditionSourceExpires`
    - currently named with old service wording.
    - verifies timed condition source expiration removes the condition and emits remove event.

11. `Factory_UsesPeerIdForPlayersAndReservedRangeForAgents`
    - verifies player ids equal peer ids and agent ids use reserved range.

## Known Testing Limitation

Plain NUnit tests cannot safely allocate some Godot native objects such as `Godot.Collections.Array` or construct `Node` subclasses without a Godot runtime.

Current approach:
- test service logic with `LivingEntityServiceCore`
- inject a fake `ILivingEntityLogger`
- avoid delta unit tests that allocate Godot arrays

Needed later:
- Godot-context integration tests for:
  - `LivingEntityService` singleton node
  - `GetDelta`
  - `ApplyDelta`
  - full state sync application

## Follow-Up Work

1. Rename test methods with `EffectService_...` to `LivingEntityService_...`.
2. Add Godot integration tests for delta serialization.
3. Move condition side effects for movement/presentation into subscribers.
4. Decide whether recurring modifiers should remain self-applying or be fully owned by service/runtime active effects.
5. Implement actual multiplayer sync using `LivingEntityService.GetDeltas` and `ApplyDeltas`.
6. Add saved-state loading to `LivingEntityStateFactory`.
