using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-REGRESSION-001 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp, sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute, sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline, sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack

public sealed class ShieldBoostTests // TEST-SUPPORT: ABILITY-REGRESSION-002 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp, sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute, sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline, sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
{
    [Test] // TEST-SUPPORT: ABILITY-REGRESSION-003 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
    public void TenShipTickCostAndCapacityClamp() // TEST-SUPPORT: ABILITY-REGRESSION-004 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
    {
        var states = new ShipGameplayState[10]; // TEST-SUPPORT: ABILITY-REGRESSION-005 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        using var runtime = new AbilityRuntime(GameplayTestData.Load(variants: true)); // TEST-SUPPORT: ABILITY-REGRESSION-006 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        for (var i = 0; i < states.Length; i++) // TEST-SUPPORT: ABILITY-REGRESSION-007 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        {
            states[i] = new(100, 40); // TEST-SUPPORT: ABILITY-REGRESSION-008 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
            states[i].Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "shieldVariant" }); // TEST-SUPPORT: ABILITY-REGRESSION-009 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
            runtime.Activate("shieldCapacityBoost", new(states[i], states[i], true, 0, Array.Empty<string>()), 0); // TEST-SUPPORT: ABILITY-REGRESSION-010 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
            runtime.Activate("shieldVariant", new(states[i], states[i], true, 0, Array.Empty<string>()), 0); // TEST-SUPPORT: ABILITY-REGRESSION-EXTRA-001 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        }
        states[0].Shield.Confirm(175, 140); // TEST-SUPPORT: ABILITY-REGRESSION-011 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        var before = GC.GetAllocatedBytesForCurrentThread(); // TEST-SUPPORT: ABILITY-REGRESSION-012 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        var clock = System.Diagnostics.Stopwatch.StartNew(); // TEST-SUPPORT: ABILITY-REGRESSION-013 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        for (var tick = 1; tick <= 3600; tick++) // TEST-SUPPORT: ABILITY-REGRESSION-014 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
            foreach (var state in states) state.Expire(tick); // TEST-SUPPORT: ABILITY-REGRESSION-015 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        clock.Stop(); // TEST-SUPPORT: ABILITY-REGRESSION-016 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        TestContext.Out.WriteLine($"Shield ticks: 10 ships, two behaviours each, 3600 steps, {clock.Elapsed.TotalMilliseconds:F3} ms, {GC.GetAllocatedBytesForCurrentThread() - before} bytes"); // TEST-SUPPORT: ABILITY-REGRESSION-017 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        Assert.That(states[0].Shield.Current, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-REGRESSION-018 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
        foreach (var state in states) Assert.That(state.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: ABILITY-REGRESSION-019 | Tests: sMPGWM.Tests.ShieldBoostTests.TenShipTickCostAndCapacityClamp
    }

    [Test] // TEST-SUPPORT: ABILITY-REGRESSION-020 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
    public void DuplicatesReplayAndEvictedRequestsNeverExecute() // TEST-SUPPORT: ABILITY-REGRESSION-021 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
    {
        var history = new ActivationHistory(); // TEST-SUPPORT: ABILITY-REGRESSION-022 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        Assert.That(history.Admit(1, 0, out _), Is.True); // TEST-SUPPORT: ABILITY-REGRESSION-023 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        history.Complete(1, "Accepted"); // TEST-SUPPORT: ABILITY-REGRESSION-024 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        Assert.That(history.Admit(1, 10, out var result), Is.False); // TEST-SUPPORT: ABILITY-REGRESSION-025 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        Assert.That(result, Is.EqualTo("Accepted")); // TEST-SUPPORT: ABILITY-REGRESSION-026 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        for (var request = 2; request < 100; request++) // TEST-SUPPORT: ABILITY-REGRESSION-027 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        {
            Assert.That(history.Admit(request, request * 6, out _), Is.True); // TEST-SUPPORT: ABILITY-REGRESSION-028 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
            history.Complete(request, "Accepted"); // TEST-SUPPORT: ABILITY-REGRESSION-029 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        }
        Assert.That(history.Admit(1, 1000, out result), Is.False); // TEST-SUPPORT: ABILITY-REGRESSION-030 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
        Assert.That(result, Is.EqualTo("Rejected: old request")); // TEST-SUPPORT: ABILITY-REGRESSION-031 | Tests: sMPGWM.Tests.ShieldBoostTests.DuplicatesReplayAndEvictedRequestsNeverExecute
    }

    [Test] // TEST-SUPPORT: ABILITY-REGRESSION-032 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
    public void RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline() // TEST-SUPPORT: ABILITY-REGRESSION-033 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: ABILITY-REGRESSION-034 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-REGRESSION-035 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        state.Shield.SetContribution(10, 20, 0); // TEST-SUPPORT: ABILITY-REGRESSION-036 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        state.Grants.SetSource("fixture", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: ABILITY-REGRESSION-037 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        runtime.Activate("shieldCapacityBoost", new(state, state, true, 0, Array.Empty<string>()), 100); // TEST-SUPPORT: ABILITY-REGRESSION-038 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        Assert.That(state.Shield.Capacity, Is.EqualTo(170)); // TEST-SUPPORT: ABILITY-REGRESSION-039 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        runtime.Activate("shieldCapacityBoost", new(state, state, true, 0, Array.Empty<string>()), 200); // TEST-SUPPORT: ABILITY-REGRESSION-040 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        Assert.That(state.ActiveBehaviours[0].RuntimeId, Is.EqualTo(1)); // TEST-SUPPORT: ABILITY-REGRESSION-041 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        Assert.That(state.Expire(1900), Is.False); // TEST-SUPPORT: ABILITY-REGRESSION-042 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        Assert.That(state.Expire(2000), Is.True); // TEST-SUPPORT: ABILITY-REGRESSION-043 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        Assert.That(state.Shield.Capacity, Is.EqualTo(120)); // TEST-SUPPORT: ABILITY-REGRESSION-044 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
        Assert.That(state.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: ABILITY-REGRESSION-045 | Tests: sMPGWM.Tests.ShieldBoostTests.RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline
    }

    [Test] // TEST-SUPPORT: ABILITY-REGRESSION-046 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
    public void ConfirmedRevisionsCannotBeReplayedOrRolledBack() // TEST-SUPPORT: ABILITY-REGRESSION-047 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
    {
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-REGRESSION-048 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
        Assert.That(state.Confirm(3, 150, 140, new[] { new BehaviourInstance("shieldCapacityBoost", 1, 1800) }), Is.True); // TEST-SUPPORT: ABILITY-REGRESSION-049 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
        Assert.That(state.Confirm(2, 100, 40, Array.Empty<BehaviourInstance>()), Is.False); // TEST-SUPPORT: ABILITY-REGRESSION-050 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
        Assert.That(state.Confirm(3, 100, 40, Array.Empty<BehaviourInstance>()), Is.False); // TEST-SUPPORT: ABILITY-REGRESSION-051 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
        Assert.That(state.Shield.Current, Is.EqualTo(140)); // TEST-SUPPORT: ABILITY-REGRESSION-052 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
        Assert.That(state.Confirm(4, 100, 100, Array.Empty<BehaviourInstance>()), Is.True); // TEST-SUPPORT: ABILITY-REGRESSION-053 | Tests: sMPGWM.Tests.ShieldBoostTests.ConfirmedRevisionsCannotBeReplayedOrRolledBack
    }
}
