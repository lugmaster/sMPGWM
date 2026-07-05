# StateEngine.md

**Version:** 6.2.3  
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Genre:** 2D Space RPG  
**Network Model:** Client-server model with host-authoritative state simulation, rewind, rollback, client-side prediction compatibility, and reconciliation support  
**Network Tick Rate:** 30 TPS  
**Slow Simulation Rate:** 15 TPS  
**Primary Output:** Compact final-state delta using `PackedByteArray` at the Godot-facing boundary  
**Target Active Entities:** 250 living entities  
**Definition Type:** Root PRD include file

---

## 1. Purpose

`StateEngine` is the deterministic runtime authority for RPG living-entity state.

It processes registered `LivingEntityState` instances per `NetworkTick`, validates and applies incoming `ActiveEffect` requests, resolves dirty passive stat changes, executes active effects, maintains conditions and condition contributor ownership, derives StateEngine-owned conditions, updates active-effect lifetime, supports rollback/resimulation, and emits compact final-state deltas.

The engine must be:

- deterministic,
- low-allocation,
- low-memory,
- effect-driven,
- stat-layer aware,
- rollback-safe,
- multiplayer-friendly,
- independent from rendering,
- independent from gameplay presentation logic.

The first implementation target is a deterministic host-side simulation harness. The data model must not block later client prediction, host correction, delta application, or reconciliation.

---

## 2. Scope

### 2.1 In Scope

`StateEngine` owns or coordinates:

- registered living-entity runtime state,
- fast 30 TPS state simulation,
- slow 15 TPS simulation lane execution,
- active-effect intake and validation,
- deterministic active-effect ordering,
- active-effect lifetime,
- stack policy resolution,
- blocking evaluation,
- trigger and due-interval evaluation,
- child-effect queueing and execution,
- effect-removal request processing,
- condition add/remove requests,
- condition contributor ownership,
- derived condition updates,
- dirty passive stat application through an injected passive modifier pipeline,
- active stat modifier collection,
- stat-layer resolution,
- regeneration and net-flow application,
- shield cooldown state,
- compact delta generation,
- snapshot and applied-effect history storage,
- rollback and resimulation from retained past ticks,
- deterministic validation reports,
- TRACE-only diagnostics.

### 2.2 Out of Scope

`StateEngine` must not implement:

- skills,
- items,
- talents,
- weapons,
- ship systems,
- passive source authoring systems,
- hit acquisition,
- damage target selection,
- transport-level network synchronization,
- UI rendering,
- gameplay presentation logic,
- direct controller ownership logic.

External systems may create active-effect inputs or dirty passive-source updates. Those inputs are requests. `StateEngine` decides whether each request is valid and how it mutates state.

---

## 3. Core Ownership Rule

No gameplay system may directly mutate `LivingEntityState` runtime values.

Only `StateEngine` may mutate:

- stat runtime layers,
- condition flags,
- condition contributor ownership records,
- active-effect buffers,
- active-effect stack/runtime state,
- shield cooldown state,
- derived StateEngine-owned state,
- delta dirty state.

External systems may read state and submit requests. They must not directly write to state arrays, condition flags, active-effect buffers, contributor records, or delta metadata during runtime simulation.

---

## 4. Runtime Data Model

### 4.1 Required Runtime Types

The runtime model is:

```text
EffectDefinition = immutable static data
ActiveEffect = small pooled runtime struct
PassiveModifierSource = external persistent source state
LivingEntityState = mutable fixed-size arrays
StateEngine = deterministic simulation authority
LivingEntityStateTracker = snapshot and effect-history owner
Delta = pooled binary/buffer writer with PackedByteArray public boundary
```

### 4.2 LivingEntityState Layout

`LivingEntityState` must provide array-indexed hot-path access.

Required fields:

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

`StatType` enum values must map directly to array indices.

```csharp
var statIndex = (int)StatType.Health;
var currentHealth = state.CurrentValues[statIndex];
var currentMaxHealth = state.CurrentMaxValues[statIndex];
var effectiveMaxHealth = state.EffectiveMaxValues[statIndex];
```

The hot path must not use per-stat heap objects, dictionary lookups per stat mutation, reflection, LINQ, or enum boxing.

### 4.3 ActiveEffect Storage

The engine must use a flat active-effect buffer.

```csharp
ActiveEffect[] ActiveEffects;
int ActiveEffectCount;
```

Child effects must not be stored as nested object collections. Child effects must be resolved into separate `ActiveEffect` entries and processed through the same deterministic queue rules as normal effects.

Each child effect entry must store:

```text
ParentEffectRuntimeId
ChildDepth
ChildSequence
```

The flat model must support:

- fast iteration,
- stable insertion ordering,
- deterministic child ordering,
- rollback/resimulation,
- deterministic removals,
- one unified effect-processing pipeline.

### 4.4 Scratch Buffers

Per-tick scratch buffers must be preallocated, reused, and cleared without deallocation.

Required scratch-buffer categories:

```text
PendingEffectsThisTick
IncomingEffectsByTick
IncomingEffectsByEntity
StatModificationRecords
CurrentValueTransactions
ActiveFlatCurrentModifiers
ActivePercentCurrentModifiers
ActiveFlatCurrentMaxModifiers
ActivePercentCurrentMaxModifiers
DirtyStatIndices
DirtyPoolStats
DirtyFlowStats
DirtyAttributeStats
ConditionAddRequests
ConditionRemoveRequests
ConditionContributorRecords
EffectRemovalRequests
ChildEffectQueue
RejectedEffectRecords
DeltaWriterBuffer
```

Scratch buffers must be sized by `StateEngineConfig` and protected by deterministic overflow behavior.

---

## 5. Tick Model and Simulation Lanes

### 5.1 NetworkTick

`NetworkTick` is the authoritative simulation time unit.

```text
NetworkTick type: long
Fast tick rate: 30 TPS
```

Runtime simulation must use `long NetworkTick` values for:

- effect application tick,
- effect duration,
- effect execution interval,
- effect expiry,
- regeneration interval,
- shield recharge cooldown expiry,
- snapshot retention,
- resimulation windows,
- active-effect history.

Runtime logic must not use wall-clock time for deterministic simulation decisions.

### 5.2 Godot Physics Scheduling

Godot physics scheduling is owned by the caller.

```text
Godot physics tick: 60 Hz
Fast StateEngine tick: 30 TPS
Fast StateEngine execution: every 2nd physics frame
```

`StateEngine` must not call `_PhysicsProcess` or `_Process` internally.

### 5.3 Slow Simulation Lane

`StateEngine` supports a slow simulation lane at 15 TPS.

```text
Fast StateEngine: 30 TPS
Slow StateEngine: 15 TPS
Slow tick condition: NetworkTick % 2 == 0
```

Slow-lane processing is due only when:

```csharp
isSlowTick = networkTick % config.SlowSimulationDivisor == 0;
```

Slow-lane examples include:

- `OxygenStorage`, `OxygenProduction`, `OxygenConsumption`,
- `FoodStorage`, `FoodProduction`, `FoodConsumption`,
- `WaterStorage`, `WaterProduction`, `WaterConsumption`,
- active effects configured with `EffectProcessing: Slow`.

Fast-lane processing includes:

- `Health`,
- `Shield`,
- `EnergyStorage`,
- fast-lane flow stats,
- fast-lane attribute stats,
- active effects configured with `EffectProcessing: Fast`.

### 5.4 Duration Conversion

Durations outside runtime may be authored in milliseconds. During startup or validation, authored milliseconds must be converted once into deterministic tick values.

Rules:

```text
Minimum temporary effect duration: > 100 ms
Minimum OverTime interval: > 100 ms
Runtime representation: long NetworkTick values
```

The conversion must be deterministic and rollback-safe.

---

## 6. Instance Architecture

### 6.1 Primary Runtime Shape

The primary implementation must be instance-based.

```csharp
public sealed class StateEngine
{
    public StateEngine(
        StateEngineConfig config,
        EffectDefinitionRegistry effectDefinitionRegistry,
        StatRegistry statRegistry,
        IPassiveModifierPipeline passiveModifierPipeline,
        LivingEntityStateTracker stateTracker,
        IDeltaWriter deltaWriter,
        IStateEngineLogger logger);
}
```

An instance-based engine is required so tests, host simulation, prediction simulation, replay simulation, and isolated deterministic harnesses can run without shared global state.

### 6.2 Static Compatibility Facade

A static facade may exist for simple Godot integration and early test harnesses. The facade must delegate to a configured `StateEngine` instance and must not introduce hidden state mutation outside that instance.

```csharp
public static class StateEngineFacade
{
    public static void Initialize(StateEngine engine);

    public static void RegisterLivingEntity(long livingEntityId, LivingEntityState initialState);

    public static PackedByteArray ProcessLivingEntities(
        long networkTick,
        ReadOnlySpan<ActiveEffect> incomingActiveEffects);
}
```

The facade must be optional. Core runtime and tests must be able to use `StateEngine` instances directly.

### 6.3 Required Dependencies

`StateEngine` must use runtime-fixed dependencies during processing.

| Dependency | Responsibility |
|---|---|
| `StateEngineConfig` | Runtime limits, tick rates, retention, precision, buffer sizes. |
| `EffectDefinitionRegistry` | Immutable effect-definition lookup by `EffectDefinitionId`. |
| `EffectValidator` | Startup validation of authored effect definitions. |
| `StatRegistry` | Stat metadata, category, lane, bounds, array-index validation. |
| `IPassiveModifierPipeline` | Deterministic passive-source resolution into `EffectiveMaxValue`. |
| `LivingEntityStateTracker` | Snapshot storage, applied-effect history, replay data, pruning. |
| `IDeltaWriter` | Deterministic final-state delta serialization. |
| `IStateEngineLogger` | TRACE-only diagnostics with no disabled-path allocation. |

Runtime rules and registries must not change during simulation.

---

## 7. Public API

### 7.1 Register Living Entity

```csharp
public void RegisterLivingEntity(long livingEntityId, LivingEntityState initialState);
```

Registers a `LivingEntityState` and starts tracking it.

Registration rules:

```text
livingEntityId must be unique.
initialState.EntityId must match livingEntityId or be assigned during registration.
BaseValues[] must be initialized.
EffectiveMaxValues[] may be initialized or derived from BaseValues[] during registration.
CurrentMaxValues[] may be initialized or derived from EffectiveMaxValues[] during registration.
CurrentValues[] must be clamped to [0, CurrentMaxValue] for PoolStats.
StatType enum order must match all stat arrays.
ConditionFlags must be valid for the condition registry.
ActiveEffects[] must be initialized or allocated from engine-owned pools.
ActiveEffectCount must be within configured capacity.
Dirty bitsets must be initialized.
```

Registration must validate state shape before accepting the entity.

### 7.2 Unregister Living Entity

```csharp
public bool UnregisterLivingEntity(long livingEntityId);
```

Unregisters a living entity from future processing.

Unregistration must:

- remove the entity from current simulation sets,
- remove or mark obsolete its retained snapshots,
- remove or mark obsolete its applied-effect history,
- clear engine-owned buffers for that entity without leaking pooled data,
- produce deterministic behavior if the entity is missing.

Unregistering a missing entity must return `false` and must not throw during normal simulation.

### 7.3 Process Living Entities

```csharp
public PackedByteArray ProcessLivingEntities(
    long networkTick,
    ReadOnlySpan<ActiveEffect> incomingActiveEffects);
```

Processes one host-authoritative state tick and returns one combined compact delta for all dirty registered living entities.

`incomingActiveEffects` includes active effects from host and networked agents since the last processed tick, including valid retained past ticks that may require rollback/resimulation.

Input rules:

```text
Every effect must have AppliedNetworkTick <= networkTick.
Every effect must have TargetEntityId matching a registered living entity.
Every effect must reference a valid EffectDefinitionId.
Duplicate EffectRuntimeId values are rejected deterministically.
Incoming past effects must be within SnapshotRetentionTicks.
Incoming effects older than the retention window are rejected deterministically.
```

Output rules:

```text
The returned PackedByteArray is the compact delta.
The delta contains final authoritative changed state.
The delta is not a command replay log.
If no state changed, the returned delta may be empty.
```

### 7.4 State Access

```csharp
public bool TryGetLivingEntityState(long livingEntityId, out LivingEntityState state);
```

Read access may expose the latest state. Callers must treat returned state as read-only unless operating inside StateEngine-owned code.

If a mutable reference is exposed for performance, the project must enforce write access through code ownership and review rules.

### 7.5 Snapshot Access for Tests

```csharp
public bool TryGetSnapshot(long livingEntityId, long networkTick, out LivingEntityStateSnapshot snapshot);
```

Snapshot access is intended for deterministic replay tests, rollback tests, delta round-trip tests, and diagnostics.

---

## 8. Input Contract and Rejection Rules

### 8.1 ActiveEffect Input Contract

Each incoming `ActiveEffect` must provide:

```text
AppliedNetworkTick
TargetEntityId
SourceId
OriginId
EffectRuntimeId
EffectDefinitionId
SourceType
```

The engine may reject an incoming effect before stack resolution if:

- `AppliedNetworkTick > networkTick`,
- `AppliedNetworkTick` is older than the retained snapshot window,
- `TargetEntityId` is not registered,
- `EffectDefinitionId` is unknown,
- `EffectRuntimeId` duplicates an active, pending, or already-applied effect where uniqueness is required,
- required source/origin fields are invalid for the definition,
- incoming buffer limits are exceeded,
- child depth exceeds `MaxChildEffectDepth`,
- effect data is malformed or incompatible with validated definition data.

### 8.2 Deterministic Overflow Behavior

When incoming effects exceed configured per-tick limits, the engine must deterministically reject overflow effects and continue processing accepted effects.

Overflow rejection rules:

```text
Sort by documented stable ordering keys.
Accept effects up to the configured limit.
Reject remaining effects deterministically.
Record rejection reason in diagnostics/history when diagnostics are enabled or test history requires it.
Do not throw during normal simulation for configured overflow.
Do not allocate in the hot path while recording rejection.
```

Configured overflow applies to:

- max incoming effects per entity per tick,
- max child effects per entity per tick,
- max effects processed per entity per tick,
- max child depth,
- scratch-buffer capacity.

### 8.3 Rejected Effect History

For deterministic tests and replay support, the engine should retain enough rejection metadata to prove that a replay made the same acceptance/rejection decisions.

Required rejection metadata:

```text
NetworkTick
TargetEntityId
EffectRuntimeId
EffectDefinitionId
RejectionReason
DeterministicOrderKey
```

Retention of rejection metadata must follow snapshot/effect-history retention rules.

---

## 9. Deterministic Effect Ordering

### 9.1 Incoming Effect Ordering

When `ProcessLivingEntities` is called, incoming effects may target the current tick or past retained ticks.

Processing order for incoming effects:

1. Reject invalid future effects.
2. Reject effects older than the retained snapshot window.
3. Sort valid candidates by `AppliedNetworkTick`.
4. Group effects by `TargetEntityId`.
5. Apply deterministic tie-breakers.
6. For each affected past tick, restore the retained snapshot when resimulation is required.
7. Apply accepted incoming effects for that tick.
8. Resimulate forward to the requested `networkTick`.
9. Discard stored states older than the configured retention window.

### 9.2 Per-Tick Effect Ordering

Within a simulation tick, effects must be processed in this priority order:

1. Existing active effects by stable insertion sequence.
2. New incoming effects by sorted passed-buffer order.
3. Child effects by parent execution order and child list order.
4. Removal requests by removal ordering rules.
5. Overflow and removal tie-breakers by `EffectRuntimeId`.

### 9.3 Stable Ordering Keys

Stable ordering must use documented keys only.

```text
AppliedNetworkTick
TargetEntityId
InsertionSequence
ParentRuntimeEffectId
ChildSequence
EffectRuntimeId
```

The engine must not rely on unordered collection iteration.

### 9.4 Child Effect Ordering

Child effects must be queued into the same deterministic tick buffer.

Child effects are ordered by:

1. parent execution order,
2. child list order from the validated definition,
3. assigned child sequence,
4. `EffectRuntimeId` tie-breaker where required.

Child effects must respect `MaxChildEffectDepth`.

### 9.5 Removal Ordering

Removal requests must be deterministic.

Removal selection must use the selectors defined by `EffectRemovalRequest` and the target effect's `EffectRemovalPolicy`.

When multiple effects match a removal request, ordering must use:

1. explicit removal behavior fields,
2. oldest/newest policy,
3. stable insertion sequence,
4. `EffectRuntimeId` tie-breaker.

Removals must be queued through scratch buffers before compacting storage.

---

## 10. Tick Lifecycle Process Order

Each call to `ProcessLivingEntities` must execute these phases in order.

### Phase A — Tick Setup

1. Accept the requested `networkTick`.
2. Determine slow-lane eligibility.

```csharp
var isSlowTick = networkTick % config.SlowSimulationDivisor == 0;
```

3. Acquire registered mutable entity states.
4. Clear all per-tick scratch buffers without deallocation.
5. Prepare delta writer state.
6. Prepare deterministic rejection/diagnostic buffers when enabled.

### Phase B — Input Intake and Resimulation Selection

1. Validate incoming effect shape.
2. Sort incoming effects by deterministic ordering keys.
3. Group incoming effects by affected tick and entity.
4. Reject future, stale, malformed, unknown-target, unknown-definition, duplicate, and overflow effects deterministically.
5. Determine the earliest retained `AppliedNetworkTick` that changes current simulation.
6. If a valid past tick is affected, restore retained snapshots from that tick.
7. Rebuild pending tick inputs from applied-effect history and new accepted effects.

### Phase C — Passive Dirty Application

1. Apply dirty passive source changes through the injected passive modifier pipeline.
2. Recalculate affected `EffectiveMaxValue` values only for passive-dirty stats.
3. Mark affected stats as dirty.
4. Do not recalculate unchanged passive sources every tick.

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

### Phase D — Active Effect Acceptance

1. Add accepted incoming effects to entity effect buffers in deterministic order.
2. Resolve stack policies without allocation.
3. Apply stack behavior to accepted effects.
4. Reject discarded duplicate effects deterministically.
5. Fire `OnFirstApplication` condition requests only for accepted runtime effect instances.
6. Attach `EffectClassifications` only after stack policy accepts the runtime effect.

Discarded duplicate effects must not fire `OnFirstApplication` and must not attach classifications.

Replacement must remove the old effect first, then accept the replacement and process its own lifecycle requests.

### Phase E — Trigger, Blocking, and Due Evaluation

1. Evaluate `Dead` default blocking.
2. Evaluate `CanTriggerWhileDead` exceptions.
3. Evaluate blocking conditions.
4. Evaluate blocking classifications.
5. Apply `BlockPolicy`.
6. Evaluate trigger conditions.
7. Evaluate due intervals.
8. Evaluate simulation lane eligibility.

`Dead` blocks effects by default unless the effect explicitly sets:

```text
CanTriggerWhileDead = true
```

`BlockPolicy` behavior:

| Policy | Required Runtime Behavior |
|---|---|
| `RemoveOnFail` | Remove the active effect when blocking fails execution. |
| `Retry` | Retain the active effect and retry on a later eligible tick. |

### Phase F — Active Effect Execution

1. Execute due active effects in deterministic order.
2. Apply only effects that pass blocking, trigger, lane, lifetime, and interval checks.
3. Fire `OnFirstExecution` condition requests immediately after the first successful execution of each runtime effect.
4. Queue child effects in deterministic list order.
5. Collect stat transactions.
6. Collect active modifier contributions.
7. Collect condition requests.
8. Collect effect-removal requests.

`OnFirstExecution` must fire at most once per `ActiveEffect` runtime instance.

It must not fire when:

- the effect is merely accepted,
- execution is blocked,
- a retry attempt fails,
- a duration refresh occurs without successful execution.

### Phase G — Stat Resolution

The engine must resolve affected stats in deterministic order.

Required order:

1. Recalculate dirty passive stat values into `EffectiveMaxValue`.
2. Collect active stat modifiers from active effects valid for the current tick.
3. Resolve `CurrentMaxValue` for dirty or affected pool stats.
4. Apply pool capacity compensation.
5. Resolve active `CurrentValue` for dirty or affected flow and attribute stats.
6. Apply transactional pool `CurrentValue` modifications in deterministic order.
7. Apply due regeneration, production, and consumption operations.
8. Clamp all affected pool `CurrentValue`s to `[0, CurrentMaxValue]`.
9. Mark dirty stats for delta generation.
10. Defer derived state inference until after stat resolution.

### Phase H — Regeneration and Net-Flow Application

Regeneration, production, and consumption must use configured deterministic application intervals.

Application amount:

```text
AmountPerApplication = RegenerationValuePerMs * RegenerationApplicationInMsRuntime
```

Fast-lane resources may apply on fast ticks when due. Slow-lane resources apply only on slow ticks or by explicit deterministic StateEngine rules.

Energy net flow:

```text
EnergyNetValuePerMs = EnergyProduction.CurrentValue - EnergyConsumption.CurrentValue
```

Positive energy net flow raises `EnergyStorage.CurrentValue`. Negative energy net flow reduces `EnergyStorage.CurrentValue`.

Survival net flow uses the same production-minus-consumption model for Oxygen, Food, and Water storage.

After any net-flow application:

```text
Storage.CurrentValue = clamp(Storage.CurrentValue, 0, Storage.CurrentMaxValue)
```

### Phase I — Derived State and Conditions

After stat resolution, the engine may derive StateEngine-owned conditions.

Initial derived condition rules:

| Rule | Timing | Result |
|---|---|---|
| `Health.CurrentValue <= 0` | After stat resolution | Add or keep `Dead`. |
| `Health.CurrentValue > 0` and revive/removal permits it | After stat resolution | Remove `Dead`. |
| `Shield.CurrentValue <= 0` and shield cooldown is not already active | After shield transaction resolution | Start shield cooldown and add `ShieldCooldown`. |
| Shield cooldown expiry reached | At due tick | Remove `ShieldCooldown`; shield may be restored by later regeneration or boosts. |

Stats must not infer conditions by themselves.

### Phase J — Expiration, Removal, and Cleanup

1. Expire active effects whose `ExpiresNetworkTick` is reached.
2. Process effect-removal requests.
3. Fire `OnExpire`, `OnRemove`, and `OnExpireOrRemove` condition requests as applicable.
4. Remove expired effect classifications automatically.
5. Remove condition contributor ownership records owned by removed effects.
6. Apply compact-in-place removal.
7. Preserve stable ordering of retained active effects.
8. Do not allocate during removal.

Condition cleanup must respect `ConditionRemoveMode`.

| Remove Mode | Required Runtime Behavior |
|---|---|
| `RemoveOwnContribution` | Remove only the current `ActiveEffect` runtime instance's contribution. |
| `RemoveAllContributions` | Clear the listed condition and all contributor records for it. |

### Phase K — Dirty Marking and Delta Generation

1. Mark dirty stats, conditions, effects, and lifecycle metadata.
2. Emit a delta only if state is dirty.
3. Delta must include final authoritative changed state.
4. Delta must not require receivers to replay commands to know final state.
5. If no state changed, return an empty `PackedByteArray`.

### Phase L — Snapshot and History Storage

1. Store final state snapshots for the processed tick.
2. Store accepted applied-effect history.
3. Store required rejected-effect history for deterministic replay diagnostics.
4. Store delta comparison metadata.
5. Prune snapshots and histories older than `SnapshotRetentionTicks`.

---

## 11. Stat Resolution Contract

### 11.1 Runtime Value Layers

Stats use deterministic value layers.

| Layer | Meaning |
|---|---|
| `BaseValue` | Natural/default value supplied before registration. Runtime mutation is out of scope. |
| `EffectiveMaxValue` | Passive-adjusted value calculated from base plus passive modifiers. |
| `CurrentMaxValue` | Active runtime capacity limit for pool stats. |
| `CurrentValue` | Current pool amount, flow value, or attribute value. |

### 11.2 Passive Modifiers

Passive modifiers are not active effects.

Passive modifiers must be processed through the passive modifier pipeline and applied into `EffectiveMaxValue` by `StateEngine` when their source is added, removed, replaced, or dirtied.

Passives must not be recalculated every tick.

### 11.3 Active Effects

Active effects may target only:

```text
CurrentValue
CurrentMaxValue
```

Active effects must not target:

```text
BaseValue
EffectiveMaxValue
```

`CurrentMaxValue` is valid only for `PoolStat` capacity modifiers.

Temporary active modifiers must be recalculated from the active effect set. The engine must not rely on inverse effects to undo expired modifiers.

### 11.4 PoolStat Resolution

Pool stats use:

```text
BaseValue
EffectiveMaxValue
CurrentMaxValue
CurrentValue
```

Pool capacity resolution:

```text
EffectiveMaxValue
+ active CurrentMaxValue flat modifiers
then additive active CurrentMaxValue percentage modifiers
= CurrentMaxValue
```

After capacity resolution, pool `CurrentValue` must be clamped to:

```text
[0, CurrentMaxValue]
```

### 11.5 Capacity Compensation

When `CurrentMaxValue` increases, `CurrentValue` must increase by the same absolute capacity delta, but never above the new `CurrentMaxValue`.

When `CurrentMaxValue` decreases and `CurrentValue` exceeds the new `CurrentMaxValue`, `CurrentValue` must clamp to the new `CurrentMaxValue`.

Capacity compensation is part of capacity resolution. It is not a heal, damage, drain, or restoration effect.

### 11.6 Pool CurrentValue Transactions

Pool `CurrentValue` changes are transactional.

Examples:

- damage,
- healing,
- shield damage,
- shield boost,
- energy surplus,
- energy deficit,
- survival production,
- survival consumption,
- drains,
- restoration.

Percentage-based pool `CurrentValue` transactions use the target pool `CurrentMaxValue` at execution time unless a future schema explicitly defines a different basis.

### 11.7 FlowStat Resolution

Flow stats use:

```text
BaseValue
EffectiveMaxValue
CurrentValue
```

Flow resolution:

```text
BaseValue
+ passive modifiers
= EffectiveMaxValue

EffectiveMaxValue
+ active CurrentValue modifiers
= CurrentValue
```

`CurrentMaxValue` is not meaningful for flow stats and must not be targeted.

### 11.8 AttributeStat Resolution

Attribute stats use:

```text
BaseValue
EffectiveMaxValue
CurrentValue
```

Attribute resolution:

```text
BaseValue
+ passive modifiers
= EffectiveMaxValue

EffectiveMaxValue
+ active CurrentValue modifiers
= CurrentValue
```

`CurrentMaxValue` is not meaningful for attribute stats and must not be targeted.

### 11.9 Active Modifier Math

For `CurrentMaxValue` and non-pool `CurrentValue`, active modifiers are resolved as:

```text
EffectiveMaxValue
+ active flat modifiers
then additive active percentage modifiers
= resolved active value
```

Percentage modifiers must be additive with each other. The engine must not multiply percentage modifiers together.

### 11.10 Stat Bounds

Unless explicitly stated otherwise, stat values are clamped to:

```text
[0, maxFloat]
```

Pool `CurrentValue` is additionally clamped to:

```text
[0, CurrentMaxValue]
```

The stat model must not apply gameplay-specific caps unless those caps are represented as validated stat metadata or explicit StateEngine rules.

---

## 12. Conditions Contract

### 12.1 Condition Storage

Runtime active conditions are stored as bit flags.

```text
ulong ConditionFlags
```

Condition contributor ownership must be tracked internally by `StateEngine`.

Example:

```text
Condition: Poisoned
Contributors:
- EffectRuntimeId A
- EffectRuntimeId B
```

`LivingEntityState` exposes only whether a condition is present. Internally, the engine tracks which active effects contribute each owned condition.

Removing one contributor removes only that contribution unless a request uses `RemoveAllContributions`.

### 12.2 Condition Requests

Effects do not change conditions directly.

Effects may request condition changes through `ConditionRequests`.

The engine must validate and apply condition requests according to:

- lifecycle timing,
- remove mode,
- contributor ownership,
- target validity,
- deterministic processing order.

Supported lifecycle timings:

```text
OnFirstApplication
OnFirstExecution
OnExpire
OnRemove
OnExpireOrRemove
```

### 12.3 Dead Default Blocking Rule

`Dead` blocks effects by default.

Effects that may execute while the target is dead must explicitly set:

```text
CanTriggerWhileDead: true
```

Normal heal effects should not trigger while dead unless authored as revive/resurrection behavior.

### 12.4 Initial Condition Vocabulary

The initial `LivingEntityCondition` values are:

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

`Alive` is not a condition. If an entity is not `Dead`, it is considered alive enough for StateEngine processing.

Lifecycle states such as `Spawned`, `Initialized`, `wait_for_revive`, and `ready_for_revive` are out of scope.

---

## 13. Active Effect Runtime Contract

### 13.1 EffectDefinition

`EffectDefinition` is immutable static data.

A valid non-container effect must contain `Definition` and at least one gameplay payload:

```text
Target
ConditionRequests
EffectClassifications
EffectRemovalRequest
ChildEffectDefinitions
```

A valid container definition must set:

```yaml
IsEffectContainer: true
```

and must contain at least one child effect. Container definitions must not define direct target, direct condition requests, direct classifications, or direct removal requests.

### 13.2 ActiveEffect

`ActiveEffect` is pooled runtime state.

It must store runtime progress such as:

```text
EffectRuntimeId
EffectDefinitionId
AppliedNetworkTick
SourceType
SourceId
OriginId
TargetEntityId
ParentEffectRuntimeId
ChildDepth
InsertionSequence
StackCount
ExpiresNetworkTick
LastExecutionNetworkTick
HasExecuted
AccumulatedModifierState where required
```

Runtime progress, timers, stack counts, accumulators, parent-child runtime links, and deterministic ordering state belong to `ActiveEffect`, not to `EffectDefinition`.

### 13.3 Effect Classification

Effect classifications are static metadata attached to accepted `ActiveEffect` instances.

They are used for:

- blocking,
- removal request matching,
- debug and inspection identity,
- trigger conditions,
- UI/debug grouping where explicitly exposed.

Classifications must not mutate target `LivingEntityState` conditions. They disappear automatically when the corresponding `ActiveEffect` expires or is removed.

### 13.4 Stacking

Stack policy must be resolved before `OnFirstApplication` fires.

Supported canonical stack fields:

```text
StackGroupId
StackScope
StackPolicy
StackBehavior
MaxStacks
MaxStackOverflowPolicy
```

`MaxStacks` is the only canonical stack limit field.

Overflow behavior must be deterministic.

Supported overflow policies:

```text
ReplaceOldest
RejectNewest
```

### 13.5 Blocking

Supported block policies:

```text
RemoveOnFail
Retry
```

Blocking may evaluate:

- target conditions,
- active-effect classifications,
- `Dead` default blocking,
- trigger-specific condition/classification requirements.

### 13.6 Trigger and Duration

Canonical trigger values:

```text
Immediate
OverTime
WhileActive
OnExpire
OnRemoval
```

Canonical duration values:

```text
Once
Temporary
UntilRemoved
```

`Temporary` duration and `OverTime` interval values must be converted from milliseconds to ticks during startup or validation.

### 13.7 Child Effects

Child definitions must be validated before runtime.

Runtime child effects must:

- reference an existing child definition,
- not exceed `MaxChildEffectDepth`,
- be queued in deterministic child list order,
- be represented as flat `ActiveEffect` entries,
- store parent runtime ID and child depth,
- use the same acceptance, blocking, trigger, stack, removal, and delta rules as other active effects.

---

## 14. Rollback, Snapshot, and Resimulation

### 14.1 State Tracker Responsibility

`LivingEntityStateTracker` must store state history and applied-effect history.

It is used for:

- resimulating from corrected ticks,
- deleting obsolete state and effect history,
- generating authoritative deltas after rollback/resimulation,
- comparing predicted and corrected states,
- deterministic replay tests.

### 14.2 Snapshot Retention

Snapshot and effect-history retention must be exposed in `StateEngineConfig`.

Default:

```text
SnapshotRetentionTicks = 45
```

For any received correction/delta tick, the receiver must still have enough data to resimulate from that tick to the current tick.

Resimulation window:

```text
current NetworkTick - corrected delta NetworkTick
```

### 14.3 Snapshot Content

A snapshot must provide everything required to restore a tick and resimulate to the current tick.

Snapshot fields must include:

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

### 14.4 Applied Effect History

Applied-effect history must include accepted active effects by tick and enough rejection metadata to support deterministic replay diagnostics.

Required applied-effect history fields:

```text
AppliedNetworkTick
TargetEntityId
SourceId
OriginId
EffectRuntimeId
EffectDefinitionId
AcceptedOrRejected
RejectionReason when rejected
DeterministicOrderKey
```

### 14.5 Resimulation Rules

When an accepted past effect changes a retained tick:

1. Restore the retained snapshot at the earliest affected tick.
2. Rebuild applied-effect input for that tick from retained history and new accepted effects.
3. Resimulate every tick up to the requested current `networkTick`.
4. Use identical deterministic order, configuration, registries, stat metadata, and effect definitions.
5. Generate the final authoritative delta from the simulated current tick.

The engine must retain enough snapshots and effect history to replay the configured window deterministically.

---

## 15. Delta Generation Contract

### 15.1 Public Delta Representation

The public Godot-compatible delta representation must be:

```text
PackedByteArray
```

The engine should not use `Godot.Collections.Array` internally as its primary delta format.

Preferred internal implementation:

```text
Pooled byte buffer
Pooled binary writer
Deterministic binary layout
Little-endian numeric encoding
No per-field heap allocations
```

### 15.2 Delta Semantics

The delta must include final authoritative changed state.

The delta is not a replay command list. A receiver must be able to apply the final changed state without replaying the original active effects.

If a correction requires local resimulation, the receiver may use the delta tick and retained history to resimulate, but the payload itself must still represent authoritative final changed state for the included fields.

### 15.3 Delta Header

Suggested header:

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

### 15.4 Final State Payload

Suggested final-state payload:

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

### 15.5 Delta Versioning

Delta format must be versioned.

```text
DeltaVersion: ushort or byte
```

Versioning is required because future stat/effect changes may alter payload layout.

### 15.6 Payload Flags

Suggested payload flags:

```text
HasFinalStats
HasFinalConditions
HasFinalEffects
HasPassiveLayerChanges
HasActiveLayerChanges
CompressedPayload
```

The first implementation does not need compression, but the flag must be reserved.

---

## 16. Configuration

`StateEngineConfig` must expose core runtime limits and deterministic conversion settings. Runtime values must not be hardcoded in hot-path logic.

Suggested initial fields:

```text
StateTicksPerSecond = 30
SlowSimulationDivisor = 2
RegenerationApplicationIntervalMs = 500
GlobalRegenerationApplicationTicks = converted at startup
RegenerationApplicationInMsRuntime = deterministic converted interval
MaxChildEffectDepth = 5
MaxEffectsProcessedPerEntityPerTick = configured
MaxIncomingEffectsPerEntityPerTick = 30
MaxChildEffectsPerEntityPerTick = 3
SnapshotRetentionTicks = 45
StatComparisonPrecisionDecimals = 3
ShieldRechargeDuration default = content-defined stat value
TraceEnabled = configured/build dependent
DeltaVersion = configured/current payload version
```

Configuration values authored in milliseconds must be converted to deterministic `NetworkTick` values during startup or validation.

Runtime simulation must not use wall-clock time for deterministic decisions.

---

## 17. Logging and Diagnostics

Logging is for debugging, balancing, deterministic test inspection, and validation output.

Logging must only execute when TRACE is enabled.

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

Diagnostics may record:

- rejected effects,
- stack decisions,
- blocking decisions,
- first application/execution events,
- stat changes,
- condition contributor changes,
- removal decisions,
- delta payload summaries,
- resimulation start/end ticks.

Diagnostics must not alter simulation results.

---

## 18. Performance and Allocation Requirements

`StateEngine` runs at 30 TPS for fast state and 15 TPS for slow state.

The hot tick loop must avoid avoidable heap allocation.

### 18.1 Must Avoid in Hot Path

```text
Per-stat heap objects
LINQ in tick resolution
Per-tick Godot.Collections.Array allocation
String logging when TRACE is disabled
Allocating temporary collections every tick
Nested ActiveEffect child collections
Reflection
Boxing enum values
Dictionary lookups per stat mutation
Queue<ActiveEffect> in the hot path
Runtime allocation for normal effect processing
Runtime allocation for normal stat processing
Runtime allocation for normal condition processing
Runtime allocation for normal delta writing except public PackedByteArray boundary
```

### 18.2 Required Hot-Path Practices

The engine must:

- use array-indexed stat access,
- use flat active-effect buffers,
- use reusable scratch buffers,
- clear scratch buffers without deallocation,
- use deterministic compact-in-place removal,
- process only dirty, affected, or due stats where possible,
- avoid inverse-effect cleanup for expired modifiers,
- recalculate temporary active modifiers from remaining active effect set,
- keep logging disabled-path allocation-free,
- size buffers through configuration,
- reject overflow deterministically.

---

## 19. Determinism Requirements

`StateEngine` must produce identical results for identical inputs.

Required deterministic practices:

- Use `long NetworkTick` values.
- Avoid unordered collection iteration in simulation.
- Avoid random values unless seeded deterministically.
- Avoid wall-clock time.
- Use stable insertion order for active effects.
- Use deterministic child-effect order.
- Use deterministic overflow behavior.
- Use additive percentage modifiers.
- Resolve stats in the documented order.
- Compare corrected states using configured precision.
- Compare conditions by exact flag match.
- Sort removals and overflow replacement by documented keys.
- Convert authored milliseconds to ticks once before runtime simulation.
- Keep runtime rulesets fixed during processing.
- Keep registries immutable during simulation.

Default corrected-state numeric comparison precision:

```text
StatComparisonPrecisionDecimals = 3
```

If random behavior is required later, it must use deterministic seeds derived from stable simulation fields.

```text
NetworkTick
EntityId
EffectRuntimeId
SourceId
OriginId
```

---

## 20. Validation Requirements

Validation must occur before normal gameplay simulation.

Invalid definitions, invalid stat metadata, invalid lanes, and invalid target compatibility must fail fast with deterministic validation reports.

### 20.1 Effect Definition Validation

`EffectValidator` must check:

- YAML parses successfully.
- `Definition.EffectDefinitionId` is present.
- `Definition.Name` is present.
- `Definition.Description` is present.
- A non-container effect has at least one gameplay payload.
- A container uses `IsEffectContainer: true`.
- A container has children.
- A container omits direct target, condition, classification, and removal payloads.
- Each effect defines at most one stat target.
- Referenced child effect IDs exist.
- Child effect graph is acyclic.
- Child effect graph does not exceed `MaxChildEffectDepth`.
- Deprecated field names and enum values are rejected.
- `MaxStacks`, when present, is greater than zero.
- `StackScope: ByStackGroupId` has `StackGroupId`.
- `EffectRemovalRequest`, when present, has at least one non-empty selector.
- Duration and tick interval values satisfy timing rules.
- Active effect stat targets are compatible with the stat model.

### 20.2 Stat Validation

Stat validation must check:

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
- slow-lane stats are applied only during the slow lane or by explicit deterministic StateEngine rules,
- percentage values use decimal fractions,
- all percentage modifiers are additive,
- stat names use canonical spelling.

### 20.3 Validation Output

Validation output must be deterministic.

Suggested report fields:

```text
EffectDefinitionId or StatType
ValidationViolation
FieldPath
InvalidValue
SuggestedReplacement when available
```

---

## 21. Testing and Acceptance Criteria

### 21.1 Deterministic Replay Tests

The same initial states, effect definitions, stat metadata, config, incoming effect inputs, and passive dirty inputs must produce identical final states and identical delta payloads.

### 21.2 Registration Tests

Tests must prove:

- valid entities register successfully,
- duplicate entity IDs are rejected,
- invalid stat array lengths are rejected,
- invalid initial pool values are clamped or rejected according to registration rules,
- unregistration removes the entity from future processing.

### 21.3 Effect Ordering Tests

Tests must prove:

- incoming effects are ordered by `AppliedNetworkTick`,
- existing active effects execute before new effects where specified,
- child effects follow parent execution and child list order,
- removal tie-breakers are deterministic,
- overflow rejection is deterministic.

### 21.4 Rollback and Resimulation Tests

Tests must prove:

- retained past effects can trigger rollback,
- snapshots restore complete state,
- resimulation reaches the same result as uninterrupted simulation,
- stale effects outside retention are rejected deterministically,
- snapshot pruning follows `SnapshotRetentionTicks`.

### 21.5 Stat Layer Tests

Tests must prove:

- passive modifiers resolve into `EffectiveMaxValue`,
- active capacity modifiers resolve into `CurrentMaxValue`,
- active flow/attribute modifiers resolve into `CurrentValue`,
- percentage modifiers are additive,
- pool capacity compensation is deterministic,
- pool current transactions apply in deterministic order,
- affected pool values clamp correctly.

### 21.6 Condition Tests

Tests must prove:

- condition contributor ownership works with multiple contributors,
- `RemoveOwnContribution` removes only one contributor,
- `RemoveAllContributions` clears all contributors,
- `Dead` blocks by default,
- revive-style effects can opt into `CanTriggerWhileDead`,
- derived `Dead` and `ShieldCooldown` conditions update after stat resolution.

### 21.7 Delta Round-Trip Tests

Tests must prove:

- dirty state emits a delta,
- clean state may emit an empty delta,
- delta payload contains final authoritative changed state,
- stat, condition, and effect dirty masks match changed fields,
- payload versioning is present,
- little-endian numeric encoding is stable.

### 21.8 No-Allocation Hot-Path Tests

Tests must prove normal tick processing avoids avoidable heap allocation for:

- stat resolution,
- active-effect processing,
- condition request processing,
- child effect queueing within configured limits,
- removal compaction,
- disabled TRACE logging,
- delta writing except the Godot-facing `PackedByteArray` boundary.

### 21.9 Validation Tests

Tests must prove invalid startup data fails deterministically, including:

- malformed YAML,
- unknown enum values,
- deprecated fields,
- invalid stat targets,
- invalid child graphs,
- missing required definition fields,
- invalid stacking fields,
- invalid removal requests,
- invalid timing values.

---

## 22. Implementation Notes

### 22.1 Recommended Source Layout

```text
scripts/
  state_engine/
    StateEngine.cs
    StateEngineConfig.cs
    StateEngineFacade.cs
    StateEngineScratchBuffers.cs
    LivingEntityStateTracker.cs
    Delta/
      IDeltaWriter.cs
      PackedDeltaWriter.cs
      DeltaPayloadFlags.cs
    Definitions/
      EffectDefinition.cs
      EffectDefinitionRegistry.cs
      EffectValidator.cs
    Runtime/
      ActiveEffect.cs
      LivingEntityState.cs
      LivingEntityStateSnapshot.cs
      ConditionContributorRecord.cs
      RejectedEffectRecord.cs
    Stats/
      StatType.cs
      StatRegistry.cs
      StatCategory.cs
      StatLane.cs
    Conditions/
      LivingEntityCondition.cs
      ConditionRegistry.cs
    Passive/
      IPassiveModifierPipeline.cs
```

### 22.2 Implementation Boundary

This file defines the required StateEngine runtime contract. It does not define full skill, item, talent, weapon, UI, or network transport implementations.

Any implementation that mutates `LivingEntityState` outside `StateEngine` violates this contract.
