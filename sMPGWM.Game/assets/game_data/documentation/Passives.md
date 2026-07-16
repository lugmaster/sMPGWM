# Passives

**Source PRDs:** `FLU_RPG_StateEngine_PRD.md`, `Effects.md`, `Stats.md`, `LivingEntityState.md`, `LivingEntityRegistry.md`, `TagSystem.md`  
**Version:** 1.0.0  
**Engine:** Godot 4.6.2  
**Language:** C# / Mono  
**Genre:** 2D Space RPG  
**Network Model:** Host-authoritative simulation with rollback/resimulation support  
**Network Tick Rate:** 30 TPS  
**Primary Concern:** Passive definitions, generated passive runtime effects, stat-passive resolution, reactive passive behavior, passive tags, and deterministic validation

---

## 1 Purpose

This document defines the passive system for the RPG State Engine.

The existing StateEngine PRD already establishes that passives are not `ActiveEffect` records and are not stored in `EffectDefinitions.yml`. It also establishes that passive stat modifiers resolve into `EffectiveMaxValue`, are recalculated only when their source changes, and must not be recalculated every tick by default.

This document extends that base contract with a runtime model for reactive passives such as Bloodsucker:

```text
PassiveDefinition = immutable authored passive data
PassiveEffect = source-bound runtime passive record generated from PassiveDefinition
PassiveRuntime = mutable runtime state owned by the generated PassiveEffect
PassiveSchemas.yml = validation and runtime schema contract
PassiveDefinitions.yml = authored passive definition data
```

`PassiveEffect` is processed by the StateEngine passive path. It is not an `ActiveEffect`.

---

## 2 Extracted Existing Requirements

Current PRD and documentation requirements that constrain passives:

- Passives are not `EffectDefinition` entries.
- Passives are not `ActiveEffect` runtime records.
- Active effects may last `UntilRemoved`, but that is still active lifetime behavior, not passive behavior.
- Passive source systems are external and may include items, talents, level-ups, ship configuration, permanent upgrades, and other persistent source state.
- Passive source systems are out of scope for the StateEngine, but their normalized passive inputs are in scope.
- Passive modifier resolution writes into `EffectiveMaxValue`.
- Passive modifiers must not target `CurrentValue`.
- Passive modifiers must not target `CurrentMaxValue`.
- Passive modifiers must not target `BaseValue`.
- Passive modifiers are recalculated only when a passive source is added, removed, replaced, or dirtied by source state.
- Passives must not be recalculated every tick unless their definition explicitly needs runtime evaluation.
- Passive flat modifiers are added before passive percentage modifiers.
- Passive percentage modifiers are additive with each other.
- If no passive modifier exists for a stat, `EffectiveMaxValue = BaseValue`.
- `LivingEntityState` stores resolved passive results and passive dirty/version metadata.
- `LivingEntityState` must not own the full passive source graph.
- StateEngine snapshots and replay must preserve enough passive data to resimulate deterministically.
- Delta output may need to report passive layer changes.
- `LivingEntityRegistry` owns presence and lifecycle, but not passive source data or passive cleanup internals.

Passive stat resolution:

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

---

## 3 Decisions

The passive model uses these decisions:

- `PassiveDefinition` supports both stat modifiers and reactive triggers.
- A generated `PassiveEffect` runtime record is processed by the StateEngine.
- `PassiveEffect` is not an `ActiveEffect`.
- Passive metadata uses a separate gameplay tag domain.
- Passive tags use `DomainKey: PassiveClassification` and `RootPrefix: Passive`.
- Passive tags are not `EffectClassification` tags and are not `EntityCondition` tags.
- `PassiveDefinition` declares explicit `EvaluationPolicies`.
- `PassiveEffect` lifetime is source-bound.
- Removing or replacing the passive source removes the generated `PassiveEffect` and clears its runtime state.
- Generated `PassiveEffect` records are stored in StateEngine-owned passive runtime buffers keyed by `TargetEntityId`.
- Passive runtime state is included in StateEngine snapshots.
- Passive source stacking uses a passive-adapted stacking block.
- Event-driven runtime stacks use a separate `RuntimeStacking` block.
- Reactive passives may own runtime cooldowns stored on `PassiveRuntime`.
- Reactive passive outputs are processed in the same deterministic tick through a bounded result-event queue.
- Same-event passive reactions are ordered by `TargetEntityId`, `PassiveInsertionSequence`, `PassiveRuntimeId`, then emitted active effects use normal effect ordering.
- Reactive passives may emit normal `ActiveEffectSpec` records with deterministic runtime magnitude overrides.
- Emitted active effects may trigger later reactive systems through the normal result-event pipeline, constrained by recursion depth and same-tick event limits.

---

## 4 Passive Tags

Passive tags must be separate from effect classifications and living entity conditions.

Recommended domain:

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
```

Rationale:

| Domain | Stored on | Meaning | Why not use it for passives |
|---|---|---|---|
| `EntityCondition` / `Condition.*` | `LivingEntityState` condition tag set | Mutable resolved entity state | Passive metadata is not a condition and should not imply condition contributor ownership. |
| `EffectClassification` / `Effect.*` | Accepted `ActiveEffect` metadata | Active effect blocking, trigger, removal, and inspection metadata | Passives are not active effects and must not be removed or blocked through active-effect classification rules. |
| `PassiveClassification` / `Passive.*` | `PassiveDefinition` and generated `PassiveEffect` metadata | Passive filtering, UI/debug grouping, validation, event subscription filtering | Matches passive lifetime and passive runtime ownership. |

The tag system remains context-free. It provides domain-safe tag sets and queries only. The passive pipeline and StateEngine define what passive tags mean.

Required contract tests should eventually include:

```text
Passive classifications remain separate from EntityCondition.
Passive classifications remain separate from EffectClassification.
Passive classifications do not own passive lifetime.
Passive classifications do not mutate LivingEntityState.
Passive tags cannot be used in active effect removal selectors.
```

---

## 5 Core Runtime Model

### 5.1 PassiveDefinition

`PassiveDefinition` is immutable static data loaded from `PassiveDefinitions.yml`.

It may define:

- passive identity,
- passive classification tags,
- evaluation policies,
- stat modifiers,
- reactive triggers,
- source-level stacking rules,
- event-driven runtime stacking rules,
- emitted active effect specs,
- visibility metadata.

A `PassiveDefinition` is valid only if it has at least one gameplay payload:

```text
StatModifiers
ReactiveTriggers
PassiveClassifications
ChildPassiveDefinitions or future payloads
```

Classification-only passive definitions are valid only as queryable gameplay metadata that generate a source-bound `PassiveEffect`. Pure UI-only metadata definitions are out of scope unless a future document explicitly introduces them.

### 5.2 PassiveEffect

`PassiveEffect` is a StateEngine-owned runtime record generated from a `PassiveDefinition` and a source.

It stores:

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
LastEvaluatedNetworkTick
LastTriggeredNetworkTick
RuntimeFlags
```

`PassiveEffect` exists while its source is active. Source examples:

```text
Equipped item
Learned talent
Installed ship system
Permanent upgrade
Runtime granted passive source
```

When the source is removed or replaced, the generated `PassiveEffect` is removed and its runtime state is cleared.

### 5.3 PassiveRuntime

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

The implementation may store this as fields directly on `PassiveEffect`, or as a compact parallel runtime-state array keyed by `PassiveRuntimeId`. The behavior must remain deterministic and snapshot-friendly.

### 5.4 LivingEntityState Relationship

`LivingEntityState` stores:

```text
EffectiveMaxValues[]
PassiveDirtyStats
PassiveInputVersion
PassiveResolvedVersion
Passive layer dirty metadata
```

`LivingEntityState` does not own external passive source graphs.

Generated `PassiveEffect` records are StateEngine-owned runtime data. StateEngine snapshots must include enough passive runtime data to restore and resimulate:

```text
Active PassiveEffects[]
PassiveRuntime state
Passive insertion sequence
Runtime stack counts
Runtime stack expiry ticks
Passive dirty/version metadata
```

This preserves the original `LivingEntityState` boundary while allowing reactive passives to be deterministic.

---

## 6 Evaluation Policies

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

---

## 7 Stat Passives

Stat passives contribute only to `EffectiveMaxValue`.

Valid stat passive target:

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

Percentages use decimal fractions:

```text
0.20 = 20 percent
```

Passive percentages are additive with each other.

### 7.1 Equipment Stat Passive Example

Equipment stats are source-bound passives.

Example item:

```text
Chest Armor of Vigor
Slot: ChestArmor
Stats:
- Armor +30
- Health +30
- HealthRegeneration +5
```

This should generate a passive source when equipped, not a permanent active effect.

```yaml
ChestArmorOfVigorPassive:
  Definition:
    PassiveDefinitionId: passive.item.chest_armor_of_vigor.equipped_stats
    Name: Chest Armor of Vigor
    Description: Equipped chest armor stat bonuses.
  PassiveProcessing: Fast
  PassiveClassifications:
    - Passive.Source.Item
    - Passive.Stat.Armor
    - Passive.Stat.Health
    - Passive.Stat.HealthRegeneration
  EvaluationPolicies:
    - EvaluationPolicy: OnSourceDirty
  StatModifiers:
    - TargetStat: Armor
      TargetValueKind: EffectiveMaxValue
      Manipulation: Raise
      Calculation: Flat
      Value: 30.0
    - TargetStat: Health
      TargetValueKind: EffectiveMaxValue
      Manipulation: Raise
      Calculation: Flat
      Value: 30.0
    - TargetStat: HealthRegeneration
      TargetValueKind: EffectiveMaxValue
      Manipulation: Raise
      Calculation: Flat
      Value: 5.0
  Stacking:
    StackScope: SameSource
    StackPolicy: Discard
    StackBehavior: IndependentStacks
  Visibility: Menu
```

The item/equipment system owns slot rules and source lifecycle:

```text
Equip Chest Armor of Vigor
-> remove or replace the old ChestArmor passive source, if present
-> add passive source passive.item.chest_armor_of_vigor.equipped_stats
-> mark Armor, Health, and HealthRegeneration passive layers dirty
-> StateEngine recalculates affected EffectiveMaxValue values
```

Unequipping the item removes the passive source. The generated `PassiveEffect` is removed, its runtime state is cleared, and affected `EffectiveMaxValue` values are recalculated from the remaining passive sources.

This is not `DurationType: UntilRemoved` active-effect behavior. Equipment stats do not need active-effect triggering, blocking, condition lifecycle, active-effect removal requests, or active effect classifications.

`HealthRegeneration` must use the stat registry's canonical unit. If designers author regeneration in a friendlier unit such as per second, import validation should convert it to the canonical runtime unit before simulation.

---

## 8 Reactive Passives

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

The first implementation should include only the events required by actual passive definitions. Bloodsucker requires `DamageDealt`.

Reactive passives may:

- check event fields,
- update `PassiveRuntime`,
- gain or refresh runtime stacks,
- start or check runtime cooldowns,
- emit `ActiveEffectSpec` outputs,
- mark passive state dirty,
- schedule same-tick bounded reaction work.

Reactive passives must not directly mutate `LivingEntityState`.

---

## 9 ActiveEffectSpec Output

Reactive passives emit normal `ActiveEffectSpec` records when they need to mutate RPG state.

Bloodsucker emits a heal spec:

```text
EffectDefinitionId: effect.raise.heal
TargetEntityId: Passive owner
SetByCaller Target.Value: AppliedDamage * RuntimeStackCount * 0.03
```

The referenced `EffectDefinition` remains immutable. The runtime spec carries the deterministic magnitude override.

Fairy Revival emits a revive spec:

```text
EffectDefinitionId: effect.health.revive
TargetEntityId: Passive owner
SetByCaller Target.Value: Health.EffectiveMaxValue * 0.50
```

`Health.EffectiveMaxValue` means the revive amount includes base Health and passive Health bonuses. It does not include temporary active max-health effects. If a future item or talent should revive from the final active capacity, use `Health.CurrentMaxValue` instead.

The StateEngine processes the emitted spec through normal active-effect validation, blocking, stat transaction, dirty-state, and result-event rules.

---

## 10 Stacking

The passive system has two stack layers.

### 10.1 Source-Level Stacking

`Stacking` controls how multiple sources generating the same passive combine.

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

The names are passive-specific where needed. Use `ByPassiveDefinitionId`, not `ByEffectDefinitionId`.

### 10.2 Runtime Stacking

`RuntimeStacking` controls event-driven stacks owned by one generated `PassiveEffect`.

Bloodsucker uses runtime stacks:

```text
MaxStacks: 10
ValuePerStack: 0.03
MaxOutputMultiplier: 0.30
StackDurationMs: 5000
ExpirationPolicy: RefreshAllOnGain
ApplyNewStackBeforeOutput: true
```

This separates "two sources grant Bloodsucker" from "Bloodsucker currently has 7 combat stacks".

---

## 11 Bloodsucker Example

Bloodsucker is player-facing passive gameplay, but mechanically it is not a stat-only passive.

It is a reactive passive:

```text
When the owner deals final applied damage:
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

---

## 12 Fairy Revival Example

Fairy Revival is a talent passive that reacts to death.

It is modeled as a source-bound reactive `PassiveEffect` with runtime cooldown:

```text
When Condition.Dead is added to the owner:
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

Recommended authored form:

```yaml
FairyRevival:
  Definition:
    PassiveDefinitionId: passive.talent.fairy_revival
    Name: Fairy Revival
    Description: Revives the owner on death for 50% passive-adjusted max Health, then enters a 5 minute cooldown.
  PassiveProcessing: Fast
  PassiveClassifications:
    - Passive.Reactive.Death
    - Passive.Cooldown.Revive
    - Passive.Source.Talent
  EvaluationPolicies:
    - EvaluationPolicy: OnEvent
      EventType: ConditionAdded
  Stacking:
    StackScope: ByPassiveDefinitionId
    StackPolicy: Discard
    StackBehavior: IndependentStacks
    MaxStacks: 1
    MaxStackOverflowPolicy: RejectNewest
  ReactiveTriggers:
    - ReactiveTriggerId: fairy_revival.on_dead
      EventType: ConditionAdded
      TriggerFilters:
        OwnerMatch: TargetEntityId
        EventCondition: Condition.Dead
      RuntimeCooldown:
        CooldownMs: 300000.0
        StartCooldownOn: OutputSuccess
      Outputs:
        - OutputKind: ActiveEffectSpec
          OutputTiming: SameTick
          ActiveEffectSpec:
            EffectDefinitionId: effect.health.revive
            TargetEntity: PassiveOwner
            SetByCaller:
              Field: Target.Value
              Formula:
                Stat: Health
                StatValueKind: EffectiveMaxValue
                Multiplier: 0.50
                ClampMin: 0.0
  Visibility: Hud
```

The StateEngine must process the emitted revive effect through the normal active-effect path. The revive effect is responsible for restoring Health and requesting removal of `Condition.Dead` according to active-effect rules.

If Health remains `<= 0` after same-tick revive processing, the existing StateEngine derived-condition rule may reapply `Condition.Dead`.

Fairy Revival cooldown is not an active effect. It is passive runtime state:

```text
CooldownUntilNetworkTick
LastTriggeredNetworkTick
```

This cooldown must be included in snapshots and rollback/resimulation.

---

## 13 Result Event Queue

Reactive passive processing uses a bounded same-tick result-event queue.

Ordering:

```text
1. Original event production order from StateEngine resolution
2. TargetEntityId
3. PassiveInsertionSequence
4. PassiveRuntimeId
5. Emitted ActiveEffectSpec normal effect ordering
```

The StateEngine must define limits:

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

---

## 14 Snapshot and Replay

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

---

## 15 Validation

Passive validation happens at startup.

The validator must check:

- YAML parses successfully.
- `Definition.PassiveDefinitionId`, `Definition.Name`, and `Definition.Description` are present.
- A passive has at least one gameplay payload.
- Classification-only passives are queryable gameplay metadata, not UI-only metadata.
- Passive tags use `PassiveClassification` and canonical `Passive.*` names.
- Passive direct assignment fields use assignable passive tags.
- Passive selector fields may use queryable parent/category passive tags.
- Stat modifiers target `EffectiveMaxValue` only.
- Stat modifier target stats exist in the stat registry.
- Percentage values use decimal fractions.
- `EvaluationPolicies` are present and compatible with the passive payload.
- `OnEvent` policies specify an event type.
- `EveryMs` policies specify an interval greater than 100 ms.
- Runtime stack durations are greater than 100 ms when present.
- Runtime cooldown durations are greater than 100 ms when present.
- Source-level stacking fields are valid.
- `ByStackGroupId` requires `StackGroupId`.
- `MaxStacks`, when present, is greater than zero.
- Reactive outputs reference existing `EffectDefinitionId` values.
- Runtime set-by-caller fields target explicitly allowed override fields.
- Set-by-caller formulas reference valid event values or valid stat layer values.
- Event filters use valid tag domains.
- Generated runtime records have unique `PassiveRuntimeId` values.
- Passive runtime snapshots contain enough state to restore and resimulate.

Invalid passive definitions must fail fast before normal simulation.

---

## 16 Questionnaire Outcome

The following design questions were resolved:

| Question | Decision |
|---|---|
| Should passives support reactive behavior? | Yes. `PassiveDefinition` supports stat modifiers and reactive triggers. |
| Should passive tags share domains with effects or conditions? | No. Add `PassiveClassification` with root `Passive`. |
| Bloodsucker stack math? | 10 stacks, 3 percent per stack, 30 percent maximum. |
| Bloodsucker same-event stack timing? | Gain stack first, then calculate heal for that same event. |
| Bloodsucker stack lifetime? | Temporary shared stack window. |
| Bloodsucker stack duration? | 5000 ms. |
| Reactive passive output path? | Emit normal `ActiveEffectSpec`, not direct mutation. |
| Dynamic heal amount? | Use set-by-caller runtime magnitude override. |
| Damage value used? | Final applied damage after mitigation and clamping. |
| Self/reflected damage? | Exclude self-damage; allow reflected/retaliation damage if credited to owner. |
| Heal feedback loop? | Allowed through normal result-event pipeline with limits. |
| Runtime ownership? | Generate `PassiveEffect` processed by StateEngine. |
| Evaluation cadence? | Explicit `EvaluationPolicy`. |
| Runtime storage? | StateEngine-owned passive runtime buffers keyed by target. |
| Passive lifetime? | Source-bound. |
| Duplicate passive sources? | Use existing stack mechanic concepts adapted to passives. |
| Passive stack scopes? | `ByPassiveDefinitionId`, `SameSource`, `SameSourceAndOrigin`, `ByStackGroupId`. |
| Passive stack policies? | Same concepts as active effects. |
| Two stack layers? | Yes. `Stacking` for source stacking, `RuntimeStacking` for event-driven stacks. |
| Definition validity? | Require at least one payload and validate all static data at startup. |
| Reactive output timing? | Same deterministic tick through bounded result-event queue. |
| Same-event reaction ordering? | `TargetEntityId`, `PassiveInsertionSequence`, `PassiveRuntimeId`, then normal effect ordering. |
| Passive runtime snapshots? | Include active passive runtime state in snapshots. |
| Fairy Revival modeling? | Reactive passive with runtime cooldown that emits a same-tick revive `ActiveEffectSpec`. |
| Fairy Revival max-health basis? | `Health.EffectiveMaxValue` for passive-adjusted max Health. Use `CurrentMaxValue` only if active capacity buffs should count. |

---

## 17 Open Follow-Up Questions

These are intentionally left for future implementation passes:

- Should passive output overflow reject or defer when the same-tick reaction queue is full?
- Should passive event subscriptions be precompiled per event type for cache locality?
- Should stat-only passives use a smaller runtime record than reactive passives?
- Should passive source systems provide source slot ordering, and should that ordering participate in tie-breakers?
- Should UI expose source-level stacks and runtime stacks separately?
- Should `PassiveDefinition` support child passive definitions in v1 or keep them future-only?
- Should emitted `ActiveEffectSpec` support a general set-by-caller payload map or only `Target.Value` in v1?
- Should passive runtime IDs be globally unique or entity-local plus insertion sequence?

---

## Final Contract

Passives are immutable definitions that generate source-bound `PassiveEffect` runtime records processed by the StateEngine passive path.

Stat passives resolve into `EffectiveMaxValue`. Reactive passives consume deterministic result events, update `PassiveRuntime`, and emit normal active effect specs when they need state mutation.

Passive metadata uses the separate `PassiveClassification` gameplay tag domain. Passive tags are not living entity conditions and are not active effect classifications.

The system must be deterministic, startup validated, source-bound, snapshot-backed, rollback-safe, and allocation-conscious.
