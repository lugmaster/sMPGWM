using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;
public sealed class AbilityPreparationTests
{
    [TestCase("AddFlat", false)]
    [TestCase("AddBaselineFraction", false)]
    [TestCase("AddFlat", true)]
    [TestCase("AddBaselineFraction", true)]
    public void OverflowingDefinitionModifierSumsFailLoading(string operation, bool unsupported)
    {
        var error = Assert.Throws<ArgumentException>(() => GameplayTestData.Load(edit: (file, document) =>
        {
            if (unsupported && file == "AbilityDefinitions.yml")
                GameplayTestData.Find((JArray)document["AbilityDefinitions"]!, "shieldCapacityBoost")["CooldownMs"] = 1;
            if (file != "BehaviourDefinitions.yml") return;
            var behaviour = GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "shieldCapacityBoost");
            var modifier = new JObject
            {
                ["StatId"] = "shield", ["ValueKind"] = "Maximum", ["Operation"] = operation, ["Value"] = double.MaxValue
            };
            behaviour["Modifiers"] = new JArray(modifier, modifier.DeepClone());
        }));
        Assert.That(error!.Message, Does.Contain("BehaviourDefinitions.yml").And.Contain("shieldCapacityBoost")
            .And.Contain("Modifiers").And.Contain("sum overflows"));
    }

    [Test]
    public void DifferentModifierOperationsHaveIndependentSums()
    {
        var data = GameplayTestData.Load(edit: (file, document) =>
        {
            if (file != "BehaviourDefinitions.yml") return;
            var behaviour = GameplayTestData.Find((JArray)document["BehaviourDefinitions"]!, "shieldCapacityBoost");
            behaviour["Modifiers"] = new JArray(
                new JObject { ["StatId"] = "shield", ["ValueKind"] = "Maximum", ["Operation"] = "AddFlat", ["Value"] = double.MaxValue },
                new JObject { ["StatId"] = "shield", ["ValueKind"] = "Maximum", ["Operation"] = "AddBaselineFraction", ["Value"] = double.MaxValue });
        });
        Assert.That(data.Abilities.TryGetPlan("shieldCapacityBoost", out var plan), Is.True);
        Assert.That(plan.Behaviour.Flat, Is.EqualTo(double.MaxValue));
        Assert.That(plan.Behaviour.Fraction, Is.EqualTo(double.MaxValue));
    }

    [Test]
    public void CataloguesOwnPlansAndSessionsRemainIndependent()
    {
        var data = GameplayTestData.Load();
        Assert.That(data.Abilities.TryGetPlan("shieldCapacityBoost", out var plan), Is.True);
        using var first = new AbilityRuntime(data);
        using var second = new AbilityRuntime(data);
        Assert.That(first.Definitions.Abilities.TryGetPlan("shieldCapacityBoost", out var firstPlan), Is.True);
        Assert.That(second.Definitions.Abilities.TryGetPlan("shieldCapacityBoost", out var secondPlan), Is.True);
        Assert.That(firstPlan, Is.SameAs(plan));
        Assert.That(secondPlan, Is.SameAs(plan));
        var separate = GameplayTestData.Load();
        Assert.That(separate.Abilities.TryGetPlan("shieldCapacityBoost", out var separatePlan), Is.True);
        Assert.That(separatePlan, Is.Not.SameAs(plan));
        var state = new ShipRuntime(100, 40);
        state.Grants.SetSource("skill", new[] { "shieldCapacityBoost" });
        var context = new AbilityActivationContext(state, state, 0);
        Assert.That(first.Activate("shieldCapacityBoost", context, 100), Is.EqualTo(AbilityActivationResult.Accepted));
        var firstId = state.ActiveBehaviours[0].RuntimeId;
        first.Dispose();
        Assert.That(first.Activate("shieldCapacityBoost", context, 100), Is.EqualTo(AbilityActivationResult.SessionEnded));
        var otherState = new ShipRuntime(100, 40);
        otherState.Grants.SetSource("skill", new[] { "shieldCapacityBoost" });
        var otherContext = new AbilityActivationContext(otherState, otherState, 0);
        Assert.That(second.Activate("shieldCapacityBoost", otherContext, 200), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(otherState.ActiveBehaviours[0].RuntimeId, Is.EqualTo(firstId));
        Assert.That(data.Abilities.TryGetPlan("shieldCapacityBoost", out var retained), Is.True);
        Assert.That(retained, Is.SameAs(plan));
    }
}
