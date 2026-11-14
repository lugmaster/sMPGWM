using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;

public sealed class ShieldBoostTests
{
    [Test]
    public void TenShipTickCostAndCapacityClamp()
    {
        var states = new ShipRuntime[10];
        using var runtime = new AbilityRuntime(GameplayTestData.Load(variants: true));
        for (var i = 0; i < states.Length; i++)
        {
            states[i] = new(100, 40);
            states[i].Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "shieldVariant" });
            runtime.Activate("shieldCapacityBoost", new(states[i], states[i], 0), 0);
            runtime.Activate("shieldVariant", new(states[i], states[i], 0), 0);
        }
        states[0].SetShieldCurrent(140);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        for (var tick = 1; tick <= 3600; tick++)
            foreach (var state in states) state.Expire(tick);
        clock.Stop();
        TestContext.Out.WriteLine($"Shield ticks: 10 ships, two behaviours each, 3600 steps, {clock.Elapsed.TotalMilliseconds:F3} ms, {GC.GetAllocatedBytesForCurrentThread() - before} bytes");
        Assert.That(states[0].Shield.Current, Is.EqualTo(100));
        foreach (var state in states) Assert.That(state.Shield.Capacity, Is.EqualTo(100));
    }

    [Test]
    public void DuplicatesReplayAndEvictedRequestsNeverExecute()
    {
        var history = new ActivationHistory();
        Assert.That(history.Admit(1, 0, out _), Is.True);
        history.Complete(1, AbilityActivationResult.Accepted);
        Assert.That(history.Admit(1, 10, out var result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.Accepted));
        for (var request = 2; request < 100; request++)
        {
            Assert.That(history.Admit(request, request * 6, out _), Is.True);
            history.Complete(request, AbilityActivationResult.Accepted);
        }
        Assert.That(history.Admit(1, 1000, out result), Is.False);
        Assert.That(result, Is.EqualTo(AbilityActivationResult.OldRequest));
    }

    [Test]
    public void RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline()
    {
        using var runtime = new AbilityRuntime(GameplayTestData.Load());
        var state = new ShipRuntime(120, 40);
        state.Grants.SetSource("fixture", new[] { "shieldCapacityBoost" });
        runtime.Activate("shieldCapacityBoost", new(state, state, 0), 100);
        Assert.That(state.Shield.Capacity, Is.EqualTo(170));
        runtime.Activate("shieldCapacityBoost", new(state, state, 0), 200);
        Assert.That(state.ActiveBehaviours[0].RuntimeId, Is.EqualTo(1));
        Assert.That(state.Expire(1900), Is.False);
        Assert.That(state.Expire(2000), Is.True);
        Assert.That(state.Shield.Capacity, Is.EqualTo(120));
        Assert.That(state.Shield.Current, Is.EqualTo(40));
    }

    [Test]
    public void ConfirmedRevisionsCannotBeReplayedOrRolledBack()
    {
        var state = new ShipRuntime(100, 40);
        Assert.That(state.Confirm(3, 150, 140, new[] { new BehaviourInstance("shieldCapacityBoost", 1, 1800) }, 0, Array.Empty<string>(), state.Stats with { ShieldCapacity = 150 }, 0, false, true, 0, 0, 1, false, 0, 0), Is.True);
        Assert.That(state.Confirm(2, 100, 40, Array.Empty<BehaviourInstance>(), 0, Array.Empty<string>(), state.Stats with { ShieldCapacity = 100 }, 0, false, true, 0, 0, 1, false, 0, 0), Is.False);
        Assert.That(state.Confirm(3, 100, 40, Array.Empty<BehaviourInstance>(), 0, Array.Empty<string>(), state.Stats with { ShieldCapacity = 100 }, 0, false, true, 0, 0, 1, false, 0, 0), Is.False);
        Assert.That(state.Shield.Current, Is.EqualTo(140));
        Assert.That(state.Confirm(4, 100, 100, Array.Empty<BehaviourInstance>(), 0, Array.Empty<string>(), state.Stats with { ShieldCapacity = 100 }, 0, false, true, 0, 0, 1, false, 0, 0), Is.True);
    }
}
