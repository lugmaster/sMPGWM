# CombatResolver.md

**Version:** 0.1.0
**Engine:** Godot 4.6.2
**Language:** C# / Mono
**Genre:** 2D Space RPG
**Network Model:** Host-authoritative deterministic combat resolution
**Network Tick Rate:** 30 TPS
**Status:** Seed document for the next spec round
**Compatible With:** `ActionSystem.md` version 1.0.0, `FLU_RPG_StateEngine_PRD.md` version 6.4.1

---

## 1. Purpose

`CombatResolver` is the proposed authoritative pipeline between `ActionSystem` and `StateEngine`.

This document is intentionally not a complete combat formula specification. It captures the decisions already made while refining `ActionSystem.md`, so the next spec round can continue from a stable boundary.

Core idea:

```text
ActionSystem decides whether something was hit.
CombatResolver decides what that hit means numerically.
StateEngine applies the final resolved RPG state mutations.
```

---

## 2. Agreed Boundary

`ActionSystem` owns:

- action validation,
- target acquisition,
- hit detection,
- projectile simulation,
- cooldown commit,
- resource reservation,
- `EffectIntent` emission.

`CombatResolver` owns:

- deterministic intent resolution,
- damage math,
- heal math when heal scaling or crit-heal rules exist,
- crit rolls,
- armor and resistance handling,
- shield and health routing,
- immunity and mitigation result records when they are combat-specific,
- final applied amount calculation,
- gameplay-authoritative combat result events,
- resolved `ActiveEffectSpec` emission.

`StateEngine` owns:

- applying resolved effect specs,
- mutating `LivingEntityState`,
- active-effect stacking and lifetime,
- condition contributor ownership,
- passive runtime processing,
- final deltas,
- state snapshots.

---

## 3. Why CombatResolver Exists

The current effect model can apply a resolved stat change:

```yaml
TargetStat: Health
TargetValueKind: CurrentValue
Manipulation: Reduce
Calculation: Flat
Value: 50.0
```

That is not enough to answer combat questions such as:

- Was this a critical hit?
- Which damage kind applies?
- Did armor reduce the hit?
- Did resistance reduce or amplify the hit?
- Did shield absorb damage before Health?
- Did invulnerability, immunity, or friendly-fire policy reject the result?
- How much final applied damage should Bloodsucker consume?
- What should be written to combat log?

Those are combat resolution responsibilities, not target acquisition responsibilities and not generic active-effect lifecycle responsibilities.

---

## 4. Phase Order

Recommended authoritative tick phase:

```text
1. StateEngine pre-resolve phase
   - resolve passive dirty state
   - resolve active modifiers already active at tick start
   - expose read-only combat view

2. ActionSystem phase
   - validate actions and cooldowns
   - acquire targets and resolve hits
   - emit EffectIntent records

3. CombatResolver phase
   - read tick-start CombatReadView only
   - consume EffectIntent records
   - emit CombatResultEvent records
   - emit resolved ActiveEffectSpec records

4. StateEngine apply phase
   - consume resolved specs
   - mutate state
   - emit deltas and snapshots
```

Effects applied in a tick do not affect later combat resolution in the same tick. They affect the next tick.

This avoids cross-calls between `CombatResolver` and `StateEngine` in one simulation step.

---

## 5. CombatReadView

`CombatResolver` should not scan arbitrary active effects per hit. It should read compact tick-start values.

Suggested read-only view:

```text
EntityId -> dense entity index
CurrentValues[]
CurrentMaxValues[]
EffectiveMaxValues[]
Condition masks
active effect classification masks when needed
Armor
CriticalHitChance
CriticalHitBonusDamage
BonusCriticalHitChance
LifeLeech
RetaliationDamage
CooldownReduction when needed by non-combat resolver logic
BonusDamageKinetic
BonusDamageThermal
BonusDamageExplosive
ResistanceKinetic
ResistanceThermal
ResistanceExplosive
Faction relation lookup
```

The view is immutable during the resolver phase.

---

## 6. EffectIntent

`EffectIntent` is the general input record.

Possible v1 intent kinds:

```text
Damage
Heal
ResourceChange
ApplyEffect
Cleanse
Revive
Interrupt
```

Suggested common fields:

```text
EffectIntentId
IntentKind
NetworkTick
ActionRuntimeId
ActionDefinitionId
SourceEntityId
TargetEntityId
SourceId
OriginId
HitSequence
TargetSequence
PayloadId
SetByCallerValues
IntentFlags
```

---

## 7. DamageIntent

`DamageIntent` is one intent type, not the whole resolver.

Suggested fields:

```text
DamagePayloadId
BaseDamage
DamageKind
CanCrit
CriticalPolicy
ShieldRoutingPolicy
ArmorPolicy
ResistancePolicy
FriendlyFirePolicySnapshot
```

Open for next spec:

```text
Should shield routing happen before armor, after armor, or by damage kind?
Should armor be flat reduction, percentage mitigation, rating curve, or hybrid?
Should resistance use decimal fraction, rating curve, or additive/multiplicative layers?
Should crit chance be clamped, overflow-converted, or allowed above 100 percent?
Should shield overflow spill to Health by default?
```

---

## 8. CombatResultEvent

`CombatResultEvent` is gameplay-authoritative deterministic resolver output.

It can drive:

- reactive passives,
- Bloodsucker-style final-applied-damage healing,
- retaliation,
- combat log,
- floating combat text,
- replay diagnostics,
- analytics.

Suggested fields:

```text
NetworkTick
ActionRuntimeId
SourceEntityId
TargetEntityId
IntentKind
DamageKind
RawAmount
BonusAdjustedAmount
CriticalApplied
CriticalMultiplier
MitigatedAmount
ShieldDamage
HealthDamage
FinalAppliedAmount
RejectedReason
HitSequence
TargetSequence
```

The detailed presentation combat log should be optional and ring-buffered. Gameplay-critical result events must be retained for deterministic passive processing and rollback/resimulation.

---

## 9. Resolved ActiveEffectSpec Output

After resolution, `CombatResolver` emits one or more `ActiveEffectSpec` records for `StateEngine`.

Because the current effect model has a single stat target per effect, a damage result that affects Shield and Health should emit separate specs:

```text
Reduce Shield.CurrentValue by shield damage
Reduce Health.CurrentValue by health damage
```

The specs carry resolved values. `StateEngine` applies them through the normal effect path, including blocking, stacking, condition requests, dirty tracking, result events, and deltas.

Open for next spec:

```text
Should resolved damage specs bypass generic effect blocking if blocking was already handled by CombatResolver?
Should combat-specific immunity be resolved before specs are emitted, or stay as effect blocking?
Should damage-to-shield and damage-to-health use separate EffectDefinitionIds?
Should final damage use set-by-caller Target.Value overrides?
```

---

## 10. Deterministic Randomness

Combat randomness belongs in `CombatResolver`.

Examples:

- critical hits,
- proc chance,
- random damage variance if allowed,
- randomized chain modifiers if allowed.

Random seeds must be deterministic and rollback-safe.

Suggested seed inputs:

```text
HostCombatSeed
NetworkTick
ActionRuntimeId
EffectIntentId
HitSequence
TargetEntityId
DamageSequence
```

The next spec must define whether any combat randomness is allowed in v1 and exactly how random streams are separated.

---

## 11. Snapshot and Replay

Combat resolver state must be snapshot/replay safe.

Likely retained data:

```text
EffectIntent history
CombatResultEvent history
Combat RNG state or reproducible seed inputs
resolved ActiveEffectSpec history
rejection records
resolver insertion sequences
```

Given identical:

```text
tick-start state
EffectIntent records
definition registries
tag registry hash
resolver config
random seed inputs
NetworkTick range
```

the resolver must produce identical result events and resolved specs.

---

## 12. Relationship With Passives

The StateEngine PRD already defines reactive passives such as Bloodsucker consuming final applied damage after mitigation and clamping.

`CombatResultEvent` is a good source for those reactions because it records final applied values before presentation filtering.

Open for next spec:

```text
Should reactive passives consume CombatResultEvent directly, or should StateEngine translate resolver events into its existing result-event queue?
Should CombatResultEvent and StateEngine result events share one bounded queue?
How should event ordering work when a resolved effect later fails StateEngine blocking?
```

---

## 13. Initial Contract

The next complete `CombatResolver` spec should define:

- intent schemas,
- damage payload schemas,
- shield routing,
- armor formulas,
- resistance formulas,
- critical hit formulas,
- bonus damage layers,
- immunity and rejection rules,
- final applied damage event rules,
- output `ActiveEffectSpec` construction,
- deterministic ordering,
- deterministic RNG,
- snapshot contents,
- validation rules,
- acceptance tests.

Until then, `ActionSystem.md` should treat `CombatResolver` as an authoritative sibling pipeline that consumes `EffectIntent` records and produces gameplay-authoritative `CombatResultEvent` and resolved `ActiveEffectSpec` records.

