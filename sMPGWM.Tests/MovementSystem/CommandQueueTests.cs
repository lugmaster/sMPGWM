using Godot; // TEST-SUPPORT: COMMAND-RING-001 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound, sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands, sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected, sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
using sMPGWM.Scripts.MovementSystem; // TEST-SUPPORT: COMMAND-RING-002 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound, sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands, sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected, sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: COMMAND-RING-003 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound, sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands, sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected, sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction

namespace sMPGWM.Tests.MovementSystem; // TEST-SUPPORT: COMMAND-RING-004 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound, sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands, sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected, sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction

public sealed class CommandQueueTests // TEST-SUPPORT: COMMAND-RING-005 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound, sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands, sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected, sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
{

    [Test] // TEST-SUPPORT: COMMAND-RING-006 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
    public void FullWindowPreservesOutOfOrderCommandsAcrossWraparound() // TEST-SUPPORT: COMMAND-RING-007 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
    {
        var queue = new CommandQueue(); // TEST-SUPPORT: COMMAND-RING-008 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
        for (var completed = 0L; completed < 3200; completed += 32) // TEST-SUPPORT: COMMAND-RING-009 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
        {
            for (var offset = 32; offset > 0; offset--) // TEST-SUPPORT: COMMAND-RING-010 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
            {
                var input = new ShipInputState(Vector2.Right, offset, true, false, false, true); // TEST-SUPPORT: COMMAND-RING-011 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
                Assert.That(queue.Accept(new(completed + offset, input), completed), Is.True); // TEST-SUPPORT: COMMAND-RING-012 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
            }
            Assert.That(queue.Accept(new(completed + 33, default), completed), Is.False); // TEST-SUPPORT: COMMAND-RING-013 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
            for (var offset = 1; offset <= 32; offset++) // TEST-SUPPORT: COMMAND-RING-014 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
                Assert.That(queue.Consume(completed + offset).AimAngle, Is.EqualTo(offset)); // TEST-SUPPORT: COMMAND-RING-015 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.FullWindowPreservesOutOfOrderCommandsAcrossWraparound
        }
    }


    [Test] // TEST-SUPPORT: COMMAND-RING-016 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
    public void ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands() // TEST-SUPPORT: COMMAND-RING-017 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
    {
        var queue = new CommandQueue(); // TEST-SUPPORT: COMMAND-RING-018 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        var input = new ShipInputState(Vector2.Right, 0, true, true, false, true); // TEST-SUPPORT: COMMAND-RING-019 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        for (var tick = 1; tick <= 32; tick++) // TEST-SUPPORT: COMMAND-RING-020 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
            Assert.That(queue.Accept(new(tick, default), 0), Is.True); // TEST-SUPPORT: COMMAND-RING-021 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        Assert.That(queue.Accept(new(65, input), 64), Is.True); // TEST-SUPPORT: COMMAND-RING-022 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        Assert.That(queue.Consume(33), Is.EqualTo(default(ShipInputState))); // TEST-SUPPORT: COMMAND-RING-023 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        Assert.That(queue.Consume(65), Is.EqualTo(input)); // TEST-SUPPORT: COMMAND-RING-024 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        Assert.That(queue.Consume(71), Is.EqualTo(input)); // TEST-SUPPORT: COMMAND-RING-025 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        Assert.That(queue.Consume(72).Direction, Is.EqualTo(Vector2.Zero)); // TEST-SUPPORT: COMMAND-RING-026 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
        Assert.That(queue.Consume(72).AutomaticBraking, Is.True); // TEST-SUPPORT: COMMAND-RING-027 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.ExpiredSlotsCanBeReusedWithoutConsumingFutureCommands
    }


    [Test] // TEST-SUPPORT: COMMAND-RING-028 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
    public void DuplicatesDoNotRefreshArrivalAndConflictsAreRejected() // TEST-SUPPORT: COMMAND-RING-029 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
    {
        var queue = new CommandQueue(); // TEST-SUPPORT: COMMAND-RING-030 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        var input = new ShipInputState(Vector2.Right, 0, false, false, false, true); // TEST-SUPPORT: COMMAND-RING-031 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.Accept(new(32, input), 0), Is.True); // TEST-SUPPORT: COMMAND-RING-032 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.Accept(new(32, input), 1), Is.True); // TEST-SUPPORT: COMMAND-RING-033 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.LatestArrival, Is.Zero); // TEST-SUPPORT: COMMAND-RING-034 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.Accept(new(32, default), 1), Is.False); // TEST-SUPPORT: COMMAND-RING-035 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.Accept(new(33, input), 1), Is.True); // TEST-SUPPORT: COMMAND-RING-036 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.LatestArrival, Is.EqualTo(1)); // TEST-SUPPORT: COMMAND-RING-037 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.Consume(32), Is.EqualTo(input)); // TEST-SUPPORT: COMMAND-RING-038 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
        Assert.That(queue.Consume(33), Is.EqualTo(input)); // TEST-SUPPORT: COMMAND-RING-039 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.DuplicatesDoNotRefreshArrivalAndConflictsAreRejected
    }


    [Test] // TEST-SUPPORT: COMMAND-RING-040 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
    public void AcceptAndConsumeAllocateNothingAfterConstruction() // TEST-SUPPORT: COMMAND-RING-041 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
    {
        var queue = new CommandQueue(); // TEST-SUPPORT: COMMAND-RING-042 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        var input = new ShipInputState(Vector2.Right, 0, true, true, false, true); // TEST-SUPPORT: COMMAND-RING-043 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        for (var tick = 1L; tick <= 1000; tick++) // TEST-SUPPORT: COMMAND-RING-044 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        {
            queue.Accept(new(tick, input), tick - 1); // TEST-SUPPORT: COMMAND-RING-045 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
            queue.Consume(tick); // TEST-SUPPORT: COMMAND-RING-046 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        }
        var before = GC.GetAllocatedBytesForCurrentThread(); // TEST-SUPPORT: COMMAND-RING-047 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        var accepted = true; // TEST-SUPPORT: COMMAND-RING-048 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        var consumed = true; // TEST-SUPPORT: COMMAND-RING-049 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        for (var tick = 1001L; tick <= 61000; tick++) // TEST-SUPPORT: COMMAND-RING-050 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        {
            accepted &= queue.Accept(new(tick, input), tick - 1); // TEST-SUPPORT: COMMAND-RING-051 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
            consumed &= queue.Consume(tick) == input; // TEST-SUPPORT: COMMAND-RING-052 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before; // TEST-SUPPORT: COMMAND-RING-053 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        Assert.That(accepted && consumed, Is.True); // TEST-SUPPORT: COMMAND-RING-054 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
        Assert.That(allocated, Is.Zero); // TEST-SUPPORT: COMMAND-RING-055 | Tests: sMPGWM.Tests.MovementSystem.CommandQueueTests.AcceptAndConsumeAllocateNothingAfterConstruction
    }
}

