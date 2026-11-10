# Gameplay increment 2 — Typed registries and reusable ability runtime

Status: implemented; verification results are recorded below.

Date: 2026-11-10.

Predecessor: [Shield Capacity Boost vertical slice](gameplay-increment-01-shield-capacity-vertical-slice.md).

## Outcome

A content author can add another ability using the supported mechanics, include it in the development loadout, and activate it without adding an ability-specific C# branch. Different Behaviour definitions coexist; abilities referencing the same Behaviour refresh its existing per-target instance. The host owns execution, expiration, and replicated state.

## Definition architecture

Every active gameplay catalogue has a typed read-only registry. Registries expose GetById, TryGetById, and cached GetAll collections. Definitions contain immutable values and read-only nested collections. Raw YAML/JToken objects do not enter runtime execution.

| Namespace | Registries |
| --- | --- |
| AbilitySystem | AbilityRegistry, EffectRegistry, BehaviourRegistry, TargetingRegistry, BehaviourClassificationRegistry, AbilityLoadoutRegistry |
| StatSystem | StatRegistry |
| ConditionSystem | ConditionRegistry |
| ProjectileSystem | ProjectileRegistry |
| CombatSystem | DamageSourceRegistry |
| Existing Items / ShipSystem | ItemRegistry (also categories), ShipRegistry |

GameplayDefinitions.Current publishes one GameplayCatalogues object. GameplayCatalogueLoader reads the explicit catalogue manifest through FileLoader, which validates schemas and preserves existing user-override/reset behavior. Typed construction and cross-reference validation finish before publication. GameplayCatalogueValidator checks references, resource-only operations, targeting shape, periodic boundaries, and prohibited effect/Behaviour cycles. Diagnostics identify the file, definition, field, and missing reference.

Effects use separate typed variants for ApplyBehaviour, Revive, RestoreResource, RemoveBehaviours, Sequence, DealDamage, SearchArea, and LaunchProjectile. Definition availability does not imply execution support.

ItemRegistry retains typed ItemBehaviourGrant records, including While and Lifetime. Ability references, Behaviour grant references, and item damage-source references are checked against the gameplay catalogues. Applying item grants and consuming items remain separate work.

Attributes does not declare resource categories. The registry preserves the existing validator's resource IDs: health, shield, energy_storage, oxygen, food, water. Legacy energy/battery aliases are unchanged. PassiveDefinitions/PassiveSchemas remain inactive legacy content and are excluded from loading and compatibility until migration.

## Runtime and authority

GameSessionFactory creates and owns a fresh AbilityRuntime. The runtime compiles supported definitions into execution plans once per session, checks grants and activation context, and applies the resolved Behaviour. Networking retains authenticated sender/control-generation checks, bounded request history and queues, and result/state transport.

AbilityGrants tracks concrete source IDs independently. Removing one grant source leaves other sources intact. Ending an ability grant does not remove independently applied timed Behaviours. This is an ability-availability foundation, not implementation of source-bound Behaviour lifetime.

Supported execution:

- Instant, zero-cost, zero-cooldown abilities.
- Alive self or allied targets, including self-only targeting and authored range.
- ApplyBehaviour with a timed, per-target-definition RefreshDuration Behaviour.
- Shield maximum AddFlat and AddBaselineFraction contributions.
- Ability-level blocking evaluated against supplied caster conditions.

The world currently has no condition-producing gameplay. Its activation adapter explicitly supplies an empty condition set; Behaviour definitions that would produce conditions, disable capabilities, or execute hooks remain unsupported. Condition state, hostile faction/life-state simulation, combat, cooldowns, spending, projectiles, independent stacks, and source-bound effects require subsequent stories.

Unsupported definitions remain inspectable. AbilityRuntime.IsSupported returns a dependency-path explanation. Unsupported execution is rejected, and an unsupported development-loadout entry prevents session composition with a diagnostic.

ShipGameplayState exposes BehaviourInstance values (definition ID, runtime ID, deadline). Reapplication preserves instance identity and its modifier; a different definition gets a separate contribution. Expiration releases only that instance. Capacity increases never heal and decreases clamp current Shield. Arithmetic remains max(0, B + sum(flat) + B * sum(fraction)).

Simulation remains 60 Hz. Milliseconds round upward to whole simulation ticks. Expiration runs before activation on the same tick. Reliable dirty snapshots publish at most every second simulation step (30 Hz). Clients use confirmed capacities/current values and never apply the replicated modifiers again.

## Multiplayer and presentation

GameplaySnapshotCodec carries multiple Behaviour records per ship. It validates the complete message before state application: row shapes, finite resource values, entity IDs, revisions, known definitions, unique definition/instance identities, and positive deadlines. Bounds are 64 entity rows, 64 active Behaviours per entity, and 64-character ability/Behaviour IDs. Runtime admission rejects excess instances rather than evicting an unrelated instance.

Late join uses the existing reliable baseline and preserves original deadlines. Stale revisions cannot restore expired instances. Unknown entities cannot be created by gameplay messages. Session disposal clears requests and execution plans.

Compatibility includes every loaded catalogue/schema, actual user file content, and a GameplayProtocol marker for the changed wire shape. The HUD displays names and remaining time for every confirmed active Behaviour, retaining its 5 Hz presentation refresh and target-selection controls.

### Hotbar icon follow-up

The user's graphical LAN run reached InGame and completed client authentication/join, but exposed a missing ability icon. The definition and texture loaded correctly: the new action key was `ability:shieldCapacityBoost`, while HotbarControl.Refresh still recognized action visuals only for integer keys. It fell through to item rendering, which cleared the texture because an action has no item definition ID.

The fix supplies icons in a dictionary keyed by stable action identity. DevelopmentAbilityLoadout passes that mapping through the new UiCanvas.BindHotbar and HotbarControl.Bind dictionary overloads. HotbarControl resolves every ActionHotbarBinding through the mapping during refresh. Its existing list overload converts legacy numeric action keys into the same mapping; item and weapon rendering retain their existing paths. Moving or rebinding slots preserves the correct image. DefinitionRegistry needed no change.

Regression coverage: `sMPGWM.Tests.UiSystem.HotbarIconTests.ActionTexturesSurviveRefreshMoveAndRebind` runs for named ability keys and legacy numeric keys. It checks the actual Godot button texture after initial binding, refresh, movement, rebinding, and clearing.

## Authoring another supported ability

1. Add a Behaviour with a distinct ID, Shield-maximum modifier, Timed lifetime, and RefreshDuration stacking.
2. Add an ApplyBehaviour effect referencing it.
3. Add an instant, free ability referencing that effect and an appropriate targeting profile.
4. Add its ability ID to the development entry in AbilityLoadoutDefinitions.yml.

The loadout controls explicit host grants and hotbar order; it is not inferred from all registry entries or ExampleLoadout metadata. Its schema limits the list to ten hotbar slots. It is active only in development sessions. Existing FileLoader behavior still applies: packaged reload configuration resets user overrides unless reload_config_on_start is disabled.

## Verification

- Content validation: 129 definitions across twelve catalogues; nine malformed-content checks rejected.
- Architecture full-suite run: 245 outer NUnit tests passed; the Godot orchestration additionally reported 159 engine tests passed, zero skipped. The two explicit stress/process scenarios remain opt-in.
- Latest hotbar-fix verification: the targeted Godot orchestration passed with 161 engine tests and zero skipped, including both new icon regression cases. The entire outer suite and multiplayer process scenarios were not rerun for this presentation-only fix.
- Automated coverage: typed variants and collection immutability, item grants, contextual reference diagnostics, duplicate IDs, cycles, wrong stat kinds, schema errors, unsupported mechanics, grant ownership, independent deadlines, shared-Behaviour refresh, teardown, and malformed/stale multi-Behaviour snapshots.
- Original normal-session integration coverage: host admission, deduplication, ally targeting, range rejection, expiration, and the ten-ship snapshot bound.
- The two-Behaviour snapshot test verifies exact late-join state and stale-revision rejection, and measures ten ships against a 4 KiB serialized payload bound.
- Measured two-Behaviour payload: 1,808 bytes for ten ships (Godot Variant data, excluding transport framing). Ten ships with two Behaviours over 3,600 simulation steps: 0.387 ms total and 2,280 allocated bytes including stopwatch and expiry work. This is a focused state-loop measurement, not a whole-game performance claim. No forced GC is used.
- Test-only declarations and executable lines carry unique TEST-SUPPORT IDs and fully qualified test-method references.

Commands from the repository root:

```text
python tools/validate_gameplay_data.py
dotnet test sMPGWM.Tests --logger "console;verbosity=normal"
dotnet test sMPGWM.Tests --no-restore --filter FullyQualifiedName~GodotIntegrationTests --logger "console;verbosity=normal"
python tools/run_shield_slice.py
python tools/run_shield_slice.py --players 10 --impaired --variants
```

The variants harness copies the game to a temporary project and adds a +25 Shield / 20-second Behaviour plus its effect, ability, and loadout entry there. The regular +50 / 30-second Behaviour remains alongside it. Both use normal hotbar routing. The last client joins at twelve seconds; logs compare equal entity revisions across processes and check unchanged current Shield. The fixture never edits the real gameplay catalogues. Raw logs and staged files are retained in the reported temporary directory.

The default three-player run passed with all processes returning zero: 619 observations across eighteen entity revisions, no conflicts, current Shield always 40. Trace: [default-loadout regression](evidence/shield-architecture-regression-enet-3.txt).

The final ten-player two-Behaviour run passed with 20–60 ms one-way delay, 1% loss, and delay-induced reordering (seed 1042). All ten processes returned zero, observed combined capacity 175, and later returned to 100 without healing. Cross-process comparison found no conflicts across 6,644 observations and 107 entity revisions. Trace: [two-Behaviour impaired ENet](evidence/ability-architecture-enet-10-impaired.txt).

The latest build reports the existing UiIcon.Size member-hiding warning and NUnit2009 on the repeated GetAll() identity assertion in AbilityArchitectureTests. Neither prevented the build or test run.

Steam multiplayer across separate authenticated accounts and graphical pointer testing remain outside this automated ENet verification.

## Handover entry points

Paths below are relative to the repository root.

| Area | Starting file |
| --- | --- |
| Published catalogue set | `sMPGWM.Game/Scripts/GamePlaySystem/GameplayDefinitions.cs` |
| Typed loading and validation | `sMPGWM.Game/Scripts/GameplayDataSystem/GameplayCatalogueLoader.cs`, `GameplayCatalogueValidator.cs` |
| Execution support, admission, and application | `sMPGWM.Game/Scripts/AbilitySystem/AbilityRuntime.cs` |
| Per-ship instances and expiration | `sMPGWM.Game/Scripts/GamePlaySystem/ShipGameplayState.cs` |
| Session composition and ownership | `sMPGWM.Game/Scripts/GamePlaySystem/GameSessionFactory.cs` |
| Request routing and state replication | `sMPGWM.Game/Scripts/NetworkSystem/NetworkManager.Gameplay.cs`, `GameplaySnapshotCodec.cs` |
| Development grants and action icons | `sMPGWM.Game/Scripts/Util/Mocks/DevelopmentAbilityLoadout.cs` |
| Hotbar presentation | `sMPGWM.Game/Scripts/UiSystem/HudSystem/Hotbar/HotbarControl.cs` |

The next gameplay story has not been selected. Continue discussing the user's next theme before expanding execution capabilities. Preserve the supported/deferred distinction above and the agreed gameplay in [design context and decisions](gameplay-design-context-and-decisions.md).
