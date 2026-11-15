using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Tests;

public sealed class WeaponRuntimeStateTests
{
    [Test]
    public void SixtyRoundsReloadAt236AndFireAgainAt326()
    {
        var state = new WeaponRuntimeState(new AmmunitionSupplyDefinition(60, 1500));
        for (var tick = 0; tick <= 236; tick++)
            Assert.That(state.TryAccept(tick, 15, true), Is.EqualTo(tick % 4 == 0), "tick " + tick);
        Assert.That(state.Sequence, Is.EqualTo(60));
        Assert.That(state.Rounds, Is.Zero);
        Assert.That(state.ReloadStartTick, Is.EqualTo(236));
        Assert.That(state.ReloadDeadlineTick, Is.EqualTo(326));
        for (var tick = 237; tick < 326; tick++)
        {
            Assert.That(state.CompleteReload(tick), Is.False);
            Assert.That(state.TryAccept(tick, 15, true), Is.False);
        }
        Assert.That(state.CompleteReload(326), Is.True);
        Assert.That(state.TryAccept(326, 15, true), Is.True);
        Assert.That(state.Rounds, Is.EqualTo(59));
        Assert.That(state.CompleteReload(327), Is.False);
    }

    [Test]
    public void ManualReloadPreservesRoundsAndDuplicateDoesNotExtendDeadline()
    {
        var state = new WeaponRuntimeState(new AmmunitionSupplyDefinition(60, 1500));
        Assert.That(state.TryStartReload(0), Is.False);
        state.TryAccept(0, 15, true);
        Assert.That(state.TryStartReload(1), Is.True);
        var revision = state.Revision;
        Assert.That(state.TryStartReload(20), Is.False);
        Assert.That(state.Revision, Is.EqualTo(revision));
        Assert.That(state.Rounds, Is.EqualTo(59));
        Assert.That(state.ReloadDeadlineTick, Is.EqualTo(91));
        Assert.That(state.TryAccept(20, 15, true), Is.False);
        Assert.That(state.CancelReload(), Is.True);
        Assert.That(state.CompleteReload(91), Is.False);
        Assert.That(state.CancelReload(), Is.False);
        Assert.That(state.Rounds, Is.EqualTo(59));
        Assert.That(state.Sequence, Is.EqualTo(1));
        Assert.That(state.NextEligibleTick, Is.EqualTo(4));
    }

    [Test]
    public void CaptureIsIndependentAndReloadOnlyChangesHaveNewRevisions()
    {
        var state = new WeaponRuntimeState(new AmmunitionSupplyDefinition(60, 1500));
        state.TryAccept(0, 15, true);
        var captured = state.Capture();
        state.TryStartReload(1);
        Assert.That(state.Revision, Is.GreaterThan(captured.Revision));
        Assert.That(state.Sequence, Is.EqualTo(captured.Sequence));
        Assert.That(captured.IsReloading, Is.False);
        state.CompleteReload(91);
        Assert.That(captured.Rounds, Is.EqualTo(59));
        Assert.That(state.Rounds, Is.EqualTo(60));
    }

    [Test]
    public void EnergySupplyNeverOwnsRoundsOrReloads()
    {
        var state = new WeaponRuntimeState(new EnergySupplyDefinition(4));
        Assert.That(state.TryStartReload(0), Is.False);
        Assert.That(state.Rounds, Is.Zero);
        Assert.Throws<ArgumentException>(() => state.Restore(2, 0, 0, -1, 1, -1, -1));
        Assert.Throws<ArgumentException>(() => state.Restore(2, 0, 0, -1, 0, 0, 90));
        Assert.That(state.Revision, Is.EqualTo(1));
    }

    [TestCase(1, 1)]
    [TestCase(17, 2)]
    [TestCase(1500, 90)]
    public void ReloadDurationRoundsUp(double milliseconds, long ticks)
    {
        var state = new WeaponRuntimeState(new AmmunitionSupplyDefinition(60, milliseconds));
        Assert.That(state.ReloadDurationTicks, Is.EqualTo(ticks));
    }

    [Test]
    public void InvalidRestoreCannotPartiallyReplaceState()
    {
        var state = new WeaponRuntimeState(new AmmunitionSupplyDefinition(60, 1500));
        state.TryAccept(0, 15, true);
        var captured = state.Capture();
        Assert.Throws<ArgumentException>(() => state.Restore(20, 50, 4, 40, 61, -1, -1));
        Assert.Throws<ArgumentException>(() => state.Restore(20, 50, 4, 40, 50, 1, 92));
        Assert.Throws<ArgumentException>(() => state.Restore(20, double.NaN, 4, 40, 50, -1, -1));
        Assert.That(state.SameAs(captured), Is.True);
    }
}
