using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests;

public sealed class ResourcePoolTests
{
    [Test]
    public void InvalidContributionCannotCorruptThePool()
    {
        var pool = new ResourcePool(100, 40);
        pool.SetContribution(1, 50, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.SetContribution(1, double.NaN, 0));
        Assert.Throws<InvalidOperationException>(() => pool.SetContribution(2, 0, double.MaxValue));
        Assert.That(pool.Capacity, Is.EqualTo(150));
        Assert.That(pool.Current, Is.EqualTo(40));
        Assert.That(pool.RemoveOwner(2), Is.False);
    }

    [Test]
    public void ModifierInsertionOrderDoesNotChangeBaselineArithmetic()
    {
        var first = new ResourcePool(100, 40);
        var second = new ResourcePool(100, 40);
        first.SetContribution(1, 20, .1);
        first.SetContribution(2, 50, .2);
        second.SetContribution(2, 50, .2);
        second.SetContribution(1, 20, .1);
        Assert.That(first.Capacity, Is.EqualTo(200));
        Assert.That(second.Capacity, Is.EqualTo(first.Capacity));
    }

    [Test]
    public void ContributionsPreserveCurrentAndRemoveOnlyTheirOwner()
    {
        var pool = new ResourcePool(100, 40);
        pool.SetContribution(1, 20, 0);
        pool.SetContribution(2, 50, 0);
        Assert.That(pool.Capacity, Is.EqualTo(170));
        pool.SetContribution(2, 50, 0);
        Assert.That(pool.Capacity, Is.EqualTo(170));
        pool.RemoveOwner(2);
        Assert.That(pool.Capacity, Is.EqualTo(120));
        Assert.That(pool.Current, Is.EqualTo(40));
    }

    [Test]
    public void FractionsUseBaseAndCapacityReductionClampsCurrent()
    {
        var pool = new ResourcePool(100, 100);
        pool.SetContribution(1, 50, .2);
        Assert.That(pool.Capacity, Is.EqualTo(170));
        pool.SetContribution(2, -100, 0);
        Assert.That(pool.Current, Is.EqualTo(70));
        pool.RemoveOwner(2);
        Assert.That(pool.Current, Is.EqualTo(70));
    }
}
