using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;

public sealed class WeaponEnergyTests
{
    [Test]
    public void ExactCostSpendsOnceAndFailurePreservesRevision()
    {
        using var runtime = new ShipRuntime();
        runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 0, 10,
            false, false, false, true, true), 1);
        runtime.InitializeResources();
        Assert.That(runtime.TrySpendEnergy(4), Is.False);
        runtime.AdvanceResources(0);
        var revision = runtime.Revision;
        Assert.That(runtime.TrySpendEnergy(4), Is.True);
        Assert.That(runtime.Energy.Current, Is.Zero);
        Assert.That(runtime.Revision, Is.EqualTo(revision + 1));
        Assert.That(runtime.TrySpendEnergy(4), Is.False);
        Assert.That(runtime.Revision, Is.EqualTo(revision + 1));
    }

    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void InvalidCostDoesNotMutate(double cost)
    {
        using var runtime = new ShipRuntime();
        var revision = runtime.Revision;
        Assert.Throws<ArgumentOutOfRangeException>(() => runtime.TrySpendEnergy(cost));
        Assert.That(runtime.Revision, Is.EqualTo(revision));
        Assert.That(runtime.Energy.Current, Is.Zero);
    }
}
