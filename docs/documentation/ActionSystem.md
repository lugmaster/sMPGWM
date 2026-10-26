# ActionSystem.md

**Version:** 1.0.0
**Engine:** Godot 4.6.2
**Language:** C# / Mono
**Genre:** 2D Space RPG
**Network Model:** Host-authoritative action simulation with client prediction and reconciliation support
**Network Tick Rate:** 30 TPS
**Primary Output:** Deterministic damage `EffectIntent`, resolved cost `ActiveEffectSpec`, projectile runtime state, action result records, and host acknowledgement data
**Compatible With:** `FLU_RPG_StateEngine_PRD.md` version 6.4.1, `StateEngine.md` version 6.4.1, `TagSystem.md` version 1.1.0, `CombatResolver.md` version 1.0.0
**Definition Type:** Initial action, cooldown, targeting, and projectile PRD

---

## 1. Purpose

`ActionSystem` is the authoritative gameplay layer that turns skills, active item uses, weapons, AI actions, scripts, and environment triggers into deterministic simulation outputs.

It owns:

- action eligibility,
- action target requirements,
- target acquisition and hit validation,
- authoritative projectile simulation,
- action cooldown state,
- action resource reservation,
- action commit and refund policy,
- action request history,
- action prediction and reconciliation metadata,
- damage `EffectIntent` output for `CombatResolver`,
- resolved internal cost effects for the `StateEngine`.

`ActionSystem` does not mutate `LivingEntityState` directly. It reads tick-start state, commits action runtime state, and emits normalized records that are later applied by the authoritative simulation pipeline.

The core boundary is:

```text
Skill / Active Item / Weapon / AI / Environment / Script
-> ActionSystem
-> TargetSystem / ProjectileSystem / CooldownLedger / ResourceReservationLedger
-> damage EffectIntent records
-> CombatResolver
-> resolved ActiveEffectSpec records
-> StateEngine
```

This preserves the existing `StateEngine` rule that target acquisition and hit validation are upstream systems, while giving skills and active items a deterministic way to trigger effects.

---

## 2. Design Goals

The system must be:

- deterministic,
- host-authoritative,
- rollback-safe,
- client-predictable where facts are known,
- low-allocation in hot paths,
- data-oriented,
- compatible with the existing effect-driven StateEngine,
- independent from rendering and presentation physics,
- suitable for hitscan weapons, projectiles, area skills, direct-target skills, and active items.

The first implementation must support projectiles from the beginning because rockets, torpedoes, and other projectile weapons are core gameplay features.

---

## 3. Scope

### 3.1 In Scope

`ActionSystem` covers:

- action definitions,
- skill and active item action binding,
- weapon action binding,
- action runtime IDs and request IDs,
- local player action request normalization,
- host authoritative validation,
- cooldown checks and cooldown start,
- cooldown groups,
- `CooldownReduction` snapshot-at-commit,
- resource checks and reservation,
- internal resource-cost effect emission,
- target requirements,
- target query shapes,
- target filters,
- relationship and friendly-fire filtering,
- line-of-sight policy,
- hitscan delivery,
- projectile delivery,
- projectile impact target acquisition,
- projectile penetration,
- chain target selection,
- action commit policy,
- refund policy,
- deterministic rejection reasons,
- action snapshots,
- projectile snapshots,
- client prediction policy,
- reconciliation metadata,
- action result records for combat log and diagnostics.

### 3.2 Out of Scope

`ActionSystem` must not own:

- mutation of `LivingEntityState`,
- stat-layer resolution,
- active-effect stacking,
- active-effect blocking,
- active-effect lifetime,
- condition contributor ownership,
- passive runtime processing,
- final damage formulas,
- crit formulas,
- armor and resistance formulas,
- final combat result math,
- final state deltas,
- transport-level packet delivery.

Damage resolving belongs to `CombatResolver`. Already resolved costs, buffs, heals, cleanses, revives, and other known mutations may be emitted as `ActiveEffectSpec` records directly.

---

## 4. Existing PRD Constraints Preserved

The existing StateEngine PRD states:

- `StateEngine` processes normalized simulation inputs, not raw player actions.
- `StateEngine` does not perform target acquisition.
- `TargetEntityId` on `ActiveEffect` assumes an upstream system already performed targeting, hit validation, and gameplay authorization.
- Skills, items, usable items, weapons, and damage targeting are out of scope for `StateEngine`.
- Only `StateEngine` may mutate RPG state arrays, conditions, active-effect buffers, passive runtime state, and deltas.
- Client prediction is allowed only when cooldown, resource, range, target, and blocking facts are known.
- Runtime decisions use `NetworkTick`, not wall-clock time.
- The hot path must avoid avoidable allocation.

`ActionSystem` fills the upstream gap without moving target acquisition into `StateEngine`.

---

## 5. Core Concepts

### 5.1 ActionDefinition

`ActionDefinition` is immutable authored data describing how an action is activated, validated, delivered, and converted into intents.

Examples:

```text
skill.throw_magical_grenade
skill.heal_staff_cast
weapon.light_laser
weapon.railgun
weapon.rocket_launcher
item.active.shield_booster
```

An action may be triggered by:

- a skill,
- an active skill on an item,
- a weapon,
- a ship system,
- an AI behavior,
- an environment script,
- a quest script.

### 5.2 ActionRuntimeId

`ActionRuntimeId` uniquely identifies one committed action runtime instance.

It is used for:

- deterministic ordering,
- projectile ownership,
- cooldown causality,
- combat result attribution,
- client request mapping,
- rollback and replay,
- combat log correlation.

### 5.3 ActionRequest

`ActionRequest` is the normalized runtime request submitted to the host or local prediction instance.

It contains input facts such as:

```text
ClientRequestId
SubmittedInputTick
RequestedActionDefinitionId
SourceEntityId
SourceSlotId
AimPoint
AimDirection
RequestedTargetEntityId
InputSequence
ClientPredictedActionRuntimeId
```

The host validates and either rejects or commits the request.

### 5.4 Target Context

Every committed action has at least one target context.

Target context is not always a living entity. Valid examples:

```text
Self entity
Target entity
Aim point
Impact point
Direction vector
Area center
Projectile runtime
```

A grenade can validly target an empty point. Its impact target set may later be empty. A healing staff requires a valid friendly living entity before the action commits.

### 5.5 EffectIntent

`EffectIntent` is a deterministic runtime delivery envelope for an unresolved effect.

V1 uses `EffectIntent` narrowly for unresolved damage:

```text
ActionSystem proves delivery.
ActionSystem emits EffectIntent with EffectDefinitionId and runtime hit facts.
CombatResolver looks up the referenced EffectKind: Damage definition.
CombatResolver emits resolved Shield/Health ActiveEffectSpec records.
```

Do not use `EffectIntent` for already resolved gameplay mutations. Use `ActiveEffectSpec` directly when the final mutation is already known.

### 5.6 ActiveEffectSpec

`ActiveEffectSpec` is the resolved effect input consumed by `StateEngine`.

`ActionSystem` emits internal cost specs directly because resource cost is resolved at action commit.

Clean rule:

```text
Use ActiveEffectSpec when the final mutation is already known.
Use EffectIntent when unresolved damage must be resolved by CombatResolver.
```

Examples that should usually go directly to `StateEngine` as `ActiveEffectSpec`:

```text
Energy cost
fixed heal
fixed shield boost
revive with already resolved amount
cleanse or effect-removal request
buff/debuff application
condition-only effect
```

Damage with crits, resistance, armor, Shield routing, bypass fractions, or final applied damage events should flow through `CombatResolver`.

---

## 6. High-Level Runtime Flow

Per authoritative `NetworkTick`:

1. Build a tick-start read view from already-resolved state.
2. Receive and sort `ActionRequest` records.
3. Validate source entity, action definition, slot, ownership, cooldowns, resource availability, target requirement, and range.
4. Resolve final action costs and reserve resources in the per-tick reservation ledger.
5. Commit accepted actions according to `CommitPolicy`.
6. Start action and group cooldowns.
7. Emit internal cost `ActiveEffectSpec` records.
8. Resolve hitscan target queries immediately.
9. Spawn or advance authoritative projectile runtime records.
10. Resolve projectile hit queries using swept shape or ray tests.
11. Emit damage `EffectIntent` records from hits, impacts, areas, chains, and direct actions when delivered effects require combat resolving.
12. Pass damage `EffectIntent` records to `CombatResolver`.
13. Pass resolved `ActiveEffectSpec` records to `StateEngine`.
14. Store action, cooldown, projectile, request, hit, result, and rejection history for rollback/resimulation.
15. Emit host acknowledgement and action result records.

The orchestrator owns phase ordering. Systems communicate through buffers, not arbitrary cross-calls.

---

## 7. Phase Ordering With StateEngine and CombatResolver

The recommended authoritative simulation phase order is:

```text
1. StateEngine pre-resolve phase
   - apply dirty passive source changes
   - resolve active modifiers already active at tick start
   - expose read-only state views

2. ActionSystem phase
   - validate actions
   - check cooldowns and resources
   - resolve final action costs from tick-start cost-reduction stats
   - perform target acquisition and hit detection
   - update cooldown and projectile runtime state
   - emit damage EffectIntent records and internal cost specs

3. CombatResolver phase
   - read tick-start combat view only
   - resolve EffectIntent records
   - emit CombatResultEvent and resolved ActiveEffectSpec records

4. StateEngine apply phase
   - apply internal cost specs and resolved gameplay specs
   - mutate LivingEntityState
   - process effects, conditions, passives, result events, deltas, and snapshots
```

Effects applied in the same `NetworkTick` do not affect combat resolution later in that same tick. They affect the next tick. This avoids action-order abuse and prevents cross-system feedback loops.

---

## 8. Authoring Model

### 8.1 Suggested ActionDefinition Shape

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

  Source:
    SourceType: Skill
    SourceId: skill.throw_magical_grenade

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

  Targeting:
    TargetRelationshipMask:
      - Hostile
    FriendlyFirePolicy: DisallowHarmfulEffectsOnFriendly
    AllowDead: false
    AllowUntargetable: false

  Payload:
    Effects:
      - EffectDefinitionId: effect.damage.explosive.grenade
```

### 8.2 Skill and Active Item Binding

Skills and active item abilities trigger actions by referencing `ActionDefinitionId`.

Examples:

```text
SkillDefinition -> ActionDefinitionId
ItemActiveSkillDefinition -> ActionDefinitionId
WeaponDefinition -> PrimaryActionDefinitionId
WeaponDefinition -> SecondaryActionDefinitionId
```

Item slot, ammo, durability, inventory ownership, and equipment rules are owned by item/inventory systems. `ActionSystem` receives only normalized source identity and validated source availability where possible, then performs authoritative action validation.

---

## 9. Target Requirements

`TargetRequirement` controls what must be valid before an action can commit.

Supported v1 values:

| Value | Meaning |
|---|---|
| `None` | No external target required. The source/self context is enough. |
| `ValidPoint` | The aimed world point is valid and within action constraints. |
| `ValidEntity` | A specific target entity exists and passes generic entity checks. |
| `ValidLivingEntity` | Target is a registered living entity. |
| `ValidFriendlyLivingEntity` | Target is a living entity and relation is friendly. |
| `ValidHostileLivingEntity` | Target is a living entity and relation is hostile. |
| `MinTargetsInShape` | A shape query must find at least `MinTargetCount` valid targets. |

`MinTargetsInShape` means "commit only if the target query finds enough valid targets in the selected shape."

Examples:

```text
Heal Nova:
TargetRequirement = MinTargetsInShape
MinTargetCount = 1
TargetShape = Radius
AllowedRelationship = Friendly

Magical Grenade:
TargetRequirement = ValidPoint
MinTargetsInShape is not used.
Empty impact is still a valid committed action.

Healing Staff:
TargetRequirement = ValidFriendlyLivingEntity
Invalid enemy or empty-space click is rejected before cooldown and cost commit.
```

---

## 10. Commit and Refund Policy

### 10.1 CommitPolicy

`CommitPolicy` defines when cooldown and cost are committed.

Supported v1 values:

| Value | Meaning |
|---|---|
| `CommitOnActivationAccepted` | Commit once source, cooldown, resource, range, and target requirement pass. |
| `CommitAfterTargetRequirementPassed` | Commit only after the strict target requirement passes. |
| `CommitOnProjectileLaunch` | Commit when projectile runtime is created. |

For most actions, `CommitOnActivationAccepted` and `CommitAfterTargetRequirementPassed` are equivalent after validation. The distinction is useful for authoring clarity and diagnostics.

### 10.2 RefundPolicy

`RefundPolicy` defines what happens after commit if later impact or effect application finds no useful target.

Supported v1 values:

| Value | Meaning |
|---|---|
| `NoRefundAfterCommit` | Once committed, cost and cooldown remain even if impact finds no target. |
| `RefundOnNoCommittedTarget` | Refund only if no target requirement was actually committed. |
| `RefundOnAuthoritativeRejectOnly` | Refund only when the host rejects the action before commit. |

Recommended default:

```text
NoRefundAfterCommit
```

Example:

```text
Magical grenade:
- Valid empty point commits action.
- Cooldown starts.
- Energy is spent.
- If explosion finds no enemies, no refund.

Healing staff:
- Invalid target fails before commit.
- No cooldown.
- No energy cost.
```

---

## 11. Cooldown System

### 11.1 Cooldown Tracks

Actions support both direct action cooldowns and shared cooldown groups.

An action is usable only when all required cooldown tracks are ready:

```text
ActionCooldownId
CooldownGroupIds[]
optional future category/global cooldowns
```

On commit, the action may start:

- its own action cooldown,
- one or more shared group cooldowns,
- future category cooldowns.

### 11.2 CooldownGroup Tags

Cooldown groups use the `CooldownGroup` gameplay tag domain from `TagSystem.md`.

Examples:

```text
Cooldown.Weapon.Light
Cooldown.Weapon.Heavy
Cooldown.Skill
Cooldown.Skill.Defensive
Cooldown.Item.Consumable
```

The tag system provides identity and compiled masks. It does not track time. Cooldown runtime state belongs to `ActionSystem`.

### 11.3 CooldownReduction

`CooldownReduction` is snapshotted when the cooldown starts.

Formula:

```text
finalCooldownMs = authoredCooldownMs * max(0.0, 1.0 - CooldownReduction.CurrentValue)
CooldownUntilNetworkTick = currentTick + MsToTicksCeil(finalCooldownMs)
```

Later changes to `CooldownReduction` do not affect already-running cooldowns unless a future explicit cooldown-modifying action or effect changes cooldown state.

### 11.4 CooldownLedger

Runtime cooldown state is stored in a compact, snapshot-backed `CooldownLedger`.

Suggested fields:

```text
OwnerEntityId
CooldownTrackId
CooldownTrackKind
SourceId
ActionRuntimeId
StartedNetworkTick
CooldownUntilNetworkTick
OriginalDurationTicks
FinalDurationTicks
CooldownReductionSnapshot
InsertionSequence
RuntimeFlags
```

The ledger must support deterministic lookup by owner plus cooldown track, without hot-path string lookup.

---

## 12. Resource Costs and Reservations

`ActionSystem` checks resources but does not mutate stats directly.

Resource commit uses tick-start state, per-resource cost-reduction stats, and a per-tick reservation ledger.

Supported v1 cost-reduction stats:

```text
EnergyCostReduction
OxygenCostReduction
FoodCostReduction
WaterCostReduction
```

Cost-reduction stats are `AttributeStat` values maintained by the StateEngine stat pipeline. They use decimal fractions.

Final cost:

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

Resource availability:

```text
availableForAction = tickStartCurrentValue - alreadyReservedThisTick
```

If enough resource remains after cost reduction:

1. Reserve the final amount.
2. Commit the action.
3. Emit an internal cost `ActiveEffectSpec`.
4. `StateEngine` applies the cost during the apply phase.

Internal cost specs must be unblockable except for hard validation failure. Gameplay blocking should be checked before action commit, not by blocking the cost effect.

Cost effect definitions are resolved `EffectKind: StatChange` effects, not damage intents.

Suggested cost spec:

```text
EffectDefinitionId: effect.internal.cost.energy
TargetEntityId: source entity
TargetStat: EnergyStorage
TargetValueKind: CurrentValue
Manipulation: Reduce
Calculation: Flat
Value: reserved final cost
SourceId: action source
OriginId: source entity
```

Cooldown and cost commit together.

---

## 13. Target System

### 13.1 Responsibility

The target system performs authoritative candidate acquisition and target filtering for actions and projectiles.

It owns:

- point validation,
- entity lookup,
- shape queries,
- ray and swept-shape queries,
- line-of-sight checks,
- relationship filtering,
- condition filtering,
- target tag filtering,
- deterministic ordering and caps.

It does not apply effects or mutate state.

### 13.2 Target Shapes

Supported v1 target shapes:

| Shape | Use |
|---|---|
| `Self` | Source entity. |
| `SingleEntity` | Direct target entity. |
| `Point` | World point, usually mouse or aim point. |
| `PointRadius` | Circle query around a point. |
| `Cone` | Cone from source, muzzle, or impact direction. |
| `Line` | Hitscan line, beam, railgun, or ray query. |
| `SweptCircle` | Projectile motion from previous position to next position with radius. |
| `SweptCapsule` | Projectile or beam-like swept volume. |
| `ChainFromImpact` | Deterministic target chain after initial hit or impact point. |

### 13.3 Target Query Output

Target queries write into preallocated buffers:

```text
TargetCandidate[]
TargetHit[]
ResolvedTarget[]
RejectedTarget[]
```

Suggested `TargetHit` fields:

```text
TargetEntityId
ColliderId
HitPoint
HitNormal
DistanceFromQueryOrigin
TravelFraction
RelationshipBit
TargetConditionMask
TargetClassificationMask
HitSequence
```

### 13.4 Deterministic Ordering

Target hits are ordered by shape-specific stable keys.

General ordering:

```text
Distance or travel fraction
TargetEntityId
ColliderId
HitSequence
```

Chain ordering:

```text
JumpIndex
Distance from previous chain target
TargetEntityId
ColliderId
```

The implementation must not depend on Godot collection iteration order.

---

## 14. Collision, Tags, Factions, and Target Filtering

### 14.1 CollisionManager Role

`CollisionManager.cs` currently provides collision layer and mask presets:

```text
Player
Enemy
Projectile
Structure
Asteroid
Pickup
Shield
EnergyWeapon
TargetScanner
```

This is suitable for broad physics filtering only.

It must not be the final source of faction or friendly-fire truth. `Player` vs `Enemy` is too coarse for multiplayer RPG rules such as allied players, neutral factions, pets, temporary mind control, PvP zones, and faction reputation.

### 14.2 TagSystem Role

`TagSystem.md` already defines relevant domains:

```text
ActionClassification
TargetClassification
CooldownGroup
FactionRelation
```

The tag system is suitable for authored metadata and compiled mask queries. The existing runtime implementation uses efficient `ulong[]` masks and precompiled query masks.

The tag system does not perform:

- world queries,
- physics queries,
- raycasts,
- faction calculation,
- line-of-sight checks.

### 14.3 FactionRelationService

Faction and team relationships are calculated by an authoritative `FactionRelationService`.

The hot-path result is a compact bit:

```text
TargetRelationshipBit:
Self     = 1 << 0
Friendly = 1 << 1
Hostile  = 1 << 2
Neutral  = 1 << 3
```

The authored filter stores an allowed relationship mask:

```text
TargetRelationshipMask = Self | Friendly | Hostile | Neutral
```

Hot-path check:

```text
if ((allowedRelationshipMask & candidateRelationshipBit) == 0)
    reject;
```

### 14.4 TargetFilter

`TargetFilter` combines:

- broad collision candidate mask,
- authoritative relationship bit,
- target classification tag queries,
- condition masks,
- line-of-sight result,
- friendly-fire policy,
- dead/untargetable flags,
- action-specific rules.

Suggested flags:

```text
AllowSelf
AllowDead
AllowUntargetable
RequireLivingEntity
RequireDetectable
RequireLineOfSight
```

---

## 15. Friendly Fire

Friendly fire is configured per action with a project default.

`TargetRelationshipMask` decides which targets are valid for acquisition. `FriendlyFirePolicy` decides whether harmful effects may affect friendly targets found by AoE or projectile impact.

Supported v1 values:

| Policy | Meaning |
|---|---|
| `UseProjectDefault` | Use global project rule. |
| `DisallowHarmfulEffectsOnFriendly` | Harmful intents are not emitted for friendly targets. |
| `AllowHarmfulEffectsOnFriendly` | Harmful intents may affect friendly targets. |
| `AllowOnlySelfDamage` | Allows self-damage but blocks harm to other friendly targets. |

Friendly fire is not determined only by collision layer.

---

## 16. Line of Sight and Occlusion

Line-of-sight is authored per action.

Supported v1 `LineOfSightPolicy`:

| Policy | Meaning |
|---|---|
| `None` | No LOS check. |
| `RequireClearLine` | Require unobstructed ray/shape line. |
| `IgnoreSoftCover` | Ignore configured soft blockers. |
| `BlockedByWorldOnly` | World/static blockers obstruct LOS. |
| `BlockedByWorldAndEntities` | World and entity blockers obstruct LOS. |

Recommended defaults:

```text
Self action: None
Nova/radius around self: None
Direct target spell: RequireClearLine
Hitscan weapon: BlockedByWorldAndEntities
Projectile launch: RequireClearLine to launch path when applicable
Projectile flight: collision/swept query owns obstruction
```

---

## 17. Delivery Types

Supported v1 `DeliveryType`:

| Type | Meaning |
|---|---|
| `InstantSelf` | Immediate self-target action. |
| `InstantTarget` | Immediate direct target action. |
| `InstantArea` | Immediate radius/cone/line query. |
| `Hitscan` | Authoritative ray, line, or shape query at activation tick. |
| `Projectile` | Authoritative projectile runtime simulated over ticks. |

The action owns delivery shape. The effect payload does not know whether it came from a cone, nova, mouse point, projectile, or railgun.

For area effects, `ActionSystem` emits one intent per resolved target. `EffectDefinition` remains single-target as required by the StateEngine PRD.

---

## 18. Hitscan

Hitscan is an immediate authoritative query at the activation tick.

Examples:

```text
Laser
Railgun with no travel time
Instant beam
Direct scan
```

Hitscan may use:

- ray query,
- line segment query,
- swept circle query,
- cone query,
- shape query.

Railgun-style behavior should use:

```text
DeliveryType: Hitscan
TargetShape: Line
PenetrationPolicy: PierceAllUntilRange
```

If the weapon has visible travel time, use `DeliveryType: Projectile` instead.

---

## 19. Projectile System

### 19.1 Principle

Gameplay projectiles are host-authoritative, tick-based, data-oriented runtime records.

Godot `Area2D`, `RigidBody2D`, and collision callbacks may support presentation, anticipation, debug, and local helper visuals. They are not the source of gameplay truth.

Authoritative projectile hits use deterministic swept ray or swept shape queries from previous position to next position.

### 19.2 ProjectileDefinition

Suggested fields:

```yaml
ProjectileDefinition:
  ProjectileDefinitionId: projectile.magical_grenade
  SpeedUnitsPerSecond: 650.0
  Radius: 8.0
  MaxLifetimeMs: 3000.0
  MaxRangeUnits: 1200.0
  CollisionMaskPreset: Projectile
  QueryShape: SweptCircle
  PenetrationPolicy: StopOnFirstHit
  ImpactTargetShape: ImpactRadius
  ImpactRadius: 180.0
  ImpactLineOfSightPolicy: BlockedByWorldOnly
```

### 19.3 ProjectileRuntime

Suggested compact runtime fields:

```text
ProjectileRuntimeId
ActionRuntimeId
ActionDefinitionId
ProjectileDefinitionId
SourceEntityId
OriginId
SourceId
LaunchNetworkTick
LastSimulatedNetworkTick
ExpiresNetworkTick
PreviousPosition
CurrentPosition
Velocity
Radius
RemainingRange
PenetrationRemaining
HitCount
InsertionSequence
ProjectileFlags
PayloadSnapshotId
```

The projectile carries an immutable payload snapshot:

```text
target filters
impact behavior
effect definition references
source and origin identity
friendly-fire policy
line-of-sight policy
deterministic random seed when needed
```

### 19.4 Projectile Hit Detection

Recommended query model:

```text
previousPosition -> nextPosition
SweptCircle or SweptCapsule query
Sort hits deterministically
Apply penetration policy
Resolve impact target shape
Emit damage EffectIntent records for unresolved damage effects
```

Fast projectiles should not rely on discrete overlap at the final position. Swept queries prevent tunneling.

### 19.5 Impact Target Shapes

Supported v1 `ImpactTargetShape`:

| Shape | Meaning |
|---|---|
| `DirectHitOnly` | Only the directly hit target receives payload. |
| `ImpactRadius` | Query targets around impact point. |
| `ConeFromImpact` | Query cone from impact point and impact direction. |
| `ChainFromImpact` | Build deterministic target chain from direct or impact target. |

Examples:

```text
Bullet:
ImpactTargetShape = DirectHitOnly

Rocket:
ImpactTargetShape = ImpactRadius

Fragmenting blast:
ImpactTargetShape = ConeFromImpact

Chain lightning:
ImpactTargetShape = ChainFromImpact
```

### 19.6 ChainFromImpact

Chain selection must be deterministic.

Suggested fields:

```text
MaxJumps
MaxRangePerJump
AllowRepeatTargets
RequireLineOfSightPerJump
FalloffPolicy
FalloffPerJump
```

Ordering:

```text
distance from previous target
TargetEntityId
ColliderId
```

Recommended default:

```text
AllowRepeatTargets = false
```

---

## 20. Penetration Policy

`PenetrationPolicy` controls how many direct hits a hitscan or projectile path may produce.

Supported v1 values:

| Policy | Meaning |
|---|---|
| `StopOnFirstHit` | First valid direct hit stops the path. |
| `PierceCount` | Hit up to a configured number of valid targets. |
| `PierceAllUntilRange` | Hit all valid targets along path until range/lifetime ends. |
| `StopOnWorldThenImpact` | Travel stops on world hit and then impact behavior resolves. |

Railgun example:

```text
DeliveryType: Hitscan
TargetShape: Line
PenetrationPolicy: PierceAllUntilRange
```

Rocket example:

```text
DeliveryType: Projectile
PenetrationPolicy: StopOnWorldThenImpact
ImpactTargetShape: ImpactRadius
```

---

## 21. EffectIntent Output

Actions emit damage intents only when the delivered effect requires combat resolving.

Suggested base fields:

```text
EffectIntentId
EffectDefinitionId
ActionRuntimeId
ActionDefinitionId
SourceEntityId
TargetEntityId
SourceId
OriginId
NetworkTick
HitSequence
TargetSequence
PayloadId
SetByCallerValues
IsReactiveDamage
ReactiveDepth
RootActionRuntimeId
RootEffectDefinitionId
IntentFlags
```

The referenced `EffectDefinitionId` must resolve to:

```text
EffectKind: Damage
```

Damage formulas and defense policy live on the referenced `EffectDefinition`, not on the action intent:

```text
DamageKind
BaseDamage
CanCrit
CriticalRollScope
ShieldBypassFraction
ArmorBypassFraction
ResistanceBypassFraction
```

Already resolved effects should be emitted as `ActiveEffectSpec` records instead of `EffectIntent` records.

Examples:

```text
Action cost -> ActiveEffectSpec
Fixed heal -> ActiveEffectSpec
Shield boost -> ActiveEffectSpec
Cleanse -> ActiveEffectSpec
Revive with resolved amount -> ActiveEffectSpec
Damage with crit/resistance/armor/Shield routing -> EffectIntent
```

Resolver math is specified in `CombatResolver.md` version 1.0.0.

---

## 22. Combat Result and Combat Log

`CombatResolver` creates pending combat result records and resolved damage specs. Gameplay-authoritative `CombatResultEvent` records are emitted after `StateEngine` confirms the actual applied Shield and Health changes.

These records may drive:

- reactive passives such as Bloodsucker,
- retaliation,
- combat log,
- floating combat text,
- analytics,
- replay diagnostics.

Suggested fields:

```text
NetworkTick
ActionRuntimeId
SourceEntityId
TargetEntityId
EffectIntentId
EffectDefinitionId
DamageKind
BaseDamage
CriticalApplied
CriticalMultiplier
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

The full detailed combat log should be optional and ring-buffered. Minimal deterministic events required by gameplay must remain in the authoritative simulation history.

---

## 23. Runtime Data Ownership

### 23.1 Owned by ActionSystem

```text
ActionRequestHistory
ActionRuntimeRecords
CooldownLedger
ResourceReservationLedger
ProjectileRuntimeRecords
ProjectileHitHistory
ActionRngState
ActionInsertionSequences
ActionRejectionRecords
ClientActionRequestMap
ActionResultRecords
```

### 23.2 Owned by StateEngine

```text
LivingEntityState
stat arrays
conditions
condition contributors
active effects
passive effects
passive runtime
shield cooldown
deltas
StateEngine snapshots
```

### 23.3 Shared Read-Only Inputs

The orchestrator may expose read-only views:

```text
ActionReadView
TargetReadView
CombatReadView
StateReadView
```

Read views must be stable for the phase and must not allow mutation.

---

## 24. Snapshot and Rollback

`ActionRuntimeState` is separate from `LivingEntityState`, but it is snapshot-backed beside StateEngine history.

Snapshots must include:

```text
CooldownLedger
ResourceReservationLedger
ActiveProjectiles
ProjectileHitHistory
ActionRequestHistory
ActionRuntimeRecords
ActionRngState
ActionInsertionSequences
Deterministic rejection records
Client request mapping
```

Action snapshots should use the same retention window as StateEngine by default:

```text
SnapshotRetentionTicks = 45
```

If an authoritative correction changes an action, projectile, cooldown, or result event at a retained tick, the orchestrator restores both StateEngine and ActionSystem snapshots and resimulates forward deterministically.

---

## 25. Deterministic Ordering

Action requests are processed in stable order:

```text
SubmittedInputTick
SourceEntityId
InputSequence
ClientRequestId
ActionDefinitionId
```

Committed action outputs use:

```text
NetworkTick
ActionInsertionSequence
ActionRuntimeId
HitSequence
TargetSequence
EffectIntentId
```

Projectile simulation uses:

```text
LaunchNetworkTick
ProjectileInsertionSequence
ProjectileRuntimeId
travel fraction
TargetEntityId
ColliderId
```

All overflow behavior must sort by documented stable keys, accept up to capacity, reject remaining records deterministically, and retain rejection metadata when diagnostics are enabled.

---

## 26. Client Prediction

Client prediction is selective.

The client may predict:

- local cooldown/resource reservation,
- self-target actions,
- direct valid-target actions with known state,
- hitscan actions when target facts are known,
- projectile launch visuals,
- projectile runtime when deterministic facts are available.

Projectile impact gameplay remains host-confirmed unless deterministic projectile state and target facts are fully known locally.

The client must send stable request identity:

```text
ClientRequestId
ClientActionSequence
SubmittedInputTick
SourceEntityId
ActionDefinitionId
RequestedTargetEntityId
AimPoint
AimDirection
```

The host responds with:

```text
ClientRequestId
AcceptedOrRejected
RejectionReason
AuthoritativeActionRuntimeId
AuthoritativeAppliedNetworkTick
StartedCooldownTracks
SpawnedProjectileRuntimeIds
```

If the host rejects or changes a predicted action, the client removes the predicted gameplay result and replays retained local prediction history.

Presentation may anticipate and later cancel/fade visuals. Gameplay state must follow the authoritative host result.

---

## 27. Performance Requirements

Hot path requirements:

- no LINQ in action/projectile/target hot loops,
- no per-tick temporary collection allocation,
- no string lookup in action processing,
- no Godot collections in authoritative hot path,
- no reliance on collision callback order,
- use dense integer IDs for definitions and runtime records,
- use flat arrays and ring buffers,
- use reusable scratch buffers,
- use bitmasks for relationship and tag checks,
- use deterministic compact-in-place removal,
- size buffers through config,
- reject overflow deterministically.

Required scratch buffers:

```text
PendingActionRequests
AcceptedActions
RejectedActions
CooldownChecks
ResourceReservations
TargetCandidates
TargetHits
ResolvedTargets
RejectedTargets
ProjectileStepBuffer
ProjectileHitBuffer
ImpactTargetBuffer
EffectIntentBuffer
InternalCostEffectBuffer
ActionResultEventBuffer
ClientActionAckBuffer
```

---

## 28. Validation Requirements

Startup validation must reject invalid action data.

Action validation checks:

- `ActionDefinitionId`, `Name`, and `Description` are present.
- referenced cooldown group tags exist and use `CooldownGroup` domain.
- action classification tags exist and use `ActionClassification` domain.
- target classification tags exist and use `TargetClassification` domain.
- referenced projectile definitions exist for `DeliveryType: Projectile`.
- non-projectile actions do not reference projectile-only fields.
- cooldown durations are greater than zero unless action intentionally has no cooldown.
- cooldown durations convert deterministically to ticks.
- `CooldownReductionPolicy` is valid.
- cost stats are valid pool stats when costs mutate pool values.
- cost reduction stats, when present, are valid cost-reduction attribute stats for the spent pool.
- cost effect definitions exist and are internal/unblockable.
- cost effect definitions use `EffectKind: StatChange`.
- payload effect definitions exist.
- payload effects delivered as `EffectIntent` use `EffectKind: Damage`.
- target requirement is compatible with delivery type.
- `MinTargetsInShape` includes a positive `MinTargetCount`.
- line-of-sight policy is valid.
- friendly-fire policy is valid.
- penetration policy is valid for hitscan/projectile delivery.
- impact target shape is valid for projectile delivery.
- chain settings have positive max jumps and range.
- referenced damage effects validate under `CombatResolver.md`.
- definitions do not require runtime string parsing in the hot path.

Invalid definitions must fail before simulation.

---

## 29. Config

Suggested initial config:

```text
MaxActionRequestsPerTick
MaxAcceptedActionsPerTick
MaxRejectedActionsPerTick
MaxCooldownTracksPerEntity
MaxCooldownGroupsPerAction
MaxCostsPerAction
MaxTargetCandidatesPerQuery
MaxResolvedTargetsPerAction
MaxActiveProjectiles
MaxProjectileHitsPerTick
MaxImpactTargetsPerProjectile
MaxChainJumps
MaxEffectIntentsPerTick
MaxInternalCostEffectsPerTick
MaxActionResultEventsPerTick
ActionSnapshotRetentionTicks
ProjectileSnapshotRetentionTicks
DefaultFriendlyFirePolicy
DefaultLineOfSightPolicy
ProjectileSimulationTicksPerSecond = 30
```

Defaults should align with `StateEngineConfig` retention and tick rate unless there is a clear reason to diverge.

---

## 30. Examples

### 30.1 Magical Grenade

```text
Input: click valid empty point.
TargetRequirement: ValidPoint.
CommitPolicy: CommitOnActivationAccepted.
RefundPolicy: NoRefundAfterCommit.
Cost: 20 EnergyStorage.
Cooldown: 5 seconds.
DeliveryType: Projectile.
ImpactTargetShape: ImpactRadius.
FriendlyFirePolicy: DisallowHarmfulEffectsOnFriendly.

Result:
- host accepts action,
- reserves energy,
- starts cooldown,
- emits internal energy cost spec,
- spawns projectile,
- projectile explodes at impact,
- impact query may find zero enemies,
- no refund after commit.
```

### 30.2 Healing Staff

```text
Input: click target.
TargetRequirement: ValidFriendlyLivingEntity.
CommitPolicy: CommitAfterTargetRequirementPassed.
Cooldown: 3 seconds.
DeliveryType: InstantTarget.

Result:
- enemy target or empty space fails before commit,
- no cost,
- no cooldown,
- valid friendly target commits and emits an already resolved healing `ActiveEffectSpec`.
```

### 30.3 Railgun

```text
DeliveryType: Hitscan.
TargetShape: Line.
PenetrationPolicy: PierceAllUntilRange.
TargetRelationshipMask: Hostile.
LineOfSightPolicy: BlockedByWorldAndEntities.

Result:
- host performs deterministic line query,
- hits are ordered by distance and EntityId,
- every valid hostile along the line emits a damage `EffectIntent`.
```

### 30.4 Chain Lightning

```text
DeliveryType: Hitscan or Projectile impact.
ImpactTargetShape: ChainFromImpact.
MaxJumps: 5.
MaxRangePerJump: 350 units.
AllowRepeatTargets: false.
TargetRelationshipMask: Hostile.

Result:
- first target is direct hit or nearest valid impact target,
- each next target is selected by distance then EntityId,
- each jump emits a separate damage `EffectIntent` with `HitSequence` and `TargetSequence`.
```

---

## 31. Acceptance Criteria

The v1 implementation is accepted when tests prove:

- skills and active items can trigger actions.
- actions do not mutate `LivingEntityState` directly.
- actions commit only when cooldown, resource, range, and target requirements pass.
- invalid healing target does not start cooldown or spend resource.
- grenade at valid empty point spends resource and starts cooldown.
- projectile launch is authoritative and snapshot-backed.
- projectile swept query prevents tunneling.
- projectile impact supports direct hit, radius, cone, and chain shapes.
- railgun line hit can affect every valid target along the line.
- friendly fire is controlled by action policy and project default.
- faction relationships use runtime relation bits, not collision layers only.
- collision masks are used only for broad query filtering.
- tag masks are used for authored target/action/cooldown metadata.
- line-of-sight policy is applied per action.
- cooldown groups block shared actions.
- `CooldownReduction` is snapshotted at commit.
- per-pool cost-reduction stats affect final reserved cost.
- cost reduction can reduce final cost to zero.
- negative cost reduction increases final cost.
- cost reservation prevents same-tick double-spending.
- internal cost effects are applied by StateEngine.
- unresolved delivered damage effects emit damage `EffectIntent` records.
- already resolved effects emit `ActiveEffectSpec` records directly.
- action runtime state restores and resimulates after rollback.
- client prediction removes rejected predicted actions during reconciliation.
- deterministic ordering is stable across replay.
- overflow rejection is deterministic.
- hot-path action processing has no avoidable allocation.

---

## 32. Questionnaire Outcome

Resolved design decisions:

| Question | Decision |
|---|---|
| Boundary with StateEngine | `ActionSystem` owns cooldowns, target acquisition, hit validation, and emits normalized records to the existing StateEngine pipeline. |
| Projectiles in v1 | Yes. Projectiles are core and included from the beginning. |
| Projectile hit source | Host-authoritative tick-based swept ray/shape queries. Godot callbacks are presentation/helpers only. |
| Projectile impact target shapes | `DirectHitOnly`, `ImpactRadius`, `ConeFromImpact`, `ChainFromImpact`. |
| Chain lightning | Model as deterministic `ChainFromImpact`. |
| Refund behavior | Support refundable and non-refundable actions through target requirements and commit/refund policy. |
| Target requirements | Use explicit `TargetRequirement`, including `MinTargetsInShape`. |
| Cooldowns | Support `ActionCooldownId` and optional `CooldownGroupId`/group tags. |
| CooldownReduction | Snapshot at commit. |
| Penetration | Support `StopOnFirstHit`, `PierceCount`, `PierceAllUntilRange`, `StopOnWorldThenImpact`. |
| Railgun | Hitscan or projectile line delivery with `PierceAllUntilRange`. |
| Factions and teams | Use authoritative relation service plus efficient relation bitmasks. |
| CollisionManager role | Broad physics filtering only. |
| TagSystem role | Authored metadata and compiled masks, not faction or physics calculation. |
| LOS | Authored per action with `LineOfSightPolicy`. |
| Friendly fire | Per action with project default. |
| Runtime snapshots | Action runtime state is separate from `LivingEntityState` but snapshot-backed. |
| Randomness | Delivery/hit randomness in ActionSystem; combat-result randomness in CombatResolver. |
| Combat boundary | Damage `EffectIntent` into CombatResolver; resolved specs into StateEngine. |
| Same-tick combat reads | CombatResolver reads tick-start state. Same-tick effects affect next tick. |
| Combat log | Final `CombatResultEvent` records are gameplay-authoritative after StateEngine confirms applied values. |
| Resource costs | ActionSystem resolves and reserves final costs, StateEngine applies internal cost effects. |
| Blocked costs | Internal committed costs are unblockable except hard validation failure. |
| Client prediction | Predict selectively when facts are known; host result wins. |
| CombatResolver documentation | `CombatResolver.md` version 1.0.0 defines damage resolving. |

---

## 33. Open Follow-Up Questions

These are intentionally left for future implementation passes:

```text
Should action definitions support channeling in v1 or later?
Should ammo and magazine reloads be modeled as resources, cooldowns, or item runtime state?
Should projectile steering/homing be deterministic in v1?
Should target acquisition support spatial partition data independent from Godot physics?
Should projectile collision include destructible world geometry in v1?
Should action result packets include all combat log data or only IDs for a local log lookup?
Should action cooldown reductions support explicit cooldown-modifying effects in v1?
Should action definitions support multiple payload phases such as launch, travel tick, impact, and expire?
Should client prediction simulate projectile impacts when target facts are known, or keep all impact gameplay host-only at first?
```

---

## 34. Final Contract

`ActionSystem` is the authoritative upstream action resolver for skills, active items, weapons, projectiles, and targeting.

It validates action use, commits cooldown and cost state, acquires targets, simulates projectiles, and emits deterministic damage `EffectIntent` records when delivered effects require combat resolving. It never directly mutates `LivingEntityState`.

`CombatResolver` consumes damage intents and produces pending combat results plus resolved `ActiveEffectSpec` records. `StateEngine` applies those specs, confirms actual applied values, and remains the only system that mutates RPG entity state through effects, conditions, stats, passives, deltas, and snapshots.
