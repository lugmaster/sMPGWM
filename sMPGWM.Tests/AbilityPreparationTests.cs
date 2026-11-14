using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;
public sealed class AbilityPreparationTests
{
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
