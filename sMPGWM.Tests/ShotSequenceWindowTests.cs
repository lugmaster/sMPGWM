using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Tests;

public sealed class ShotSequenceWindowTests
{
    [Test]
    public void ReorderingIsAcceptedOncePerItemWithin64Sequences()
    {
        var window = new ShotSequenceWindow();
        Assert.That(window.Remember(1, 100, 1, 1), Is.True);
        Assert.That(window.Remember(1, 99, 1, 1), Is.True);
        Assert.That(window.Remember(1, 99, 1, 1), Is.False);
        Assert.That(window.Remember(1, 37, 1, 1), Is.True);
        Assert.That(window.Remember(1, 36, 1, 1), Is.False);
        Assert.That(window.Remember(2, 100, 1, 1), Is.True);
        Assert.That(window.Remember(1, 164, 2, 2), Is.True);
        Assert.That(window.Remember(1, 100, 2, 2), Is.False);
    }

    [Test]
    public void ActiveIdentitiesCannotBeEvictedButExpiredIdentitiesRecycleIndefinitely()
    {
        var window = new ShotSequenceWindow();
        for (ulong item = 1; item <= ShotSequenceWindow.Capacity; item++)
            Assert.That(window.Remember(item, 1, 1, 1), Is.True);
        Assert.That(window.Remember(513, 1, 1, 1), Is.False);
        Assert.That(window.CapacityDrops, Is.EqualTo(1));
        Assert.That(window.Remember(1, 1, 1, 1), Is.False);
        for (var round = 1; round <= 20; round++)
        {
            var now = 1 + round * (ShotSequenceWindow.RetentionTicks + 1);
            for (ulong i = 1; i <= ShotSequenceWindow.Capacity; i++)
                Assert.That(window.Remember((ulong)(round * ShotSequenceWindow.Capacity) + i, 1, now, now), Is.True);
        }
        window.Clear();
        Assert.That(window.CapacityDrops, Is.Zero);
        Assert.That(window.Remember(1, 1, 1, 1), Is.True);
    }
}
