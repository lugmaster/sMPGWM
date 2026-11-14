using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.MovementSystem;

public sealed class CommandQueueTests
{

    [Test]
    public void FullWindowPreservesOutOfOrderCommandsAcrossWraparound()
    {
        var queue = new CommandQueue();
        for (var completed = 0L; completed < 3200; completed += 32)
        {
            for (var offset = 32; offset > 0; offset--)
            {
                var input = new ShipInputState(Vector2.Right, offset, true, false, false, true);
                Assert.That(queue.Accept(new(completed + offset, input), completed), Is.True);
            }
            Assert.That(queue.Accept(new(completed + 33, default), completed), Is.False);
            for (var offset = 1; offset <= 32; offset++)
                Assert.That(queue.Consume(completed + offset).AimAngle, Is.EqualTo(offset));
        }
    }


    [Test]
    public void ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands()
    {
        var queue = new CommandQueue();
        var input = new ShipInputState(Vector2.Right, 0, true, true, false, true);
        for (var tick = 1; tick <= 32; tick++)
            Assert.That(queue.Accept(new(tick, default), 0), Is.True);
        Assert.That(queue.Accept(new(65, input), 64), Is.True);
        Assert.That(queue.Consume(33), Is.EqualTo(default(ShipInputState)));
        Assert.That(queue.Consume(65), Is.EqualTo(input));
        Assert.That(queue.Consume(71), Is.EqualTo(input));
        Assert.That(queue.Consume(72).Direction, Is.EqualTo(Vector2.Zero));
        Assert.That(queue.Consume(72).AutomaticBraking, Is.True);
    }


    [Test]
    public void DuplicatesDoNotRefreshArrivalAndConflictsAreRejected()
    {
        var queue = new CommandQueue();
        var input = new ShipInputState(Vector2.Right, 0, false, false, false, true);
        Assert.That(queue.Accept(new(32, input), 0), Is.True);
        Assert.That(queue.Accept(new(32, input), 1), Is.True);
        Assert.That(queue.LatestArrival, Is.Zero);
        Assert.That(queue.Accept(new(32, default), 1), Is.False);
        Assert.That(queue.Accept(new(33, input), 1), Is.True);
        Assert.That(queue.LatestArrival, Is.EqualTo(1));
        Assert.That(queue.Consume(32), Is.EqualTo(input));
        Assert.That(queue.Consume(33), Is.EqualTo(input));
    }


    [Test]
    public void AcceptAndConsumeAllocateNothingAfterConstruction()
    {
        var queue = new CommandQueue();
        var input = new ShipInputState(Vector2.Right, 0, true, true, false, true);
        for (var tick = 1L; tick <= 1000; tick++)
        {
            queue.Accept(new(tick, input), tick - 1);
            queue.Consume(tick);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        var accepted = true;
        var consumed = true;
        for (var tick = 1001L; tick <= 61000; tick++)
        {
            accepted &= queue.Accept(new(tick, input), tick - 1);
            consumed &= queue.Consume(tick) == input;
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(accepted && consumed, Is.True);
        Assert.That(allocated, Is.Zero);
    }
}

