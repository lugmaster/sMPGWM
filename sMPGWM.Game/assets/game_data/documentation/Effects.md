# Effects

## 1. Scope and Source Precedence

This document describes the canonical `ActiveEffect` model for the FLU RPG State Engine.

Source precedence for this refinement was:

1. `stats.md` version 6.2.
2. The provided updated Effect schema and example definitions.
3. Explicit clarification decisions from the effect-refinement questionnaire.
4. PRD 6.1 as context only.

The StateEngine is effect-driven: gameplay systems create `ActiveEffect` inputs, and only the StateEngine mutates `LivingEntityState` stats, conditions, active effects, and deltas.

Passives are not effects. Passive stat sources from items, talents, level-ups, ship configuration, or permanent upgrade state are processed by a separate passive modifier pipeline and are not represented by `ActiveEffect` or `EffectDefinition` entries.

---

## 2. EffectDefinition

An `EffectDefinition` is immutable static data. Runtime state belongs to `ActiveEffect`.

A valid effect definition must contain `Definition` and at least one executable payload:

- `Target`
- `ConditionAndClassificationRequests`
- `EffectRemovalRequest`
- `ChildEffectDefinitions`

A valid container has:

- `IsEffectContainer: true`,
- no direct `Target`,
- no direct `ConditionAndClassificationRequests`,
- no direct `EffectRemovalRequest`,
- at least one child effect.

Each definition is validated at startup. A definition is valid only if all referenced child effects are valid and the child graph is acyclic.

---

## 3. Active Effects and Passives

Effects in this document are active runtime effects only.

Removed concepts:

```text
EffectType: Passive
DurationType: Permanent
TargetValueKind: Current
TargetValueKind: Max
TriggerType: OverTimeConstant
```

Canonical replacements:

```text
Passive effects -> separate passive modifier pipeline
Permanent active effect -> DurationType: UntilRemoved
Current -> CurrentValue
Max -> CurrentMaxValue when targeting pool capacity
OverTimeConstant -> WhileActive
```

An active effect may last until explicitly removed, but this is not passive behavior. Such effects use:

```yaml
Duration:
  DurationType: UntilRemoved
```

---

## 4. Stat Model Used by Effects

Effects use the stat value layers defined by `stats.md`:

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

Active effects must not target:

```text
BaseValue
EffectiveMaxValue
```

`EffectiveMaxValue` is produced only by the passive modifier pipeline.

---

## 5. Stat Categories

Effects validate their targets against stat categories.

### 5.1 PoolStat

Pool stats have current amount and runtime capacity.

```text
Health
Shield
EnergyStorage
OxygenStorage
FoodStorage
WaterStorage
```

Valid effect target combinations:

| TargetValueKind | Meaning |
|---|---|
| `CurrentValue` | Transactional pool change such as damage, heal, drain, restore, cost, production application, or consumption application. |
| `CurrentMaxValue` | Runtime capacity modifier such as temporary max Health buff, Shield capacity debuff, or EnergyStorage capacity buff. |

`CurrentValue` is clamped after every individual `CurrentValue` stat modification record.

`CurrentMaxValue` is resolved from all active capacity modifiers and clamped only after full capacity resolution. Intermediate capacity values may temporarily fall outside valid bounds during resolution because early clamping would change the final stat-modification outcome.

After final capacity resolution:

```text
CurrentMaxValue = clamp(CurrentMaxValue, 0, maxFloat)
CurrentValue = clamp(CurrentValue, 0, CurrentMaxValue)
```

### 5.2 FlowStat

Flow stats describe rates such as production, consumption, and regeneration.

```text
HealthRegeneration
ShieldRegeneration
EnergyProduction
EnergyConsumption
OxygenProduction
OxygenConsumption
FoodProduction
FoodConsumption
WaterProduction
WaterConsumption
```

Flow stats are authored as values per millisecond:

```text
RegenerationValuePerMs
```

The target field remains named `Value`. For FlowStats, `Value` represents a rate contribution in the stat's canonical value unit.

FlowStats may be actively modified only as:

```yaml
Trigger:
  TriggerType: WhileActive
Target:
  TargetValueKind: CurrentValue
  ApplicationMode: WhileActive
```

### 5.3 AttributeStat

Attribute stats describe effective combat, movement, and system values.

Examples:

```text
Armor
Speed
Acceleration
AfterburnerSpeed
AfterburnerAcceleration
AttackSpeed
CriticalHitChance
CriticalHitBonusDamage
BonusCriticalHitChance
CooldownReduction
LifeLeech
RetaliationDamage
ShieldRechargeDuration
BonusDamage<DamageKind>
Resistance<DamageKind>
```

AttributeStats may be actively modified only as:

```yaml
Trigger:
  TriggerType: WhileActive
Target:
  TargetValueKind: CurrentValue
  ApplicationMode: WhileActive
```

---

## 6. Effect Target

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

Fields:

| Field | Meaning |
|---|---|
| `TargetStat` | The stat to affect. |
| `TargetValueKind` | `CurrentValue` or `CurrentMaxValue`. |
| `Manipulation` | `Raise` or `Reduce`. |
| `Calculation` | `Flat` or `Percentage`. |
| `Value` | Numeric amount. For FlowStats this is a value-per-ms rate contribution where applicable. |
| `ApplicationMode` | Optional. Determines whether the target is applied instantly, while active, or accumulated while active. |

`ApplicationMode` is optional. If omitted, the validator/runtime applies deterministic defaults based on `TriggerType`, stat category, and `TargetValueKind`.

---

## 7. ApplicationMode

`ApplicationMode` controls how a stat target contributes to the state.

| ApplicationMode | Meaning |
|---|---|
| `Instant` | Creates a transactional stat modification record when the effect triggers or is due. |
| `WhileActive` | Contributes a recalculated active modifier while the ActiveEffect exists. The contribution disappears when the effect expires or is removed. |
| `AccumulateWhileActive` | Each due tick increases an accumulated active modifier stored in the ActiveEffect runtime state. The accumulated contribution disappears when the effect expires or is removed. |

### 7.1 Default ApplicationMode Rules

If `ApplicationMode` is omitted, use these defaults:

| TriggerType | Stat category | TargetValueKind | Default ApplicationMode |
|---|---|---|---|
| `Immediate` | PoolStat | `CurrentValue` | `Instant` |
| `OverTime` | PoolStat | `CurrentValue` | `Instant` per due tick |
| `OnExpire` | PoolStat | `CurrentValue` | `Instant` |
| `OnRemoval` | PoolStat | `CurrentValue` | `Instant` |
| `WhileActive` | PoolStat | `CurrentMaxValue` | `WhileActive` |
| `OverTime` | PoolStat | `CurrentMaxValue` | `AccumulateWhileActive` |
| `WhileActive` | AttributeStat | `CurrentValue` | `WhileActive` |
| `WhileActive` | FlowStat | `CurrentValue` | `WhileActive` |

Explicit `ApplicationMode` is allowed in authored definitions as long as it is valid for the chosen trigger/stat/value combination.

### 7.2 Valid Stat Target Combinations

| TriggerType | Valid stat target combination |
|---|---|
| `Immediate` | `PoolStat.CurrentValue` with `ApplicationMode: Instant` only. |
| `OverTime` | `PoolStat.CurrentValue` with `ApplicationMode: Instant`, or `PoolStat.CurrentMaxValue` with `ApplicationMode: AccumulateWhileActive`. |
| `WhileActive` | `PoolStat.CurrentMaxValue`, `AttributeStat.CurrentValue`, or `FlowStat.CurrentValue` with `ApplicationMode: WhileActive`. |
| `OnExpire` | `PoolStat.CurrentValue` with `ApplicationMode: Instant` only. |
| `OnRemoval` | `PoolStat.CurrentValue` with `ApplicationMode: Instant` only. |

Invalid combinations include:

```text
WhileActive + PoolStat.CurrentValue
Immediate + AttributeStat.CurrentValue
Immediate + FlowStat.CurrentValue
Immediate + PoolStat.CurrentMaxValue
OnExpire + PoolStat.CurrentMaxValue
OnRemoval + PoolStat.CurrentMaxValue
Any AttributeStat/FlowStat + CurrentMaxValue
```

### 7.3 AccumulateWhileActive

`AccumulateWhileActive` is used for effects that worsen or grow over time, but whose accumulated contribution must still disappear when the effect ends.

Example:

```yaml
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
  Calculation: Flat
  Value: 10.0
  ApplicationMode: AccumulateWhileActive
```

Result:

```text
Tick 1: accumulated active capacity modifier = -10
Tick 2: accumulated active capacity modifier = -20
Tick 3: accumulated active capacity modifier = -30
Effect removed: accumulated contribution disappears
```

The accumulated contribution is runtime state on `ActiveEffect`, not mutable data on `EffectDefinition`.

Recommended runtime fields:

```text
AppliedTickCount
AccumulatedFlatModifier
AccumulatedPercentageModifier
```

The implementation may store either accumulated values or enough deterministic counters to recompute them during rollback/resimulation.

No dedicated accumulation cap field exists in the first implementation. Caps are controlled by `Duration`, `TickIntervalMs`, `Value`, `MaxStacks`, and final stat clamping.

For `Calculation: Percentage`, each due tick accumulates another additive percentage contribution based on the passive baseline. For `Manipulation: Reduce`, accumulated percentage reduction is capped at `-100%`. For `Manipulation: Raise`, no special schema cap exists in the first implementation.

### 7.4 Stacking with AccumulateWhileActive

`AccumulateWhileActive` follows normal stacking first.

If duplicate handling discards the new effect, no new accumulator is created.

If duplicate handling refreshes the existing effect, the accumulated value is kept and only duration is refreshed.

If duplicate handling replaces the existing effect, the old ActiveEffect and its accumulator are removed and a new ActiveEffect starts with a fresh accumulator.

If `StackBehavior: IndependentStacks` is used, each accepted duplicate ActiveEffect has its own independent accumulator. Later stacks must not retroactively change earlier accumulated values.

---

## 8. Percentage Calculation Rules

Percentage values are decimal fractions:

```text
0.05 = 5%
0.50 = 50%
1.00 = 100%
```

Percentage modifiers are additive, not multiplicative.

### 8.1 Active Modifier Percentage Basis

For active stat modifiers:

| Target | Percentage basis |
|---|---|
| `PoolStat.CurrentMaxValue` | `EffectiveMaxValue` |
| `AttributeStat.CurrentValue` | passive/equipment-adjusted effective baseline |
| `FlowStat.CurrentValue` | passive/equipment-adjusted effective baseline |

Example:

```text
Armor passive baseline: 150
WhileActive armor debuff: Reduce Percentage 0.20
Effective Armor.CurrentValue: 120
```

### 8.2 Instant Pool CurrentValue Percentage Basis

For `ApplicationMode: Instant` targeting `PoolStat.CurrentValue`, percentage values use the pool's `CurrentMaxValue` at execution time.

Example:

```text
Health.CurrentMaxValue: 200
Instant heal: Raise Percentage 0.20
Heal amount: 40
```

---

## 9. Pool Capacity Compensation

Pool capacity compensation belongs to stat resolution, but effects must be authored with this behavior in mind.

Order for PoolStats:

1. Resolve `EffectiveMaxValue` from base and passives.
2. Resolve active `CurrentMaxValue` modifiers.
3. Clamp final `CurrentMaxValue` to `[0, maxFloat]`.
4. Apply positive capacity compensation only if `CurrentMaxValue` is raised above `EffectiveMaxValue`.
5. Apply `CurrentValue` modification records in deterministic order.
6. Clamp `CurrentValue` after each `CurrentValue` modification record.
7. Final clamp `CurrentValue` to `[0, CurrentMaxValue]`.

Positive capacity compensation is based only on the positive capacity delta gained this tick. It is not repeatedly applied every tick while a buff remains active.

Removing a capacity debuff that returns `CurrentMaxValue` toward `EffectiveMaxValue` does not raise `CurrentValue`.

Applying a capacity buff that raises `CurrentMaxValue` above `EffectiveMaxValue` raises `CurrentValue` by the positive gained capacity delta, capped at the new `CurrentMaxValue`.

---

## 10. Trigger

`Trigger` defines when an effect executes or contributes to active state.

| TriggerType | Meaning |
|---|---|
| `Immediate` | Executes when processed. |
| `OverTime` | Executes when due according to `TickIntervalMs` while active. Requires `TickIntervalMs > 100` and `DurationType: Temporary` or `UntilRemoved`. |
| `WhileActive` | Contributes active modifiers, conditions, or classifications while the ActiveEffect exists. It does not tick and ignores `TickIntervalMs`. |
| `OnExpire` | Executes once when the effect expires naturally by duration. Requires `DurationType: Temporary`. |
| `OnRemoval` | Executes once when the effect is explicitly removed. Requires `DurationType: Temporary` or `UntilRemoved`. |

### 10.1 Immediate

`Immediate` effects execute once when accepted and then are consumed unless they create child effects or lifecycle behavior requiring retention.

Valid stat target:

```text
PoolStat.CurrentValue + ApplicationMode: Instant
```

### 10.2 OverTime

`OverTime` effects are active for their lifetime, but they only create stat modification records when due in the current simulation step.

Valid stat targets:

```text
PoolStat.CurrentValue + ApplicationMode: Instant
PoolStat.CurrentMaxValue + ApplicationMode: AccumulateWhileActive
```

### 10.3 WhileActive

`WhileActive` replaces the old `OverTimeConstant` concept.

It contributes while the `ActiveEffect` exists. It does not execute repeatedly every engine tick.

Use it for:

- temporary Armor debuff while poisoned,
- temporary AttackSpeed buff while overheating,
- temporary Shield capacity buff,
- temporary FlowStat rate modifier,
- condition carrier such as `Overheated`.

`WhileActive` must not target `CurrentValue` of a PoolStat.

### 10.4 OnExpire and OnRemoval

`OnExpire` and `OnRemoval` may target stats only as transactional `Instant` changes to `PoolStat.CurrentValue`.

Examples:

- delayed explosion damage when a bomb effect expires,
- heal when a temporary effect is removed,
- energy refund when a channel is interrupted.

---

## 11. TriggerConditions

Trigger conditions are optional additional gates.

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

`OnStatReduced` and `OnStatRaised` trigger from validated stat modification records, not from the final consolidated outcome.

`OnStatValueBelow` and `OnStatValueAbove` trigger from final resolved stat values after all modifications are applied.

Condition/classification trigger checks are evaluated when the effect is in turn. They do not look ahead to effects that execute later in the same turn.

---

## 12. Duration

| DurationType | Meaning |
|---|---|
| `Once` | Executes once, then is consumed. |
| `Temporary` | Remains active for `DurationMS`, then expires. |
| `UntilRemoved` | Remains active until explicitly removed. This is not passive behavior. |

Rules:

- `Temporary` requires `DurationMS > 100`.
- `Once` and `UntilRemoved` should omit `DurationMS`.
- Runtime duration is converted from milliseconds to `NetworkTick` values.
- Runtime simulation must not use wall-clock duration values.
- Temporary effects expire when `CurrentNetworkTick >= ExpiresNetworkTick`.

---

## 13. Conditions and Classifications

### 13.1 Conditions

Conditions are target `LivingEntityState` flags. Effects may request that conditions are added or removed.

```yaml
ConditionAndClassificationRequests:
  ConditionsAddRequest: [ Poisoned ]
```

```yaml
ConditionAndClassificationRequests:
  ConditionsRemoveRequest: [ Dead ]
```

Condition requests are allowed with all trigger types.

Trigger-specific meaning:

| TriggerType | Condition request behavior |
|---|---|
| `Immediate` | Applies transactionally when the effect triggers. |
| `WhileActive` | Contributes while the ActiveEffect exists. Contribution is removed when the effect expires or is removed. |
| `OverTime` | The effect is active for its lifetime. Condition contribution begins after the effect has ticked at least once and is removed when the effect expires or is removed. |
| `OnExpire` | Applies transactionally when the effect expires naturally. |
| `OnRemoval` | Applies transactionally when the effect is explicitly removed. |

If multiple active effects contribute the same condition, the condition remains present until all contributing effects are gone or until deterministic StateEngine rules resolve otherwise.

### 13.2 Classifications

Classifications are metadata attached to `ActiveEffect`, not target-state flags.

```yaml
ConditionAndClassificationRequests:
  ClassificationsAddRequest: [ Poison, Debuff ]
```

Classifications are used for:

- blocking against active effect metadata,
- removal request matching,
- debug/inspection identity,
- classification trigger conditions.

`ClassificationsRemoveRequest` is invalid. Classifications disappear automatically when the ActiveEffect expires or is removed.

For `OverTime`, classifications are considered active-effect metadata during the effect lifetime. If the implementation distinguishes contribution activation, any trigger-gated classification contribution begins after the effect has ticked at least once.

---

## 14. Blocking

Blocking is evaluated before trigger and trigger conditions.

```yaml
Blocking:
  BlockPolicy: RemoveOnFail
  BlockedByCondition: [ Invulnerable ]
  BlockedByClassification: [ CannotBeHealed ]
```

| BlockPolicy | Meaning |
|---|---|
| `RemoveOnFail` | The effect is consumed/removed when blocked. Default. |
| `Retry` | The effect remains active and retries when due, until it succeeds, expires, or is removed. |

`Dead` blocks effects by default. Effects that must execute while dead need:

```yaml
CanTriggerWhileDead: true
```

---

## 15. EffectRemovalRequest

Effects do not directly remove other effects. They request removal and the StateEngine validates and applies it deterministically.

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

Rules:

- Selector fields are combined with AND.
- Values inside one selector list are combined with OR.
- If `EffectRemovalBehaviour` is omitted, all matching active effects are requested for removal.
- Since passives are not effects, passive modifiers cannot be removed through `EffectRemovalRequest`.

Example:

```yaml
EffectRemovalRequest:
  Classifications: [ Poison ]
```

### 15.1 EffectRemovalPolicy

Canonical values:

```text
ByAnySource
BySourceId
ByOriginId
```

Default:

```text
ByAnySource
```

### 15.2 EffectRemovalBehaviour

```yaml
EffectRemovalBehaviour:
  MaxRemoveCount: 3
  RemoveOldestFirst: true
```

Ordering is deterministic:

1. `RemoveOldestFirst`,
2. `AppliedNetworkTick`,
3. `InsertionSequence`,
4. `EffectRuntimeId`.

---

## 16. Stacking

Canonical fields:

```yaml
Stacking:
  StackGroupId: 123
  StackScope: ByStackGroupId
  StackPolicy: Stack
  StackBehavior: AddStackRefreshDuration
  MaxStacks: 5
  MaxStackOverflowPolicy: ReplaceOldest
```

### 16.1 StackScope

| StackScope | Meaning |
|---|---|
| `ByEffectDefinitionId` | Duplicate identity is the same `EffectDefinitionId`. Default. |
| `SameSource` | Duplicate identity includes the same `SourceId`. |
| `SameSourceAndOrigin` | Duplicate identity includes the same `SourceId` and `OriginId`. |
| `ByStackGroupId` | Duplicate identity uses `StackGroupId`; requires `StackGroupId`. |

### 16.2 StackPolicy

| StackPolicy | Meaning |
|---|---|
| `Stack` | Accept duplicate according to `StackBehavior`. |
| `Replace` | Replace an existing duplicate with the new effect. |
| `Discard` | Discard the new duplicate. |

### 16.3 StackBehavior

| StackBehavior | Meaning |
|---|---|
| `IndependentStacks` | Each stack is a separate `ActiveEffect`. Default. |
| `RefreshDuration` | Value does not stack; duration is refreshed. |
| `AddStackRefreshDuration` | Stack count increases and duration is refreshed. |
| `AddStackNoRefresh` | Stack count increases and duration is not refreshed. |

### 16.4 MaxStacks

`MaxStacks` is the only canonical stack limit field.

Removed/deprecated fields:

```text
LimitStacks
MaxStacksToOverflow
```

When `MaxStacks` is reached, use `MaxStackOverflowPolicy`.

Canonical overflow values:

```text
ReplaceOldest
RejectNewest
```

---

## 17. Child Effects

Child effects are stored as separate `ActiveEffect` entries. They are not nested runtime collections.

Canonical field:

```yaml
ChildEffectDefinitions:
  - ChildEffectDefinitionId: effect.buff.weapon.overheating.cooldown
    ChildEffectTrigger: OnParentExpired
```

Supported child triggers:

| ChildEffectTrigger | Meaning |
|---|---|
| `BoundToParent` | Default. Child is created when parent triggers and expires/is removed with the parent. |
| `OnParentExpired` | Child is created when parent naturally expires. Child is then independent. |
| `OnParentRemoved` | Child is created when parent is explicitly removed. Child is then independent. |
| `OnParentExpiredOrRemoved` | Child is created when parent either expires or is removed. Child is then independent. |

Child effects are processed in deterministic list order.

---

## 18. Examples

### 18.1 Heal

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

Result: raises current Health once and clamps Health after the modification record.

### 18.2 Corroding Poison

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

Result: reduces effective Armor by 5% while active. It does not reduce Armor again every tick. When the effect ends, its contribution is removed and Armor is recalculated.

### 18.3 Max Health Rot

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
    Calculation: Flat
    Value: 10.0
    ApplicationMode: AccumulateWhileActive
```

Result: each due tick adds another `-10` active Health capacity contribution. When the effect expires or is removed, the accumulated contribution disappears and Health capacity snaps back according to remaining modifiers.

### 18.4 Overheating

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
    Value: 0.50
    ApplicationMode: WhileActive
  ChildEffectDefinitions:
    - ChildEffectDefinitionId: effect.buff.weapon.overheating.cooldown
      ChildEffectTrigger: OnParentExpired
```

Result: AttackSpeed is increased while the buff is active. When the buff expires naturally, the cooldown child starts.

---

## 19. Validation Checklist

At startup, the `EffectValidator` must check:

- YAML parses successfully.
- Required fields are present.
- Enum values are canonical.
- `EffectType` is not used.
- `DurationType: Permanent` is not used; use `UntilRemoved` instead.
- `TriggerType: OverTimeConstant` is not used; use `WhileActive` instead.
- `TargetValueKind: Current` and `TargetValueKind: Max` are not used; use `CurrentValue` and `CurrentMaxValue`.
- `BlockPolicy` uses `RemoveOnFail` or `Retry`.
- `EffectRemovalPolicy` uses `ByAnySource`, `BySourceId`, or `ByOriginId`.
- `Calculation: Percentage` uses decimal fractions.
- `TriggerType: OverTime` has `TickIntervalMs > 100`.
- `TriggerType != OverTime` omits or ignores `TickIntervalMs`.
- `DurationType: Temporary` has `DurationMS > 100`.
- `DurationType: Once` and `DurationType: UntilRemoved` omit `DurationMS` in canonical definitions.
- `WhileActive` does not target `PoolStat.CurrentValue`.
- `CurrentMaxValue` is used only for PoolStats.
- AttributeStat and FlowStat active modifiers use `WhileActive + CurrentValue + ApplicationMode: WhileActive`.
- `Immediate`, `OnExpire`, and `OnRemoval` stat targets use `PoolStat.CurrentValue + ApplicationMode: Instant`.
- `OverTime` stat targets use either `PoolStat.CurrentValue + ApplicationMode: Instant` or `PoolStat.CurrentMaxValue + ApplicationMode: AccumulateWhileActive`.
- Explicit `ApplicationMode`, when present, is valid for the selected trigger/stat/value combination.
- `AccumulateWhileActive` runtime state is stored on `ActiveEffect`, not on `EffectDefinition`.
- `AccumulateWhileActive` percentage reductions cap at `-100%` contribution.
- FlowStat target values are authored in the stat's canonical per-ms unit where applicable.
- `ClassificationsRemoveRequest` is not used.
- `StackScope: ByStackGroupId` has `StackGroupId`.
- `MaxStacks`, when present, is greater than zero.
- `StackBehavior: IndependentStacks` with `AccumulateWhileActive` creates independent accumulators.
- Refreshing an `AccumulateWhileActive` effect preserves accumulated value.
- Replacing an `AccumulateWhileActive` effect removes the old accumulator and starts fresh.
- Child effect IDs exist.
- Child effect graph has no cycles.
- Child effect graph does not exceed the configured maximum depth.
- Stat names use canonical spelling from `stats.md`.
