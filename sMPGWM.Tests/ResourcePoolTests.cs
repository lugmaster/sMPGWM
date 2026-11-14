using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class ResourcePoolTests
{
    [Test]
    public void InvalidContributionCannotCorruptThePool()
    {
        var pool = new ShipStatState();
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false));
        pool.SetContribution(new(StatContributionKind.Effect, 1), 50, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.SetContribution(new(StatContributionKind.Effect, 1), double.NaN, 0));
        Assert.Throws<InvalidOperationException>(() => pool.SetContribution(new(StatContributionKind.Effect, 2), 0, double.MaxValue));
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(150));

        Assert.That(pool.RemoveSource(new(StatContributionKind.Effect, 2)), Is.False);
    }

    [Test]
    public void ModifierInsertionOrderDoesNotChangeBaselineArithmetic()
    {
        var first = new ShipStatState();
        first.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false));
        var second = new ShipStatState();
        second.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false));
        first.SetContribution(new(StatContributionKind.Effect, 1), 20, .1);
        first.SetContribution(new(StatContributionKind.Effect, 2), 50, .2);
        second.SetContribution(new(StatContributionKind.Effect, 2), 50, .2);
        second.SetContribution(new(StatContributionKind.Effect, 1), 20, .1);
        Assert.That(first.Effective.ShieldCapacity, Is.EqualTo(221));
        Assert.That(second.Effective.ShieldCapacity, Is.EqualTo(first.Effective.ShieldCapacity));
    }

    [Test]
    public void ContributionsPreserveCurrentAndRemoveOnlyTheirOwner()
    {
        var pool = new ShipStatState();
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false));
        pool.SetContribution(new(StatContributionKind.Effect, 1), 20, 0);
        pool.SetContribution(new(StatContributionKind.Effect, 2), 50, 0);
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(170));
        pool.SetContribution(new(StatContributionKind.Effect, 2), 50, 0);
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(170));
        pool.RemoveSource(new(StatContributionKind.Effect, 2));
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(120));

    }

    [Test]
    public void FractionsUseBaseAndCapacityReductionClampsCurrent()
    {
        var resource = new ResourcePool(100, 100);
        var pool = new ShipStatState();
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false));
        pool.SetContribution(new(StatContributionKind.Effect, 1), 50, .2);
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(180));
        pool.SetContribution(new(StatContributionKind.Effect, 2), -100, 0);
        resource.SetCapacity(pool.Effective.ShieldCapacity);
        Assert.That(resource.Current, Is.EqualTo(60));
        pool.RemoveSource(new(StatContributionKind.Effect, 2));
        resource.SetCapacity(pool.Effective.ShieldCapacity);
        Assert.That(resource.Current, Is.EqualTo(60));
    }
}
