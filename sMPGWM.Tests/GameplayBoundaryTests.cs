using Newtonsoft.Json.Linq; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-001 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState, sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions, sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection, sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment, sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-002 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState, sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions, sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection, sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment, sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-003 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState, sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions, sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection, sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment, sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
using sMPGWM.Scripts.Items; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-004 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState, sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions, sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection, sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment, sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics

namespace sMPGWM.Tests; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-005 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState, sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions, sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection, sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment, sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics

public sealed class GameplayBoundaryTests // TEST-SUPPORT: GAMEPLAY-BOUNDARY-006 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState, sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions, sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection, sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment, sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
{

    [Test] // TEST-SUPPORT: GAMEPLAY-BOUNDARY-007 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
    public void ResourceMutationsOwnRevisionAndPublishCommittedState() // TEST-SUPPORT: GAMEPLAY-BOUNDARY-008 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
    {
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-009 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        var initial = state.Shield; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-010 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        var observedRevision = 0L; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-011 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        var observedCurrent = 0d; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-012 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        state.Changed += () => { observedRevision = state.Revision; observedCurrent = state.Shield.Current; }; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-013 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.SetShieldCurrent(17), Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-014 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.Revision, Is.EqualTo(2)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-015 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(observedRevision, Is.EqualTo(2)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-016 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(observedCurrent, Is.EqualTo(17)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-017 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(initial.Current, Is.EqualTo(40)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-018 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.SetShieldCurrent(17), Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-019 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.Throws<ArgumentOutOfRangeException>(() => state.SetShieldCurrent(double.NaN)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-020 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.Throws<ArgumentOutOfRangeException>(() => state.SetShieldCurrent(101)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-021 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.Revision, Is.EqualTo(2)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-022 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.Shield.Current, Is.EqualTo(17)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-023 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.SetShieldCurrent(100), Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-024 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
        Assert.That(state.Revision, Is.EqualTo(3)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-025 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ResourceMutationsOwnRevisionAndPublishCommittedState
    }

    [Test] // TEST-SUPPORT: GAMEPLAY-BOUNDARY-026 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
    public void EligibilityUsesLifeFactionAndConditions() // TEST-SUPPORT: GAMEPLAY-BOUNDARY-027 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-028 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        var caster = new ShipGameplayState(100, 40); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-029 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        var target = new ShipGameplayState(100, 40); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-030 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        caster.Grants.SetSource("skill", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-031 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        caster.SetFaction(1); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-032 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        target.SetFaction(1); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-033 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        var context = new AbilityActivationContext(caster, target, 0); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-034 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        var dead = runtime.Definitions.Conditions.GetById("dead"); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-035 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        var silenced = runtime.Definitions.Conditions.GetById("silenced"); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-036 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        caster.SetCondition(dead, true); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-037 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo("Rejected: caster is dead")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-038 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        caster.SetCondition(dead, false); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-039 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        caster.SetCondition(silenced, true); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-040 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo("Rejected: blocked by condition")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-041 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        caster.SetCondition(silenced, false); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-042 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        target.SetCondition(dead, true); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-043 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo("Rejected: target is not a living ally")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-044 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        target.SetCondition(dead, false); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-045 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        target.SetFaction(2); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-046 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo("Rejected: target is not a living ally")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-047 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        target.SetFaction(0); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-048 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo("Rejected: target is not a living ally")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-049 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(target.ActiveBehaviours, Is.Empty); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-050 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        target.SetFaction(1); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-051 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo("Accepted")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-052 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
        Assert.That(target.Shield.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-053 | Tests: sMPGWM.Tests.GameplayBoundaryTests.EligibilityUsesLifeFactionAndConditions
    }

    [Test] // TEST-SUPPORT: GAMEPLAY-BOUNDARY-054 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
    public void ObserverFailureDoesNotBecomeActivationRejection() // TEST-SUPPORT: GAMEPLAY-BOUNDARY-055 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-056 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-057 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        state.Grants.SetSource("skill", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-058 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        var context = new AbilityActivationContext(state, state, 0); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-059 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        var failure = new InvalidOperationException("Observer failed"); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-060 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        state.Changed += () => throw failure; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-061 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(Assert.Throws<InvalidOperationException>(() => runtime.Activate("shieldCapacityBoost", context, 0)), Is.SameAs(failure)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-062 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(state.Revision, Is.EqualTo(2)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-063 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(state.ActiveBehaviours.Count, Is.EqualTo(1)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-064 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(state.Shield.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-065 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(Assert.Throws<InvalidOperationException>(() => runtime.Activate("shieldCapacityBoost", context, 1)), Is.SameAs(failure)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-066 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(state.Revision, Is.EqualTo(3)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-067 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(1801)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-068 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
        Assert.That(state.ActiveBehaviours.Count, Is.EqualTo(1)); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-069 | Tests: sMPGWM.Tests.GameplayBoundaryTests.ObserverFailureDoesNotBecomeActivationRejection
    }

    [Test] // TEST-SUPPORT: GAMEPLAY-BOUNDARY-070 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
    public void GrantAndGlobalSourcesDoNotRequireEquipment() // TEST-SUPPORT: GAMEPLAY-BOUNDARY-071 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
    {
        var data = GameplayTestData.Load(edit: (file, document) => // TEST-SUPPORT: GAMEPLAY-BOUNDARY-072 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        {
            if (file == "AbilityDefinitions.yml") // TEST-SUPPORT: GAMEPLAY-BOUNDARY-073 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
                GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost")["Availability"] = "AllShips"; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-074 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        }); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-075 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        using var runtime = new AbilityRuntime(data); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-076 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        var ability = data.Abilities.GetById("shieldCapacityBoost"); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-077 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-078 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(AbilitySource.AllShips.IsAvailable(ability, state, null), Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-079 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(runtime.Activate(ability.Id, new(state, state, 0), 0), Is.EqualTo("Accepted")); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-080 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        var restricted = ability with { Availability = AbilityAvailability.ExampleLoadout }; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-081 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(AbilitySource.AllShips.IsAvailable(restricted, state, null), Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-082 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        var source = AbilitySource.Grant("skill:pilot"); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-083 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(source.IsAvailable(restricted, state, null), Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-084 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        state.Grants.SetSource("skill:pilot", new[] { ability.Id }); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-085 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(source.IsAvailable(restricted, state, null), Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-086 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        state.Grants.RemoveSource("skill:pilot"); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-087 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(source.IsAvailable(restricted, state, null), Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-088 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        state.Grants.SetSource("item:123", new[] { ability.Id }); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-089 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(AbilitySource.Grant("item:123").IsAvailable(restricted, state, null), Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-090 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(new AbilitySource((AbilitySourceKind)99, "", 0).IsValid, Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-091 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
        Assert.That(AbilitySource.Equipment("shield_generator", 0).IsValid, Is.False); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-092 | Tests: sMPGWM.Tests.GameplayBoundaryTests.GrantAndGlobalSourcesDoNotRequireEquipment
    }

    [Test] // TEST-SUPPORT: GAMEPLAY-BOUNDARY-093 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
    public void UnsupportedContentHasExplicitDiagnostics() // TEST-SUPPORT: GAMEPLAY-BOUNDARY-094 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-095 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        var item = new ItemDefinition("passive", "Passive", "Resource", "", false, 1, null!) // TEST-SUPPORT: GAMEPLAY-BOUNDARY-096 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        {
            AbilityIds = new[] { "delayedExplosion" }, // TEST-SUPPORT: GAMEPLAY-BOUNDARY-097 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
            BehaviourGrants = new[] { new ItemBehaviourGrant("infection", BehaviourGrantWhile.Carried, BehaviourGrantLifetime.WhileSourceActive) } // TEST-SUPPORT: GAMEPLAY-BOUNDARY-098 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        }; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-099 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        var diagnostics = runtime.GetSupportDiagnostics(new[] { item }); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-100 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        var passiveFound = false; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-101 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        var abilityFound = false; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-102 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        foreach (var diagnostic in diagnostics) // TEST-SUPPORT: GAMEPLAY-BOUNDARY-103 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        {
            if (diagnostic.Contains("Item 'passive'") && diagnostic.Contains("infection") && diagnostic.Contains("not implemented")) passiveFound = true; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-104 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
            if (diagnostic.Contains("Item 'passive'") && diagnostic.Contains("delayedExplosion")) abilityFound = true; // TEST-SUPPORT: GAMEPLAY-BOUNDARY-105 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        }
        Assert.That(passiveFound, Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-106 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        Assert.That(abilityFound, Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-107 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
        Assert.That(runtime.IsSupported("shieldCapacityBoost", out _), Is.True); // TEST-SUPPORT: GAMEPLAY-BOUNDARY-108 | Tests: sMPGWM.Tests.GameplayBoundaryTests.UnsupportedContentHasExplicitDiagnostics
    }
}
