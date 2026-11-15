using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Tests;

public sealed class WeaponCadenceTests
{
    [TestCase(15, 4)]
    [TestCase(3, 20)]
    [TestCase(60, 1)]
    public void ImmediateFirstShotAndIndependentRate(double rate, int interval)
    {
        var cadence = new WeaponCadence();
        for (var tick = 0; tick <= interval * 20; tick++)
        {
            Assert.That(cadence.TryAccept(tick, rate, true), Is.EqualTo(tick % interval == 0), $"Tick {tick}");
            Assert.That(cadence.TryAccept(tick, rate, true), Is.False, "An item cannot fire twice in one tick.");
        }
        Assert.That(cadence.Sequence, Is.EqualTo(21));
    }

    [TestCase(7.0)]
    [TestCase(13.0)]
    [TestCase(37.0)]
    public void FractionalCadenceRetainsLongRunRate(double rate)
    {
        var cadence = new WeaponCadence();
        const int seconds = 120;
        for (var tick = 0; tick < seconds * 60; tick++) cadence.TryAccept(tick, rate, true);
        Assert.That(cadence.Sequence, Is.InRange((long)(rate * seconds) - 1, (long)(rate * seconds) + 1));
    }

    [Test]
    public void ReleaseAndRepressCannotBypassUnexpiredCooldown()
    {
        var cadence = new WeaponCadence();
        Assert.That(cadence.TryAccept(0, 3, true), Is.True);
        Assert.That(cadence.TryAccept(1, 3, false), Is.False);
        for (var tick = 2; tick < 20; tick++) Assert.That(cadence.TryAccept(tick, 3, true), Is.False);
        Assert.That(cadence.TryAccept(20, 3, true), Is.True);
        Assert.That(cadence.Sequence, Is.EqualTo(2));
    }

    [Test]
    public void LongIdleResumesImmediatelyWithoutCatchUpBurst()
    {
        var cadence = new WeaponCadence();
        Assert.That(cadence.TryAccept(0, 15, true), Is.True);
        for (var tick = 1; tick < 100; tick++) Assert.That(cadence.TryAccept(tick, 15, false), Is.False);
        Assert.That(cadence.TryAccept(100, 15, true), Is.True);
        for (var tick = 101; tick < 104; tick++) Assert.That(cadence.TryAccept(tick, 15, true), Is.False);
        Assert.That(cadence.TryAccept(104, 15, true), Is.True);
        Assert.That(cadence.Sequence, Is.EqualTo(3));
    }

    [Test]
    public void ResumeResetsFractionalDebtEvenInFirstDueTick()
    {
        var cadence = new WeaponCadence();
        Assert.That(cadence.TryAccept(0, 11, true), Is.True);
        for (var tick = 1; tick <= 5; tick++) Assert.That(cadence.TryAccept(tick, 11, false), Is.False);
        Assert.That(cadence.TryAccept(6, 11, true), Is.True);
        for (var tick = 7; tick <= 11; tick++) Assert.That(cadence.TryAccept(tick, 11, true), Is.False);
        Assert.That(cadence.TryAccept(12, 11, true), Is.True);
    }

    [Test]
    public void RestoredDeadlineAndSequencePreserveCooldownAndIdentity()
    {
        var source = new WeaponCadence();
        Assert.That(source.TryAccept(40, 3, true), Is.True);
        var restored = new WeaponCadence();
        restored.Restore(source.NextEligibleTick, source.Sequence, source.LastAcceptedTick);
        for (var tick = 41; tick < 60; tick++) Assert.That(restored.TryAccept(tick, 3, true), Is.False);
        Assert.That(restored.TryAccept(60, 3, true), Is.True);
        Assert.That(restored.Sequence, Is.EqualTo(2));
        Assert.That(source.Sequence, Is.EqualTo(1));
    }

    [Test]
    public void IdenticalItemsHaveIndependentSequencesAndCooldowns()
    {
        var first = new WeaponCadence();
        var second = new WeaponCadence();
        Assert.That(first.TryAccept(0, 15, true), Is.True);
        Assert.That(second.TryAccept(0, 15, true), Is.True);
        Assert.That(first.TryAccept(4, 15, true), Is.True);
        Assert.That(second.Sequence, Is.EqualTo(1));
        Assert.That(first.Sequence, Is.EqualTo(2));
    }
}
