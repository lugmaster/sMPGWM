using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.MovementSystem;
public class EquipmentMovementTests
{
    [Test]
    public void UnpoweredMovementPreservesVelocityDespiteEveryControl()
    {
        var codec = new sMPGWM.Tests.MovementSystem.MovementCodecFixture();
        var velocity = new Vector2(300, -180);
        var result = MovementPhysics.MovementStep(velocity, Vector2.One, Vector2.Right, Vector2.Down,
            900, 50, .15f, 1f / 60, true, false);
        Assert.That(result, Is.EqualTo(velocity));
        var slow = new Vector2(1, 2);
        Assert.That(MovementPhysics.MovementStep(slow, Vector2.Zero, Vector2.Right, Vector2.Down,
            900, 0, .15f, 1f / 60, true, false), Is.EqualTo(slow));
    }
    [Test]
    public void TenConfiguredShipsFitAndRoundTrip()
    {
        var codec = new sMPGWM.Tests.MovementSystem.MovementCodecFixture();
        var samples = new MovementSample[10];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = new(i + 1, Guid.NewGuid().ToString("N"), 1, 0, new Vector2(i, i), .5f,
                Vector2.One, default, 20, 0, 7, new(350, 600, 3, 2, 4, 9, i % 2 == 0));
        var bytes = codec.Snapshot(Guid.NewGuid(), 20, samples);
        Assert.That(bytes.Length, Is.EqualTo(1105));
        Assert.That(bytes.Length, Is.LessThanOrEqualTo(MovementWire.MaxBytes));
        Assert.That(codec.ReadSnapshot(bytes, out _, out _, out var decoded), Is.True);
        Assert.That(decoded, Is.EqualTo(samples));
    }
    [Test]
    public void MalformedMovementConfigurationIsRejected()
    {
        var codec = new sMPGWM.Tests.MovementSystem.MovementCodecFixture();
        var sample = new MovementSample(1, Guid.NewGuid().ToString("N"), 1, 0, Vector2.Zero, 0,
            Vector2.Zero, default, 20, 0, 7, new(350, 600, 3, 2, 4, 9, true));
        var bytes = codec.Snapshot(Guid.NewGuid(), 20, new[] { sample });
        bytes[^1] = 2;
        Assert.That(codec.ReadSnapshot(bytes, out _, out _, out _), Is.False);
        var invalid = sample with { Configuration = sample.Configuration with { Speed = float.NaN } };
        Assert.That(codec.ReadSnapshot(codec.Snapshot(Guid.NewGuid(), 20, new[] { invalid }),
            out _, out _, out _), Is.False);
        invalid = sample with { ConfigurationRevision = -1 };
        Assert.That(codec.ReadSnapshot(codec.Snapshot(Guid.NewGuid(), 20, new[] { invalid }),
            out _, out _, out _), Is.False);
    }
    [Test, Category("Godot")]
    public void ConfigurationChangesDiscardIncompatiblePredictionHistory()
    {
        var codec = new sMPGWM.Tests.MovementSystem.MovementCodecFixture();
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            ship.InstallMovementConfiguration(1, new(350, 600, 3, 2, 4, 9, true));
            var prediction = new LocalMovementPrediction();
            prediction.ApplyBaseline(0, MovementStateAdapter.Capture(1, owner, 0), ship);
            ship.SetInput(new(Vector2.Right, 0, false, false, false, false));
            for (var tick = 1; tick <= 6; tick++) prediction.Step(1, owner);
            var authoritative = MovementStateAdapter.Capture(1, owner, 4) with
            {
                Position = new Vector2(12, 20), ConfigurationRevision = 2,
                Configuration = ship.MovementConfiguration with { Enabled = false }
            };
            Assert.That(prediction.Reconcile(1, owner, 4, authoritative), Is.True);
            Assert.That(prediction.Tick, Is.EqualTo(4));
            Assert.That(prediction.CopyRecentCommands(new MovementCommand[MovementWire.MaxCommands]), Is.Zero);
            Assert.That(ship.GlobalPosition, Is.EqualTo(authoritative.Position));
            Assert.That(ship.MovementConfiguration.Enabled, Is.False);
            var energy = ship.Runtime.Energy.Current;
            prediction.Step(1, owner);
            Assert.That(ship.Runtime.Energy.Current, Is.EqualTo(energy));
            Assert.That(prediction.Reconcile(1, owner, 3, authoritative with { ConfigurationRevision = 1 }), Is.True);
            Assert.That(ship.MovementConfigurationRevision, Is.EqualTo(2));
        }
        finally { world.Free(); }
    }
}

