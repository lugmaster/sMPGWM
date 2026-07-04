version: 6.2.2
# Effects

## 1 Effect Driven Gameplay

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
- condition requests,
- classification metadata,
- blocking rules,
- trigger timing,
- duration and expiration,
- stacking behavior,
- child effects,
- effect-removal requests,
- player visibility.

Definitions are data. Runtime progress, timers, stack counts, accumulators, parent-child runtime links, and deterministic ordering state belong to `ActiveEffect`.

---

## 2 EffectDefinition

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
ConditionAndClassificationRequests
EffectRemovalRequest
ChildEffectDefinitions
```

A valid container definition has:

```yaml
IsEffectContainer: true
ChildEffectDefinitions:
  - ChildEffectDefinitionId: effect.child.example
```

and must not define direct `Target`, direct `ConditionAndClassificationRequests`, or direct `EffectRemovalRequest`.

Each definition is valid only if all referenced child definitions are valid, the child graph is acyclic, and the graph does not exceed the configured maximum child depth.

### 2.1 EffectDefinition Fields

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
  ConditionAndClassificationRequests:
    ConditionsAddRequest: []
    ConditionsRemoveRequest: []
    ClassificationsAddRequest: []
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
| `ConditionAndClassificationRequests` | no | none | Optional condition requests and classification metadata. |
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

### 2.2 EffectDefinition Validity Rules

The validator must reject invalid or deprecated data at startup. Runtime systems should not need to defend against malformed definitions during normal simulation.

General validity rules:

- YAML must parse successfully.
- `Definition.EffectDefinitionId`, `Definition.Name`, and `Definition.Description` are required and non-null.
- A non-container effect must have at least one executable payload: `Target`, `ConditionAndClassificationRequests`, `EffectRemovalRequest`, or `ChildEffectDefinitions`.
- A container must set `IsEffectContainer: true`, must contain at least one child effect, and must omit direct `Target`, `ConditionAndClassificationRequests`, and `EffectRemovalRequest`.
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

- If `ConditionAndClassificationRequests` is present, at least one contained request array must contain at least one value.
- `ConditionsAddRequest` and `ConditionsRemoveRequest` mutate target `LivingEntityState` conditions through the StateEngine.
- `ClassificationsAddRequest` attaches metadata to the `ActiveEffect` only.
- `ClassificationsRemoveRequest` is invalid.

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

### 2.3 Single Target Rule

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

### 2.4 Effect Classification

Effect classifications are metadata attached to active effects. They are not target-state conditions and do not execute logic by themselves.

Classifications are used for:

- blocking against active effect metadata,
- removal request matching,
- debug and inspection identity,
- trigger conditions such as `OnClassificationsPresent`,
- grouping effects as `Poison`, `Buff`, `Debuff`, `Fire`, `Overheating`, and similar authored categories.

Classifications are added through:

```yaml
ConditionAndClassificationRequests:
  ClassificationsAddRequest:
    - Poison
    - Debuff
```

`ClassificationsRemoveRequest` is invalid. Classifications disappear automatically when the corresponding `ActiveEffect` expires or is removed.

Conditions and classifications are deliberately separate:

| Concept | Stored on | Mutates target state | Removed how |
|---|---|---:|---|
| Condition | `LivingEntityState` | yes | By condition rules, condition requests, or contributing effect lifetime. |
| Classification | `ActiveEffect` metadata | no | Automatically when the active effect disappears. |

Condition requests are allowed with all trigger types. Trigger-specific behavior:

| TriggerType | Condition request behavior |
|---|---|
| `Immediate` | Applies transactionally when the effect triggers. |
| `WhileActive` | Contributes while the `ActiveEffect` exists and is removed when the effect expires or is removed. |
| `OverTime` | The effect is active for its lifetime. Condition contribution begins after the effect has ticked at least once and is removed when the effect expires or is removed. |
| `OnExpire` | Applies transactionally when the effect expires naturally. |
| `OnRemoval` | Applies transactionally when the effect is explicitly removed. |

If multiple active effects contribute the same condition, the condition remains present until all contributing effects are gone or until deterministic StateEngine condition rules remove it.

For `WhileActive` effects, classification metadata is present while the `ActiveEffect` exists. For `OverTime` effects, the effect is active during its lifetime; if the implementation distinguishes contribution activation, trigger-gated classification contribution begins after the first due tick.

### 2.5 Effect Target

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

#### 2.5.1 EffectTarget Fields

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `TargetStat` | Stat | yes | The stat to affect. Must use canonical stat spelling. |
| `TargetValueKind` | enum | yes | `CurrentValue` or `CurrentMaxValue`. |
| `Manipulation` | enum | yes | `Raise` or `Reduce`. |
| `Calculation` | enum | yes | `Flat` or `Percentage`. |
| `Value` | float | yes | Numeric amount. Percentage values use decimal fractions. FlowStats use the stat's canonical per-ms unit where applicable. |
| `ApplicationMode` | enum | no | `Instant`, `WhileActive`, or `AccumulateWhileActive`. Inferred if omitted. |

#### 2.5.2 EffectTarget Examples

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

### 2.6 EffectTrigger

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

#### 2.6.1 Trigger Conditions

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

#### 2.6.2 OnStatRaised / OnStatReduced Rule

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

#### 2.6.3 OnStatValueBelow / OnStatValueAbove Rule

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

### 2.7 Effect Duration

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

### 2.8 Blocking

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
  ConditionAndClassificationRequests:
    ConditionsRemoveRequest:
      - Dead
  CanTriggerWhileDead: true
```

#### 2.8.1 BlockPolicy

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

### 2.9 Effect Removal

Effects do not directly remove other effects. They request removal, and the StateEngine validates and applies the request deterministically.

The requester must trigger successfully before its `EffectRemovalRequest` is evaluated.

Passive modifiers are not effects and cannot be removed through `EffectRemovalRequest`.

#### 2.9.1 EffectRemovalRequest

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

- Selector fields are combined with `AND`.
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

#### 2.9.2 EffectRemovalPolicy

`EffectRemovalPolicy` controls whether this active effect is allowed to be removed by another effect's `EffectRemovalRequest`.

Canonical values:

| EffectRemovalPolicy | Meaning |
|---|---|
| `ByAnySource` | Any matching requester may remove this effect. Default. |
| `BySourceId` | Only a requester with the same `SourceId` may remove this effect. |
| `ByOriginId` | Only a requester with the same `OriginId` may remove this effect. |

The old names `AnySource` and misspelled `ByAnSource` are invalid. Use `ByAnySource`.

#### 2.9.3 Removal Validation

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

#### 2.9.4 Removal Ordering

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

#### 2.9.5 Removal Validity

A removal request may remove nothing when:

- no active effect matches the selectors,
- matching active effects have a removal policy that rejects the requester,
- selectors reference stats, conditions, classifications, sources, origins, stack groups, definitions, or runtime IDs that are not present,
- the request targets passive modifiers, which are outside the active effect model.

Failure to remove must be deterministic and must not partially depend on collection iteration order.

---

### 2.10 Visibility

`Visibility` defines whether an effect is shown to the player.

| Visibility | Meaning |
|---|---|
| `Internal` | Hidden from HUD and menus. Default. |
| `Hud` | Shown in the HUD and skill menu. |
| `Menu` | Shown only in the skill menu. |

Visibility is presentation metadata. It must not change simulation behavior.

---

### 2.11 Stacking

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

#### 2.11.1 StackScope

`StackScope` defines which identity fields are used to detect duplicate effects on the same target.

| StackScope | Meaning |
|---|---|
| `ByEffectDefinitionId` | Duplicate identity uses the same `EffectDefinitionId`. Default. |
| `SameSource` | Duplicate identity includes the same `EffectDefinitionId` and `SourceId`. |
| `SameSourceAndOrigin` | Duplicate identity includes the same `EffectDefinitionId`, `SourceId`, and `OriginId`. |
| `ByStackGroupId` | Duplicate identity uses `StackGroupId`; requires `StackGroupId`. |

#### 2.11.2 StackPolicy

`StackPolicy` defines whether duplicate effects are accepted.

| StackPolicy | Meaning |
|---|---|
| `Stack` | Accept the duplicate according to `StackBehavior`. Default. |
| `Replace` | Replace an existing duplicate with the new effect. |
| `Discard` | Discard the new duplicate. |

`Discard` is not the same as rejecting duplicate `EffectRuntimeId`; `EffectRuntimeId` uniqueness is validated before stacking.

#### 2.11.3 StackBehavior

The canonical field name is `StackBehavior`.

| StackBehavior | Meaning | Recommended representation |
|---|---|---|
| `IndependentStacks` | Each accepted stack is a separate `ActiveEffect`. Default. | Separate entry per duplicate. |
| `RefreshDuration` | Value does not stack; duration is refreshed. | One `ActiveEffect` entry. |
| `AddStackRefreshDuration` | `StackCount` increases and duration is refreshed. | One `ActiveEffect` entry with `StackCount`. |
| `AddStackNoRefresh` | `StackCount` increases and existing duration remains unchanged. | One `ActiveEffect` entry with `StackCount`. |

When a value scales by stack count, the scaling must be deterministic and based on `StackCount`, not on duplicated mutation of definition data.

#### 2.11.4 MaxStackOverflowPolicy

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

`RefreshOldest` is not canonical in 6.2.1 and must not be used.

#### 2.11.5 StackGroupId

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

#### 2.11.6 StackIdentityKey

`StackIdentityKey` is the deterministic runtime key used to detect duplicates. It is derived from the target and the selected `StackScope`.

Recommended construction:

| StackScope | StackIdentityKey contents |
|---|---|
| `ByEffectDefinitionId` | `TargetEntityId + EffectDefinitionId` |
| `SameSource` | `TargetEntityId + EffectDefinitionId + SourceId` |
| `SameSourceAndOrigin` | `TargetEntityId + EffectDefinitionId + SourceId + OriginId` |
| `ByStackGroupId` | `TargetEntityId + StackGroupId` |

The exact binary representation is implementation-specific, but it must be deterministic, stable during rollback/resimulation, and independent of collection order.

#### 2.11.7 Stacking Examples

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

### 2.12 ChildEffects

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
  ConditionAndClassificationRequests:
    ConditionsAddRequest:
      - Overheated
    ClassificationsAddRequest:
      - OverheatingCooldown
      - Debuff
```

Result:

```text
AttackSpeed is increased while Overheating is active.
When Overheating expires naturally, OverheatingCooldown starts.
OverheatingCooldown applies Overheated while active.
```

#### 2.12.1 Child Effect Storage

Child effects are stored as separate `ActiveEffect` entries. They are not nested runtime collections.

Each child stores:

```text
ParentEffectRuntimeId
ChildDepth
```

Child effects are queued in the same deterministic tick buffer as normal effects. This keeps iteration fast, avoids recursive traversal, avoids child collection allocation, and lets child processing use the same deterministic queue rules as normal effects.

---

## 3 ActiveEffect

An `ActiveEffect` is runtime effect state. It references immutable `EffectDefinition` data by ID and stores runtime-only execution state.

It should be implemented as a compact pooled struct or equivalent data-oriented runtime record.

The `ActiveEffect` must not copy mutable definition data. It should store IDs, counters, ticks, stack state, accumulated runtime modifiers, and parent-child runtime links.

### 3.1 ActiveEffect Fields

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
    public RuntimeFlags RuntimeFlags;
}
```

The implementation may store either accumulated values or enough deterministic counters to recompute them during rollback/resimulation.

### 3.2 EffectRuntimeId

`EffectRuntimeId` is unique to each `ActiveEffect`. Duplicate runtime IDs are invalid and must be rejected before stacking logic.

This prevents double execution of the same runtime effect input.

`EffectRuntimeId` is also used as a deterministic tie-breaker for removal and overflow ordering.

### 3.3 EffectDefinitionId

Each `ActiveEffect` references one immutable `EffectDefinition` through `EffectDefinitionId`.

The `EffectDefinitionRegistry` provides fast lookup for the static configuration needed by the StateEngine when processing an active effect.

`EffectDefinitionId` participates in default duplicate detection through `StackScope: ByEffectDefinitionId`.

---

## 4 Effect Sources

Effect source information is used for causality, stacking, removal, analytics, debugging, and deterministic replay.

The canonical 6.2.1 runtime schema requires:

```text
SourceId
OriginId
TargetEntityId
AppliedNetworkTick
```

`SourceType` can be kept as external provenance metadata or derived from the `SourceId` namespace. It is not required by the canonical 6.2.1 `ActiveEffect` runtime schema unless the implementation intentionally extends it.

### 4.1 SourceType

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

### 4.2 SourceId

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

### 4.3 OriginId

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

### 4.4 TargetEntityId

`TargetEntityId` identifies the entity to which the effect should be applied.

The StateEngine does not perform target acquisition. It assumes that each effect input was produced by an upstream gameplay system that already performed targeting, hit validation, and gameplay authorization.

The StateEngine still validates whether the effect can execute against the current target state through blocking, trigger conditions, duration, stacking, and removal rules.

### 4.5 NetworkTick on ActiveEffect

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

### 4.6 ActiveEffect Storage

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
