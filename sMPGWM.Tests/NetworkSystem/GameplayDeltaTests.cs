using Godot;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class GameplayDeltaTests
{
    [Test]
    public void ResourceChangesDoNotResendConfigurationOrEffectsAndRoundTrip()
    {
        var definitions = GameplayTestData.Load();
        var world = new GameWorld();
        using var publisher = new GameplayDeltaState();
        using var receiver = new GameplayDeltaReceiver();
        using var host = new ShipRuntime(100);
        host.ApplyEquipment(new ResolvedShipStats(10, 20, 1, 1, 1, 10, 0, 100,
            1000, 1000, 120, 0, 100, true, true, true, true, true), 1);
        host.InitializeResources();
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var ship = world.Players[1].ControlledShip!;
            string? Resolve(int index) => index == 1 ? ship.EntityId : null;
            var first = publisher.Encode(1, host)!;
            Assert.That(first[3].AsGodotArray().Count, Is.EqualTo(6));
            Assert.That(first[4].AsGodotArray().Count, Is.EqualTo(2));
            Assert.That(receiver.Apply(new() { first }, world, definitions, Resolve), Is.True);
            host.AdvanceResources(1);
            var delta = publisher.Encode(1, host)!;
            Assert.That(delta[3].AsGodotArray(), Is.Empty);
            Assert.That(delta[4].AsGodotArray(), Is.Empty);
            Assert.That(receiver.Apply(new() { delta }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.Energy.Current, Is.EqualTo(host.Energy.Current));
            Assert.That(publisher.Encode(1, host), Is.Null);

            host.SetFaction(7);
            var config = publisher.Encode(1, host)!;
            Assert.That(config[3].AsGodotArray().Count, Is.EqualTo(6));
            Assert.That(config[4].AsGodotArray(), Is.Empty);
            Assert.That(receiver.Apply(new() { config }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.FactionId, Is.EqualTo(7));

            host.SetCondition(definitions.Conditions.GetById("dead"), true);
            var death = publisher.Encode(1, host)!;
            Assert.That(death[4].AsGodotArray().Count, Is.EqualTo(2));
            Assert.That(receiver.Apply(new() { death }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.IsAlive, Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void InvalidResourceBatchDoesNotPartiallyApplyAndSeedSupportsLateJoin()
    {
        var definitions = GameplayTestData.Load();
        var world = new GameWorld();
        using var publisher = new GameplayDeltaState();
        using var receiver = new GameplayDeltaReceiver();
        using var host = new ShipRuntime(100);
        host.InitializeResources();
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var ship = world.Players[1].ControlledShip!;
            string? Resolve(int index) => index == 1 ? ship.EntityId : null;
            Assert.That(receiver.Apply(new() { publisher.Encode(1, host)! }, world, definitions, Resolve), Is.True);
            receiver.Seed(ship.EntityId, ship.Runtime);
            host.SetHealthCurrent(90);
            var row = publisher.Encode(1, host)!;
            var revision = ship.Runtime.Revision;
            using var duplicate = row.Duplicate(true);
            Assert.That(receiver.Apply(new() { row, duplicate }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
            var values = duplicate[5].AsGodotArray(); values[9] = double.NaN;
            Assert.That(receiver.Apply(new() { duplicate }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
            Assert.That(receiver.Apply(new() { row }, world, definitions, Resolve), Is.True);
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(90));
            host.AdvanceResources(10);
            host.SetHealthCurrent(80);
            var next = publisher.Encode(1, host)!;
            using var missingConfiguration = next.Duplicate(true);
            missingConfiguration[1] = host.Revision;
            Assert.That(receiver.Apply(new() { missingConfiguration }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(90));
            Assert.That(receiver.Apply(new() { next }, world, definitions, Resolve), Is.True);
            host.SetHealthCurrent(70);
            var later = publisher.Encode(1, host)!;
            using var backwards = later.Duplicate(true);
            backwards[5].AsGodotArray()[4] = 9L;
            Assert.That(receiver.Apply(new() { backwards }, world, definitions, Resolve), Is.False);
            Assert.That(ship.Runtime.Health.Current, Is.EqualTo(80));
            Assert.That(receiver.Apply(new() { later }, world, definitions, Resolve), Is.True);
        }
        finally { world.Clear(); world.Free(); }
    }
}
