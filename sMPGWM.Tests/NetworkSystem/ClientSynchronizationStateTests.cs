using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests.NetworkSystem;

public class ClientSynchronizationStateTests
{
    [Test]
    public void WarmupObservationsKeepSessionAliveWithoutEnablingReconciliation()
    {
        ulong now = 100;
        var state = new ClientSynchronizationState(() => now);
        var session = Guid.NewGuid();
        state.ApplyBaseline(session, 10); state.Activate();
        for (int i = 1; i <= 12; i++)
        {
            now += 200;
            Assert.That(state.ObserveSnapshot(session, 10 + i), Is.True);
            Assert.That(state.NeedsRecovery(), Is.False);
        }
        Assert.That(state.Phase, Is.EqualTo(ClientSynchronizationPhase.Warming));
        state.FinishWarmup();
        Assert.That(state.Phase, Is.EqualTo(ClientSynchronizationPhase.Predicting));
    }

    [Test]
    public void RecoveryRequiresMatchingSessionAndRequestAndHasBoundedRetries()
    {
        ulong now = 100;
        var state = new ClientSynchronizationState(() => now);
        var session = Guid.NewGuid();
        state.ApplyBaseline(session, 10); state.Activate();
        now += 501;
        Assert.That(state.NeedsRecovery(), Is.True);
        Assert.That(state.BeginRecovery(), Is.True);
        Assert.That(state.BeginRecovery(), Is.False);
        Assert.That(state.ObserveSnapshot(session, 20), Is.False);
        Assert.That(state.AcceptsBaseline(Guid.NewGuid(), state.Request), Is.False);
        Assert.That(state.AcceptsBaseline(session, state.Request - 1), Is.False);
        now += 1001;
        Assert.That(state.RetryDue(), Is.True);
        Assert.That(state.RetryDue(), Is.False);
        Assert.That(state.TimedOut, Is.False);
        Assert.That(state.AcceptsBaseline(session, state.Request), Is.True);
        now += 4000;
        Assert.That(state.TimedOut, Is.True);
        state.ApplyBaseline(session, 25);
        Assert.That(state.Phase, Is.EqualTo(ClientSynchronizationPhase.Warming));
        Assert.That(state.TimedOut, Is.False);
        state.Stop();
        Assert.That(state.AcceptsBaseline(session, state.Request), Is.False);
        Assert.That(state.BeginRecovery(), Is.False);
    }

    [Test]
    public void DuplicateAndWrongSessionSnapshotsDoNotExtendLiveness()
    {
        ulong now = 0;
        var state = new ClientSynchronizationState(() => now);
        var session = Guid.NewGuid();
        state.ApplyBaseline(session, 10); state.Activate();
        for (int i = 0; i < 6; i++)
        {
            now += 200;
            Assert.That(state.ObserveSnapshot(session, 10), Is.False);
            Assert.That(state.ObserveSnapshot(Guid.NewGuid(), 20), Is.False);
            Assert.That(state.NeedsRecovery(), Is.EqualTo(i == 5));
        }
    }

    [Test]
    public void ClockSlewsBoundsLeadAndRejectsExpiredProbe()
    {
        ulong now = 0;
        var clock = new NetworkClock(() => now);
        clock.Observe(100, true);
        now = 1000;
        Assert.That(clock.HostEstimate, Is.EqualTo(160));
        clock.Observe(500, false);
        Assert.That(clock.HostEstimate, Is.EqualTo(161));
        now++;
        Assert.That(clock.TryProbe(out var token), Is.True);
        now += 100;
        Assert.That(clock.Reply(token, 167), Is.True);
        Assert.That(clock.Rtt, Is.EqualTo(.1).Within(.0001));
        Assert.That(clock.Lead, Is.InRange(3, 24));
        now += 1001;
        Assert.That(clock.TryProbe(out token), Is.True);
        now += 2001;
        Assert.That(clock.Reply(token, 300), Is.False);
        Assert.That(clock.Reply(token, 300), Is.True, "Consumed probe cannot trigger recovery twice.");
    }
}
