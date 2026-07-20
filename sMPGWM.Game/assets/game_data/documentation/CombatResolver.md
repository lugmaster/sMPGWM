# CombatResolver.md

**Version:** 1.0.0
**Engine:** Godot 4.6.2
**Language:** C# / Mono
**Genre:** 2D Space RPG
**Network Model:** Host-authoritative deterministic combat resolution
**Network Tick Rate:** 30 TPS
**Status:** Initial complete v1 contract
**Compatible With:** `ActionSystem.md` version 1.0.0+, `FLU_RPG_StateEngine_PRD.md` version 6.4.1

---

## 1. Purpose

`CombatResolver` is the authoritative deterministic pipeline between `ActionSystem` and `StateEngine` for unresolved combat payloads.

Core rule:

```text
ActionSystem decides whether something was delivered.
CombatResolver decides what unresolved damage means numerically.
StateEngine applies final resolved RPG state mutations.
```

The resolver exists because damage is not just a resolved `Health.CurrentValue` reduction. Damage may involve:

- damage kind,
- source-side bonus damage,
- critical hits,
- resistance,
- armor,
- Shield routing,
- Shield bypass,
- armor bypass,
- resistance bypass,
- final applied damage events,
- reactive passives such as Bloodsucker or AcidAttacks,
- combat log records.

These are combat resolution responsibilities, not targeting responsibilities and not generic active-effect lifecycle responsibilities.

---

## 2. V1 Scope

### 2.1 In Scope

V1 covers:

- `EffectKind: Damage`,
- damage effect definitions,
- damage `EffectIntent` runtime envelopes,
- deterministic damage sorting,
- deterministic critical hit rolls,
- resistance rating-curve mitigation,
- armor rating-curve mitigation,
- Shield-first routing after resistance and before armor,
- `ShieldBypassFraction`,
- `ArmorBypassFraction`,
- `ResistanceBypassFraction`,
- same-tick pending Shield and Health ledgers,
- resolved `ActiveEffectSpec` output,
- StateEngine-confirmed combat result events,
- reactive damage passive rules,
- action cost boundary,
- deterministic replay and snapshot data,
- validation rules,
- YAML examples for actions, effect intents, damage effects, costs, and passives.

### 2.2 Out of Scope

V1 does not define:

- target acquisition,
- hit validation,
- projectile simulation,
- cooldown commit,
- resource reservation,
- action request validation,
- transport delivery,
- final StateEngine delta format,
- UI combat text formatting,
- non-damage resolver math for healing, cleanse, revive, interruption, or generic resource drains.

Non-damage effects may be represented by `ActiveEffectSpec` directly when their final mutation is already known.

---

## 3. System Boundary

### 3.1 ActionSystem Owns

`ActionSystem` owns:

- action validation,
- source ownership validation,
- cooldown checks and commit,
- resource availability checks,
- resource reservation,
- action cost resolution,
- target acquisition,
- hit detection,
- projectile simulation,
- friendly-fire target filtering,
- `EffectIntent` emission for unresolved damage effects,
- direct internal `ActiveEffectSpec` emission for resolved costs and already resolved effects.

`ActionSystem` does not mutate `LivingEntityState`.

### 3.2 CombatResolver Owns

`CombatResolver` owns:

- deterministic damage intent ordering,
- damage effect lookup,
- source combat stat reads from tick-start views,
- target combat stat reads from tick-start views,
- critical hit RNG,
- source bonus damage application,
- resistance mitigation,
- armor mitigation,
- Shield routing,
- bypass policy execution,
- same-tick pending Shield and Health ledgers,
- pending combat result records,
- resolved damage `ActiveEffectSpec` output.

`CombatResolver` does not mutate `LivingEntityState`.

### 3.3 StateEngine Owns

`StateEngine` owns:

- accepting resolved `ActiveEffectSpec` records,
- creating accepted `ActiveEffect` runtime instances where needed,
- applying stat transactions,
- active-effect stacking and lifetime,
- active-effect blocking validation,
- condition contributor ownership,
- passive runtime processing,
- confirmed result events,
- final deltas,
- snapshots.

Only `StateEngine` mutates RPG state arrays, active effect buffers, condition state, passive runtime state, and deltas.

---

## 4. Core Terms

### 4.1 EffectDefinition

`EffectDefinition` is immutable authored gameplay data.

For this resolver contract, a damage definition is explicit:

```yaml
EffectKind: Damage
Damage:
  DamageKind: Explosive
  BaseDamage: 100.0
  CanCrit: true
  CriticalRollScope: PerAction
  ShieldBypassFraction: 0.0
  ArmorBypassFraction: 0.15
  ResistanceBypassFraction: 0.0
```

Metadata tags such as `Effect.Damage.Explosive` may describe or classify the effect, but tags must not define executable damage behavior by themselves.

### 4.2 EffectIntent

`EffectIntent` is the runtime delivery envelope for an unresolved effect.

V1 uses `EffectIntent` for unresolved damage only.

It carries runtime facts:

```text
which effect definition was delivered
who caused it
who was hit
which action delivered it
which hit and target sequence produced it
which runtime overrides apply
which root action/effect created it when reactive
```

It does not duplicate damage formulas that already live in the referenced `EffectDefinition`.

### 4.3 ActiveEffectSpec

`ActiveEffectSpec` is resolved input for `StateEngine`.

Use it directly when the final stat mutation is already known:

```text
Action energy cost
Fixed heal
Fixed shield boost
Revive with already resolved amount
Cleanse or effect-removal request
Buff/debuff application
Condition-only effect
```

Use `EffectIntent` when a resolver must calculate the final mutation from combat rules.

### 4.4 ActiveRuntimeEffect

`ActiveRuntimeEffect` is the StateEngine-owned accepted runtime instance after an `ActiveEffectSpec` is accepted.

It owns runtime lifetime, trigger state, stacking, blocking retry behavior, condition ownership, interval progress, removal behavior, and deterministic ordering state.

### 4.5 Clean Rule

```text
Use ActiveEffectSpec when the final mutation is already known.
Use EffectIntent when unresolved damage must be resolved by CombatResolver.
```

This prevents `EffectIntent` from becoming a second effect system.

---

## 5. EffectKind

`EffectKind` is the executable discriminator for authored effect behavior.

V1 values relevant to this document:

| EffectKind | Meaning | Runtime path |
|---|---|---|
| `Damage` | Unresolved damage with combat rules. | `EffectIntent` -> `CombatResolver` -> resolved `ActiveEffectSpec`. |
| `StatChange` | Already resolved stat mutation. | `ActiveEffectSpec` -> `StateEngine`. |

Existing StateEngine container, condition, removal, and child-effect behavior remains governed by the StateEngine effect schema. Future documents may expand `EffectKind` names for those behaviors, but v1 `CombatResolver` only requires `Damage` and resolved `StatChange`.

Rules:

- `EffectKind: Damage` must define a `Damage` section.
- `EffectKind: Damage` must not define direct `Target`.
- Damage to Shield and Health is emitted by `CombatResolver` as separate resolved `StatChange` specs.
- `EffectKind: StatChange` may define the normal StateEngine `Target`.
- `EffectClassifications` remain metadata only.

---

## 6. Authoritative Host Architecture

The recommended host path is a staged, buffer-driven pipeline:

```text
ActionRequestBuffer
-> ActionSystem
-> EffectIntentBuffer
-> CombatResolver
-> ResolvedCombatSpecBuffer
-> StateEngine
-> AppliedEffectResultBuffer
-> CombatResultFinalizer
-> ResultEventQueue
```

The host is authoritative. Clients may predict where facts are known, but the host result wins.

### 6.1 Performance Shape

Use data-oriented buffers:

```text
EffectIntentBuffer
SortedEffectIntentIndices
PendingDamageResultBuffer
ResolvedActiveEffectSpecBuffer
AppliedEffectResultBuffer
CombatResultEventBuffer
TargetPendingShieldLedger
TargetPendingHealthLedger
CombatRngScratch
RejectedCombatIntentBuffer
```

Hot path requirements:

- no LINQ,
- no per-hit allocation,
- no string lookup,
- no Godot collections,
- no dependency on physics callback order,
- dense integer definition IDs,
- dense entity indices,
- flat arrays and ring buffers,
- deterministic compact-in-place removal,
- fixed-capacity scratch buffers,
- deterministic overflow rejection.

### 6.2 Same-Tick Pending Ledgers

`CombatResolver` reads tick-start `CombatReadView`, but it must still resolve multiple hits against the same target in stable same-tick order.

Therefore it maintains resolver-local pending ledgers:

```text
pendingShieldRemaining[targetIndex]
pendingHealthRemaining[targetIndex]
```

Initialization:

```text
pendingShieldRemaining = tickStart Shield.CurrentValue
pendingHealthRemaining = tickStart Health.CurrentValue
```

When a resolved damage intent reserves Shield or Health damage, the pending ledgers are reduced in resolver order. This avoids two same-tick hits both assuming the same Shield points are available.

The ledgers are not authoritative state. They only calculate deterministic resolver output. `StateEngine` still applies the actual stat changes and confirms final applied amounts.

### 6.3 Blocking Read Consistency

Resolved damage specs still pass normal StateEngine validation and blocking.

To avoid routing damage through pending ledgers for an effect that is predictably blocked, `CombatResolver` must perform a read-only precheck for blocking facts available in `CombatReadView`, such as:

```text
Condition.Dead
Condition.Invulnerable
blocked active-effect classification masks
target existence
damage effect validation state
```

StateEngine remains the final authority. If StateEngine later rejects or clamps a spec, final combat events use StateEngine-confirmed applied values.

---

## 7. Phase Order

Per authoritative `NetworkTick`:

```text
1. StateEngine pre-resolve phase
   - apply dirty passive source changes
   - resolve passive EffectiveMaxValue changes
   - resolve active modifiers already active at tick start
   - expose read-only state and combat views

2. ActionSystem phase
   - validate actions
   - check cooldowns and resources
   - resolve final action costs
   - reserve resources
   - acquire targets and hits
   - update projectile runtime state
   - emit internal cost ActiveEffectSpec records
   - emit damage EffectIntent records

3. CombatResolver phase
   - read tick-start CombatReadView
   - sort EffectIntent records deterministically
   - resolve damage
   - reserve pending Shield and Health damage in resolver-local ledgers
   - emit pending combat result records
   - emit resolved ActiveEffectSpec records

4. StateEngine apply phase
   - apply internal cost specs
   - apply resolved damage specs
   - apply other direct specs
   - mutate LivingEntityState
   - derive conditions such as Dead and ShieldCooldown
   - produce applied effect result records
   - emit deltas and snapshots

5. CombatResultFinalizer / result-event phase
   - consume pending combat results and applied effect results
   - emit confirmed CombatResultEvent records
   - feed confirmed result events to reactive passives
   - enqueue passive-emitted specs or intents within same-tick limits
```

Effects applied in a tick do not change the tick-start combat view for later original action resolution in that same tick.

Reactive passive outputs are bounded same-tick work and use explicit depth and event flags.

---

## 8. CombatReadView

`CombatResolver` reads compact tick-start values. It must not scan arbitrary active effects per hit.

Required view data:

```text
EntityId -> dense entity index
CurrentValues[]
CurrentMaxValues[]
EffectiveMaxValues[]
Condition masks
active effect classification masks when needed by blocking
ShieldCooldown state
Armor.CurrentValue
CriticalHitChance.CurrentValue
CriticalHitBonusDamage.CurrentValue
BonusDamage<DamageKind>.CurrentValue
Resistance<DamageKind>.CurrentValue
Faction relation lookup when needed by resolver diagnostics
```

Valid crit stats:

```text
CriticalHitChance
CriticalHitBonusDamage
```

`BonusCriticalHitChance` is invalid in v1. Any previous document occurrence should be treated as a documentation error or migration input.

The view is immutable during the resolver phase.

---

## 9. Damage Effect Schema

Suggested v1 authored shape:

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.damage.explosive.grenade
    Name: Grenade Explosive Damage
    Description: Explosive damage resolved through CombatResolver.
  EffectKind: Damage
  EffectClassifications:
    - Effect.Damage.Explosive
  Damage:
    DamageKind: Explosive
    BaseDamage: 100.0
    CanCrit: true
    CriticalRollScope: PerAction
    ShieldBypassFraction: 0.0
    ArmorBypassFraction: 0.15
    ResistanceBypassFraction: 0.0
  Visibility: Hud
```

### 9.1 Damage Fields

| Field | Required | Meaning |
|---|---:|---|
| `DamageKind` | yes | Damage vocabulary entry such as `Kinetic`, `Thermal`, `Explosive`, or `Acid`. |
| `BaseDamage` | yes | Authored base amount before source bonuses and crit. |
| `CanCrit` | yes | Explicitly allows or blocks critical hit rolls. |
| `CriticalRollScope` | when `CanCrit: true` | Defines whether one roll is shared or each target rolls independently. |
| `ShieldBypassFraction` | yes | Fraction of post-resistance/post-crit damage that skips Shield. |
| `ArmorBypassFraction` | yes | Fraction of target Armor ignored before armor curve. |
| `ResistanceBypassFraction` | yes | Fraction of target damage-kind resistance ignored before resistance curve. |

Fractions are deterministic decimal values clamped by validation to:

```text
[0.0, 1.0]
```

### 9.2 CriticalRollScope

Supported v1 values:

| Value | Meaning |
|---|---|
| `PerTarget` | Each target hit rolls independently. Recommended default for ordinary AoE and chain damage. |
| `PerAction` | All target intents from the same action and same damage effect share one crit result. Useful for boss attacks, ultimates, and intentionally dramatic casts. |

Shared roll key for `PerAction`:

```text
ActionRuntimeId + EffectDefinitionId
```

Two fireballs cast in the same simulation tick do not crit together because they have different `ActionRuntimeId` values.

---

## 10. EffectIntent

V1 `EffectIntent` is a runtime envelope for unresolved damage.

Suggested fields:

```text
EffectIntentId
NetworkTick
EffectDefinitionId
ActionRuntimeId
ActionDefinitionId
SourceEntityId
TargetEntityId
SourceId
OriginId
HitSequence
TargetSequence
SetByCallerValues
IsReactiveDamage
ReactiveDepth
RootActionRuntimeId
RootEffectDefinitionId
IntentFlags
```

Ordering keys:

```text
NetworkTick
ActionRuntimeId
HitSequence
TargetSequence
EffectIntentId
TargetEntityId
EffectDefinitionId
```

Rules:

- The referenced `EffectDefinitionId` must exist.
- The referenced effect must have `EffectKind: Damage`.
- `EffectIntent` does not mutate state.
- `EffectIntent` does not own damage formulas.
- `SetByCallerValues` may override explicitly allowed runtime fields such as `Damage.BaseDamage`.
- Runtime overrides must be validated against an allowlist.

---

## 11. Damage Resolution Formula

### 11.1 Final V1 Order

V1 resolves damage in this order:

```text
1. Start with BaseDamage.
2. Apply source-side BonusDamage<DamageKind>.CurrentValue.
3. Resolve critical hit if CanCrit is true.
4. Apply ResistanceBypassFraction and resistance rating curve.
5. Apply ShieldBypassFraction split.
6. Route non-bypassed post-resistance damage into Shield first.
7. Spill remaining non-bypassed damage forward to Health.
8. Add shield-bypassed damage to the Health-routed amount.
9. Apply ArmorBypassFraction and armor rating curve to all Health-routed damage.
10. Emit resolved Shield and Health damage specs.
```

Implications:

```text
Resistance protects both Shield and Health.
Shield absorbs before armor.
Armor protects Health only.
ShieldBypassFraction skips Shield only.
ArmorBypassFraction controls armor piercing.
ResistanceBypassFraction controls resistance piercing.
```

### 11.2 Base and Bonus Damage

Base amount:

```text
baseDamage = Damage.BaseDamage
```

If `SetByCallerValues` overrides base damage:

```text
baseDamage = SetByCaller Damage.BaseDamage
```

Source-side damage bonus:

```text
bonusDamage = Source.BonusDamage<DamageKind>.CurrentValue
preCritDamage = max(0.0, baseDamage + bonusDamage)
```

V1 does not define generic percentage bonus damage. Add it later as an explicit stat or damage field if needed.

### 11.3 Critical Hits

Only damage definitions with `CanCrit: true` roll critical hits.

Effective crit chance:

```text
effectiveCritChance = clamp(Source.CriticalHitChance.CurrentValue, 0.0, 1.0)
```

Crit multiplier:

```text
critMultiplier = 1.0 + Source.CriticalHitBonusDamage.CurrentValue
```

If the deterministic roll succeeds:

```text
postCritDamage = preCritDamage * critMultiplier
```

If not:

```text
postCritDamage = preCritDamage
```

Rules:

- `CriticalHitChance` is literal chance.
- Effective crit chance is clamped to `[0, 1]`.
- No overflow crit conversion exists in v1.
- `CriticalHitBonusDamage` is an additive bonus multiplier.
- Simple deterministic uniform RNG is used.
- V1 uses deterministic float math and test tolerances rather than fixed-point quantization.

### 11.4 Resistance

Resistance uses the same rating-curve family as armor.

Default:

```text
ResistanceConstant = 100.0
```

Effective resistance:

```text
effectiveResistance = max(
    0.0,
    Target.Resistance<DamageKind>.CurrentValue * (1.0 - ResistanceBypassFraction)
)
```

Reduction:

```text
resistanceReduction = effectiveResistance / (effectiveResistance + ResistanceConstant)
```

Post-resistance damage:

```text
postResistanceDamage = postCritDamage * (1.0 - resistanceReduction)
```

Rules:

- Resistance cannot drop below `0` in v1.
- Negative resistance does not amplify damage in v1.
- `ResistanceConstant` defaults to `100.0`.
- `ResistanceConstant` remains separately configurable from `ArmorConstant`.

### 11.5 Shield Routing

Shield routing happens after resistance and before armor.

Split:

```text
shieldBypassedDamage = postResistanceDamage * ShieldBypassFraction
shieldRoutedDamage = postResistanceDamage - shieldBypassedDamage
```

If target has `Condition.ShieldCooldown`, Shield cannot absorb:

```text
shieldDamageResolved = 0.0
healthRoutedFromShield = shieldRoutedDamage
```

Otherwise:

```text
availableShield = pendingShieldRemaining[targetIndex]
shieldDamageResolved = min(shieldRoutedDamage, availableShield)
healthRoutedFromShield = shieldRoutedDamage - shieldDamageResolved
pendingShieldRemaining[targetIndex] -= shieldDamageResolved
```

All Health-routed damage:

```text
preArmorHealthDamage = shieldBypassedDamage + healthRoutedFromShield
```

### 11.6 Armor

Armor uses a configurable League-of-Legends-style rating curve.

Default:

```text
ArmorConstant = 100.0
```

Effective armor:

```text
effectiveArmor = max(
    0.0,
    Target.Armor.CurrentValue * (1.0 - ArmorBypassFraction)
)
```

Reduction:

```text
armorReduction = effectiveArmor / (effectiveArmor + ArmorConstant)
```

Health damage:

```text
healthDamageResolved = preArmorHealthDamage * (1.0 - armorReduction)
```

Then clamp through the pending Health ledger:

```text
healthDamageAppliedEstimate = min(healthDamageResolved, pendingHealthRemaining[targetIndex])
overkillEstimate = healthDamageResolved - healthDamageAppliedEstimate
pendingHealthRemaining[targetIndex] -= healthDamageAppliedEstimate
```

Rules:

- Armor cannot drop below `0` in v1.
- Negative armor does not amplify damage in v1.
- `ShieldBypassFraction` does not bypass armor.
- `ArmorBypassFraction` controls armor piercing.
- Final confirmed applied Health damage comes from StateEngine application results.

### 11.7 Minimum Damage

V1 allows `0` final damage.

No minimum `1` damage rule exists.

---

## 12. Resolved ActiveEffectSpec Output

A damage result can affect Shield and Health, but the StateEngine effect model is single-target.

Therefore `CombatResolver` emits separate resolved specs:

```text
Reduce Shield.CurrentValue by shieldDamageResolved
Reduce Health.CurrentValue by healthDamageResolved
```

Suggested fields carried on each resolved spec:

```text
CombatResolutionId
PendingCombatResultId
EffectDefinitionId
TargetEntityId
SourceEntityId
OriginId
SourceId
ActionRuntimeId
RootActionRuntimeId
RootEffectDefinitionId
SetByCaller Target.Value
ResolvedDamagePart: Shield | Health
```

Output specs must use internal resolved stat-change definitions.

Example resolved Shield spec:

```yaml
ActiveEffectSpec:
  EffectDefinitionId: effect.combat.resolved.shield_damage
  TargetEntityId: entity.enemy.42
  SourceEntityId: entity.player.17
  OriginId: entity.player.17
  SourceId: weapon.rocket_launcher
  ActionRuntimeId: action.runtime.901
  CombatResolutionId: combat.resolution.1201
  SetByCaller:
    Target.Value: 35.0
  ResolvedDamagePart: Shield
```

Example resolved Health spec:

```yaml
ActiveEffectSpec:
  EffectDefinitionId: effect.combat.resolved.health_damage
  TargetEntityId: entity.enemy.42
  SourceEntityId: entity.player.17
  OriginId: entity.player.17
  SourceId: weapon.rocket_launcher
  ActionRuntimeId: action.runtime.901
  CombatResolutionId: combat.resolution.1201
  SetByCaller:
    Target.Value: 18.0
  ResolvedDamagePart: Health
```

If the resolved amount is `0`, the implementation may omit the spec and still emit a pending combat result with zero resolved value.

---

## 13. StateEngine Confirmation and Combat Events

`CombatResolver` emits pending combat result records before StateEngine application.

Gameplay-authoritative `CombatResultEvent` records are emitted after StateEngine confirms actual applied changes.

This is required because StateEngine may:

- block an effect,
- clamp a pool transaction,
- reject malformed input,
- apply Shield cooldown rules,
- derive `Condition.Dead`,
- execute same-tick passive outputs.

### 13.1 PendingCombatResult

Suggested fields:

```text
PendingCombatResultId
CombatResolutionId
NetworkTick
EffectIntentId
EffectDefinitionId
ActionRuntimeId
ActionDefinitionId
SourceEntityId
TargetEntityId
SourceId
OriginId
DamageKind
BaseDamage
BonusDamage
PreCritDamage
CriticalApplied
CriticalRoll
CriticalChance
CriticalMultiplier
PostCritDamage
ResistanceBypassFraction
EffectiveResistance
ResistanceReduction
PostResistanceDamage
ShieldBypassFraction
ShieldDamageResolved
ArmorBypassFraction
EffectiveArmor
ArmorReduction
HealthDamageResolved
HealthDamageAppliedEstimate
OverkillEstimate
IsReactiveDamage
ReactiveDepth
RootActionRuntimeId
RootEffectDefinitionId
RejectedReason
HitSequence
TargetSequence
```

### 13.2 CombatResultEvent

Confirmed event fields:

```text
NetworkTick
CombatResolutionId
EffectIntentId
EffectDefinitionId
ActionRuntimeId
ActionDefinitionId
SourceEntityId
TargetEntityId
SourceId
OriginId
EffectKind
DamageKind
BaseDamage
BonusDamage
CriticalApplied
CriticalMultiplier
ResistanceReduction
ArmorReduction
ShieldDamageResolved
ShieldDamageApplied
HealthDamageResolved
HealthDamageApplied
OverkillAmount
FinalAppliedAmount
RejectedReason
IsReactiveDamage
ReactiveDepth
RootActionRuntimeId
RootEffectDefinitionId
HitSequence
TargetSequence
```

`FinalAppliedAmount` for damage is:

```text
ShieldDamageApplied + HealthDamageApplied
```

For passives that care about actual harm to hull/Health, use:

```text
HealthDamageApplied
```

### 13.3 DamageDealt and DamageTaken

`DamageDealt` and `DamageTaken` mirror the same resolved and applied fields from source and target perspectives.

Both include:

```text
ShieldDamageResolved
ShieldDamageApplied
HealthDamageResolved
HealthDamageApplied
OverkillAmount
```

---

## 14. Reactive Passives

Reactive passives consume StateEngine-confirmed result events, not preliminary resolver estimates.

This is necessary for correctness because passives such as Bloodsucker and AcidAttacks must use damage that was actually applied after mitigation, Shield, armor, blocking, and clamping.

### 14.1 Stat Passive Example: PerfidAttacks

`PerfidAttacks` is a stat passive.

It raises `CriticalHitChance` by `0.05`.

Flow:

```text
Passive source added
-> StateEngine passive pipeline updates CriticalHitChance
-> CombatResolver reads CriticalHitChance.CurrentValue from CombatReadView
-> crit-capable damage effects can roll against the new chance
```

No `EffectIntent` is emitted by this passive.

### 14.2 Reactive Damage Passive Example: AcidAttacks

`AcidAttacks` reacts to confirmed non-reactive Health damage.

Behavior:

```text
When owner deals confirmed Health damage:
1. Ignore events with IsReactiveDamage = true.
2. Use HealthDamageApplied after target Health clamping.
3. Emit a derived damage EffectIntent.
4. Set BaseDamage = HealthDamageApplied * 0.10.
5. Use Acid damage.
6. Bypass Shield.
7. Bypass 75 percent of Armor.
8. Do not trigger reactive damage passives from this reactive damage event.
```

Required reactive damage flags:

```text
IsReactiveDamage = true
ReactiveDepth = 1
RootActionRuntimeId = original action runtime
RootEffectDefinitionId = original damage effect
```

Global v1 reactive damage rules:

- Reactive damage does not trigger reactive damage passives.
- Reactive damage bypasses Shield by default.
- Reactive damage still emits `DamageDealt` and `DamageTaken` events.
- Non-damage reactive passives may react to reactive damage events if their filters match.
- Reactive damage uses its own derived `EffectIntentId` and effect identity.

This prevents recursive Shield bypass and damage loops while keeping logs and non-damage reactions accurate.

---

## 15. Action Costs

Action costs are not resolved by `CombatResolver`.

Cost flow:

```text
ActionSystem reads tick-start resource and cost-reduction stats.
ActionSystem resolves final cost.
ActionSystem reserves the final amount.
ActionSystem emits an internal resolved ActiveEffectSpec.
StateEngine applies the cost as an internal StatChange effect.
```

Cost specs are:

- internal,
- resolved before emission,
- unblockable except hard validation failure,
- applied by StateEngine,
- included in normal state deltas and snapshots.

Cost reduction stats are per resource pool:

```text
EnergyCostReduction
OxygenCostReduction
FoodCostReduction
WaterCostReduction
```

Formula:

```text
costMultiplier = max(0.0, 1.0 - CostReductionStat.CurrentValue)
finalCost = authoredCost * costMultiplier
```

Examples:

```text
EnergyCostReduction =  0.20 -> finalCost = 80 percent
EnergyCostReduction =  1.00 -> finalCost = 0 percent
EnergyCostReduction = -0.25 -> finalCost = 125 percent
```

Cost reduction may reach `1.0`, allowing temporary free casting.

Negative cost reduction increases cost.

---

## 16. Deterministic Randomness

Combat randomness belongs to `CombatResolver`.

V1 randomness:

```text
critical hit roll
```

V1 does not include:

```text
random damage variance
random resistance variance
random armor variance
random Shield variance
proc RNG outside critical hits
```

Suggested deterministic seed inputs:

```text
HostCombatSeed
NetworkTick
ActionRuntimeId
EffectDefinitionId
EffectIntentId
HitSequence
TargetSequence
TargetEntityId
CriticalRollScope
```

For `CriticalRollScope: PerAction`, use the shared roll key:

```text
HostCombatSeed
NetworkTick
ActionRuntimeId
EffectDefinitionId
```

The implementation may either store RNG state or regenerate rolls from deterministic seed inputs. Regeneration from seed inputs is preferred for rollback simplicity.

---

## 17. Snapshot and Replay

Combat resolution must be deterministic under rollback and resimulation.

Retained data:

```text
EffectIntent history
PendingCombatResult history
Resolved ActiveEffectSpec history
AppliedEffectResult references
CombatResultEvent history
Rejected combat intent records
Resolver insertion sequences
RNG seed inputs or RNG state
Reactive damage output records
```

Given identical:

```text
tick-start CombatReadView
EffectIntent records
EffectDefinition registry
StateEngine applied result records
resolver config
tag registry hash
random seed inputs
NetworkTick range
```

the resolver and finalizer must produce identical specs and events.

---

## 18. Config

Suggested `CombatResolverConfig` fields:

```text
ArmorConstant = 100.0
ResistanceConstant = 100.0
MaxEffectIntentsPerTick
MaxResolvedDamageSpecsPerTick
MaxPendingCombatResultsPerTick
MaxCombatResultEventsPerTick
MaxReactiveDamageOutputsPerTick
MaxReactiveDepth = 1 for damage-passive-to-damage-passive chains
AllowReactiveDamageToTriggerReactiveDamage = false
DefaultReactiveDamageShieldBypassFraction = 1.0
DeterministicFloatComparisonTolerance
CombatSnapshotRetentionTicks
EnableDetailedCombatLog
```

Defaults should align with StateEngine and ActionSystem retention windows unless there is a clear reason to diverge.

---

## 19. Validation

Startup validation must reject invalid combat data.

Damage effect validation:

- `EffectKind: Damage` includes `Damage`.
- `EffectKind: Damage` does not define direct `Target`.
- `DamageKind` exists.
- `BaseDamage` is present and non-negative.
- `CanCrit` is explicitly present.
- `CriticalRollScope` is present when `CanCrit: true`.
- `CriticalRollScope` is absent or ignored when `CanCrit: false`.
- `ShieldBypassFraction`, `ArmorBypassFraction`, and `ResistanceBypassFraction` are present.
- Bypass fractions are in `[0.0, 1.0]`.
- `DamageKind` has concrete stat mappings for `BonusDamage<DamageKind>` and `Resistance<DamageKind>`.
- Set-by-caller overrides target explicitly allowed fields.
- Direct `EffectClassifications` are metadata and use valid effect-classification tags.
- Damage output stat-change definitions exist.
- Damage output stat-change definitions target only `Shield.CurrentValue` or `Health.CurrentValue`.
- Reactive damage definitions that are emitted from damage passives use Shield bypass in v1.
- Reactive damage passives ignore `IsReactiveDamage` events.

Config validation:

- `ArmorConstant > 0`.
- `ResistanceConstant > 0`.
- buffer sizes are positive.
- overflow behavior is deterministic.
- snapshot retention is sufficient for rollback/resimulation.

Stat validation:

- `CriticalHitChance` exists.
- `CriticalHitBonusDamage` exists.
- `BonusCriticalHitChance` is not used by CombatResolver v1.
- `Armor` exists.
- `Shield` and `Health` exist.
- concrete `BonusDamage<DamageKind>` stats exist.
- concrete `Resistance<DamageKind>` stats exist.

---

## 20. YAML Examples

### 20.1 Action Delivering Damage

```yaml
ActionDefinition:
  Definition:
    ActionDefinitionId: skill.throw_magical_grenade
    Name: Throw Magical Grenade
    Description: Throws a grenade projectile that explodes on impact.
  ActionClassifications:
    - Action.Skill
    - Action.Projectile
    - Action.Offensive
  Activation:
    TargetRequirement: ValidPoint
    CommitPolicy: CommitOnActivationAccepted
    RefundPolicy: NoRefundAfterCommit
    RangeMs: 1200.0
    LineOfSightPolicy: RequireClearLine
  Cooldown:
    ActionCooldownId: cooldown.action.throw_magical_grenade
    CooldownGroupIds:
      - Cooldown.Skill
    CooldownMs: 5000.0
    CooldownReductionPolicy: SnapshotAtCommit
  Costs:
    - CostStat: EnergyStorage
      CostReductionStat: EnergyCostReduction
      TargetValueKind: CurrentValue
      Value: 20.0
      SpendEffectDefinitionId: effect.internal.cost.energy
  Delivery:
    DeliveryType: Projectile
    ProjectileDefinitionId: projectile.magical_grenade
  Payload:
    Effects:
      - EffectDefinitionId: effect.damage.explosive.grenade
```

### 20.2 Damage Effect

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.damage.explosive.grenade
    Name: Grenade Explosive Damage
    Description: Deals explosive damage resolved by CombatResolver.
  EffectKind: Damage
  EffectClassifications:
    - Effect.Damage.Explosive
  Damage:
    DamageKind: Explosive
    BaseDamage: 100.0
    CanCrit: true
    CriticalRollScope: PerAction
    ShieldBypassFraction: 0.0
    ArmorBypassFraction: 0.15
    ResistanceBypassFraction: 0.0
  Visibility: Hud
```

### 20.3 Runtime EffectIntent

```yaml
EffectIntent:
  EffectIntentId: intent.7001
  NetworkTick: 45120
  EffectDefinitionId: effect.damage.explosive.grenade
  ActionRuntimeId: action.runtime.901
  ActionDefinitionId: skill.throw_magical_grenade
  SourceEntityId: entity.player.17
  TargetEntityId: entity.enemy.42
  SourceId: skill.throw_magical_grenade
  OriginId: entity.player.17
  HitSequence: 0
  TargetSequence: 2
  IsReactiveDamage: false
  ReactiveDepth: 0
```

### 20.4 Resolved Damage Specs

```yaml
ResolvedActiveEffectSpecs:
  - EffectDefinitionId: effect.combat.resolved.shield_damage
    TargetEntityId: entity.enemy.42
    SourceEntityId: entity.player.17
    OriginId: entity.player.17
    SourceId: skill.throw_magical_grenade
    ActionRuntimeId: action.runtime.901
    CombatResolutionId: combat.resolution.1201
    ResolvedDamagePart: Shield
    SetByCaller:
      Target.Value: 35.0

  - EffectDefinitionId: effect.combat.resolved.health_damage
    TargetEntityId: entity.enemy.42
    SourceEntityId: entity.player.17
    OriginId: entity.player.17
    SourceId: skill.throw_magical_grenade
    ActionRuntimeId: action.runtime.901
    CombatResolutionId: combat.resolution.1201
    ResolvedDamagePart: Health
    SetByCaller:
      Target.Value: 18.0
```

### 20.5 Internal Energy Cost Effect

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.internal.cost.energy
    Name: Energy Cost
    Description: Spends already-reserved EnergyStorage.
  EffectKind: StatChange
  Trigger:
    TriggerType: Immediate
  Duration:
    DurationType: Once
  Target:
    TargetStat: EnergyStorage
    TargetValueKind: CurrentValue
    Manipulation: Reduce
    Calculation: Flat
    Value: 0.0
    ApplicationMode: Instant
  Internal:
    IsInternal: true
    IsUnblockable: true
  Visibility: Internal
```

Runtime cost spec:

```yaml
ActiveEffectSpec:
  EffectDefinitionId: effect.internal.cost.energy
  TargetEntityId: entity.player.17
  SourceEntityId: entity.player.17
  OriginId: entity.player.17
  SourceId: skill.throw_magical_grenade
  ActionRuntimeId: action.runtime.901
  SetByCaller:
    Target.Value: 20.0
```

### 20.6 PerfidAttacks Passive

```yaml
PassiveDefinition:
  Definition:
    PassiveDefinitionId: passive.perfid_attacks
    Name: Perfid Attacks
    Description: Raises critical hit chance by 5%.
  PassiveProcessing: Fast
  PassiveClassifications:
    - Passive.Stat.CriticalHitChance
  EvaluationPolicies:
    - EvaluationPolicy: OnSourceDirty
  StatModifiers:
    - TargetStat: CriticalHitChance
      TargetValueKind: EffectiveMaxValue
      Manipulation: Raise
      Calculation: Flat
      Value: 0.05
  Visibility: Menu
```

### 20.7 AcidAttacks Reactive Passive

Reactive acid damage effect:

```yaml
EffectDefinition:
  Definition:
    EffectDefinitionId: effect.damage.acid_attacks.reactive
    Name: Acid Attacks Bonus Damage
    Description: Deals acid damage based on confirmed Health damage dealt.
  EffectKind: Damage
  EffectClassifications:
    - Effect.Damage.Acid
    - Effect.ReactiveDamage
  Damage:
    DamageKind: Acid
    BaseDamage: 0.0
    CanCrit: false
    ShieldBypassFraction: 1.0
    ArmorBypassFraction: 0.75
    ResistanceBypassFraction: 0.0
  Visibility: Hud
```

Passive definition:

```yaml
PassiveDefinition:
  Definition:
    PassiveDefinitionId: passive.acid_attacks
    Name: Acid Attacks
    Description: Dealing Health damage deals bonus acid damage equal to 10% of confirmed Health damage.
  PassiveProcessing: Fast
  PassiveClassifications:
    - Passive.Reactive.DamageDealt
    - Passive.Source.Talent
  EvaluationPolicies:
    - EvaluationPolicy: OnEvent
      EventType: DamageDealt
  ReactiveTriggers:
    - ReactiveTriggerId: acid_attacks.on_damage_dealt
      EventType: DamageDealt
      TriggerFilters:
        OwnerMatch: SourceEntityId
        RequireHealthDamageApplied: true
        IgnoreReactiveDamage: true
      Outputs:
        - OutputKind: EffectIntent
          OutputTiming: SameTick
          EffectIntent:
            EffectDefinitionId: effect.damage.acid_attacks.reactive
            TargetEntity: EventTarget
            SourceEntity: PassiveOwner
            OriginEntity: PassiveOwner
            IsReactiveDamage: true
            ReactiveDepth: 1
            RootActionRuntimeId: Event.ActionRuntimeId
            RootEffectDefinitionId: Event.EffectDefinitionId
            SetByCaller:
              Field: Damage.BaseDamage
              Formula:
                EventValue: HealthDamageApplied
                Multiplier: 0.10
                ClampMin: 0.0
  Visibility: Hud
```

---

## 21. Acceptance Criteria

V1 is accepted when tests prove:

- `EffectKind: Damage` definitions validate.
- Damage definitions cannot define direct `Target`.
- `EffectIntent` references a valid damage definition.
- Direct resolved costs bypass CombatResolver.
- Cost reduction stats can reduce cost to `0`.
- Negative cost reduction increases final cost.
- `BonusCriticalHitChance` is not read by CombatResolver.
- `CriticalHitChance.CurrentValue` is clamped to `[0, 1]` for rolls.
- `CriticalHitBonusDamage.CurrentValue` controls crit multiplier.
- `CanCrit` is required on every damage definition.
- `CriticalRollScope` is required when `CanCrit: true`.
- `PerAction` crit roll is shared by `ActionRuntimeId + EffectDefinitionId`.
- Two same-tick actions do not share crit rolls.
- Resistance applies before Shield routing.
- Shield routing happens before armor.
- Armor applies to all Health-routed damage, including Shield-bypassed damage.
- Shield cannot absorb while `Condition.ShieldCooldown` is present.
- Armor and resistance use rating curves with default constants `100.0`.
- Armor and resistance cannot drop below `0`.
- Bypass fractions clamp to `[0, 1]`.
- Multiple same-tick hits against one target use pending ledgers deterministically.
- StateEngine-confirmed combat events use applied values after clamping.
- `DamageDealt` includes resolved and applied Shield/Health fields.
- AcidAttacks uses `HealthDamageApplied`, not raw or overkill damage.
- Reactive damage bypasses Shield by default.
- Reactive damage does not trigger reactive damage passives.
- Reactive damage still emits combat events for logs and non-damage passive reactions.
- Rollback/resimulation reproduces identical specs and events.
- Buffer overflow rejection is deterministic.
- Hot-path resolver processing avoids avoidable allocation.

---

## 22. Questionnaire Outcome

Resolved decisions:

| Topic | Decision |
|---|---|
| Need for `EffectIntent` | Keep it, but v1 uses it narrowly for unresolved damage. |
| Clean rule | `ActiveEffectSpec` for known mutations, `EffectIntent` for unresolved damage. |
| Executable discriminator | Use `EffectKind`. |
| Damage authoring | Damage is first-class `EffectKind: Damage`, not inferred from `TargetStat: Health`. |
| Damage target | Damage definitions do not define direct `Target`. |
| Metadata | `EffectClassifications` remain metadata only. |
| Damage formulas location | Damage resolver metadata lives on `EffectDefinition`. |
| Action boundary | Action knows defense behavior by choosing the effect definition it delivers. |
| Cost path | Costs are resolved by ActionSystem and emitted as internal `StatChange` specs. |
| Cost blocking | Committed costs are unblockable except hard validation failure. |
| Cost reduction | Per-pool cost reduction stats are read by ActionSystem. |
| Free casting | Cost reduction may reach `1.0`. |
| Cost increases | Negative cost reduction increases final cost. |
| Shield bypass | Decimal `ShieldBypassFraction` in `[0, 1]`. |
| Armor bypass | Decimal `ArmorBypassFraction` in `[0, 1]`. |
| Resistance bypass | Decimal `ResistanceBypassFraction` in `[0, 1]`. |
| Armor bypass math | Reduce effective armor before formula. |
| Resistance bypass math | Reduce effective resistance before formula. |
| Armor formula | Rating curve with configurable `ArmorConstant`, default `100.0`. |
| Resistance formula | Rating curve with configurable `ResistanceConstant`, default `100.0`. |
| Negative armor | Clamp effective armor to `0`. |
| Negative resistance | Clamp effective resistance to `0`. |
| Shield model | Resistance, then Shield, then armor for Health-routed damage. |
| Shield cooldown | Shield cannot absorb while `Condition.ShieldCooldown` is present. |
| Armor and Shield bypass | `ShieldBypassFraction` skips Shield only; armor still applies. |
| Crit stats | Use `CriticalHitChance` and `CriticalHitBonusDamage` only. |
| Invalid crit stat | `BonusCriticalHitChance` is invalid. |
| Crit flag location | `CanCrit` lives on the damage effect definition. |
| Crit explicitness | `CanCrit` is required for every damage definition. |
| Crit cap | Clamp effective crit chance to `[0, 1]`. |
| Crit RNG | Simple deterministic uniform RNG in v1. |
| Crit roll scope | Support `PerTarget` and `PerAction`. |
| PerAction key | `ActionRuntimeId + EffectDefinitionId`. |
| Minimum damage | Allow `0`. |
| Numeric model | Deterministic float math with tolerance in v1. |
| StateEngine blocking | Resolved damage specs still pass normal StateEngine validation/blocking. |
| Combat event timing | Final gameplay combat events are emitted after StateEngine confirms application. |
| Passive event source | Reactive passives consume confirmed result events. |
| Reactive damage loops | Reactive damage does not trigger reactive damage passives. |
| Reactive damage Shield | Reactive damage bypasses Shield by default. |
| Reactive damage events | Reactive damage still emits combat events marked `IsReactiveDamage`. |
| Non-damage reactions | Non-damage passives may react to reactive damage events. |
| AcidAttacks basis | Use `HealthDamageApplied` after clamping. |
| Damage event fields | Include resolved and applied Shield/Health values plus overkill. |
| Reactive identity | Derived effect identity with root action/effect references. |
| `DamageTaken` | Mirror `DamageDealt` fields from target perspective. |

---

## 23. Open Follow-Up Questions

Future passes may decide:

```text
Should healing ever use a resolver path for crit heals or healing power?
Should resource drains use resolver math or remain direct stat changes?
Should damage support percentage source-side bonuses in v1.1?
Should armor or resistance support negative vulnerability later?
Should crit overflow convert to bonus crit damage later?
Should reactive damage ever opt into Shield routing?
Should some boss passives opt into reactive damage chains with a higher depth limit?
Should deterministic combat amounts be quantized to fixed precision?
Should damage output specs become an atomic StateEngine transaction group?
Should ActionSystem support multiple payload phases such as launch, travel tick, impact, and expire?
```

---

## 24. Final Contract

`CombatResolver` is the authoritative host-side resolver for unresolved damage effects.

`ActionSystem` delivers runtime `EffectIntent` records when a damage effect reaches a target. `CombatResolver` resolves those intents using immutable damage definitions and tick-start combat views, then emits pending combat results and resolved `ActiveEffectSpec` records.

`StateEngine` applies the resolved specs and confirms actual applied Shield and Health changes. Confirmed combat result events then feed reactive passives, logs, diagnostics, analytics, and replay.

The system is deterministic, low-allocation, rollback-safe, effect-driven, and compatible with the existing host-authoritative StateEngine architecture.
