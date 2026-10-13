# EffectDefinition Field Reference

> **Scope:** Wiki reference for authored `EffectDefinition` data, derived from the provided StateEngine sources. The canonical core field set is the `EffectSchemas.yml` **6.3.0** schema used by `EffectDefinitions.yml` **6.3.0**. `CombatResolver.md` **1.0.0** adds a newer damage-authoring extension (`EffectKind` / `Damage`) that is documented separately and clearly marked because it has not been merged into the 6.3.0 YAML schema/file.

## Source basis

- `EffectDefinitions.yml` — version 6.3.0; 18 concrete authored effect definitions used for usage/examples.
- `EffectSchemas.yml` — schema version 6.3.0; canonical core schema, enum sets, defaults, validation rules, reusable trigger/condition/child schemas.
- `Effects.md` — effect semantics, target rules, lifecycle, blocking, removal, stacking, child effects, and examples.
- `FLU_RPG_StateEngine_PRD.md` / `StateEngine.md` — runtime ownership, stat-layer, deterministic timing, and validation context.
- `Stats.md` — stat categories/value-layer semantics.
- `TagSystem.md` — canonical gameplay-tag assignment/query rules.
- `CombatResolver.md` — later v1 damage-extension contract.

`Version` at the root of `EffectDefinitions.yml` is **file metadata**, not an `EffectDefinition` field, and is therefore excluded from the field list.

## Core validity summary

- `Definition` is the only always-required top-level schema field. `Definition.EffectDefinitionId`, `Definition.Name`, and `Definition.Description` are required and non-null.
- A non-container effect must have at least one gameplay payload: `Target`, `ConditionRequests`, `EffectClassifications`, `EffectRemovalRequest`, or `ChildEffectDefinitions`.
- A container (`IsEffectContainer: true`) must have at least one child and must omit direct `Target`, `ConditionRequests`, `EffectClassifications`, and `EffectRemovalRequest` payloads.
- Active effects may target at most one stat. Mutable runtime state (timers, runtime IDs, accumulators, insertion order, runtime stack counters, etc.) belongs to `ActiveEffect`, not `EffectDefinition`.
- Canonical condition/classification direct assignments use fully qualified gameplay tags. Direct assignment fields use assignable tags; selector/query fields may use parent/category tags and match expanded masks.
- Runtime timing uses deterministic `NetworkTick` values. Authored milliseconds are converted before runtime simulation.

## Field index — alphabetical by full YAML path

| Field path | Type | Required | Default | Present in `EffectDefinitions.yml` 6.3.0 |
|---|---|---|---|---:|
| `Blocking` | `object` | no | none | 15/18 |
| `Blocking.BlockedByClassification` | `array<EffectClassification>` | no | none | 4/18 |
| `Blocking.BlockedByCondition` | `array<Condition>` | no | none | 9/18 |
| `Blocking.BlockPolicy` | `enum (BlockPolicy)` | no | `RemoveOnFail` | 15/18 |
| `CanTriggerWhileDead` | `bool` | no | `false` | 7/18 |
| `ChildEffectDefinitions` | `array<ChildEffectDefinition>` | no | none | 1/18 |
| `ChildEffectDefinitions[].ChildEffectDefinitionId` | `EffectDefinitionId` | yes | none | 1/18 |
| `ChildEffectDefinitions[].ChildEffectTrigger` | `enum (ChildEffectTrigger)` | no | `BoundToParent` | 1/18 |
| `ConditionRequests` | `array<ConditionRequest>` | no | `[]` | 8/18 |
| `ConditionRequests[].ConditionsAddRequest` | `array<Condition>` | no | none | 7/18 |
| `ConditionRequests[].ConditionsRemoveRequest` | `array<Condition>` | no | none | 8/18 |
| `ConditionRequests[].RemoveMode` | `enum (ConditionRemoveMode)` | no | inferred by Timing | 8/18 |
| `ConditionRequests[].Timing` | `enum (ConditionLifecycleTiming)` | yes | none | 15/18 |
| `Definition` | `object` | yes | none | 18/18 |
| `Definition.Description` | `string` | yes | none | 18/18 |
| `Definition.EffectDefinitionId` | `string` | yes | none | 18/18 |
| `Definition.Name` | `string` | yes | none | 18/18 |
| `Duration` | `object` | no | semantic default: DurationType: Once | 18/18 |
| `Duration.DurationMS` | `float` | no | `0.0` | 11/18 |
| `Duration.DurationType` | `enum (DurationType)` | yes | `Once` | 18/18 |
| `EffectClassifications` | `array<EffectClassification>` | no | `[]` | 12/18 |
| `EffectProcessing` | `enum (EffectProcessing)` | no | `Fast` | 7/18 |
| `EffectRemovalPolicy` | `enum (EffectRemovalPolicy)` | no | `ByAnySource` | 11/18 |
| `EffectRemovalRequest` | `object` | no | none | 2/18 |
| `EffectRemovalRequest.Classifications` | `array<EffectClassification>` | no | none | 1/18 |
| `EffectRemovalRequest.Conditions` | `array<Condition>` | no | none | 0/18 |
| `EffectRemovalRequest.EffectDefinitionIds` | `array<EffectDefinitionId>` | no | none | 0/18 |
| `EffectRemovalRequest.EffectRemovalBehaviour` | `object` | no | omitted => remove all matching candidates | 1/18 |
| `EffectRemovalRequest.EffectRemovalBehaviour.MaxRemoveCount` | `int` | no | `1` | 1/18 |
| `EffectRemovalRequest.EffectRemovalBehaviour.RemoveOldestFirst` | `bool` | no | `true` | 1/18 |
| `EffectRemovalRequest.EffectRuntimeIds` | `array<EffectRuntimeId>` | no | none | 0/18 |
| `EffectRemovalRequest.EffectsTargetingStats` | `array<Stat>` | no | none | 1/18 |
| `EffectRemovalRequest.OriginIds` | `array<OriginId>` | no | none | 0/18 |
| `EffectRemovalRequest.SourceIds` | `array<SourceId>` | no | none | 0/18 |
| `EffectRemovalRequest.StackGroupIds` | `array<StackGroupId>` | no | none | 0/18 |
| `IsEffectContainer` | `bool` | no | `false` | 0/18 |
| `Stacking` | `object` | no | defaults resolve to ByEffectDefinitionId / Stack / IndependentStacks | 16/18 |
| `Stacking.MaxStackOverflowPolicy` | `enum (MaxStackOverflowPolicy)` | no | `ReplaceOldest` | 7/18 |
| `Stacking.MaxStacks` | `int` | no | none | 8/18 |
| `Stacking.StackBehavior` | `enum (StackBehavior)` | no | `IndependentStacks` | 8/18 |
| `Stacking.StackGroupId` | `int` | no | none | 3/18 |
| `Stacking.StackPolicy` | `enum (StackPolicy)` | no | `Stack` | 16/18 |
| `Stacking.StackScope` | `enum (StackScope)` | no | `ByEffectDefinitionId` | 16/18 |
| `Target` | `object` | no | none | 14/18 |
| `Target.ApplicationMode` | `enum (ApplicationMode)` | no | inferred | 14/18 |
| `Target.Calculation` | `enum (Calculation)` | yes | none | 14/18 |
| `Target.Manipulation` | `enum (Manipulation)` | yes | none | 14/18 |
| `Target.TargetStat` | `Stat` | yes | none | 14/18 |
| `Target.TargetValueKind` | `enum (TargetValueKind)` | yes | none | 14/18 |
| `Target.Value` | `float` | yes | none | 14/18 |
| `Trigger` | `object` | no | `{"TriggerType":"Immediate"}` | 18/18 |
| `Trigger.TickIntervalMs` | `float` | no | `0.0` | 3/18 |
| `Trigger.TriggerConditions` | `array<TriggerCondition>` | no | `[]` | 1/18 |
| `Trigger.TriggerConditions[].Classifications` | `array<EffectClassification>` | no | none | 0/18 |
| `Trigger.TriggerConditions[].Conditions` | `array<Condition>` | no | none | 1/18 |
| `Trigger.TriggerConditions[].Operator` | `enum (ConditionOperator)` | no | `Any` | 1/18 |
| `Trigger.TriggerConditions[].Stat` | `Stat` | no | none | 0/18 |
| `Trigger.TriggerConditions[].TargetValueKind` | `enum (TargetValueKind)` | no | none | 0/18 |
| `Trigger.TriggerConditions[].ThresholdPercent` | `float` | no | none | 0/18 |
| `Trigger.TriggerConditions[].Type` | `enum (TriggerConditionType)` | yes | none | 1/18 |
| `Trigger.TriggerType` | `enum (TriggerType)` | yes | `Immediate` | 18/18 |
| `Visibility` | `enum (Visibility)` | no | `Internal` | 18/18 |

## Core field details — alphabetical

### `Blocking`

Optional block configuration evaluated before trigger timing and trigger conditions. Any matching blocked condition or classification blocks that execution attempt.

**Type:** `object`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 15/18 definitions. Representative value from `CorrodingPoison`: `{…}`.

### `Blocking.BlockedByClassification`

Effect-classification selectors checked against active effects on the target. Any match blocks execution.

**Type:** `array<EffectClassification>`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Canonical EffectClassification query tags. Selector fields may use parent/category tags and match expanded tag masks.

**Current authored usage:** 4/18 definitions. Representative value from `CorrodingPoison`: `[Effect.Immunity.Poison, Effect.Immunity.Corrosion]`.

### `Blocking.BlockedByCondition`

Entity-condition selectors checked on the target LivingEntityState. Any match blocks execution.

**Type:** `array<Condition>`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Canonical EntityCondition query tags. Condition.Dead also blocks by default independently unless CanTriggerWhileDead is true.

**Current authored usage:** 9/18 definitions. Representative value from `DelayedExplosion`: `[Condition.Invulnerable]`.

### `Blocking.BlockPolicy`

What happens when the effect is blocked: consume/remove it, or keep it active for a later retry.

**Type:** `enum (BlockPolicy)`. **Schema requirement:** Optional. **Default:** `RemoveOnFail`.

**Rules / semantics:** Allowed: RemoveOnFail, Retry. Default: RemoveOnFail. Retry keeps the effect until its next due attempt, expiry, or explicit removal.

**Current authored usage:** 15/18 definitions. Representative value from `CorrodingPoison`: `Retry`.

### `CanTriggerWhileDead`

Overrides the default rule that Condition.Dead blocks effect execution. Set true only for effects such as revive that must execute while the target is dead.

**Type:** `bool`. **Schema requirement:** Optional. **Default:** `false`.

**Rules / semantics:** Default: false.

**Current authored usage:** 7/18 definitions. Representative value from `CleansePoison`: `false`.

### `ChildEffectDefinitions`

Ordered child-effect composition list. Children are separate runtime ActiveEffect entries; referenced definitions must exist, the child graph must be acyclic, and processing order is the authored list order.

**Type:** `array<ChildEffectDefinition>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 1/18 definitions. Representative value from `Overheating`: `[{…}]`.

### `ChildEffectDefinitions[].ChildEffectDefinitionId`

Stable ID of the EffectDefinition to create as the child.

**Type:** `EffectDefinitionId`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 1/18 definitions. Representative value from `Overheating`: `effect.buff.weapon.overheating.cooldown`.

### `ChildEffectDefinitions[].ChildEffectTrigger`

Parent-lifecycle event that creates/binds the child.

**Type:** `enum (ChildEffectTrigger)`. **Schema requirement:** Optional. **Default:** `BoundToParent`.

**Rules / semantics:** Allowed: BoundToParent, OnParentExpired, OnParentRemoved, OnParentExpiredOrRemoved. Default: BoundToParent.

**Current authored usage:** 1/18 definitions. Representative value from `Overheating`: `OnParentExpired`.

### `ConditionRequests`

Lifecycle-timed condition mutations owned by the effect runtime instance. Each entry must add and/or remove at least one condition.

**Type:** `array<ConditionRequest>`. **Schema requirement:** Optional. **Default:** `[]`.

**Current authored usage:** 8/18 definitions. Representative value from `CorrodingPoison`: `[{…}, {…}]`.

### `ConditionRequests[].ConditionsAddRequest`

EntityCondition tags to add at the configured lifecycle timing. The current EffectRuntimeId becomes a contributor for each condition.

**Type:** `array<Condition>`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Values must be fully qualified, assignable EntityCondition tags (normally leaf tags for direct assignment).

**Current authored usage:** 7/18 definitions. Representative value from `CorrodingPoison`: `[Condition.Poisoned]`.

### `ConditionRequests[].ConditionsRemoveRequest`

EntityCondition tags to remove at the configured lifecycle timing, according to RemoveMode.

**Type:** `array<Condition>`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Values must be fully qualified, assignable EntityCondition tags (normally leaf tags for direct assignment).

**Current authored usage:** 8/18 definitions. Representative value from `CorrodingPoison`: `[Condition.Poisoned]`.

### `ConditionRequests[].RemoveMode`

Controls whether condition removal deletes only this effect runtime’s contribution or all contributions for the listed condition.

**Type:** `enum (ConditionRemoveMode)`. **Schema requirement:** Optional. **Default:** inferred by Timing.

**Rules / semantics:** Allowed: RemoveOwnContribution, RemoveAllContributions. Default is inferred from Timing: application/execution => RemoveAllContributions; expiry/removal cleanup => RemoveOwnContribution.

**Current authored usage:** 8/18 definitions. Representative value from `CorrodingPoison`: `RemoveOwnContribution`.

### `ConditionRequests[].Timing`

Lifecycle moment at which the condition request executes.

**Type:** `enum (ConditionLifecycleTiming)`. **Schema requirement:** Required. **Default:** none.

**Rules / semantics:** Allowed: OnFirstApplication, OnFirstExecution, OnExpire, OnRemove, OnExpireOrRemove. Canonical 6.3.0 definitions declare Timing explicitly.

**Current authored usage:** 15/18 definitions. Representative value from `CorrodingPoison`: `OnFirstApplication`.

### `Definition`

Required immutable identity/metadata block for the effect definition.

**Type:** `object`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `{…}`.

### `Definition.Description`

Required human-readable description of the authored effect.

**Type:** `string`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `Removes poison-classified active effects from the target.`.

### `Definition.EffectDefinitionId`

Required stable effect-definition identifier used by registries, runtime references, stacking identity, child references, removal selectors, and other systems.

**Type:** `string`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `effect.cleanse.poison`.

### `Definition.Name`

Required human-readable display/debug name.

**Type:** `string`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `Cleanse Poison`.

### `Duration`

Effect lifetime configuration. If omitted by authoring conventions, the semantic default is a one-shot effect (DurationType: Once).

**Type:** `object`. **Schema requirement:** Optional. **Default:** semantic default: DurationType: Once.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `{…}`.

### `Duration.DurationMS`

Authored lifetime in milliseconds for Temporary effects. Converted to deterministic NetworkTick values before runtime simulation.

**Type:** `float`. **Schema requirement:** Optional. **Default:** `0.0`.

**Rules / semantics:** Required when DurationType is Temporary; must be > 100. Omit for Once and UntilRemoved.

**Current authored usage:** 11/18 definitions. Representative value from `CorrodingPoison`: `1500.0`.

### `Duration.DurationType`

Lifetime mode: execute once, live temporarily, or remain until explicitly removed.

**Type:** `enum (DurationType)`. **Schema requirement:** Required. **Default:** `Once`.

**Rules / semantics:** Allowed: Once, Temporary, UntilRemoved. Default: Once. Permanent is deprecated/invalid.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `Once`.

### `EffectClassifications`

Static EffectClassification gameplay-tag metadata attached to an accepted ActiveEffect. It is query/removal/blocking metadata, not a LivingEntityState condition and does not execute behavior by itself.

**Type:** `array<EffectClassification>`. **Schema requirement:** Optional. **Default:** `[]`.

**Rules / semantics:** Direct assignments must be fully qualified, assignable EffectClassification tags; parent/category tags are for selectors/query fields. Default: empty list.

**Current authored usage:** 12/18 definitions. Representative value from `CorrodingPoison`: `[Effect.Corroding, Effect.Debuff]`.

### `EffectProcessing`

Simulation lane for this effect: Fast or Slow.

**Type:** `enum (EffectProcessing)`. **Schema requirement:** Optional. **Default:** `Fast`.

**Rules / semantics:** Allowed: Fast, Slow. Default: Fast. In the provided StateEngine contract Fast runs on the 30 TPS lane and Slow on the 15 TPS lane (every second fast NetworkTick).

**Current authored usage:** 7/18 definitions. Representative value from `CleansePoison`: `Fast`.

### `EffectRemovalPolicy`

Permission policy controlling which other effect-removal requests are allowed to remove this active effect.

**Type:** `enum (EffectRemovalPolicy)`. **Schema requirement:** Optional. **Default:** `ByAnySource`.

**Rules / semantics:** Allowed: ByAnySource, BySourceId, ByOriginId. Default: ByAnySource.

**Current authored usage:** 11/18 definitions. Representative value from `CleansePoison`: `ByAnySource`.

### `EffectRemovalRequest`

Request to remove matching active effects from the target after the requester successfully triggers. Selector fields are ANDed together; values within a selector list are ORed.

**Type:** `object`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** At least one selector list must be non-empty. Passives cannot be removed through this request.

**Current authored usage:** 2/18 definitions. Representative value from `CleansePoison`: `{…}`.

### `EffectRemovalRequest.Classifications`

Matches active effects whose classification set satisfies at least one listed EffectClassification selector; parent/category selectors may match descendants through expanded tag masks.

**Type:** `array<EffectClassification>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 1/18 definitions. Representative value from `CleansePoison`: `[Effect.Damage.Poison, Effect.Corroding]`.

### `EffectRemovalRequest.Conditions`

Matches active effects that currently own a contribution to at least one listed EntityCondition.

**Type:** `array<Condition>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `EffectRemovalRequest.EffectDefinitionIds`

Matches active effects whose EffectDefinitionId is one of the listed IDs.

**Type:** `array<EffectDefinitionId>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `EffectRemovalRequest.EffectRemovalBehaviour`

Optional limit/order block for matching removals. If omitted, all matching removable active effects are requested for removal.

**Type:** `object`. **Schema requirement:** Optional. **Default:** omitted => remove all matching candidates.

**Current authored usage:** 1/18 definitions. Representative value from `CleansePoison`: `{…}`.

### `EffectRemovalRequest.EffectRemovalBehaviour.MaxRemoveCount`

Maximum number of matching effects to remove when EffectRemovalBehaviour is present.

**Type:** `int`. **Schema requirement:** Optional. **Default:** `1`.

**Rules / semantics:** Default: 1 when EffectRemovalBehaviour is present.

**Current authored usage:** 1/18 definitions. Representative value from `CleansePoison`: `3`.

### `EffectRemovalRequest.EffectRemovalBehaviour.RemoveOldestFirst`

Controls age ordering for removal. True removes oldest candidates first; false reverses age order. Tie-breakers remain deterministic.

**Type:** `bool`. **Schema requirement:** Optional. **Default:** `true`.

**Rules / semantics:** Default: true. Deterministic order then uses AppliedNetworkTick, InsertionSequence, EffectRuntimeId.

**Current authored usage:** 1/18 definitions. Representative value from `CleansePoison`: `true`.

### `EffectRemovalRequest.EffectRuntimeIds`

Matches specific runtime effect instances by EffectRuntimeId.

**Type:** `array<EffectRuntimeId>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `EffectRemovalRequest.EffectsTargetingStats`

Matches active effects whose stat target is one of the listed stats.

**Type:** `array<Stat>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 1/18 definitions. Representative value from `EnergyStorageCurse`: `[EnergyProduction]`.

### `EffectRemovalRequest.OriginIds`

Matches active effects by OriginId.

**Type:** `array<OriginId>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `EffectRemovalRequest.SourceIds`

Matches active effects by SourceId.

**Type:** `array<SourceId>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `EffectRemovalRequest.StackGroupIds`

Matches active effects by authored StackGroupId.

**Type:** `array<StackGroupId>`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `IsEffectContainer`

Marks a definition that exists only to group child effects. Containers require at least one child and must omit direct Target, ConditionRequests, and EffectRemovalRequest payloads; direct EffectClassifications must be omitted or empty.

**Type:** `bool`. **Schema requirement:** Optional. **Default:** `false`.

**Rules / semantics:** Default: false.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `Stacking`

Duplicate-detection and stack-handling configuration. Duplicate identity is scoped per target LivingEntityState.

**Type:** `object`. **Schema requirement:** Optional. **Default:** defaults resolve to ByEffectDefinitionId / Stack / IndependentStacks.

**Current authored usage:** 16/18 definitions. Representative value from `CleansePoison`: `{…}`.

### `Stacking.MaxStackOverflowPolicy`

Behavior when an accepted stack would exceed MaxStacks.

**Type:** `enum (MaxStackOverflowPolicy)`. **Schema requirement:** Optional. **Default:** `ReplaceOldest`.

**Rules / semantics:** Allowed: ReplaceOldest, RejectNewest. Default: ReplaceOldest.

**Current authored usage:** 7/18 definitions. Representative value from `CorrodingPoison`: `ReplaceOldest`.

### `Stacking.MaxStacks`

Optional canonical stack limit. When used with StackPolicy: Stack it must be greater than zero.

**Type:** `int`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** LimitStacks and MaxStacksToOverflow are invalid legacy fields.

**Current authored usage:** 8/18 definitions. Representative value from `CorrodingPoison`: `5`.

### `Stacking.StackBehavior`

How accepted duplicates are represented or refresh existing runtime state.

**Type:** `enum (StackBehavior)`. **Schema requirement:** Optional. **Default:** `IndependentStacks`.

**Rules / semantics:** Allowed: IndependentStacks, RefreshDuration, AddStackRefreshDuration, AddStackNoRefresh. Default: IndependentStacks.

**Current authored usage:** 8/18 definitions. Representative value from `CorrodingPoison`: `AddStackRefreshDuration`.

### `Stacking.StackGroupId`

Stable authored stack-group identifier. Required when StackScope is ByStackGroupId; can make different EffectDefinitionIds share duplicate identity.

**Type:** `int`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 3/18 definitions. Representative value from `Overheating`: `123`.

### `Stacking.StackPolicy`

Whether a detected duplicate is stacked, replaces an existing duplicate, or is discarded.

**Type:** `enum (StackPolicy)`. **Schema requirement:** Optional. **Default:** `Stack`.

**Rules / semantics:** Allowed: Stack, Replace, Discard. Default: Stack.

**Current authored usage:** 16/18 definitions. Representative value from `CleansePoison`: `Discard`.

### `Stacking.StackScope`

Fields used to determine duplicate identity on the same target.

**Type:** `enum (StackScope)`. **Schema requirement:** Optional. **Default:** `ByEffectDefinitionId`.

**Rules / semantics:** Allowed: ByEffectDefinitionId, SameSource, SameSourceAndOrigin, ByStackGroupId. Default: ByEffectDefinitionId.

**Current authored usage:** 16/18 definitions. Representative value from `CleansePoison`: `ByEffectDefinitionId`.

### `Target`

Optional single stat target. An EffectDefinition may define at most one direct stat target.

**Type:** `object`. **Schema requirement:** Optional. **Default:** none.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `{…}`.

### `Target.ApplicationMode`

How the target stat change is applied: instant transaction, recalculated while-active modifier, or over-time accumulated while-active modifier. May be inferred when omitted.

**Type:** `enum (ApplicationMode)`. **Schema requirement:** Optional. **Default:** inferred.

**Rules / semantics:** Allowed: Instant, WhileActive, AccumulateWhileActive. If omitted, deterministic defaults are inferred from trigger/stat category/value kind.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `WhileActive`.

### `Target.Calculation`

Whether Target.Value is interpreted as a flat amount or percentage.

**Type:** `enum (Calculation)`. **Schema requirement:** Required. **Default:** none.

**Rules / semantics:** Allowed: Flat, Percentage. Percentage values use decimal fractions. Percentage basis depends on the target: active CurrentMaxValue / attribute / flow modifiers use the passive-adjusted baseline; Instant PoolStat.CurrentValue percentages use CurrentMaxValue at execution time.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `Percentage`.

### `Target.Manipulation`

Whether the effect raises or reduces the target value.

**Type:** `enum (Manipulation)`. **Schema requirement:** Required. **Default:** none.

**Rules / semantics:** Allowed: Raise, Reduce.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `Reduce`.

### `Target.TargetStat`

Canonical stat to affect. Valid combinations depend on stat category, TriggerType, TargetValueKind, and ApplicationMode.

**Type:** `Stat`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `Armor`.

### `Target.TargetValueKind`

Active stat layer to target: CurrentValue or CurrentMaxValue. CurrentMaxValue is only valid for PoolStats.

**Type:** `enum (TargetValueKind)`. **Schema requirement:** Required. **Default:** none.

**Rules / semantics:** Allowed: CurrentValue, CurrentMaxValue. Active effects must not target BaseValue or EffectiveMaxValue.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `CurrentValue`.

### `Target.Value`

Numeric magnitude. Percentage values are decimal fractions (for example 0.05 = 5%); FlowStat values use the stat’s canonical unit, per millisecond where applicable.

**Type:** `float`. **Schema requirement:** Required. **Default:** none.

**Current authored usage:** 14/18 definitions. Representative value from `CorrodingPoison`: `0.05`.

### `Trigger`

Defines when the effect executes or contributes. If omitted, TriggerType defaults to Immediate.

**Type:** `object`. **Schema requirement:** Optional. **Default:** `{"TriggerType":"Immediate"}`.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `{…}`.

### `Trigger.TickIntervalMs`

Authored interval in milliseconds between due OverTime executions. Only valid with TriggerType: OverTime and must be greater than 100.

**Type:** `float`. **Schema requirement:** Optional. **Default:** `0.0`.

**Rules / semantics:** OnlyWithTriggerType: OverTime.

**Current authored usage:** 3/18 definitions. Representative value from `MaxHealthRot`: `1000.0`.

### `Trigger.TriggerConditions`

Optional additional gates evaluated after blocking and when the trigger is due. Each entry is evaluated according to its `Type`; the provided sources do not separately define a list-level operator beyond the per-entry `Operator` field used by condition/classification lists.

**Type:** `array<TriggerCondition>`. **Schema requirement:** Optional. **Default:** `[]`.

**Current authored usage:** 1/18 definitions. Representative value from `Revive`: `[{…}]`.

### `Trigger.TriggerConditions[].Classifications`

EffectClassification selectors used by classification-presence/missing trigger conditions.

**Type:** `array<EffectClassification>`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Required for OnClassificationsPresent / OnClassificationsMissing. Query tags may use parent/category tags.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `Trigger.TriggerConditions[].Conditions`

EntityCondition selectors used by condition-presence/missing trigger conditions.

**Type:** `array<Condition>`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Required for OnConditionsPresent / OnConditionsMissing. Query tags may use parent/category tags.

**Current authored usage:** 1/18 definitions. Representative value from `Revive`: `[Condition.Dead]`.

### `Trigger.TriggerConditions[].Operator`

Any/All aggregation for condition or classification lists.

**Type:** `enum (ConditionOperator)`. **Schema requirement:** Optional. **Default:** `Any`.

**Rules / semantics:** Allowed: Any, All. Default: Any.

**Current authored usage:** 1/18 definitions. Representative value from `Revive`: `All`.

### `Trigger.TriggerConditions[].Stat`

Stat observed by stat-based trigger conditions.

**Type:** `Stat`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Required for OnStatRaised, OnStatReduced, OnStatValueBelow, OnStatValueAbove as applicable.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `Trigger.TriggerConditions[].TargetValueKind`

Observed stat value kind for OnStatRaised and OnStatReduced.

**Type:** `enum (TargetValueKind)`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Required for OnStatRaised and OnStatReduced.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `Trigger.TriggerConditions[].ThresholdPercent`

Final resolved stat threshold for OnStatValueBelow/OnStatValueAbove, expressed in percentage points (80.0 means 80%, not 0.80).

**Type:** `float`. **Schema requirement:** Optional. **Default:** none.

**Rules / semantics:** Required for OnStatValueBelow and OnStatValueAbove.

**Current authored usage:** 0/18 definitions. The field is valid in the schema even though the supplied `EffectDefinitions.yml` does not currently use it.

### `Trigger.TriggerConditions[].Type`

Trigger-condition discriminator determining which companion fields are required and how the condition is evaluated.

**Type:** `enum (TriggerConditionType)`. **Schema requirement:** Required. **Default:** none.

**Rules / semantics:** Allowed: OnConditionsPresent, OnConditionsMissing, OnClassificationsPresent, OnClassificationsMissing, OnStatReduced, OnStatRaised, OnStatValueBelow, OnStatValueAbove.

**Current authored usage:** 1/18 definitions. Representative value from `Revive`: `OnConditionsPresent`.

### `Trigger.TriggerType`

Execution/contribution timing mode for the effect.

**Type:** `enum (TriggerType)`. **Schema requirement:** Required. **Default:** `Immediate`.

**Rules / semantics:** Allowed: Immediate, OverTime, WhileActive, OnExpire, OnRemoval. Default: Immediate. OnExpire requires Temporary; OnRemoval requires Temporary or UntilRemoved.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `Immediate`.

### `Visibility`

Presentation visibility metadata. It does not change simulation behavior.

**Type:** `enum (Visibility)`. **Schema requirement:** Optional. **Default:** `Internal`.

**Rules / semantics:** Allowed: Internal, Hud, Menu. Default: Internal.

**Current authored usage:** 18/18 definitions. Representative value from `CleansePoison`: `Hud`.

## Valid stat-target combinations

`Target` validity is not determined by an individual field alone; the trigger, stat category, target value kind, and application mode must form one of the following combinations.

| TriggerType | Stat category | TargetValueKind | ApplicationMode |
|---|---|---|---|
| `Immediate` | `PoolStat` | `CurrentValue` | `Instant` |
| `OverTime` | `PoolStat` | `CurrentValue` | `Instant` |
| `OverTime` | `PoolStat` | `CurrentMaxValue` | `AccumulateWhileActive` |
| `WhileActive` | `PoolStat` | `CurrentMaxValue` | `WhileActive` |
| `WhileActive` | `AttributeStat` | `CurrentValue` | `WhileActive` |
| `WhileActive` | `FlowStat` | `CurrentValue` | `WhileActive` |
| `OnExpire` | `PoolStat` | `CurrentValue` | `Instant` |
| `OnRemoval` | `PoolStat` | `CurrentValue` | `Instant` |

When `Target.ApplicationMode` is omitted, these same combinations determine the inferred mode.

## Canonical 6.3.0 enum values

| Enum | Values |
|---|---|
| `ApplicationMode` | `Instant`, `WhileActive`, `AccumulateWhileActive` |
| `BlockPolicy` | `RemoveOnFail`, `Retry` |
| `Calculation` | `Flat`, `Percentage` |
| `ChildEffectTrigger` | `BoundToParent`, `OnParentExpired`, `OnParentRemoved`, `OnParentExpiredOrRemoved` |
| `ConditionLifecycleTiming` | `OnFirstApplication`, `OnFirstExecution`, `OnExpire`, `OnRemove`, `OnExpireOrRemove` |
| `ConditionOperator` | `Any`, `All` |
| `ConditionRemoveMode` | `RemoveOwnContribution`, `RemoveAllContributions` |
| `DurationType` | `Once`, `Temporary`, `UntilRemoved` |
| `EffectProcessing` | `Fast`, `Slow` |
| `EffectRemovalPolicy` | `ByAnySource`, `BySourceId`, `ByOriginId` |
| `Manipulation` | `Raise`, `Reduce` |
| `MaxStackOverflowPolicy` | `ReplaceOldest`, `RejectNewest` |
| `StackBehavior` | `IndependentStacks`, `RefreshDuration`, `AddStackRefreshDuration`, `AddStackNoRefresh` |
| `StackPolicy` | `Stack`, `Replace`, `Discard` |
| `StackScope` | `ByEffectDefinitionId`, `SameSource`, `SameSourceAndOrigin`, `ByStackGroupId` |
| `TargetValueKind` | `CurrentValue`, `CurrentMaxValue` |
| `TriggerConditionType` | `OnConditionsPresent`, `OnConditionsMissing`, `OnClassificationsPresent`, `OnClassificationsMissing`, `OnStatReduced`, `OnStatRaised`, `OnStatValueBelow`, `OnStatValueAbove` |
| `TriggerType` | `Immediate`, `OverTime`, `WhileActive`, `OnExpire`, `OnRemoval` |
| `Visibility` | `Internal`, `Hud`, `Menu` |

## Stat categories referenced by the 6.3.0 schema

**PoolStat:** `Health`, `Shield`, `EnergyStorage`, `OxygenStorage`, `FoodStorage`, `WaterStorage`.

**FlowStat:** `HealthRegeneration`, `ShieldRegeneration`, `EnergyProduction`, `EnergyConsumption`, `OxygenProduction`, `OxygenConsumption`, `FoodProduction`, `FoodConsumption`, `WaterProduction`, `WaterConsumption`.

**AttributeStat:** `Armor`, `Speed`, `Acceleration`, `AfterburnerSpeed`, `AfterburnerAcceleration`, `AttackSpeed`, `CriticalHitChance`, `CriticalHitBonusDamage`, `BonusCriticalHitChance`, `CooldownReduction`, `LifeLeech`, `RetaliationDamage`, `ShieldRechargeDuration`, `BonusDamageKinetic`, `BonusDamageThermal`, `BonusDamageExplosive`, `ResistanceKinetic`, `ResistanceThermal`, `ResistanceExplosive`.

## Newer CombatResolver v1 extension fields

> These fields are defined by `CombatResolver.md` 1.0.0 but are **not present** in `EffectSchemas.yml` 6.3.0 or the supplied `EffectDefinitions.yml` 6.3.0. They are documented here because they are part of the complete provided source material, but they should be treated as a later schema extension until the YAML schema is updated.

| Field path | Type | Requirement in CombatResolver v1 | Rules |
|---|---|---|---|
| `Damage` | `object` | required when EffectKind: Damage |  |
| `Damage.ArmorBypassFraction` | `float` | required for Damage | Validation range: [0.0, 1.0]. |
| `Damage.BaseDamage` | `float` | required for Damage | Must be present and non-negative. May be overridden only through an explicitly allowed set-by-caller field. |
| `Damage.CanCrit` | `bool` | required for Damage | Must be explicitly present on every Damage definition. |
| `Damage.CriticalRollScope` | `enum` | required when CanCrit: true | Allowed: PerTarget, PerAction. Required when CanCrit is true; absent or ignored when CanCrit is false. |
| `Damage.DamageKind` | `DamageKind` | required for Damage | Must exist and have concrete BonusDamage<DamageKind> and Resistance<DamageKind> stat mappings. |
| `Damage.ResistanceBypassFraction` | `float` | required for Damage | Validation range: [0.0, 1.0]. |
| `Damage.ShieldBypassFraction` | `float` | required for Damage | Validation range: [0.0, 1.0]. |
| `EffectKind` | `enum` | required for CombatResolver-routed damage; used by resolver contract | CombatResolver v1 values: Damage, StatChange. EffectKind: Damage requires Damage and must not define direct Target. EffectKind: StatChange may use the normal Target block. |

### `Damage`

CombatResolver v1 damage payload. Not present in EffectSchemas.yml 6.3.0 or EffectDefinitions.yml 6.3.0.

**Type:** `object`. **Requirement:** required when EffectKind: Damage. **Default:** CombatResolver v1 damage payload. Not present in EffectSchemas.yml 6.3.0 or EffectDefinitions.yml 6.3.0..

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.ArmorBypassFraction`

Fraction of target Armor ignored before the armor rating curve.

**Type:** `float`. **Requirement:** required for Damage. **Default:** Fraction of target Armor ignored before the armor rating curve..

**Rules / semantics:** Validation range: [0.0, 1.0].

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.BaseDamage`

Authored base damage before source bonuses and critical-hit multiplication.

**Type:** `float`. **Requirement:** required for Damage. **Default:** Authored base damage before source bonuses and critical-hit multiplication..

**Rules / semantics:** Must be present and non-negative. May be overridden only through an explicitly allowed set-by-caller field.

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.CanCrit`

Explicitly enables or disables critical-hit rolls for this damage definition.

**Type:** `bool`. **Requirement:** required for Damage. **Default:** Explicitly enables or disables critical-hit rolls for this damage definition..

**Rules / semantics:** Must be explicitly present on every Damage definition.

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.CriticalRollScope`

Critical-roll sharing scope. PerTarget rolls independently for each target; PerAction shares one roll for ActionRuntimeId + EffectDefinitionId.

**Type:** `enum`. **Requirement:** required when CanCrit: true. **Default:** Critical-roll sharing scope. PerTarget rolls independently for each target; PerAction shares one roll for ActionRuntimeId + EffectDefinitionId..

**Rules / semantics:** Allowed: PerTarget, PerAction. Required when CanCrit is true; absent or ignored when CanCrit is false.

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.DamageKind`

Damage vocabulary entry (examples in the source include Kinetic, Thermal, Explosive, and Acid) used to select bonus-damage and resistance stats.

**Type:** `DamageKind`. **Requirement:** required for Damage. **Default:** Damage vocabulary entry (examples in the source include Kinetic, Thermal, Explosive, and Acid) used to select bonus-damage and resistance stats..

**Rules / semantics:** Must exist and have concrete BonusDamage<DamageKind> and Resistance<DamageKind> stat mappings.

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.ResistanceBypassFraction`

Fraction of the target damage-kind resistance ignored before the resistance rating curve.

**Type:** `float`. **Requirement:** required for Damage. **Default:** Fraction of the target damage-kind resistance ignored before the resistance rating curve..

**Rules / semantics:** Validation range: [0.0, 1.0].

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `Damage.ShieldBypassFraction`

Fraction of post-resistance/post-crit damage routed past Shield directly toward Health.

**Type:** `float`. **Requirement:** required for Damage. **Default:** Fraction of post-resistance/post-crit damage routed past Shield directly toward Health..

**Rules / semantics:** Validation range: [0.0, 1.0].

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

### `EffectKind`

Executable discriminator introduced by CombatResolver v1. Relevant v1 values are Damage and StatChange. Damage routes through EffectIntent -> CombatResolver; StatChange is already-resolved input for StateEngine.

**Type:** `enum`. **Requirement:** required for CombatResolver-routed damage; used by resolver contract. **Default:** Executable discriminator introduced by CombatResolver v1. Relevant v1 values are Damage and StatChange. Damage routes through EffectIntent -> CombatResolver; StatChange is already-resolved input for StateEngine..

**Rules / semantics:** CombatResolver v1 values: Damage, StatChange. EffectKind: Damage requires Damage and must not define direct Target. EffectKind: StatChange may use the normal Target block.

**Current authored usage:** 0/18 definitions in the supplied `EffectDefinitions.yml` 6.3.0.

## Source-version caveat: `Internal` block in CombatResolver example

`CombatResolver.md` contains an internal-cost example with:

```yaml
Internal:
  IsInternal: true
  IsUnblockable: true
```

However, the provided source material does not define a formal schema or field contract for `Internal`, `Internal.IsInternal`, or `Internal.IsUnblockable`. They are therefore **not listed as canonical EffectDefinition fields** above. Treat them as example-only/provisional until a schema document formally defines them.

## Deprecated / invalid legacy authoring

The 6.3.0 schema rejects or supersedes the following legacy names/values:

- `EffectType: Passive` — passives use the separate passive pipeline.
- `DurationType: Permanent` — use `UntilRemoved` for active effects.
- `TriggerType: OverTimeConstant` — use `WhileActive`.
- `TargetValueKind: Current` / `Max` — use `CurrentValue` / `CurrentMaxValue`.
- `ConditionAndClassificationRequests`, `ClassificationsAddRequest`, `ClassificationsRemoveRequest` — invalid; use top-level `EffectClassifications` plus lifecycle `ConditionRequests`.
- `SimulationLane` — use `EffectProcessing`.
- `EffectRemovalRule` — use `EffectRemovalRequest`.
- `ChildEffectDefinitionIds` / `ChildEffectBehaviour` — use `ChildEffectDefinitions[].ChildEffectDefinitionId` / `.ChildEffectTrigger`.
- `LimitStacks`, `MaxStacksToOverflow` — use `MaxStacks`.
- `MaxStackOverflowPolicy: RefreshOldest` — use `ReplaceOldest` or `RejectNewest`.

## Minimal canonical example

```yaml
Heal:
  Definition:
    EffectDefinitionId: effect.raise.heal
    Name: Heal
    Description: Raises the target entity current Health by a flat amount.
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
  Blocking:
    BlockPolicy: RemoveOnFail
    BlockedByCondition:
      - Condition.Incurable
    BlockedByClassification:
      - Effect.CannotBeHealed
  Visibility: Internal
```

---

**Maintenance note:** When `EffectSchemas.yml` changes, regenerate/review this page from the schema first, then reconcile semantic changes from `Effects.md`, `StateEngine.md`, `TagSystem.md`, and resolver-specific contracts such as `CombatResolver.md`.
