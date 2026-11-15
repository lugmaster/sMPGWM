using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Tests.NetworkSystem;

public sealed class ShotReplicationTests
{
    private static AcceptedShot Shot(Guid session, long sequence = 1) => new(session, 10, 123, 2, 1, sequence,
        1, 0, Vector2.Zero, new Vector2(10, 20), Vector2.Up, true, true);

    [Test]
    public void ExactMaximumBatchPreservesIndependentShieldContactAndFitsBudget()
    {
        var session = Guid.NewGuid();
        var source = new AcceptedShot[ShotEventCodec.MaxEvents];
        for (var i = 0; i < source.Length; i++) source[i] = Shot(session, i + 1);
        var bytes = ShotEventCodec.Encode(source);
        Assert.That(bytes.Length, Is.EqualTo(1139));
        Assert.That(bytes.Length, Is.LessThanOrEqualTo(ShotEventCodec.MaxBytes));
        var destination = new AcceptedShot[source.Length];
        Assert.That(ShotEventCodec.TryDecode(bytes, destination, out var count), Is.True);
        Assert.That(count, Is.EqualTo(source.Length));
        Assert.That(destination, Is.EqualTo(source));
        var manyFixtures = ShotEventCodec.Encode(new[] { source[0] with { Muzzle = 1000 } });
        Assert.That(ShotEventCodec.TryDecode(manyFixtures, destination, out _), Is.True);
        Assert.That(destination[0].Muzzle, Is.EqualTo(1000));
        Assert.That(ShotEventCodec.TryDecode(bytes.AsSpan(0, bytes.Length - 1), destination, out _), Is.False);
        Assert.That(ShotEventCodec.TryDecode(new byte[1201], destination, out _), Is.False);
    }

    [Test]
    public void WholeBatchRejectsInvalidGeometryFlagsIdentityAndReferences()
    {
        var shot = Shot(Guid.NewGuid());
        Assert.That(ShotEventCodec.IsValid(shot with { Origin = new Vector2(float.NaN, 0) }), Is.False);
        Assert.That(ShotEventCodec.IsValid(shot with { Normal = Vector2.Zero }), Is.False);
        Assert.That(ShotEventCodec.IsValid(shot with { Hit = false }), Is.False);
        Assert.That(ShotEventCodec.IsValid(shot with { Entity = 0 }), Is.False);
        Assert.That(ShotEventCodec.IsValid(shot with { Item = ulong.MaxValue }), Is.False);
        Assert.That(ShotEventCodec.IsValid(shot with { Profile = 2 }), Is.False);
        var bytes = ShotEventCodec.Encode(new[] { shot, shot with { Sequence = 2 } });
        bytes[^1] = 4;
        Assert.That(ShotEventCodec.TryDecode(bytes, new AcceptedShot[2], out var count), Is.False);
        Assert.That(count, Is.Zero);
    }

    [Test]
    public void SequenceWindowAllowsReorderingAndIndependentItemsButRejectsReplayAndOldSession()
    {
        var window = new ShotSequenceWindow(); var session = Guid.NewGuid();
        window.Clear(session, 10);
        Assert.That(window.Accept(session, 1, 1, 10, 20), Is.False);
        Assert.That(window.Accept(session, 1, 3, 20, 20), Is.True);
        Assert.That(window.Accept(session, 1, 1, 19, 20), Is.True);
        Assert.That(window.Accept(session, 1, 1, 19, 20), Is.False);
        Assert.That(window.Accept(session, 2, 1, 19, 20), Is.True);
        Assert.That(window.Accept(Guid.NewGuid(), 2, 2, 20, 20), Is.False);
        Assert.That(window.Accept(session, 1, 70, 20, 20), Is.True);
        Assert.That(window.Accept(session, 1, 2, 19, 20), Is.False);
        window.Clear(session, 20);
        Assert.That(window.Accept(session, 1, 71, 20, 21), Is.False);
        Assert.That(window.Accept(session, 1, 72, 21, 21), Is.True);
    }

    [Test]
    public void ExpiryReclaimsIdentityCapacityWithoutReplayingExpiredEvents()
    {
        var window = new ShotSequenceWindow(); var session = Guid.NewGuid(); window.Clear(session, 0);
        for (ulong i = 1; i <= ShotSequenceWindow.Capacity; i++) Assert.That(window.Accept(session, i, 1, 10, 10), Is.True);
        Assert.That(window.Accept(session, 9000, 1, 10, 10), Is.False);
        Assert.That(window.CapacityDrops, Is.EqualTo(1));
        Assert.That(window.Accept(session, 9000, 1, 200, 200), Is.True);
        Assert.That(window.Accept(session, 1, 1, 10, 200), Is.False);
    }

    [Test]
    public void HeldFireRequiresFreshReleaseAndExpiresWithoutInput()
    {
        var queue = new CommandQueue();
        var fire = new ShipInputState(Vector2.Right, 0, false, false, false, true, 3, 7);
        Assert.That(queue.Accept(new(1, fire), 0), Is.True);
        Assert.That(queue.Consume(1).FireMask, Is.Zero);
        queue.Accept(new(2, fire with { FireMask = 0 }), 1); queue.Consume(2);
        queue.Accept(new(3, fire), 2);
        Assert.That(queue.Consume(3).FireMask, Is.EqualTo(3));
        Assert.That(queue.Consume(10).FireMask, Is.Zero);
        queue.Accept(new(11, fire), 10);
        Assert.That(queue.Consume(11).FireMask, Is.Zero);
    }

    [Test]
    public void RecoveryCannotBeRearmedByReleasedCommandsAlreadyInFlight()
    {
        var queue = new CommandQueue(32);
        var fire = new ShipInputState(Vector2.Zero, 0, false, false, false, true, 1, 1);
        queue.Accept(new(1, default), 0); queue.Consume(1);
        queue.Accept(new(2, fire), 1);
        Assert.That(queue.Consume(2).FireMask, Is.Zero);
        queue.Accept(new(33, default), 32); queue.Consume(33);
        queue.Accept(new(34, fire), 33);
        Assert.That(queue.Consume(34).FireMask, Is.EqualTo(1));
    }

    [Test]
    public void SixRedundantCommandsAndTenShipSnapshotHaveExactBounds()
    {
        var session = Guid.NewGuid(); var entity = Guid.NewGuid().ToString("N");
        var input = new ShipInputState(Vector2.Zero, 0, false, false, false, true, 3, 42);
        var commands = new MovementCommand[MovementWire.MaxCommands];
        for (var i = 0; i < commands.Length; i++) commands[i] = new(i + 1, input);
        var bytes = MovementWire.Commands(session, entity, 1, commands);
        Assert.That(bytes.Length, Is.EqualTo(221));
        Assert.That(MovementWire.ReadCommands(bytes, out _, out _, out _, out var decoded), Is.True);
        Assert.That(decoded, Is.EqualTo(commands));
        Assert.That(MovementWire.Valid(input with { FireMask = 4 }), Is.False);
        Assert.That(MovementWire.Valid(input with { WeaponRevision = -1 }), Is.False);
        var samples = new MovementSample[10];
        for (var i = 0; i < samples.Length; i++) samples[i] = new(i + 1, Guid.NewGuid().ToString("N"), 1, 0,
            Vector2.Zero, 0, Vector2.Zero, input, 1);
        Assert.That(MovementWire.Snapshot(session, 2, samples).Length, Is.EqualTo(1225));
    }
}
