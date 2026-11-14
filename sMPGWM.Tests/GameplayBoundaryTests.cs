using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;


namespace sMPGWM.Tests;

public sealed class GameplayBoundaryTests
{

    [Test]
    public void ResourceMutationsOwnRevisionAndPublishCommittedState()
    {
        var state = new ShipRuntime(100, 40);
        var initial = state.Shield;
        var observedRevision = 0L;
        var observedCurrent = 0d;
        state.Changed += () => { observedRevision = state.Revision; observedCurrent = state.Shield.Current; };
        Assert.That(state.SetShieldCurrent(17), Is.True);
        Assert.That(state.Revision, Is.EqualTo(2));
        Assert.That(observedRevision, Is.EqualTo(2));
        Assert.That(observedCurrent, Is.EqualTo(17));
        Assert.That(initial.Current, Is.EqualTo(40));
        Assert.That(state.SetShieldCurrent(17), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => state.SetShieldCurrent(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.SetShieldCurrent(101));
        Assert.That(state.Revision, Is.EqualTo(2));
        Assert.That(state.Shield.Current, Is.EqualTo(17));
        Assert.That(state.SetShieldCurrent(100), Is.True);
        Assert.That(state.Revision, Is.EqualTo(3));
    }

    [Test]
    public void EligibilityUsesLifeFactionAndConditions()
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load());
        var caster = new ShipRuntime(100, 40);
        var target = new ShipRuntime(100, 40);
        caster.Grants.SetSource("skill", new[] { "shieldCapacityBoost" });
        caster.SetFaction(1);
        target.SetFaction(1);
        var context = new AbilityActivationContext(caster, target, 0);
        var dead = runtime.Definitions.Conditions.GetById("dead");
        var silenced = runtime.Definitions.Conditions.GetById("silenced");
        caster.SetCondition(dead, true);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.CasterIsDead));
        caster.SetCondition(dead, false);
        caster.SetCondition(silenced, true);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.BlockedByCondition));
        caster.SetCondition(silenced, false);
        target.SetCondition(dead, true);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.TargetIsNotALivingAlly));
        target.SetCondition(dead, false);
        target.SetFaction(2);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.TargetIsNotALivingAlly));
        target.SetFaction(0);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.TargetIsNotALivingAlly));
        Assert.That(target.ActiveBehaviours, Is.Empty);
        target.SetFaction(1);
        Assert.That(runtime.Activate("shieldCapacityBoost", context, 0), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(target.Shield.Capacity, Is.EqualTo(150));
    }

    [Test]
    public void ObserverFailureDoesNotBecomeActivationRejection()
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load());
        var state = new ShipRuntime(100, 40);
        state.Grants.SetSource("skill", new[] { "shieldCapacityBoost" });
        var context = new AbilityActivationContext(state, state, 0);
        var failure = new InvalidOperationException("Observer failed");
        state.Changed += () => throw failure;
        Assert.That(Assert.Throws<InvalidOperationException>(() => runtime.Activate("shieldCapacityBoost", context, 0)), Is.SameAs(failure));
        Assert.That(state.Revision, Is.EqualTo(2));
        Assert.That(state.ActiveBehaviours.Count, Is.EqualTo(1));
        Assert.That(state.Shield.Capacity, Is.EqualTo(150));
        Assert.That(Assert.Throws<InvalidOperationException>(() => runtime.Activate("shieldCapacityBoost", context, 1)), Is.SameAs(failure));
        Assert.That(state.Revision, Is.EqualTo(3));
        Assert.That(state.ActiveBehaviours[0].Deadline, Is.EqualTo(1801));
        Assert.That(state.ActiveBehaviours.Count, Is.EqualTo(1));
    }

    [Test]
    public void GrantAndGlobalSourcesDoNotRequireEquipment()
    {
        var data = GameplayTestData.Load(edit: (file, document) =>
        {
            if (file == "AbilityDefinitions.yml")
                GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost")["Availability"] = "AllShips";
        });
        using var runtime = new AbilityRuntime(data);
        var ability = data.Abilities.GetById("shieldCapacityBoost");
        var state = new ShipRuntime(100, 40);
        Assert.That(AbilitySource.AllShips.IsAvailable(ability, state, null), Is.True);
        Assert.That(runtime.Activate(ability.Id, new(state, state, 0), 0), Is.EqualTo(AbilityActivationResult.Accepted));
        var restricted = ability with { Availability = AbilityAvailability.ExampleLoadout };
        Assert.That(AbilitySource.AllShips.IsAvailable(restricted, state, null), Is.False);
        var source = AbilitySource.Grant("skill:pilot");
        Assert.That(source.IsAvailable(restricted, state, null), Is.False);
        state.Grants.SetSource("skill:pilot", new[] { ability.Id });
        Assert.That(source.IsAvailable(restricted, state, null), Is.True);
        state.Grants.RemoveSource("skill:pilot");
        Assert.That(source.IsAvailable(restricted, state, null), Is.False);
        state.Grants.SetSource("item:123", new[] { ability.Id });
        Assert.That(AbilitySource.Grant("item:123").IsAvailable(restricted, state, null), Is.False);
        Assert.That(new AbilitySource((AbilitySourceKind)99, "", 0).IsValid, Is.False);
        Assert.That(AbilitySource.Equipment("shield_generator", 0).IsValid, Is.False);
    }

    [Test]
    public void UnsupportedContentHasExplicitDiagnostics()
    {
        var catalogue = GameplayTestData.Load();
        var item = new ItemDefinition("passive", "Passive", "Resource", "", false, 1, null!)
        {
            AbilityIds = new[] { "delayedExplosion" },
            BehaviourGrants = new[] { new ItemBehaviourGrant("infection", BehaviourGrantWhile.Carried, BehaviourGrantLifetime.WhileSourceActive) }
        };
        var diagnostics = catalogue.Abilities.GetSupportDiagnostics(new[] { item });
        var passiveFound = false;
        var abilityFound = false;
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Contains("Item 'passive'") && diagnostic.Contains("infection") && diagnostic.Contains("not implemented")) passiveFound = true;
            if (diagnostic.Contains("Item 'passive'") && diagnostic.Contains("delayedExplosion")) abilityFound = true;
        }
        Assert.That(passiveFound, Is.True);
        Assert.That(abilityFound, Is.True);
        Assert.That(catalogue.Abilities.IsSupported("shieldCapacityBoost", out _), Is.True);
    }
}
