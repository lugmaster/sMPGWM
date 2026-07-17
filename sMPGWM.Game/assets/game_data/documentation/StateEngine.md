# StateEngine.md

**Version:** 6.4.1
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Genre:** 2D Space RPG  
**Network Model:** Client-server model with host-authoritative state simulation, rewind, rollback, client-side prediction, and reconciliation support
**Network Tick Rate:** 30 TPS  
**Slow Simulation Rate:** 15 TPS  
**Primary Output:** Compact final-state delta using `PackedByteArray` at the Godot-facing boundary  
**Target Active Entities:** 250 living entities  
**Definition Type:** Root PRD include file

---

## 1. Purpose

`StateEngine` is the deterministic runtime authority for RPG living-entity state.

It processes registered `LivingEntityState` instances per `NetworkTick`, validates and applies incoming `ActiveEffect` requests, resolves dirty passive stat changes, processes generated passive runtime effects, executes active effects, maintains conditions and condition contributor ownership, derives StateEngine-owned conditions, updates active-effect lifetime, supports rollback/resimulation, emits compact final-state deltas, applies received host deltas on clients, and supports client-side prediction and reconciliation for locally owned entities.

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

The host implementation remains the canonical simulation path. Version 6.4.0 additionally defines the client path for host delta application, local prediction, host correction, rollback/replay, remote entity interpolation, and baseline/resync. Version 6.4.1 integrates the passive runtime contract from `Passives.md`, including `PassiveDefinition`, generated `PassiveEffect` records, reactive passives, passive tags, passive runtime snapshots, and passive validation.

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
- generated source-bound passive runtime effects,
- reactive passive event processing,
- passive runtime cooldowns and runtime stacks,
- active stat modifier collection,
- stat-layer resolution,
- regeneration and net-flow application,
- shield cooldown state,
- compact delta generation,
- host-to-client delta application,
- client-side prediction for locally owned living entities,
- authoritative correction and replay from retained client snapshots,
- remote living-entity interpolation from host deltas,
- baseline and resync handling,
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
- transport-level network synchronization, packet delivery, reliability, ordering, encryption, and Steam Multiplayer Peer integration,
- UI rendering,
- gameplay presentation logic,
- direct controller ownership logic.

External systems may create active-effect inputs or dirty passive-source updates. Those inputs are requests. `StateEngine` decides whether each request is valid and how it mutates state.

Network transport is out of scope. The runtime contract still includes host packet record shape, delta application, acknowledgement handling, local submission result handling, and full baseline/resync application.

---

## 3. Core Ownership Rule

No gameplay system may directly mutate `LivingEntityState` runtime values.

Only `StateEngine` may mutate:

- stat runtime layers,
- condition flags,
- condition contributor ownership records,
- active-effect buffers,
- active-effect stack/runtime state,
- generated passive-effect buffers,
- passive runtime state,
- passive result-event queue state,
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
PassiveDefinition = immutable authored passive data
PassiveEffect = source-bound runtime passive record generated from PassiveDefinition
PassiveRuntime = mutable runtime state owned by a generated PassiveEffect
PassiveModifierSource = external persistent source state
LivingEntityState = mutable fixed-size arrays
StateEngine = deterministic simulation authority
LivingEntityStateTracker = snapshot and effect-history owner
Delta = pooled binary/buffer writer with PackedByteArray public boundary
DeltaReader = pooled binary/buffer reader for host packet records
ClientPredictionHistory = retained local request and predicted-effect history
ClientInterpolationBuffer = ring buffer of host-authoritative remote entity states
ClientEffectIdMap = local request identity to authoritative EffectRuntimeId map
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
PassiveInputVersion
PassiveResolvedVersion
PassiveLayerDirtyMetadata
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
GeneratedPassiveEffects
PassiveRuntimeState
PassiveFlatModifiers
PassivePercentModifiers
PassiveResultEvents
PassiveOutputEffects
RejectedPassiveRecords
RejectedEffectRecords
DeltaWriterBuffer
DeltaReaderBuffer
ClientPredictionHistoryBuffer
ClientInterpolationStateBuffer
ClientSubmissionResultBuffer
ClientAckRecordBuffer
ClientEffectIdMapBuffer
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
        PassiveDefinitionRegistry passiveDefinitionRegistry,
        StatRegistry statRegistry,
        PassiveValidator passiveValidator,
        IPassiveModifierPipeline passiveModifierPipeline,
        LivingEntityStateTracker stateTracker,
        IDeltaWriter deltaWriter,
        IDeltaReader deltaReader,
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

    public static ClientPacketApplyResult ApplyHostStatePacket(
        long localCurrentNetworkTick,
        ReadOnlySpan<byte> hostPacket);

    public static ClientBaselineApplyResult ApplyAuthoritativeBaseline(
        long baselineNetworkTick,
        ReadOnlySpan<byte> baselinePayload);
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
| `PassiveDefinitionRegistry` | Immutable passive-definition lookup by `PassiveDefinitionId`. |
| `PassiveValidator` | Startup validation of authored passive definitions and passive schemas. |
| `StatRegistry` | Stat metadata, category, lane, bounds, array-index validation. |
| `IPassiveModifierPipeline` | Deterministic passive-source resolution into `EffectiveMaxValue`. |
| `LivingEntityStateTracker` | Snapshot storage, applied-effect history, replay data, pruning. |
| `IDeltaWriter` | Deterministic final-state delta serialization. |
| `IDeltaReader` | Deterministic host packet and delta record parsing. |
| `ClientPredictionHistory` | Retained local request and predicted-effect history for replay. |
| `ClientInterpolationBuffer` | Remote entity authoritative state buffering for presentation. |
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

### 7.6 Apply Host State Packet

```csharp
public ClientPacketApplyResult ApplyHostStatePacket(
    long localCurrentNetworkTick,
    ReadOnlySpan<byte> hostPacket);
```

Applies one received host packet on a client `StateEngine` instance.

The packet may contain:

```text
StateDeltaRecords
AckRecords
LocalSubmissionResultRecords
BaselineRecords
```

Application rules:

```text
Validate packet and delta versions.
Apply BaselineRecords before delta records.
Apply AckRecords before replay.
Apply LocalSubmissionResultRecords before replay.
Apply StateDeltaRecords at their authoritative NetworkTick.
Correct locally owned entities and replay unacknowledged local predictions.
Push remote entity state into interpolation buffers.
Request a full baseline when required retained state is missing.
```

The method must not make the client authoritative. Host state always wins.

Suggested `ClientPacketApplyResult` fields:

```text
AppliedHostNetworkTick
AppliedStateDeltaCount
AppliedAckCount
AppliedSubmissionResultCount
AppliedBaselineCount
DidCorrectPrediction
ReplayStartTick
ReplayEndTick
RequiresFullBaseline
RejectedReason when packet cannot be applied
```

### 7.7 Apply Authoritative Baseline

```csharp
public ClientBaselineApplyResult ApplyAuthoritativeBaseline(
    long baselineNetworkTick,
    ReadOnlySpan<byte> baselinePayload);
```

Applies a full authoritative baseline received from the host.

Baseline application must replace authoritative client gameplay state for included living entities, reset retained snapshots to the baseline tick, rebuild interpolation buffers, and replay valid unacknowledged local predictions when permitted by acknowledgement metadata.

Suggested `ClientBaselineApplyResult` fields:

```text
BaselineNetworkTick
AppliedEntityCount
DidReplayLocalPredictions
ReplayStartTick
ReplayEndTick
RejectedReason when baseline cannot be applied
```

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
5. Add, remove, replace, or restack generated source-bound `PassiveEffect` records when passive source state changes.
6. Evaluate generated passive runtime records only when their `EvaluationPolicies` require it.
7. Queue reactive passive outputs into bounded same-tick passive result/output buffers.

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

Generated `PassiveEffect` records are not `ActiveEffect` records. They are StateEngine-owned passive runtime records, and any passive output that needs to mutate RPG state must emit a normal active-effect spec into the deterministic effect pipeline.

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
5. Store generated passive-effect and passive-runtime state needed for rollback/resimulation.
6. Prune snapshots and histories older than `SnapshotRetentionTicks`.

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

Passives must not be recalculated every tick unless a generated passive runtime record explicitly requires evaluation through `EvaluationPolicies`.

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
- PacketNetworkTick
- StateTickKind: Fast or SlowDue
- PayloadFlags
- FinalRecordCount
```

For a single-entity delta record, `EntityId`, dirty masks, and changed payload fields may live directly in that record.

For a combined host packet, the packet header must be shared and entity identity must be stored per `StateDeltaRecord`. A combined host packet may contain state records for many living entities.

### 15.4 Final State Payload

Suggested final-state payload:

```text
ChangedStats:
- EntityId
- NetworkTick
- DirtyMaskStats
- StatIndex
- BaseValue only if ever supported later
- EffectiveMaxValue if changed or required by payload flags
- CurrentMaxValue if changed or required by payload flags
- CurrentValue if changed or required by payload flags

ChangedConditions:
- EntityId
- DirtyMaskConditions
- ulong ConditionFlags

ChangedEffects:
- EntityId
- DirtyMaskEffects
- Added ActiveEffect runtime ids
- Removed ActiveEffect runtime ids
- Updated StackCount
- Updated ExpiresNetworkTick or RemainingTicks
- Authoritative AppliedNetworkTick when needed for reconciliation
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
HasAckRecords
HasLocalSubmissionResults
HasBaselineRecords
CompressedPayload
```

The first implementation does not need compression, but the flag must be reserved.

### 15.7 Host and Client Runtime Paths

The host path is the authoritative simulation path and follows the tick lifecycle in section 10.

The client path is a consumer of host-authoritative state. It may run local prediction for the locally owned entity, but it must always correct to host deltas and replay retained local prediction history after correction.

The host always wins.

#### 15.7.1 Host Path

Per host network tick:

1. Receive player input and effect requests from connected clients.
2. Normalize each client request into candidate `ActiveEffect` inputs with stable metadata.
3. Assign or validate the authoritative `NetworkTick`.
4. Validate, reject, accept, stack, execute, remove, and expire effects through `StateEngine.ProcessLivingEntities`.
5. Restore retained snapshots and resimulate if a valid past tick changes current authoritative state.
6. Generate final-state delta records for all changed living entities.
7. Add acknowledgement records for the latest processed local input/effect tick per client or owned entity.
8. Add local submission result records for client-submitted effects.
9. Add full baseline records when a client joins, resyncs, or cannot apply the delta stream.
10. Send one host packet containing co-scheduled records to every connected client.
11. Store host snapshots, applied-effect history, rejection metadata, and delta comparison metadata.

Host deltas contain final authoritative state for living entities. They are not command replay logs.

The host must send changed state for all active living entities to all players unless a later transport or interest-management layer explicitly narrows delivery. Interest management is out of scope for this file.

#### 15.7.2 Host Packet Records

The recommended host packet shape is a record envelope. Records are separate for parsing and versioning, but they may be co-scheduled in one network send.

```text
HostStatePacket:
- PacketVersion
- DeltaVersion
- HostNetworkTick
- RecordCounts
- StateDeltaRecords[]
- AckRecords[]
- LocalSubmissionResultRecords[]
- BaselineRecords[] when required
```

Required record kinds:

```text
StateDeltaRecord
AckRecord
LocalSubmissionResultRecord
BaselineRecord
```

`StateDeltaRecord` contains final authoritative state for one changed living entity at one host tick.

```text
StateDeltaRecord:
- EntityId
- NetworkTick
- StateTickKind
- PayloadFlags
- DirtyMaskStats
- DirtyMaskConditions
- DirtyMaskEffects
- ChangedStats
- ChangedConditions
- ChangedEffects
```

`AckRecord` lets the client discard local prediction history already processed by the host.

```text
AckRecord:
- ClientId or ConnectionId
- OwnedEntityId
- LastProcessedInputTick
- LastProcessedClientRequestSequence
```

`LocalSubmissionResultRecord` reports the authoritative result of locally submitted effects.

```text
LocalSubmissionResultRecord:
- ClientRequestId or ClientEffectSequence
- SourceEntityId
- TargetEntityId
- EffectDefinitionId
- SubmittedInputTick
- AuthoritativeAppliedNetworkTick
- AcceptedOrRejected
- RejectionReason when rejected
- AuthoritativeEffectRuntimeId when accepted
```

The client uses `ClientRequestId` or `ClientEffectSequence` to map predicted local effects to authoritative host effect runtime IDs.

`BaselineRecord` contains a full authoritative state baseline.

```text
BaselineRecord:
- BaselineNetworkTick
- Full LivingEntityState records for all active living entities
- Full active-effect buffers
- Full condition state and contributor records
- LastProcessedInputTick for the receiving client when available
- DeltaVersion
```

#### 15.7.3 Client Path

The client path uses a local `StateEngine` instance and client-side support buffers.

Required client-side runtime components:

```text
ClientStateEngine = local prediction and reconciliation simulation instance
DeltaReader = pooled host packet reader
ClientPredictionHistory = local submitted input/effect history
ClientEffectIdMap = ClientRequestId to authoritative EffectRuntimeId map
ClientInterpolationBuffer = host-authoritative remote entity state ring buffer
ClientBaselineState = latest full authoritative baseline
PresentationCorrectionQueue = non-authoritative visual correction events
```

Per received host packet:

1. Validate packet version, delta version, host tick, record counts, and monotonicity.
2. Apply `BaselineRecord`s first when present.
3. Apply `AckRecord`s and discard acknowledged local prediction history.
4. Apply `LocalSubmissionResultRecord`s and map or reject predicted effects.
5. Apply `StateDeltaRecord`s for the packet tick.
6. For locally owned entities, correct local prediction state and replay unacknowledged local predictions.
7. For remote entities, push host-authoritative state into the interpolation buffer.
8. Emit presentation correction events after gameplay state is corrected.
9. Prune client snapshots, prediction history, and interpolation history older than configured retention.

Client gameplay state correction happens immediately and deterministically. Presentation may smooth, fade, cancel, or interpolate visuals separately.

#### 15.7.4 Client Correction and Replay

For a received authoritative delta at tick `T`:

1. Find the retained client snapshot for tick `T`.
2. If no snapshot or baseline exists for `T`, reject the delta for gameplay application and request a full baseline.
3. Apply the authoritative final state from the host delta to tick `T`.
4. Compare predicted state against authoritative state using configured precision.
5. Remove predicted local effects that were rejected, omitted, or superseded by host results.
6. Map confirmed predicted effects from `ClientRequestId` to authoritative `EffectRuntimeId`.
7. Restore corrected state at tick `T`.
8. Replay ticks `T + 1` through current local tick using only unacknowledged local predictions and retained deterministic history.
9. Preserve host-authoritative state for every field included in the delta.
10. Emit correction metadata for presentation and diagnostics.

Numeric stat comparison must use `StatComparisonPrecisionDecimals`.

Conditions must compare by exact condition mask match.

Active-effect comparison must use authoritative runtime identifiers plus deterministic fields such as stack count, expiry tick, insertion sequence, parent/child metadata, accumulated state, and applied tick.

If the correction tick is older than the client retention window, the client must request a full baseline instead of attempting replay.

#### 15.7.5 Local Prediction Policy

The client should run local gameplay simulation only for locally owned entities.

Local prediction is allowed only for effects that pass deterministic local pre-validation using:

```text
same EffectDefinition registry
same StatRegistry
same StateEngineConfig
same deterministic timing conversion
known local LivingEntityState
known cooldown, resource, range, target, and blocking facts when available
```

Effects that depend on unknown host-only facts must use visual-only anticipation until the host confirms them.

The client must send a stable request identity with local submissions:

```text
ClientRequestId
ClientEffectSequence
SubmittedInputTick
SourceEntityId
TargetEntityId
EffectDefinitionId
```

The host returns authoritative effect identity in `LocalSubmissionResultRecord`.

If the host rejects or omits a predicted effect, the client removes that effect from gameplay state during reconciliation and replays the remaining unacknowledged predictions.

Presentation may cancel or fade the visual for a rejected predicted effect. Gameplay state must not keep the rejected effect.

#### 15.7.6 Remote Entity Presentation

Remote living entities must be driven by host deltas, not full local gameplay prediction.

The client should store remote authoritative states in an interpolation buffer and render remote entities behind the latest received host tick.

Recommended default:

```text
RemoteInterpolationDelayTicks = 2
Allowed practical range = 2 to 3 ticks
At 30 TPS, 2 ticks = about 66.7 ms
At 30 TPS, 3 ticks = 100 ms
```

This delay is normally negligible because it applies to remote entities. The local owned entity remains responsive through client-side prediction.

Remote active effects must be present and synchronized from host `ChangedEffects` records. Gameplay state for remote entities should snap to authoritative buffer states. Presentation may interpolate numeric display values, smooth movement/attachment visuals, start or stop effect visuals from host effect records, and fade removed effect visuals.

The client must not preserve remote gameplay state that conflicts with the host.

#### 15.7.7 Baseline and Resync

The client must receive a full authoritative baseline:

- on initial join,
- after reconnect,
- after version mismatch,
- after missing required delta history,
- after receiving a correction older than retained snapshots,
- when local state cannot be reconciled deterministically.

After a baseline is applied:

1. Clear obsolete prediction state except local submissions not yet covered by host acknowledgement.
2. Reset authoritative snapshots to the baseline tick.
3. Rebuild interpolation buffers from the baseline.
4. Resume delta application only for deltas newer than the baseline tick.
5. Re-run local prediction from the baseline tick when valid unacknowledged local inputs remain.

The baseline must include all active living entities and enough active-effect, condition, contributor, stat, and timing state to continue deterministic client prediction and remote interpolation.

#### 15.7.8 Client Performance Requirements

The client path must remain allocation-conscious and data-oriented.

Required practices:

- parse host packets through a pooled `DeltaReader`,
- apply only dirty fields from each `StateDeltaRecord`,
- use ring buffers for prediction snapshots and remote interpolation states,
- keep local replay windows bounded by `SnapshotRetentionTicks`,
- predict only locally owned entities by default,
- avoid full remote entity simulation,
- avoid per-record heap allocation during normal packet application,
- sort correction and replay records only by documented stable keys,
- prune acknowledged prediction history immediately after `AckRecord` application,
- store effect identity mapping in preallocated or pooled structures sized by config.

Client replay cost should scale with:

```text
owned predicted entities
unacknowledged local request count
correction window length
dirty fields in host delta records
```

It should not scale with full simulation of every remote living entity.

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
MaxPassiveEffectsPerEntity = configured
MaxReactiveEventsPerTick = configured
MaxReactiveDepth = configured
MaxPassiveOutputsPerEvent = configured
SnapshotRetentionTicks = 45
ClientPredictionRetentionTicks = 45
RemoteInterpolationDelayTicks = 2
MaxClientReplayTicks = 45
MaxUnacknowledgedClientRequests = configured
RequireBaselineOnJoin = true
RequireBaselineOnResync = true
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
- rejected passive records,
- stack decisions,
- passive runtime stack decisions,
- blocking decisions,
- first application/execution events,
- passive result-event queue summaries,
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
Runtime allocation for normal host packet reading and delta application
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
- reject overflow deterministically,
- parse host packets through pooled readers,
- keep client prediction and interpolation history in bounded ring buffers,
- prune acknowledged prediction history immediately after acknowledgement records are applied.

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
- Use deterministic client request identity mapping when reconciling predicted effects.
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

Client reconciliation must use stable local request identity fields:

```text
ClientRequestId
ClientEffectSequence
SubmittedInputTick
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
- passive modifiers target `EffectiveMaxValue` only,
- passive definitions use valid `EvaluationPolicies`,
- reactive passive outputs reference valid active effect definitions,
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
- passive source removal clears generated `PassiveEffect` records and passive runtime state,
- reactive passive outputs enter the normal active-effect pipeline,
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
- invalid timing values,
- invalid passive tag domains,
- invalid passive evaluation policies,
- invalid passive runtime stacking fields,
- invalid reactive passive output specs,
- invalid passive snapshot requirements.

### 21.10 Client Path Tests

Tests must prove:

- full baseline applies all living entity state, active effects, conditions, contributor records, and tick metadata,
- host packet record envelope parses `StateDeltaRecords`, `AckRecords`, `LocalSubmissionResultRecords`, and `BaselineRecords`,
- client discards prediction history up to `LastProcessedInputTick`,
- client maps `ClientRequestId` or `ClientEffectSequence` to authoritative `EffectRuntimeId`,
- accepted predicted local effect reconciles without duplicate active effects,
- rejected predicted local effect is removed during reconciliation,
- omitted unconfirmed predicted effect is removed when host acknowledgement proves it was not accepted,
- correction at a past tick restores retained snapshot and replays unacknowledged local predictions,
- correction older than retained snapshots requests a full baseline,
- remote living entities use interpolation buffer and do not run full gameplay prediction,
- remote active effects appear from host `ChangedEffects` records,
- gameplay state correction is immediate while presentation receives separate correction events,
- delta application avoids avoidable allocation during normal packet processing.

### 21.11 Passive Runtime Tests

Tests must prove:

- generated `PassiveEffect` records are not `ActiveEffect` records,
- passive classifications remain separate from effect classifications and entity conditions,
- passive tags cannot be used in active-effect removal selectors,
- source-bound passive lifetime is deterministic,
- `PassiveRuntime` cooldowns and runtime stacks are snapshot-backed,
- Bloodsucker stack gain, cap, expiry, same-event heal amount, and final-damage basis are deterministic,
- Fairy Revival emits one same-tick revive spec when cooldown is ready and starts a 9000 tick cooldown at 30 TPS,
- passive result-event queue ordering and overflow behavior are deterministic,
- passive validation fails fast for malformed passive definitions and schemas.

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
      DeltaReader.cs
      DeltaPayloadFlags.cs
      HostStatePacket.cs
      StateDeltaRecord.cs
      AckRecord.cs
      LocalSubmissionResultRecord.cs
      BaselineRecord.cs
    Client/
      ClientPredictionHistory.cs
      ClientEffectIdMap.cs
      ClientInterpolationBuffer.cs
      ClientPacketApplyResult.cs
      ClientBaselineApplyResult.cs
      PresentationCorrectionQueue.cs
    Definitions/
      EffectDefinition.cs
      EffectDefinitionRegistry.cs
      EffectValidator.cs
      PassiveDefinition.cs
      PassiveDefinitionRegistry.cs
      PassiveValidator.cs
    Runtime/
      ActiveEffect.cs
      PassiveEffect.cs
      PassiveRuntime.cs
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
      PassiveResultEvent.cs
      PassiveOutputEffectSpec.cs
      PassiveStackingRules.cs
```

### 22.2 Implementation Boundary

This file defines the required StateEngine runtime contract. It does not define full skill, item, talent, weapon, UI, or network transport implementations.

Any implementation that mutates `LivingEntityState` outside `StateEngine` violates this contract.

---

## 23. Version 6.4.1 Passive Runtime Contract

Version 6.4.1 incorporates the passive-system requirements from `Passives.md` into the StateEngine contract.

Passives are immutable authored definitions that generate source-bound runtime records processed by the StateEngine passive path. Stat passives resolve into `EffectiveMaxValue`. Reactive passives consume deterministic StateEngine result events, update passive runtime state, and emit normal active-effect specs when they need RPG state mutation.

### 23.1 Passive Boundary

Passives are not `EffectDefinition` entries and are not `ActiveEffect` runtime records.

`DurationType: UntilRemoved` remains active-effect lifetime behavior. It must not be used to model equipment stats, talents, permanent upgrades, ship configuration, level-up bonuses, or other persistent passive sources.

Passive source systems remain external to `StateEngine`. They may include:

```text
Items
Talents
Level-ups
Ship configuration
Permanent upgrades
Runtime granted passive sources
Other persistent source state
```

External passive source systems own source lifecycle and authoring concerns. `StateEngine` owns normalized generated passive runtime records, passive dirty/version metadata, deterministic passive evaluation, passive runtime snapshots, and any active-effect specs emitted by reactive passives.

### 23.2 Passive Runtime Types

Required passive runtime model:

```text
PassiveDefinition = immutable authored passive data
PassiveEffect = source-bound runtime passive record generated from PassiveDefinition
PassiveRuntime = mutable runtime state owned by the generated PassiveEffect
PassiveSchemas.yml = validation and runtime schema contract
PassiveDefinitions.yml = authored passive definition data
PassiveModifierSource = external source state that creates or removes generated PassiveEffects
```

`PassiveEffect` records are stored in StateEngine-owned passive runtime buffers keyed by `TargetEntityId`.

### 23.3 PassiveDefinition

`PassiveDefinition` is immutable static data loaded from `PassiveDefinitions.yml`.

It may define:

```text
Passive identity
Passive classification tags
Evaluation policies
Stat modifiers
Reactive triggers
Source-level stacking rules
Event-driven runtime stacking rules
Emitted active-effect specs
Visibility metadata
Child passive definitions or future passive payloads
```

A passive definition is valid only if it has at least one gameplay payload:

```text
StatModifiers
ReactiveTriggers
PassiveClassifications
ChildPassiveDefinitions or future payloads
```

Classification-only passive definitions are valid only as queryable gameplay metadata that generate a source-bound `PassiveEffect`. Pure UI-only metadata definitions are out of scope unless a future document explicitly introduces them.

### 23.4 PassiveEffect

`PassiveEffect` is a StateEngine-owned runtime record generated from a `PassiveDefinition` and a source.

Required fields:

```text
PassiveRuntimeId
PassiveDefinitionId
SourceId
OriginId
TargetEntityId
AppliedNetworkTick
PassiveInsertionSequence
StackCount
RuntimeStackCount
RuntimeStackExpiresNetworkTick
CooldownUntilNetworkTick
LastEvaluatedNetworkTick
LastTriggeredNetworkTick
RuntimeFlags
```

`PassiveEffect` exists while its source is active. Removing or replacing the source removes the generated `PassiveEffect` and clears its runtime state.

### 23.5 PassiveRuntime

`PassiveRuntime` is the mutable state inside or alongside a `PassiveEffect`.

Examples:

```text
Current Bloodsucker stack count
Bloodsucker stack expiry tick
Fairy Revival cooldown expiry tick
Last event consumed by a passive
Compiled evaluation flags
Source-level stack count
Dirty stat mask contributed by this passive
```

The implementation may store passive runtime state directly on `PassiveEffect`, or in compact parallel runtime-state arrays keyed by `PassiveRuntimeId`. The behavior must remain deterministic, allocation-conscious, and snapshot-friendly.

### 23.6 LivingEntityState Relationship

`LivingEntityState` stores passive stat-layer results and passive dirty/version metadata:

```text
EffectiveMaxValues[]
PassiveDirtyStats
PassiveInputVersion
PassiveResolvedVersion
PassiveLayerDirtyMetadata
```

`LivingEntityState` does not own the external passive source graph.

StateEngine snapshots must include enough passive data to restore and resimulate:

```text
Generated PassiveEffects[]
PassiveRuntime state
PassiveInsertionSequence
Source-level StackCount
RuntimeStackCount
RuntimeStackExpiresNetworkTick
CooldownUntilNetworkTick
LastEvaluatedNetworkTick
LastTriggeredNetworkTick
Passive dirty/version metadata
```

### 23.7 Passive Tags

Passive metadata uses a separate gameplay tag domain:

```text
DomainKey: PassiveClassification
RootPrefix: Passive
```

Examples:

```text
Passive.Reactive.DamageDealt.LifeLeech
Passive.Stacking.Bloodsucker
Passive.Stat.EnergyStorage
Passive.Source.Talent
Passive.Source.Item
Passive.Source.ShipSystem
Passive.Reactive.Death
Passive.Cooldown.Revive
```

Passive classifications are not `EntityCondition` tags and are not `EffectClassification` tags.

Required tag-domain behavior:

```text
Passive classifications remain separate from EntityCondition.
Passive classifications remain separate from EffectClassification.
Passive classifications do not own passive lifetime.
Passive classifications do not mutate LivingEntityState.
Passive tags cannot be used in active effect removal selectors.
```

The tag system remains context-free. It provides domain-safe tag sets and queries only. The passive pipeline and StateEngine define what passive tags mean.

### 23.8 Evaluation Policies

Each passive definition declares when its generated runtime passive effect is evaluated.

Supported initial policies:

| Policy | Meaning | Typical use |
|---|---|---|
| `OnSourceDirty` | Evaluate when the source is added, removed, replaced, or dirtied. | Permanent stat passives such as +20% EnergyStorage. |
| `OnEvent` | Evaluate when a subscribed deterministic result event is produced. | Bloodsucker reacting to `DamageDealt`. |
| `OnTickWhileRuntimeActive` | Evaluate while the runtime has active internal state. | Expiring Bloodsucker runtime stacks. |
| `EveryMs` | Evaluate periodically at a deterministic interval. | Rare passive maintenance. |

Passives must not be evaluated every tick unless their `EvaluationPolicies` require it.

Bloodsucker uses:

```yaml
EvaluationPolicies:
  - EvaluationPolicy: OnEvent
    EventType: DamageDealt
  - EvaluationPolicy: OnTickWhileRuntimeActive
```

### 23.9 Stat Passives

Stat passives contribute only to `EffectiveMaxValue`.

Valid target:

```text
TargetValueKind: EffectiveMaxValue
```

Invalid passive stat targets:

```text
BaseValue
CurrentValue
CurrentMaxValue
```

Valid calculation modes:

```text
Flat
Percentage
```

Percentage values use decimal fractions:

```text
0.20 = 20 percent
```

Passive flat modifiers are added before passive percentage modifiers. Passive percentage modifiers are additive with each other.

```text
BaseValue
+ passive flat modifiers
then additive passive percentage modifiers
= EffectiveMaxValue
```

Example:

```text
BaseValue: 100
Passive flat modifier: +20
Passive percentage modifiers: +10% and +15%
EffectiveMaxValue: (100 + 20) * 1.25 = 150
```

If no passive modifier exists for a stat:

```text
EffectiveMaxValue = BaseValue
```

Equipment stats are source-bound passives. Equipping `Chest Armor of Vigor` should generate a passive source for `Armor +30`, `Health +30`, and `HealthRegeneration +5`. Unequipping or replacing the item removes that source-bound `PassiveEffect`, clears its passive runtime state, and recalculates affected `EffectiveMaxValue` values from remaining passive sources.

This is not `DurationType: UntilRemoved` active-effect behavior. Equipment stats do not need active-effect triggering, blocking, condition lifecycle, active-effect removal requests, or active-effect classifications.

`HealthRegeneration` must use the stat registry's canonical unit. If designers author regeneration in a friendlier unit such as per second, import validation should convert it to the canonical runtime unit before simulation.

### 23.10 Reactive Passives

Reactive passives listen to deterministic StateEngine result events.

Initial event types:

```text
DamageDealt
DamageTaken
HealthRaised
HealthReduced
ConditionAdded
ConditionRemoved
ActiveEffectAccepted
ActiveEffectRemoved
```

The first implementation should include only the events required by actual passive definitions. Bloodsucker requires `DamageDealt`. Fairy Revival requires `ConditionAdded`.

Reactive passives may:

```text
Check event fields
Update PassiveRuntime
Gain or refresh runtime stacks
Start or check runtime cooldowns
Emit ActiveEffectSpec outputs
Mark passive state dirty
Schedule same-tick bounded reaction work
```

Reactive passives must not directly mutate `LivingEntityState`.

### 23.11 ActiveEffectSpec Output

Reactive passives emit normal `ActiveEffectSpec` records when they need to mutate RPG state.

The referenced `EffectDefinition` remains immutable. The runtime spec carries deterministic set-by-caller overrides such as `Target.Value`.

Bloodsucker emits:

```text
EffectDefinitionId: effect.raise.heal
TargetEntityId: Passive owner
SetByCaller Target.Value: AppliedDamage * RuntimeStackCount * 0.03
```

Fairy Revival emits:

```text
EffectDefinitionId: effect.health.revive
TargetEntityId: Passive owner
SetByCaller Target.Value: Health.EffectiveMaxValue * 0.50
```

`Health.EffectiveMaxValue` means the revive amount includes base Health and passive Health bonuses. It does not include temporary active max-health effects. A future item or talent that should revive from final active capacity must explicitly use `Health.CurrentMaxValue`.

The StateEngine processes emitted specs through normal active-effect validation, blocking, stat transaction, dirty-state, and result-event rules.

### 23.12 Passive Stacking

The passive system has two stack layers.

Source-level `Stacking` controls how multiple sources generating the same passive combine.

Supported passive stack scopes:

```text
ByPassiveDefinitionId
SameSource
SameSourceAndOrigin
ByStackGroupId
```

Supported policies and behaviors mirror active effects:

```text
StackPolicy: Stack, Replace, Discard
StackBehavior: IndependentStacks, RefreshDuration, AddStackRefreshDuration, AddStackNoRefresh
MaxStackOverflowPolicy: ReplaceOldest, RejectNewest
```

Use `ByPassiveDefinitionId`, not `ByEffectDefinitionId`.

`RuntimeStacking` controls event-driven stacks owned by one generated `PassiveEffect`.

Bloodsucker runtime stacking:

```text
MaxStacks: 10
ValuePerStack: 0.03
MaxOutputMultiplier: 0.30
StackDurationMs: 5000
ExpirationPolicy: RefreshAllOnGain
ApplyNewStackBeforeOutput: true
```

This separates "two sources grant Bloodsucker" from "Bloodsucker currently has 7 combat stacks".

### 23.13 Bloodsucker

Bloodsucker is a reactive passive.

When the owner deals final applied damage:

```text
1. Ignore self-damage.
2. Allow reflected or retaliation damage if OriginId is the passive owner.
3. Gain one runtime stack, up to 10.
4. Refresh the shared stack window to 5000 ms.
5. Calculate heal percentage after the new stack is gained.
6. Heal owner for AppliedDamage * RuntimeStackCount * 0.03.
7. Maximum heal multiplier is 0.30.
8. Emit a same-tick ActiveEffectSpec for effect.raise.heal with a set-by-caller value.
```

Examples:

```text
First qualifying hit:
Stack count becomes 1.
Heal = AppliedDamage * 0.03.

Tenth qualifying hit:
Stack count becomes 10.
Heal = AppliedDamage * 0.30.

Later hit while capped:
Stack count remains 10.
Stack expiry is refreshed.
Heal = AppliedDamage * 0.30.
```

Bloodsucker uses final applied damage after mitigation and clamping. It does not heal from theoretical raw damage.

### 23.14 Fairy Revival

Fairy Revival is a talent passive that reacts to death.

When `Condition.Dead` is added to the owner:

```text
1. Check whether CooldownUntilNetworkTick is absent or <= current NetworkTick.
2. If on cooldown, do nothing.
3. If ready, emit effect.health.revive in the same deterministic tick.
4. Override Target.Value to Health.EffectiveMaxValue * 0.50.
5. Start cooldown for 300000 ms.
```

At 30 TPS:

```text
300000 ms = 5 minutes = 9000 NetworkTicks
```

Fairy Revival uses:

```text
PassiveDefinitionId: passive.talent.fairy_revival
PassiveClassifications:
- Passive.Reactive.Death
- Passive.Cooldown.Revive
- Passive.Source.Talent
EvaluationPolicy: OnEvent, EventType: ConditionAdded
StackScope: ByPassiveDefinitionId
StackPolicy: Discard
StackBehavior: IndependentStacks
MaxStacks: 1
MaxStackOverflowPolicy: RejectNewest
ReactiveTriggerId: fairy_revival.on_dead
TriggerFilters: OwnerMatch TargetEntityId and EventCondition Condition.Dead
RuntimeCooldown: CooldownMs 300000, StartCooldownOn OutputSuccess
OutputKind: ActiveEffectSpec
OutputTiming: SameTick
EffectDefinitionId: effect.health.revive
TargetEntity: PassiveOwner
SetByCaller Target.Value = Health.EffectiveMaxValue * 0.50, ClampMin 0.0
Visibility: Hud
```

The revive effect is responsible for restoring Health and requesting removal of `Condition.Dead` according to active-effect rules.

If Health remains `<= 0` after same-tick revive processing, the existing derived-condition rule may reapply `Condition.Dead`.

Fairy Revival cooldown is passive runtime state, not an active effect:

```text
CooldownUntilNetworkTick
LastTriggeredNetworkTick
```

The cooldown must be included in snapshots and rollback/resimulation.

### 23.15 Result Event Queue

Reactive passive processing uses a bounded same-tick result-event queue.

Ordering:

```text
1. Original event production order from StateEngine resolution
2. TargetEntityId
3. PassiveInsertionSequence
4. PassiveRuntimeId
5. Emitted ActiveEffectSpec normal effect ordering
```

Required limits:

```text
MaxReactiveEventsPerTick
MaxReactiveDepth
MaxPassiveOutputsPerEvent
```

Overflow is deterministic:

```text
Accept up to configured capacity.
Reject or defer remaining records according to config.
Record deterministic rejection metadata when diagnostics are enabled.
Do not depend on collection iteration order.
```

Same-event passive reactions are ordered by `TargetEntityId`, `PassiveInsertionSequence`, and `PassiveRuntimeId`; emitted active effects then use normal active-effect ordering.

Emitted active effects may trigger later reactive systems through the normal result-event pipeline, constrained by recursion depth and same-tick event limits.

### 23.16 Passive Snapshot and Replay

Passive runtime state must be snapshot-backed.

Snapshots must include:

```text
Generated PassiveEffects[]
PassiveRuntimeId
PassiveDefinitionId
SourceId
OriginId
TargetEntityId
PassiveInsertionSequence
Source-level StackCount
RuntimeStackCount
RuntimeStackExpiresNetworkTick
CooldownUntilNetworkTick
LastEvaluatedNetworkTick
LastTriggeredNetworkTick
Passive dirty/version metadata
```

Given identical:

```text
Initial state
Passive definitions
Passive source events
Active effect inputs
Result event history
NetworkTick range
Config
Tag registry hash
```

the StateEngine must produce identical passive runtime state, final stats, conditions, active effects, and deltas.

### 23.17 Passive Validation

Passive validation happens at startup and fails fast before normal simulation.

The passive validator must check:

```text
YAML parses successfully.
Definition.PassiveDefinitionId, Definition.Name, and Definition.Description are present.
A passive has at least one gameplay payload.
Classification-only passives are queryable gameplay metadata, not UI-only metadata.
Passive tags use PassiveClassification and canonical Passive.* names.
Passive direct assignment fields use assignable passive tags.
Passive selector fields may use queryable parent/category passive tags.
Stat modifiers target EffectiveMaxValue only.
Stat modifier target stats exist in the stat registry.
Percentage values use decimal fractions.
EvaluationPolicies are present and compatible with the passive payload.
OnEvent policies specify an event type.
EveryMs policies specify an interval greater than 100 ms.
Runtime stack durations are greater than 100 ms when present.
Runtime cooldown durations are greater than 100 ms when present.
Source-level stacking fields are valid.
ByStackGroupId requires StackGroupId.
MaxStacks, when present, is greater than zero.
Reactive outputs reference existing EffectDefinitionId values.
Runtime set-by-caller fields target explicitly allowed override fields.
Set-by-caller formulas reference valid event values or valid stat layer values.
Event filters use valid tag domains.
Generated runtime records have unique PassiveRuntimeId values.
Passive runtime snapshots contain enough state to restore and resimulate.
```

### 23.18 Passive Test Requirements

Passive tests must prove:

```text
Passive classifications remain separate from EntityCondition and EffectClassification.
Passive tags cannot be used in active-effect removal selectors.
Stat passives update EffectiveMaxValue only when source state changes.
Passive flat modifiers apply before additive passive percentages.
Generated PassiveEffect lifetime is source-bound.
Removing or replacing a source clears PassiveRuntime state.
Reactive passives do not directly mutate LivingEntityState.
Bloodsucker gains a stack before calculating same-event healing.
Bloodsucker caps at 10 stacks and 30 percent healing.
Bloodsucker uses final applied damage after mitigation and clamping.
Fairy Revival emits a same-tick revive spec when Condition.Dead is added and cooldown is ready.
Fairy Revival cooldown is snapshot-backed and rollback-safe.
Passive result-event ordering is deterministic.
Passive output overflow is deterministic.
Passive snapshots restore and resimulate passive runtime state.
Passive validation fails fast for invalid definitions and schemas.
```

### 23.19 Passive Open Follow-Up Questions

Open follow-up questions retained from `Passives.md`:

```text
Should passive output overflow reject or defer when the same-tick reaction queue is full?
Should passive event subscriptions be precompiled per event type for cache locality?
Should stat-only passives use a smaller runtime record than reactive passives?
Should passive source systems provide source slot ordering, and should that ordering participate in tie-breakers?
Should UI expose source-level stacks and runtime stacks separately?
Should PassiveDefinition support child passive definitions in v1 or keep them future-only?
Should emitted ActiveEffectSpec support a general set-by-caller payload map or only Target.Value in v1?
Should passive runtime IDs be globally unique or entity-local plus insertion sequence?
```
