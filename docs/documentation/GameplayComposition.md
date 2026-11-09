# Gameplay composition MVP

This is the authoring contract agreed during the effect-by-effect design review. It supersedes the active-effect shape in `Effects.md`, `EffectLayout.yml`, and `EffectDefinition_Field_Reference.md` for this increment. Those documents remain historical design input. Existing `PassiveDefinitions.yml` and `PassiveSchemas.yml` also remain legacy proposals; their old effect references are not part of this increment's executable catalogue. Migrating those passive examples requires a separate review.

These files specify the next implementation increment. They do not implement ability activation, combat, projectiles, item grants, or network synchronization in C#. The existing item loader does not yet consume `AbilityIds` or `BehaviourGrants`.

## Files and responsibilities

All files are under `sMPGWM.Game/assets/game_data`, with `Version`, named collections, and `Id` fields matching the item authoring conventions. Each new catalogue has a Draft 7 JSON Schema in YAML. The existing effect schema filename remains `EffectDefinitionSchema.yml`.

| Catalogue | Responsibility |
| --- | --- |
| AbilityDefinitions | Activation, selected target, costs, cooldown, root effect |
| EffectDefinitions | Typed operations: revive, restore resource, deal damage, apply/remove Behaviour, sequence, area search, projectile launch |
| BehaviourDefinitions | Lifetime, stacking, modifiers, condition contributions, periodic state, lifecycle hooks, lethal protection |
| TargetingDefinitions | Relation, life state, mechanical restriction, range or area radius |
| ProjectileDefinitions | Movement and collision settings; the launch effect supplies the impact effect |
| BehaviourClassificationDefinitions | Named selection categories for attached Behaviours |
| Attributes / Conditions | Referenced stat and condition IDs, with AttributeSchema / ConditionSchema |
| ItemDefinitions / ItemSchema | Existing items plus optional ability references and source-bound Behaviour grants |

Modifiers and validators are small inline values, not separate named entities in this increment. A modifier affects one stat. A Behaviour owns several modifiers when they must stack, expire, and be removed together. Effect kinds have distinct schema branches so unrelated fields cannot accidentally coexist.

Catalogue IDs are exact, case-sensitive references, distinct from gameplay tags. Behaviour classification IDs are selection keys, not aliases for entity conditions. Integrating these keys with the existing tag registry must preserve that distinction. A classification match is exact in this MVP; no implicit tag hierarchy is assumed.

## Agreed examples

| Original entry | Ability / effect / Behaviour outcome |
| --- | --- |
| revive | Every ship has an instant, ranged ally-wreck revival; no cost or cooldown; set Health to min(50, effective maximum). No lasting Behaviour. |
| disruptiveExplosion | EMP projectile impact searches hostile mechanical ships; applies 1.5-second disruption. Interrupt casts/channels, disable movement/weapons/shields/abilities; refresh on reapplication. Launched projectiles continue. |
| removeDisrupted | Self-or-ally ability removes removable disruption Behaviours and restores 10% maximum stored energy, even when no disruption was removed. |
| infection | Direct and projectile abilities share one application effect. Three modifiers per independent 1.5-second stack, each -5% baseline; five stacks per target; overflow replaces oldest. |
| removeInfection | Self-or-ally removal of up to three eligible infection-classified stacks, oldest first. Source-bound grants are excluded. |
| shieldRecharge | Self-only restoration of 50% effective maximum Shield; Disabled and Disrupted recipients cannot receive it. |
| onDeathFailSafe | Self-or-ally application of a 20-second Behaviour. One lethal damage result is prevented and Health is set to 10% effective maximum; consume the protection. No threshold trigger, excess-damage limit, revival, or Shield restoration. |
| delayedExplosion | Direct hostile attachment; independent two-second charges; natural expiration searches hostiles around the carrier and deals 100 physical damage through combat rules. Carrier is included when eligible. Disarm and death remove without detonation. |
| energyProductionSurge | Self-or-ally +25% production for 2.5 seconds; refresh without stacking. |
| energyStorageMalfunction | Hostile until-removed -50% storage capacity; on accepted application only, remove removable production buffs. New buffs coexist; duplicate applications are discarded. |
| maxHealthRot | Hostile independent five-second Behaviours, at most five; periodic ticks accumulate -5% baseline Health capacity each. Expiration/removal releases only that instance's accumulator; no healing. Minimum effective Health capacity is 1. |
| raiseHealth | Self-or-ally restoration of 35 Health; reject Dead and Incurable. |
| overheating | Self-or-ally +50% attack rate for five seconds; ability cooldown 15 seconds. Expiration or early removal applies the penalty except when removal reason is Death. |
| overheatingCooldown | Follow-up Behaviour: -75% attack rate and Overheated for 2.5 seconds; no ordinary cleansing, but death removes it. It has no separate ability. |
| shieldCapacityBoost | Self-or-ally +50 Shield capacity for 30 seconds; refresh without stacking; capacity only, never fills Shield. |

## Shared execution semantics

Shared lifecycle, arithmetic, and authority guarantees are engine contracts documented here, not a configurable gameplay-rules catalogue.

Only the host mutates authoritative gameplay state. The client sends an activation request, not effect outputs. The request identifies an ability, caster, target, request ID, and source item instance when applicable. Authenticate the sender and check ownership, ability availability, current conditions, range, resources, cooldown, and target eligibility on the host. Revalidate at execution. Keep request deduplication scoped to the authenticated session; retries return the prior result rather than executing twice.

AbilityExecution owns pending/casting state; it is not a target Behaviour. Immutable definitions do not contain request IDs, counters, timers, or network state. Replicate confirmed state changes and activation results. Clients may show pending presentation, but never independently commit death prevention or revival. The 30 Hz network cadence does not mandate the simulation cadence; convert authored milliseconds to the chosen fixed simulation ticks consistently.

Costs and cooldowns commit once on successful activation. Launched projectiles commit on launch, not on a later hit. For an instant sequence, success means at least one child succeeded. A capped resource restoration on an otherwise valid target counts as valid execution even if the numeric delta is zero. `ContinueOnFailure` permits later children to execute after a no-match cleanse or rejected child; it is not an atomic transaction.

Effect context carries original caster/owner attribution, concrete source instance, execution ID, selected target, and current position. Area hostility is relative to the original caster, not to the bomb carrier. A Behaviour expiration uses its owner's current position. The projectile impact supplies its hit position and hit entity. World impacts still permit area searches, but direct target effects with no entity fail. Projectiles expire without executing impact effects when their maximum lifetime runs out.

An area search snapshots eligible entities and processes them in stable entity-ID order, rechecking life state before each operation. No maximum target count is imposed for these examples. The caster and allies are excluded from hostile searches. Target selection checks relation and life state; effect/Behaviour checks validate recipient conditions as well. Disruption can interrupt existing execution without cancelling already launched projectiles.

## Lifetime, ownership, and removal

Independent applications use the authored Behaviour lifetime. Source-bound item or passive grants use `WhileSourceActive` instead: attachment ownership comes from the source, while modifiers and reactions come from the Behaviour definition. They are maintained once per concrete source instance and do not need to be reapplied each tick. Such grants are outside ordinary timed-stack replacement and cleanse selection. This prevents an enemy application or overflow from evicting an equipped passive. They still contribute to the same stat aggregation.

`infectedCargo` demonstrates one Infection contribution while carried. Its duration is bound to carrying, rather than the ordinary 1.5 seconds. Removing the item removes its contribution; ordinary Infection cleanse cannot. A passive provider will use the same source-binding mechanism. Passive definitions themselves are not migrated here.

Death removes all independently applied Behaviours, including otherwise non-cleansable Overheated. It suspends source-bound grants: modifiers, conditions, periodic operations, and reactions cease contributing; attached runtime state remains. Suspended timers pause. Revival resumes those grants and ordinary energy/shield operation. Ending a grant while dead removes it permanently. Death cleanup must complete before a wreck becomes revivable. Choosing respawn immediately invalidates that wreck for revival. Cleanup must not generate gameplay follow-up effects from the dead entity.

Conditions are contributor-owned. Remove one instance's contribution without clearing another's. `infected` may be contributed by Infection, Health Rot, or carried cargo. Stat aggregation and condition ownership must survive independent expiry and removal correctly.

Refresh keeps the existing instance and extends its deadline; it does not replay OnApply. Discard rejects the new application and its hooks. Independent creates a new instance. Bounded overflow replaces the oldest eligible independent instance using AppliedTick, InsertionSequence, RuntimeId. Delayed Explosion has no authored stack cap. Runtime processing budgets must bound queues without changing ordinary stacking semantics.

Removal selectors combine different fields with AND; values within a classification or stat list match ANY. Removing a Behaviour removes all its modifiers, not only the modifier used to select it. Ordinary removal always excludes source-bound grants and instances with OrdinaryRemoval=false. Removing no matches is harmless. Lifecycle removal reasons include Death, Dispel, SourceLost, Consumed, and Replaced; expiration is a separate event.

## Arithmetic, blocking, and timing

All percentages are fractions: 0.05 means 5%. Behaviour fractions add against the baseline before Behaviour contributions, avoiding recursive compounding. Five Infection stacks therefore contribute -25% to each affected stat. Equipment/passive stat foundations must be included in the baseline before temporary Behaviour fractions; source-bound fraction grants themselves must not recursively redefine their own baseline.

Resource restoration uses effective maximum at execution time. Capacity increases never fill current resources. Capacity decreases clamp current resources. Removing a capacity penalty restores capacity only. Health capacity cannot drop below 1. Fail Safe runs after mitigation and before death commits, preserves attribution, consumes only one eligible protection in stable instance order, and sets positive Health rather than briefly killing and reviving the ship. It responds to lethal damage, not explicit despawning or a scripted kill command.

BlockedPolicy Remove rejects blocked admission and removes an existing Behaviour if the blocking condition later appears. Suppress retains the instance without contributions while blocked; ordinary lifetime continues. This preserves the old Infection Retry intent without unbounded queued application retries. Death suspension is a separate rule and pauses source-bound timers. Health Rot skips blocked ticks without catch-up; the Infected contribution starts on its first successful tick and lasts until removal.

Timed lifetimes use a half-open interval: expiration is processed before a periodic operation due at the same tick. Thus Health Rot has ticks at +1, +2, +3, and +4 seconds, then expires at +5 seconds. This boundary convention is an implementation choice, not an extra gameplay promise from the discussion. The accumulator is removed at expiry regardless of tick count.

## Provisional tuning and implementation boundaries

Range 600, radius 200, projectile speeds 800/900, and projectile lifetime 5 seconds are explicit placeholder values in Godot world units and milliseconds. They were not balance decisions from the review. Unspecified ability costs and cooldowns are zero for examples; only Revive's free activation and Overheating's 15-second cooldown were explicitly settled. Revive is available to every ship; the other abilities are tagged ExampleLoadout for a future test loadout and are not automatically granted in the current runtime.

Energy restoration uses the explicit energy_storage pool. Legacy energy/battery IDs remain in Attributes to avoid silently rewriting unrelated content. Future stat integration must reconcile those legacy aliases, production/consumption rates, and passive baselines.

Runtime integration must register every new catalogue and schema in content loading and multiplayer compatibility hashing. The current GameConfig still refers to the absent legacy EffectSchemas.yml and does not inventory these new catalogues; that pre-existing integration mismatch is not fixed by these authoring files. Item category data also contains pre-existing Values fields rejected by ItemCategorySchema; this validator uses category IDs without claiming that unrelated legacy catalogue is schema-clean.

## Validation

Install `tools/gameplay-validation-requirements.txt` into a virtual environment and run `python tools/validate_gameplay_data.py`. It validates all new schemas and catalogues, existing items with their new grants, stat/condition IDs, damage/category references, effect/Behaviour graph cycles, and selected semantic constraints. Negative checks reject invalid percentages, unknown fields, dangling IDs, duplicate IDs, wrong operation payloads, invalid stat kinds, cycles, and wrong targeting profiles.

Runtime tests remain for the implementation increment: host request deduplication; competing revivals; lethal-hit interception; independent stack expiry; source-owned conditions; death suspension/resumption; self-only Shield Recharge; projectile attribution; removal reasons; and capacity changes without healing. Content validation does not demonstrate those runtime behaviours.
