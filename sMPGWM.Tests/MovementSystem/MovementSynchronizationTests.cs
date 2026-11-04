using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.MovementSystem;

public class MovementSynchronizationTests
{
    [Test]
    public void InterpolationWrapsAnglesHoldsUnderrunsAndSnapsDiscontinuities()
    {
        var lower = new MovementSample(1, Guid.NewGuid().ToString("N"), 1, 0,
            Vector2.Zero, Mathf.DegToRad(179), Vector2.Zero, default, 0, 1);
        var upper = lower with { Position = new Vector2(10, 0), Rotation = Mathf.DegToRad(-179), AppliedFlags = 4 };
        var middle = MovementPresentation.Sample(2, lower, 6, upper, 4);
        Assert.That(middle.Position, Is.EqualTo(new Vector2(5, 0)));
        Assert.That(Math.Abs(Math.Abs(middle.Rotation) - Mathf.Pi), Is.LessThan(.001));
        Assert.That(middle.AppliedFlags, Is.EqualTo(1));
        Assert.That(MovementPresentation.Sample(2, lower, 6, upper, 50).Position, Is.EqualTo(upper.Position));
        Assert.That(MovementPresentation.Sample(2, lower, 6, upper, 50).AppliedFlags, Is.EqualTo(4));
        Assert.That(MovementPresentation.Sample(2, lower, 6, upper with { Revision = 1 }, 4).Position, Is.EqualTo(upper.Position));
    }
    private static ShipInputState Thrust => new(Vector2.Right, 0, true, true, false, true);
    [Test]
    public void QueueEnforcesDeadlinesDuplicatesBoundsAndRelease()
    {
        var q = new CommandQueue();
        Assert.That(q.Accept(new(10, Thrust), 10), Is.False);
        Assert.That(q.Accept(new(43, Thrust), 10), Is.False);
        Assert.That(q.Accept(new(11, Thrust), 10), Is.True);
        Assert.That(q.Accept(new(11, Thrust), 10), Is.True);
        Assert.That(q.Accept(new(11, default), 10), Is.False);
        Assert.That(q.Consume(11), Is.EqualTo(Thrust));
        for (int tick = 12; tick <= 17; tick++) Assert.That(q.Consume(tick), Is.EqualTo(Thrust));
        var released = q.Consume(18);
        Assert.That(released.Direction, Is.EqualTo(Vector2.Zero));
        Assert.That(released.HasAim || released.Afterburner || released.Brake, Is.False);
        Assert.That(released.AutomaticBraking, Is.True);
    }
    [Test]
    public void CommandsPreserveTransitionsAndRejectMalformedData()
    {
        var id = Guid.NewGuid(); var entity = Guid.NewGuid().ToString("N");
        var commands = new[] { new MovementCommand(20, Thrust), new MovementCommand(21, default) };
        var bytes = MovementWire.Commands(id, entity, 3, commands);
        Assert.That(MovementWire.ReadCommands(bytes, out var session, out var parsedEntity, out var generation, out var parsed), Is.True);
        Assert.That((session, parsedEntity, generation), Is.EqualTo((id, entity, 3L)));
        Assert.That(parsed, Is.EqualTo(commands));
        Assert.That(MovementWire.ReadCommands(bytes[..^1], out _, out _, out _, out _), Is.False);
        bytes[^1] = 255;
        Assert.That(MovementWire.ReadCommands(bytes, out _, out _, out _, out _), Is.False);
        Assert.That(MovementWire.Valid(Thrust with { AimAngle = float.NaN }), Is.False);
        Assert.That(MovementWire.Valid(Thrust with { Direction = new Vector2(2, 0) }), Is.False);
    }
    [Test]
    public void FullCapacitySnapshotFitsOneDatagramAndRejectsDuplicateIdentity()
    {
        var session = Guid.NewGuid();
        var samples = Enumerable.Range(1, 10).Select(p => new MovementSample(p, Guid.NewGuid().ToString("N"), 1, 0,
            new Vector2(p, 2), 3, Vector2.One, Thrust, 10)).ToArray();
        var bytes = MovementWire.Snapshot(session, 12, samples);
        Assert.That(bytes.Length, Is.LessThanOrEqualTo(MovementWire.MaxBytes));
        Assert.That(MovementWire.ReadSnapshot(bytes, out var id, out var tick, out var parsed), Is.True);
        Assert.That(id, Is.EqualTo(session)); Assert.That(tick, Is.EqualTo(12)); Assert.That(parsed, Is.EqualTo(samples));
        samples[1] = samples[0];
        Assert.That(MovementWire.ReadSnapshot(MovementWire.Snapshot(session, 12, samples), out _, out _, out _), Is.False);
        Assert.That(MovementWire.ReadSnapshot(bytes.Concat(new byte[1]).ToArray(), out _, out _, out _), Is.False);
    }
}
