using System.Collections.Concurrent;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
public class RuntimeIdTests
{
    [Test]
    public void ConcurrentCreationProducesUniqueNonzeroIds()
    {
        var ids = new ConcurrentBag<ulong>();
        Parallel.For(0, 10_000, _ => ids.Add(ItemEntityId.Create().Value));

        Assert.That(ids, Has.None.EqualTo(0UL));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(10_000));
        Assert.That(default(ItemEntityId).Value, Is.Zero);
    }

    [Test]
    public void SequenceUsesUnsignedRangePastLongMaxValue()
    {
        var sequence = new IdSequence(long.MaxValue);
        Assert.That(sequence.Next(), Is.EqualTo((ulong)long.MaxValue + 1));
        Assert.That(sequence.Next(), Is.EqualTo((ulong)long.MaxValue + 2));
    }

    [Test]
    public void ExhaustedSequenceNeverWrapsOrReusesIds()
    {
        var sequence = new IdSequence(ulong.MaxValue - 1);
        Assert.That(sequence.Next(), Is.EqualTo(ulong.MaxValue));
        Assert.Throws<OverflowException>(() => sequence.Next());
        Assert.Throws<OverflowException>(() => sequence.Next());
    }
}
