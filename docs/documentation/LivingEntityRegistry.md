# LivingEntityRegistry

**Source PRDs:** `FLU_RPG_StateEngine_PRD.md`, `LivingEntityState.md`, `Effects.md`, `TagSystem.md`  
**Version:** 1.0.0  
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Genre:** 2D Space RPG  
**Network Model:** Host-authoritative or standalone server simulation with rollback/resimulation support  
**Network Tick Rate:** 30 TPS  
**Primary Concern:** Authority-gated living entity presence, lifecycle, and deterministic system cleanup coordination

---

## Table of Contents

- [1 Purpose](#1-purpose)
- [2 Decision Summary](#2-decision-summary)
- [3 Industry Alignment](#3-industry-alignment)
- [4 Scope](#4-scope)
- [5 Core Definition](#5-core-definition)
- [6 Runtime Architecture Context](#6-runtime-architecture-context)
- [7 Responsibilities](#7-responsibilities)
- [8 Non-Goals](#8-non-goals)
- [9 Ownership and Authority](#9-ownership-and-authority)
- [10 Lifecycle Model](#10-lifecycle-model)
- [11 Registration Model](#11-registration-model)
- [12 Removal Model](#12-removal-model)
- [13 Query and Read Access Model](#13-query-and-read-access-model)
- [14 ILivingEntityInterface Contract](#14-ilivingentityinterface-contract)
- [15 Public API Direction](#15-public-api-direction)
- [16 StateEngine Integration](#16-stateengine-integration)
- [17 Effects Integration](#17-effects-integration)
- [18 LivingEntityStateTracker Relationship](#18-livingentitystatetracker-relationship)
- [19 Lifecycle History and Rollback](#19-lifecycle-history-and-rollback)
- [20 Determinism Requirements](#20-determinism-requirements)
- [21 Performance and Allocation Requirements](#21-performance-and-allocation-requirements)
- [22 Validation Requirements](#22-validation-requirements)
- [23 Testing and Acceptance Criteria](#23-testing-and-acceptance-criteria)
- [24 Example Flows](#24-example-flows)
- [25 Minimal Clarifying Snippets](#25-minimal-clarifying-snippets)

---

## 1 Purpose

`LivingEntityRegistry` is the authoritative runtime registry for living entity presence and lifecycle.

It provides the central lookup place where systems can check whether an entity implementing `ILivingEntityInterface` is registered, present, pending removal, removed, or historically present at a retained `NetworkTick`.

The registry coordinates entity lifecycle events for systems such as:

- `StateEngine`,
- `LivingEntityStateTracker`,
- active effect processing,
- passive modifier processing,
- player session systems,
- spawned agent systems,
- heartbeat or liveness monitors,
- AI, targeting, UI, debug, and analytics systems.

The registry does not own those systems' internal data. It tells them which entities are present and emits deterministic lifecycle events so each system can create, stop, clean up, retain, or purge its own state.

---

## 2 Decision Summary

The required design decisions are:

- `LivingEntityRegistry` is an instance-scoped runtime service, not a hidden global singleton.
- It is a world/session-level lifecycle registry, not a StateEngine-owned list.
- Registration is authority-gated.
- Registration and removal requests are batched and committed at deterministic tick boundaries.
- Removal is represented as a lifecycle event with an effective `NetworkTick`.
- The registry owns compact lifecycle history only.
- `LivingEntityStateTracker` continues to own StateEngine state snapshots and applied-effect history.
- General systems receive read-only registry views.
- Runtime mutation of `LivingEntityState` remains owned by `StateEngine`.
- System cleanup is system-owned: the registry never deletes active effects, condition contributors, snapshots, passive caches, UI state, or AI state directly.
- Public Godot `Autoload` access may exist as a facade, but core simulation must work through explicit instances.

This refines the earlier StateEngine PRD wording where `StateEngine.RegisterLivingEntity(...)` was the primary registration entry point. In the refined architecture, that API may remain as a compatibility facade or test helper, but authoritative registration flows through `LivingEntityRegistry`.

---

## 3 Industry Alignment

The registry follows patterns used by proven game architectures:

- Unreal Engine separates spawned/destroyed actor lifecycle from specific gameplay systems. Actors can hold components, while components provide specialized capabilities.
- Unreal Gameplay Ability System uses an Ability System Component and an interface boundary for ability, attribute, and effect interaction instead of making every system mutate arbitrary actor state.
- Godot groups can decouple scene discovery, but groups are not authoritative deterministic lifecycle state. `LivingEntityRegistry` provides tick-stamped lifecycle semantics that groups do not provide.
- Godot `Autoload` can expose session services, but a core registry should remain instance-based so tests, replay, host simulation, client prediction, and standalone server simulation can run without hidden shared state.
- ECS-style runtime architecture commonly keeps entity presence/indexing separate from system-owned component/state storage. Each system removes its own data when an entity leaves the world.

Reference concepts:

```text
Unreal Actors:
https://dev.epicgames.com/documentation/en-us/unreal-engine/actors-in-unreal-engine

Unreal Components:
https://dev.epicgames.com/documentation/en-us/unreal-engine/components-in-unreal-engine

Unreal Gameplay Ability System:
https://dev.epicgames.com/documentation/en-us/unreal-engine/gameplay-ability-system-for-unreal-engine

Godot Groups:
https://docs.godotengine.org/en/stable/tutorials/scripting/groups.html

Godot Autoload:
https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html
```

The practical conclusion for this project is:

```text
LivingEntityRegistry owns presence and lifecycle.
StateEngine owns RPG state mutation.
LivingEntityStateTracker owns state/effect history.
Other systems own their own caches and cleanup.
```

---

## 4 Scope

### 4.1 In Scope

`LivingEntityRegistry` must support:

- registering living entities from authoritative spawn/session systems,
- request-based removal from authorized lifecycle systems,
- central presence lookup by `EntityId`,
- deterministic active-entity iteration,
- tick-boundary commit of lifecycle changes,
- read-only lifecycle and registration views,
- compact lifecycle event history,
- historical presence queries for rollback/resimulation,
- deterministic duplicate and conflict handling,
- lifecycle event polling by systems,
- validation of `ILivingEntityInterface` registration input,
- integration with `StateEngine`, effects, passive systems, and state history.

### 4.2 Out of Scope

`LivingEntityRegistry` must not implement:

- spawning scene nodes,
- instancing Godot scenes,
- player heartbeat checks,
- network peer liveness detection,
- player authentication,
- matchmaking,
- AI behavior,
- target acquisition,
- combat rules,
- active effect execution,
- stat mutation,
- condition mutation,
- passive source resolution,
- snapshot storage,
- applied-effect history storage,
- delta serialization,
- UI presentation state,
- direct Godot group membership as authoritative state.

Those systems may call the registry, but their own logic remains outside the registry.

---

## 5 Core Definition

A `LivingEntityRegistry` is an authoritative, deterministic runtime registry that maps stable `EntityId` values to compact lifecycle records for entities implementing `ILivingEntityInterface`.

It answers:

```text
Is this entity registered?
Is this entity present for this NetworkTick?
What lifecycle event changed this entity?
Which living entities are currently present in deterministic order?
What registration metadata was accepted for this entity?
Was this entity present at a retained historical tick?
```

It does not answer:

```text
What is this entity's current Health?
Should this effect execute?
Should this player be disconnected?
Should this entity be spawned?
Should this target be selected?
Should this condition be applied?
Should this active effect be removed for gameplay reasons?
```

Those questions belong to the owning gameplay systems.

---

## 6 Runtime Architecture Context

### 6.1 LivingEntity

A `LivingEntity` is any gameplay object that can participate in RPG state.

Examples:

- player-controlled ship,
- AI-controlled ship,
- enemy,
- NPC,
- creature,
- friendly entity,
- neutral entity,
- hostile entity.

A `LivingEntity` implements `ILivingEntityInterface` to provide stable identity, registration metadata, and initial state construction data.

### 6.2 LivingEntityRegistry

The registry owns the authoritative lifecycle record for each living entity.

It is the central source of truth for whether an entity is present in the current simulation session.

It must be shared by systems through dependency injection or an explicit session/service container. A Godot `Autoload` facade may expose the current instance for scene convenience, but the registry itself must remain instance-based.

### 6.3 StateEngine

`StateEngine` consumes registry lifecycle events.

On registration events, it creates or imports the `LivingEntityState` it owns for simulation.

On removal events, it removes the entity from current processing and cleans up StateEngine-owned effects, conditions, dirty state, pending inputs, and current state storage.

`StateEngine` remains the only system that mutates runtime RPG state.

### 6.4 LivingEntityStateTracker

`LivingEntityStateTracker` stores StateEngine snapshots and applied-effect history.

It does not own entity presence. It uses registry lifecycle events and historical presence queries to know when state history is valid, when processing must stop, and when retained history can be purged.

### 6.5 External Lifecycle Systems

External systems may request lifecycle changes:

| System | Registry Interaction |
|---|---|
| `SpawnSystem` | Requests registration for spawned agents or NPCs. |
| `PlayerSessionSystem` | Requests registration for accepted players. |
| `HeartbeatMonitor` | Requests removal when a player becomes unreachable. |
| `SceneUnloadSystem` | Requests removal for entities owned by an unloaded scene or area. |
| `ServerAuthority` | Approves or rejects lifecycle requests. |
| `TestHarness` | Registers deterministic test entities. |

Those systems do not directly mutate StateEngine state.

---

## 7 Responsibilities

`LivingEntityRegistry` must:

- accept registration requests from authorized sources,
- accept removal requests from authorized sources,
- assign deterministic registration sequence values,
- commit lifecycle changes at `NetworkTick` boundaries,
- store current lifecycle state per `EntityId`,
- provide deterministic current entity iteration,
- expose read-only registration and lifecycle views,
- expose lifecycle events through deterministic polling,
- retain compact lifecycle history for rollback/resimulation windows,
- reject duplicate or invalid registration deterministically,
- reject removal of missing entities deterministically,
- prevent unsafe `EntityId` reuse within retained history,
- notify registered interfaces through lifecycle hooks when safe,
- avoid owning or mutating system-specific state.

---

## 8 Non-Goals

`LivingEntityRegistry` must not:

- become a global mutable singleton required by core simulation,
- be owned by `StateEngine`,
- own active effects,
- own conditions,
- own condition contributor records,
- own current stat arrays,
- own passive source data,
- own state snapshots,
- own applied-effect history,
- own network transport,
- own heartbeat connections,
- own player session authentication,
- own object spawning,
- own Godot scene tree hierarchy,
- own movement, AI, weapons, skills, items, or UI behavior,
- call gameplay effect logic as part of entity removal,
- use object references as authoritative entity identity.

---

## 9 Ownership and Authority

### 9.1 Authority-Gated Registration

Only authorized systems may create authoritative living entity registry entries.

Recommended authorized registration sources:

```text
ServerAuthority
HostAuthority
PlayerSessionSystem
SpawnSystem
DeterministicTestHarness
```

Non-authoritative systems may query registry state but must not create authoritative entries.

Examples of systems that should not register entities directly:

```text
UI
HUD
Inventory UI
Client prediction presentation
Debug overlay
HeartbeatMonitor
TargetingSystem
EffectSystem
PassiveModifierPipeline
```

### 9.2 Authorized Removal Requests

Removal may be requested by authorized lifecycle systems.

Examples:

```text
HeartbeatMonitor -> unreachable player
PlayerSessionSystem -> disconnected player
SpawnSystem -> despawned agent
SceneUnloadSystem -> area unloaded
ServerAuthority -> administrative removal
TestHarness -> deterministic cleanup
```

The registry validates authority and records the accepted or rejected request.

### 9.3 Object Reference Policy

`EntityId` is the authoritative identity.

The registry must not use Godot node references, C# object references, or interface object identity as authoritative identity.

The registry may store an optional weak runtime object handle for lifecycle callbacks, diagnostics, or editor/debug tooling, but simulation correctness must not depend on that object reference staying alive.

If an entity object disappears unexpectedly, an owning monitor or scene lifecycle system must request registry removal.

---

## 10 Lifecycle Model

### 10.1 Lifecycle States

The registry uses explicit lifecycle states.

```text
Unknown
PendingRegistration
Registered
RemovalRequested
Removed
Purged
```

| State | Meaning |
|---|---|
| `Unknown` | No current or retained lifecycle record exists. |
| `PendingRegistration` | Registration request accepted but not committed. |
| `Registered` | Entity is present for current simulation after commit. |
| `RemovalRequested` | Removal request accepted but effective tick is not committed yet. |
| `Removed` | Entity is not present; compact history is retained. |
| `Purged` | Current record and retained lifecycle history are no longer required. |

### 10.2 Tick-Boundary Commit

Lifecycle changes are committed at deterministic `NetworkTick` boundaries.

Required tick order:

1. Gather lifecycle requests from authorized systems.
2. Commit registry lifecycle changes for `NetworkTick`.
3. Publish lifecycle events for that tick.
4. Systems consume lifecycle events.
5. `StateEngine` processes the tick using the committed registry presence set.
6. Systems retain or purge their own data according to their retention rules.

`CommitLifecycleChanges(networkTick)` must be called before systems process that tick.

### 10.3 Presence Semantics

An entity is present for tick `T` if:

```text
There is a committed Registered event with EffectiveNetworkTick <= T
and there is no committed Removed event with EffectiveNetworkTick <= T.
```

A registration effective at tick `T` means the entity may be processed for tick `T`.

A removal effective at tick `T` means the entity must not be processed as present for tick `T`; systems may still run cleanup for the removal event at tick `T`.

### 10.4 Request Timing

In normal runtime mode:

- requests accepted before commit for tick `T` may be committed at tick `T`,
- requests received after commit for tick `T` must not affect tick `T`,
- late requests are committed no earlier than the next valid commit tick,
- past-tick lifecycle mutation is rejected unless a dedicated replay/correction mode is explicitly implemented.

This prevents mid-tick registry changes from changing system behavior inside the same simulation tick.

---

## 11 Registration Model

### 11.1 Registration Input

Registration requires:

```text
EntityId
ILivingEntityInterface reference or registration provider
RegistrationSourceKind
RequestedNetworkTick
Authority context
Initial LivingEntityState provider or snapshot
Entity classification metadata when available
```

The registry must validate the entity before accepting the pending registration.

### 11.2 Registration Source Kinds

Recommended source kinds:

```text
PlayerSession
SpawnedAgent
SpawnedNpc
SpawnedEnemy
ScriptedEntity
RestoredFromSave
TestHarness
```

Source kind is lifecycle metadata. It must not replace gameplay tags, faction state, AI behavior, or presentation state.

### 11.3 EntityId Rules

`EntityId` must be stable for the lifetime of a living entity.

Rules:

- `EntityId` must be unique among current and retained lifecycle records.
- `EntityId` must not be reused within the lifecycle history retention window.
- Recommended: never reuse `EntityId` values within a simulation session.
- If small IDs must be reused later, use a generation field or `LivingEntityHandle` so stale references can be detected deterministically.

### 11.4 Registration Conflict Policy

Duplicate registration for an already present entity must be rejected deterministically.

Duplicate registration for an entity with retained `Removed` history must be rejected until the reuse policy allows it.

Registration with invalid `ILivingEntityInterface` data must be rejected before commit.

Registration with invalid initial state must be rejected before StateEngine imports it.

### 11.5 Deterministic Registration Order

Committed registrations must receive a stable registration sequence.

Recommended active entity iteration order:

```text
1. RegistrationNetworkTick
2. RegistrationSequence
3. EntityId
```

The registry must not expose dictionary iteration order as authoritative processing order.

---

## 12 Removal Model

### 12.1 Removal Requests

Removal is requested, not performed directly by arbitrary systems.

Required removal request fields:

```text
EntityId
RemovalReason
RequestedNetworkTick
RequestedBySystem
Authority context
Optional diagnostic message
```

### 12.2 Removal Reasons

Recommended removal reasons:

```text
Despawned
PlayerDisconnected
PlayerUnreachable
SessionEnded
SceneUnloaded
AuthorityShutdown
SaveRestoreReplaced
TestCleanup
InvalidRegistration
Administrative
Unknown
```

Death is not a removal reason by itself.

`Condition.Dead` remains a StateEngine-owned condition. A dead entity may remain registered and present.

### 12.3 Removal Commit

When a removal request is committed:

- the registry records a `Removed` lifecycle event,
- the entity is no longer present for the effective tick,
- deterministic event sequence is updated,
- the entity remains in compact lifecycle history until retention expires,
- systems receive the removal event and clean their own data.

### 12.4 Removal of Missing Entities

Removal of a missing entity must be deterministic.

Recommended behavior:

```text
Return rejected removal request.
Record diagnostic metadata when configured.
Do not throw during normal simulation.
Do not create a Removed event for an entity that was never registered.
```

### 12.5 Same-Tick Conflicts

If registration and removal for the same `EntityId` are both committed for the same tick, the registry must resolve the conflict deterministically.

Recommended default:

```text
Process accepted requests by RequestSequence.
If final state after all same-tick requests is Removed, the entity is not present for that tick.
Emit lifecycle events in the resolved deterministic order.
StateEngine may skip simulation work for an entity that registers and removes in the same tick.
```

Content and authority systems should avoid same-tick register/remove conflicts outside tests and exceptional cleanup paths.

---

## 13 Query and Read Access Model

### 13.1 Current Presence Queries

All systems may query current entity presence through read-only API.

Required queries:

```text
IsRegistered(EntityId)
IsRemovalRequested(EntityId)
TryGetRecord(EntityId, out LivingEntityRecordView)
GetOrderedPresentEntityIds()
```

### 13.2 Historical Presence Queries

Rollback and replay-capable systems require retained historical presence queries.

Required queries:

```text
WasPresentAt(EntityId, NetworkTick)
TryGetLifecycleAt(EntityId, NetworkTick, out LivingEntityLifecycleState)
GetLifecycleEventsInRange(startTick, endTick)
```

### 13.3 Read-Only Views

The registry may expose read-only registration records.

A read-only record may include:

```text
EntityId
LifecycleState
RegistrationNetworkTick
RemovalNetworkTick when removed
RegistrationSequence
RegistrationSourceKind
RemovalReason when removed
Entity kind metadata
Debug name
Optional weak object handle
Initial state metadata
```

The read-only record must not expose mutable `LivingEntityState` arrays, active-effect buffers, condition contributor records, or system-owned collections.

### 13.4 Current State Read Access

General systems must not receive mutable `LivingEntityState` through the registry.

If read-only current RPG state is exposed, it must be a StateEngine-owned read model keyed by `EntityId`.

The registry may help route or validate the lookup, but it must not become the owner of current mutable RPG state.

### 13.5 Event Polling

Authoritative systems should consume lifecycle events through deterministic polling.

Godot signals or C# events may exist for presentation/debug layers, but authoritative simulation must not depend on signal delivery order, scene tree order, or asynchronous callbacks.

Recommended event consumption:

```text
System stores last consumed lifecycle event sequence.
After registry commit, system asks for events since that sequence.
System processes events in registry order.
System updates its own cursor.
```

---

## 14 ILivingEntityInterface Contract

### 14.1 Interface Purpose

`ILivingEntityInterface` is the minimal contract an entity implements so it can be registered as a living entity.

It must stay small.

It should expose identity, registration metadata, initial state construction data, and optional lifecycle hooks.

It must not expose movement, combat, AI, UI, heartbeat, networking, inventory, or direct RPG state mutation behavior.

### 14.2 Required Interface Semantics

The interface must provide:

```text
EntityId
Entity kind or source metadata
Initial LivingEntityState provider or state construction data
Registration validation metadata
Optional lifecycle hooks
Optional debug name
```

The interface must not allow:

```text
Direct mutation of LivingEntityState runtime arrays
Direct active effect mutation
Direct condition contributor mutation
Direct snapshot mutation
Direct registry state mutation
```

### 14.3 Suggested Interface Shape

```csharp
public interface ILivingEntityInterface
{
    long EntityId { get; }

    LivingEntityKind EntityKind { get; }

    string DebugName { get; }

    bool TryCreateInitialState(
        long registrationNetworkTick,
        out LivingEntityState initialState,
        out LivingEntityRegistrationError error);

    LivingEntityRegistrationMetadata GetRegistrationMetadata();

    void OnLivingEntityRegistered(
        in LivingEntityRegistrationContext context);

    void OnLivingEntityRemovalRequested(
        in LivingEntityRemovalContext context);

    void OnLivingEntityRemoved(
        in LivingEntityRemovalContext context);
}
```

Lifecycle hooks must not mutate StateEngine state directly.

If hook delivery fails because the object handle is gone, the registry must remain correct. Missing hook delivery is diagnostic, not authoritative simulation state.

### 14.4 Entity Kind

Suggested initial kinds:

```text
Player
Agent
Npc
Enemy
Friendly
Neutral
Hostile
Creature
Scripted
Test
```

`LivingEntityKind` is registry metadata only. It must not replace gameplay tags, faction systems, AI controllers, or targeting filters.

---

## 15 Public API Direction

### 15.1 Primary Runtime Type

```csharp
public sealed class LivingEntityRegistry
{
    public LivingEntityRegistry(
        LivingEntityRegistryConfig config,
        ILivingEntityRegistryAuthority authority,
        ILivingEntityRegistryLogger logger);
}
```

The registry is instance-based.

### 15.2 Registration API

```csharp
public LivingEntityRegistryRequestResult RequestRegister(
    in LivingEntityRegistrationRequest request);
```

`RequestRegister` validates authority and input shape, then queues the request for tick-boundary commit.

It must not mutate StateEngine state.

### 15.3 Removal API

```csharp
public LivingEntityRegistryRequestResult RequestRemove(
    in LivingEntityRemovalRequest request);
```

`RequestRemove` validates authority and queues removal for tick-boundary commit.

It must not delete system-owned data directly.

### 15.4 Commit API

```csharp
public LivingEntityRegistryCommitResult CommitLifecycleChanges(
    long networkTick);
```

Commit rules:

- commit only eligible requests for `networkTick`,
- sort requests deterministically,
- update current records,
- append lifecycle events,
- update ordered present entity list,
- retain compact history,
- return deterministic commit summary.

### 15.5 Query API

```csharp
public bool IsRegistered(long entityId);

public bool IsRemovalRequested(long entityId);

public bool WasPresentAt(
    long entityId,
    long networkTick);

public bool TryGetRecord(
    long entityId,
    out LivingEntityRecordView record);

public ReadOnlySpan<long> GetOrderedPresentEntityIds();
```

The implementation may use a caller-provided buffer instead of returning spans if ownership or lifetime rules require it.

### 15.6 Event API

```csharp
public ulong CurrentLifecycleEventSequence { get; }

public int CopyLifecycleEventsSince(
    ulong lastConsumedSequence,
    Span<LivingEntityLifecycleEvent> destination);
```

Lifecycle event iteration must be deterministic and allocation-free after initialization.

### 15.7 Optional Godot Facade

A Godot facade may exist:

```csharp
public partial class LivingEntityRegistryAutoload : Node
{
    public static LivingEntityRegistry Instance { get; private set; }
}
```

Rules:

- the facade must delegate to an explicit registry instance,
- tests and core simulation must not require the facade,
- the facade must not create a second source of truth,
- the facade must not hide lifecycle commits.

---

## 16 StateEngine Integration

### 16.1 Dependency Direction

`StateEngine` consumes `LivingEntityRegistry`; the registry does not consume `StateEngine`.

Recommended constructor refinement:

```csharp
public sealed class StateEngine
{
    public StateEngine(
        StateEngineConfig config,
        LivingEntityRegistry livingEntityRegistry,
        EffectDefinitionRegistry effectDefinitionRegistry,
        StatRegistry statRegistry,
        IPassiveModifierPipeline passiveModifierPipeline,
        LivingEntityStateTracker stateTracker,
        IDeltaWriter deltaWriter,
        IStateEngineLogger logger);
}
```

### 16.2 Registration Event Handling

When `StateEngine` consumes a `Registered` event:

1. Read the committed registration record.
2. Obtain or build the initial `LivingEntityState`.
3. Validate state shape through stat and condition validation.
4. Allocate or assign StateEngine-owned buffers.
5. Insert the entity into StateEngine processing order.
6. Store the initial state snapshot if required.
7. Mark initial delta state when networking requires it.

### 16.3 Removal Event Handling

When `StateEngine` consumes a `Removed` event:

1. Stop processing the entity for the effective tick.
2. Remove pending incoming effects targeting the entity or reject them deterministically.
3. Remove current active effects owned by the entity state.
4. Clear condition contributor ownership for that entity.
5. Clear dirty state for that entity.
6. Return StateEngine-owned buffers to pools or mark them reusable.
7. Record terminal cleanup metadata for deltas/history when required.
8. Ask `LivingEntityStateTracker` to retain or purge history according to retention rules.

The registry does not perform those steps itself.

### 16.4 StateEngine Register API Compatibility

Earlier documents describe:

```csharp
RegisterLivingEntity(long livingEntityId, LivingEntityState initialState)
```

After introducing `LivingEntityRegistry`, that method should become one of:

```text
Compatibility facade that submits a registry registration request.
Test-only helper that uses a registry-backed path.
Internal StateEngine reaction to a registry Registered event.
```

It should not remain the primary authoritative lifecycle entry point.

---

## 17 Effects Integration

### 17.1 Target Presence

Every incoming `ActiveEffect` must target a registered living entity at the effect's applied tick.

StateEngine must check registry presence:

```text
ActiveEffect.TargetEntityId must be present at AppliedNetworkTick.
If the target is not present, reject the effect deterministically.
```

Recommended rejection reason:

```text
TargetEntityNotRegistered
```

The rejection may be retained in applied-effect history when deterministic replay diagnostics require it.

### 17.2 Removal of Target Entity

When a target entity is removed:

- active effects on that target are cleaned up by StateEngine,
- effect classifications attached to those active effects disappear with the active effects,
- condition contributors for that entity are discarded by StateEngine,
- no registry-owned effect mutation occurs.

Entity lifecycle removal is not the same as gameplay cleanse, dispel, expiration, or normal active-effect removal.

### 17.3 Effect Lifecycle Triggers During Entity Removal

Default rule:

```text
Registry-driven entity removal performs hard system cleanup.
It does not fire gameplay OnRemove, OnExpire, or OnExpireOrRemove child-effect consequences by default.
```

Reason:

```text
The target is no longer present, and lifecycle cleanup must not create new gameplay effects against a removed entity unless a future explicit rule allows it.
```

StateEngine may still record internal cleanup records for diagnostics, replay, or delta consistency.

If future gameplay requires despawn consequences, those should be represented by explicit despawn effects before registry removal becomes effective.

### 17.4 Source and Origin Presence

The registry is authoritative only for living entity presence.

Default effect validation requires target presence.

Source or origin presence is effect-definition or StateEngine policy:

```text
Some effects may remain valid after their source entity is removed.
Some effects may require source presence.
Some sources may be non-living entities such as weapons, items, traps, or environment systems.
```

The registry should not globally invalidate effects only because `SourceId` or `OriginId` no longer maps to a present living entity.

---

## 18 LivingEntityStateTracker Relationship

### 18.1 Separation of Responsibility

`LivingEntityRegistry` owns:

```text
Entity lifecycle records
Current presence set
Lifecycle events
Compact lifecycle history
Registration metadata
Removal metadata
```

`LivingEntityStateTracker` owns:

```text
LivingEntityState snapshots
Applied active-effect history
Rejected effect history when required
Replay metadata
Snapshot pruning
State comparison metadata
```

### 18.2 Removal and History

When an entity is removed:

- the registry marks it absent at the effective tick,
- StateEngine stops current processing,
- `LivingEntityStateTracker` may retain historical snapshots until rollback retention expires,
- effect history may be retained until replay no longer needs it,
- final purge occurs only when both lifecycle and state/effect history retention allow it.

### 18.3 Historical Presence

`LivingEntityStateTracker` may ask:

```text
Was EntityId present at NetworkTick?
When was EntityId removed?
When was EntityId registered?
```

The registry answers those questions from compact lifecycle history.

It does not provide full state frames.

---

## 19 Lifecycle History and Rollback

### 19.1 History Purpose

Lifecycle history is required so rollback/resimulation can distinguish:

```text
Entity was present and should have state.
Entity was not yet registered.
Entity was already removed.
Entity was removed during the replay window.
Incoming effect targeted a missing entity.
```

### 19.2 Retention Window

Lifecycle history retention should align with StateEngine rollback retention.

Default:

```text
LifecycleHistoryRetentionTicks = SnapshotRetentionTicks
SnapshotRetentionTicks = 45
```

The implementation may configure a larger lifecycle retention window.

Recommended:

```text
LifecycleHistoryRetentionTicks >= SnapshotRetentionTicks
```

### 19.3 Retained Data

Lifecycle history should retain compact records:

```text
EntityId
LifecycleEventType
EffectiveNetworkTick
RequestNetworkTick
RegistrationSequence
LifecycleEventSequence
RegistrationSourceKind
RemovalReason
Authority source
Result or rejection reason when configured
```

It must not retain full `LivingEntityState` snapshots.

### 19.4 Purge Rules

An entity lifecycle record may be purged when:

```text
Entity is removed.
Lifecycle history is older than retention.
StateEngine state history no longer needs it.
Applied-effect history no longer needs it.
Delta/reconciliation systems no longer need it.
EntityId reuse policy allows purge.
```

Recommended first implementation:

```text
Do not reuse EntityId values during a session.
Purge removed records only after all retention windows pass.
```

---

## 20 Determinism Requirements

`LivingEntityRegistry` must produce identical lifecycle results for identical inputs.

Requirements:

- Use `long NetworkTick`.
- Commit lifecycle changes only at tick boundaries.
- Assign deterministic request sequence values.
- Sort pending requests by documented stable keys.
- Avoid dictionary iteration order for active entity iteration.
- Avoid object reference identity as an ordering key.
- Avoid thread scheduling as an ordering dependency.
- Reject invalid requests deterministically.
- Keep authority rules fixed during simulation.
- Keep lifecycle history deterministic.
- Do not use wall-clock time in lifecycle decisions.
- Do not use Godot scene tree order as authoritative ordering.

Recommended request ordering:

```text
1. EffectiveNetworkTick
2. RequestSequence
3. EntityId
4. RequestKind
```

Recommended active entity ordering:

```text
1. RegistrationNetworkTick
2. RegistrationSequence
3. EntityId
```

---

## 21 Performance and Allocation Requirements

The target runtime scale matches the StateEngine target scale:

```text
Max living entities active at once: 250
Average active effects per entity: 35
High-load player active effects: 70 or more
Network tick rate: 30 TPS
Target platform: desktop PC
```

Registry operations are lower frequency than StateEngine stat/effect processing, but the registry must still avoid avoidable per-tick allocation.

The registry should:

- preallocate current record capacity from config,
- preallocate pending request capacity from config,
- preallocate lifecycle event buffers from config,
- maintain deterministic ordered entity ID storage,
- compact removed current entries deterministically,
- expose read-only spans or caller-filled buffers where practical,
- avoid LINQ in commit and query hot paths,
- avoid reflection,
- avoid string formatting when diagnostics are disabled,
- avoid Godot collection allocation in authoritative paths,
- bound pending request overflow deterministically.

Recommended config:

```text
MaxRegisteredLivingEntities = 250
MaxPendingLifecycleRequestsPerTick = 128
LifecycleHistoryRetentionTicks = 45
MaxLifecycleEventsRetained = MaxRegisteredLivingEntities * (LifecycleHistoryRetentionTicks + safety margin)
RejectDuplicateEntityId = true
AllowEntityIdReuseWithinSession = false
```

Overflow must not silently corrupt registry state.

Recommended overflow behavior:

```text
Reject excess requests deterministically.
Record rejection metadata when diagnostics are enabled.
Continue simulation with accepted requests.
```

---

## 22 Validation Requirements

Validation must fail fast for invalid registration data before normal simulation accepts the entity.

Registration validation must check:

- `EntityId` is valid and non-reserved,
- `EntityId` is not already registered,
- `EntityId` is not blocked by retained history reuse policy,
- request source is authorized to register,
- `ILivingEntityInterface` is present or a valid registration provider exists,
- initial `LivingEntityState` can be created,
- initial state `EntityId` matches the registration `EntityId`,
- initial state `NetworkTick` matches registration tick or documented initial tick policy,
- stat arrays match `StatRegistry`,
- condition tag set or condition flags are valid for condition registry,
- active effect buffer is empty or valid according to StateEngine import policy,
- dirty bitsets are initialized consistently.

Removal validation must check:

- request source is authorized to request removal,
- `EntityId` is currently registered or removal rejection is recorded,
- requested effective tick is valid for normal runtime mode,
- removal reason is valid,
- duplicate removal requests resolve deterministically.

Startup validation must check:

- config capacities are positive,
- lifecycle retention is compatible with StateEngine retention,
- authority policy exists,
- event buffer capacity can retain required history,
- active entity order policy is configured.

---

## 23 Testing and Acceptance Criteria

### 23.1 Registration Tests

Tests must verify:

- valid player registration commits at the expected tick,
- valid spawned agent registration commits at the expected tick,
- duplicate registration is rejected deterministically,
- unauthorized registration is rejected,
- invalid initial state is rejected,
- active entity order is deterministic.

### 23.2 Removal Tests

Tests must verify:

- authorized removal request commits at the expected tick,
- unreachable player removal is requested by a heartbeat-like system and committed by the registry,
- missing entity removal returns deterministic rejection,
- duplicate removal requests resolve deterministically,
- removal effective at tick `T` makes the entity absent for tick `T`,
- death condition does not unregister an entity by itself.

### 23.3 StateEngine Integration Tests

Tests must verify:

- StateEngine creates state from a registry `Registered` event,
- StateEngine stops processing an entity after a registry `Removed` event,
- active effects targeting removed entities are rejected as `TargetEntityNotRegistered`,
- StateEngine cleans up active effects, condition contributors, dirty state, and current state storage,
- registry does not directly mutate StateEngine state.

### 23.4 History and Rollback Tests

Tests must verify:

- `WasPresentAt(EntityId, tick)` is correct across registration and removal ticks,
- lifecycle history is retained for the configured rollback window,
- StateEngineTracker can use lifecycle history during resimulation,
- purging does not occur before all retention windows expire,
- entity ID reuse is rejected within the configured retained window.

### 23.5 Determinism Tests

Tests must verify:

- identical request inputs produce identical lifecycle event output,
- request ordering is stable,
- active entity iteration is stable,
- same-tick conflicts resolve deterministically,
- no dictionary or scene tree iteration order affects simulation output.

### 23.6 No-Allocation Tests

Tests should verify no avoidable allocation during:

- lifecycle commit with no pending requests,
- lifecycle commit with normal pending requests inside configured capacity,
- active entity iteration,
- lifecycle event polling with caller-provided buffers,
- current presence queries.

---

## 24 Example Flows

### 24.1 Spawned Agent Registration

```text
SpawnSystem creates an agent object implementing ILivingEntityInterface.
SpawnSystem submits RequestRegister with source kind SpawnedAgent.
LivingEntityRegistry validates authority and queues registration.
CommitLifecycleChanges(T) commits the registration.
Registry emits Registered(EntityId, T).
StateEngine consumes the event and creates LivingEntityState.
AI, targeting, UI, and debug systems can now query registry presence.
```

### 24.2 Registered Player

```text
PlayerSessionSystem accepts a player.
Player entity implements ILivingEntityInterface.
PlayerSessionSystem submits RequestRegister with source kind PlayerSession.
Registry commits the registration at tick T.
StateEngine imports initial player LivingEntityState.
HUD and gameplay systems can check IsRegistered(EntityId).
```

### 24.3 Unreachable Player Removal

```text
HeartbeatMonitor detects that a connected player is unreachable.
HeartbeatMonitor does not own StateEngine data.
HeartbeatMonitor submits RequestRemove(EntityId, PlayerUnreachable).
Registry validates removal authority and queues the request.
CommitLifecycleChanges(T) commits Removed(EntityId, T).
StateEngine sees the entity is absent and removes its own state/effects/history references.
LivingEntityStateTracker retains historical snapshots until rollback retention expires.
UI and targeting systems drop their own references after consuming the removal event.
```

### 24.4 Effect Target Removed

```text
Registry commits Removed(TargetEntityId, T).
An incoming ActiveEffect targets TargetEntityId at AppliedNetworkTick T or later.
StateEngine checks registry presence.
Target is not present.
StateEngine rejects the effect with TargetEntityNotRegistered.
Registry does not inspect or mutate the effect.
```

### 24.5 Death Without Removal

```text
StateEngine resolves Health.CurrentValue <= 0.
StateEngine applies Condition.Dead.
LivingEntityRegistry still reports the entity as registered and present.
The entity remains targetable or untargetable according to conditions/effects, not registry lifecycle.
Only an authorized lifecycle system may request removal.
```

---

## 25 Minimal Clarifying Snippets

### 25.1 Registration Request

```csharp
public readonly struct LivingEntityRegistrationRequest
{
    public readonly long EntityId;
    public readonly long RequestedNetworkTick;
    public readonly LivingEntityRegistrationSourceKind SourceKind;
    public readonly ILivingEntityInterface LivingEntity;
    public readonly LivingEntityAuthorityToken Authority;
}
```

### 25.2 Removal Request

```csharp
public readonly struct LivingEntityRemovalRequest
{
    public readonly long EntityId;
    public readonly long RequestedNetworkTick;
    public readonly LivingEntityRemovalReason Reason;
    public readonly LivingEntityAuthorityToken Authority;
    public readonly string DiagnosticMessage;
}
```

### 25.3 Lifecycle Event

```csharp
public readonly struct LivingEntityLifecycleEvent
{
    public readonly ulong EventSequence;
    public readonly long EntityId;
    public readonly long EffectiveNetworkTick;
    public readonly LivingEntityLifecycleEventType EventType;
    public readonly LivingEntityRegistrationSourceKind SourceKind;
    public readonly LivingEntityRemovalReason RemovalReason;
    public readonly LivingEntityLifecycleResult Result;
}
```

### 25.4 StateEngine Tick Integration

```csharp
registry.CommitLifecycleChanges(networkTick);

var eventCount = registry.CopyLifecycleEventsSince(
    stateEngineLastLifecycleEventSequence,
    lifecycleEventBuffer);

stateEngine.ApplyLifecycleEvents(
    lifecycleEventBuffer.Slice(0, eventCount));

stateEngine.ProcessLivingEntities(
    networkTick,
    incomingActiveEffects);
```

### 25.5 Presence Check

```csharp
if (!livingEntityRegistry.WasPresentAt(effect.TargetEntityId, effect.AppliedNetworkTick))
{
    RejectEffect(effect, EffectRejectionReason.TargetEntityNotRegistered);
}
```

---

## Final Contract

`LivingEntityRegistry` is the authoritative, instance-scoped lifecycle and presence registry for entities implementing `ILivingEntityInterface`.

It owns registration records, current presence, removal requests, deterministic lifecycle events, active entity ordering, and compact lifecycle history.

It does not own or mutate StateEngine state, active effects, conditions, snapshots, passive source data, player heartbeat logic, spawning logic, or UI state.

Systems use it to answer whether an entity is present and to coordinate deterministic cleanup. Each system remains responsible for its own data when a lifecycle event says an entity has registered or been removed.
