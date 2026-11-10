using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
namespace sMPGWM.Tests; // TEST-SUPPORT: PREPARATION-2 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
public sealed class AbilityPreparationTests // TEST-SUPPORT: PREPARATION-3 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
{
    [Test] // TEST-SUPPORT: PREPARATION-5 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
    public void CataloguesOwnPlansAndSessionsRemainIndependent() // TEST-SUPPORT: PREPARATION-6 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
    {
        var data = GameplayTestData.Load(); // TEST-SUPPORT: PREPARATION-8 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(data.Abilities.TryGetPlan("shieldCapacityBoost", out var plan), Is.True); // TEST-SUPPORT: PREPARATION-9 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        using var first = new AbilityRuntime(data); // TEST-SUPPORT: PREPARATION-10 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        using var second = new AbilityRuntime(data); // TEST-SUPPORT: PREPARATION-11 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(first.Definitions.Abilities.TryGetPlan("shieldCapacityBoost", out var firstPlan), Is.True); // TEST-SUPPORT: PREPARATION-12 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(second.Definitions.Abilities.TryGetPlan("shieldCapacityBoost", out var secondPlan), Is.True); // TEST-SUPPORT: PREPARATION-13 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(firstPlan, Is.SameAs(plan)); // TEST-SUPPORT: PREPARATION-14 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(secondPlan, Is.SameAs(plan)); // TEST-SUPPORT: PREPARATION-15 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        var separate = GameplayTestData.Load(); // TEST-SUPPORT: PREPARATION-16 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(separate.Abilities.TryGetPlan("shieldCapacityBoost", out var separatePlan), Is.True); // TEST-SUPPORT: PREPARATION-17 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(separatePlan, Is.Not.SameAs(plan)); // TEST-SUPPORT: PREPARATION-18 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: PREPARATION-19 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        state.Grants.SetSource("skill", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: PREPARATION-20 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        var context = new AbilityActivationContext(state, state, 0); // TEST-SUPPORT: PREPARATION-21 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(first.Activate("shieldCapacityBoost", context, 100), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: PREPARATION-22 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        var firstId = state.ActiveBehaviours[0].RuntimeId; // TEST-SUPPORT: PREPARATION-23 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        first.Dispose(); // TEST-SUPPORT: PREPARATION-24 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(first.Activate("shieldCapacityBoost", context, 100), Is.EqualTo(AbilityActivationResult.SessionEnded)); // TEST-SUPPORT: PREPARATION-25 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        var otherState = new ShipGameplayState(100, 40); // TEST-SUPPORT: PREPARATION-26 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        otherState.Grants.SetSource("skill", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: PREPARATION-27 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        var otherContext = new AbilityActivationContext(otherState, otherState, 0); // TEST-SUPPORT: PREPARATION-28 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(second.Activate("shieldCapacityBoost", otherContext, 200), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: PREPARATION-29 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(otherState.ActiveBehaviours[0].RuntimeId, Is.EqualTo(firstId)); // TEST-SUPPORT: PREPARATION-30 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(data.Abilities.TryGetPlan("shieldCapacityBoost", out var retained), Is.True); // TEST-SUPPORT: PREPARATION-31 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
        Assert.That(retained, Is.SameAs(plan)); // TEST-SUPPORT: PREPARATION-32 | Tests: sMPGWM.Tests.AbilityPreparationTests.CataloguesOwnPlansAndSessionsRemainIndependent
    }
}
