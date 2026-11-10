using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests;

public sealed class ShieldBoostTests
{
    [Test]
    public void TenShipTickCostAndCapacityClamp()
    {
        var states = new ShipGameplayState[10];
        var definition = new ShieldBoostDefinition("ability", "Boost", "icon", "behaviour", 1800, 50, 0, 600);
        for (var i = 0; i < states.Length; i++)
        {
            states[i] = new(100, 40);
            states[i].Apply(definition, 0, i + 1);
        }
        states[0].Shield.Confirm(150, 140);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        for (var tick = 1; tick <= 3600; tick++)
            foreach (var state in states) state.Expire(tick);
        clock.Stop();
        TestContext.Out.WriteLine($"Shield ticks: 10 ships, 3600 steps, {clock.Elapsed.TotalMilliseconds:F3} ms, {GC.GetAllocatedBytesForCurrentThread() - before} bytes");
        Assert.That(states[0].Shield.Current, Is.EqualTo(100));
        foreach (var state in states) Assert.That(state.Shield.Capacity, Is.EqualTo(100));
    }

    [Test]
    public void DuplicatesReplayAndEvictedRequestsNeverExecute()
    {
        var history = new ActivationHistory();
        Assert.That(history.Admit(1, 0, out _), Is.True);
        history.Complete(1, "Accepted");
        Assert.That(history.Admit(1, 10, out var result), Is.False);
        Assert.That(result, Is.EqualTo("Accepted"));
        for (var request = 2; request < 100; request++)
        {
            Assert.That(history.Admit(request, request * 6, out _), Is.True);
            history.Complete(request, "Accepted");
        }
        Assert.That(history.Admit(1, 1000, out result), Is.False);
        Assert.That(result, Is.EqualTo("Rejected: old request"));
    }

    [Test]
    public void RefreshPreservesInstanceAndExpiresOnlyAtNewDeadline()
    {
        var definition = new ShieldBoostDefinition("ability", "Boost", "icon", "behaviour", 1800, 50, 0, 600);
        var state = new ShipGameplayState(100, 40);
        state.Shield.SetContribution(10, 20, 0);
        state.Apply(definition, 100, 20);
        Assert.That(state.Shield.Capacity, Is.EqualTo(170));
        state.Apply(definition, 200, 30);
        Assert.That(state.BehaviourInstance, Is.EqualTo(20));
        Assert.That(state.Expire(1900), Is.False);
        Assert.That(state.Expire(2000), Is.True);
        Assert.That(state.Shield.Capacity, Is.EqualTo(120));
        Assert.That(state.Shield.Current, Is.EqualTo(40));
    }

    [Test]
    public void ConfirmedRevisionsCannotBeReplayedOrRolledBack()
    {
        var state = new ShipGameplayState(100, 40);
        Assert.That(state.Confirm(3, 150, 140, 1, 1800), Is.True);
        Assert.That(state.Confirm(2, 100, 40, 0, 0), Is.False);
        Assert.That(state.Confirm(3, 100, 40, 0, 0), Is.False);
        Assert.That(state.Shield.Current, Is.EqualTo(140));
        Assert.That(state.Confirm(4, 100, 100, 0, 0), Is.True);
    }
}
