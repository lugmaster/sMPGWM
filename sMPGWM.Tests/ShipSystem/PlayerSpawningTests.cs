using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Tests.ShipSystem;

[Category("Godot")]
public class PlayerSpawningTests
{
    [Test]
    public void StartupMessageSurvivesVariantRoundTrip()
    {
        var original = PlayerStartupData.Create();
        using var payload = original.ToVariant();
        Assert.That(payload.VariantType, Is.EqualTo(Variant.Type.String));
        Assert.That(PlayerStartupData.TryFromVariant(payload, out var received), Is.True);
        Assert.That(received, Is.EqualTo(original));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void InvalidPayloadIsRejected(string message)
    {
        Assert.That(PlayerStartupData.TryFromVariant(message, out _), Is.False);
        Assert.That(PlayerStartupData.TryFromVariant(42, out _), Is.False);
    }

    [Test]
    public void ApprovedRecordRoundTripsAndRejectsMalformedPayloads()
    {
        var original = new ShipSpawnRecord(2, Guid.NewGuid().ToString("N"), "fighter", new Vector2(12, 30), 1.5f, Vector2.Zero);
        using var payload = original.ToVariant();
        Assert.That(ShipSpawnRecord.TryFromVariant(payload, out var parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(original));
        Assert.That(ShipSpawnRecord.TryFromVariant("fighter", out _), Is.False);
        Assert.That(PlayerStartupData.Create().ShipId, Is.EqualTo("fighter"));
    }
    [Test]
    public void ThreeConfiguredShipsHaveIndependentStateAndIdempotentSpawns()
    {
        var manager = new GameWorld();
        try
        {
            var records = Enumerable.Range(1, 3).Select(peer => new ShipSpawnRecord(peer,
                Guid.NewGuid().ToString("N"), "fighter", new Vector2(peer * 140, 300), 0, Vector2.Zero)).ToArray();
            foreach (var record in records) manager.Spawn(record);
            var ship = manager.Players[2].ControlledShip!;
            ship.Position = new Vector2(900, 600);
            manager.Spawn(records[1]);
            Assert.That(manager.Players, Has.Count.EqualTo(3));
            Assert.That(ship.Position, Is.EqualTo(new Vector2(900, 600)));
            Assert.That(ship.Data, Is.SameAs(manager.Players[1].ControlledShip!.Data));
            Assert.That(ship.State, Is.Not.SameAs(manager.Players[1].ControlledShip!.State));
            Assert.That(ship.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon, Has.Length.EqualTo(20));
            Assert.That(ship.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture, Is.Not.Null);
            Assert.That(ship.LocallyControlled, Is.False);
            Assert.Throws<InvalidOperationException>(() => manager.Spawn(records[1] with { EntityId = Guid.NewGuid().ToString("N") }));
            manager.Despawn(2); manager.Despawn(2);
            Assert.That(manager.Players, Has.Count.EqualTo(2));
            manager.Clear(); manager.Clear();
            Assert.That(manager.Players, Is.Empty);
        }
        finally { manager.Free(); }
    }
    [Test]
    public void ShipSimulationIsExplicitAndIndependentOfLocalInputOwnership()
    {
        var world = new Node2D();
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        host.AddChild(world);
        var ship = ShipFactory.Create(ShipRegistry.GetById("fighter"), Guid.NewGuid().ToString("N"),
            Vector2.Zero, 0, Vector2.Zero);
        world.AddChild(ship);
        try
        {
            var input = new ShipInputState(Vector2.Right, Mathf.Pi / 2, true, false, false, true);
            ship.InstallMovementConfiguration(1, new(350, 600, 12, 1.1f, 1.3f, 10, true)); // TEST-SUPPORT: SPAWN-EQUIPMENT-001 | Tests: sMPGWM.Tests.ShipSystem.PlayerSpawningTests.ShipSimulationIsExplicitAndIndependentOfLocalInputOwnership
            ship.SetInput(input);
            ship.Simulate(0.1);
            Assert.That(ship.Rotation, Is.GreaterThan(0));
            Assert.That(ship.Velocity.Length(), Is.GreaterThan(0));

            ship.SetLocalControl(true);
            Assert.That(ship.IsPhysicsProcessing(), Is.False, "Only SynchronizationCoordinator should schedule simulation.");
            ship.SetInput(input);
            ship.Simulate(0.1);
            Assert.That(ship.Rotation, Is.GreaterThan(0));
            Assert.That(ship.Velocity.Length(), Is.GreaterThan(0));

            var rotation = ship.Rotation;
            ship.ClearInput();
            ship.Simulate(0.1);
            Assert.That(ship.Rotation, Is.EqualTo(rotation), "Clearing input must release aiming.");
            ship.SetLocalControl(false);
            var position = ship.Position;
            ship.SetInput(input);
            ship.Simulate(0.1);
            Assert.That(ship.Position, Is.Not.EqualTo(position));
        }
        finally { world.Free(); }
    }

    [Test]
    public void ShipTransferClearsOldAssociationAndSurvivesOldOwnerRemoval()
    {
        var world = new Node2D();
        var first = new PlayerSession { ControllingPeerId = 1 };
        var second = new PlayerSession { ControllingPeerId = 2 };
        var ship = ShipFactory.Create(ShipRegistry.GetById("fighter"), Guid.NewGuid().ToString("N"), new Vector2(20, 30), 0, Vector2.Zero);
        var changes = 0;
        first.ControlledShipChanged += (_, _) => changes++;
        try
        {
            world.AddChild(ship);
            first.AssignShip(ship); first.AssignShip(ship);
            Assert.That(changes, Is.EqualTo(1));
            second.AssignShip(ship);
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(first.ControlledShip, Is.Null);
            Assert.That(ship.GetParent(), Is.SameAs(world));
            first.AssignShip(null);
            Assert.That(GodotObject.IsInstanceValid(ship), Is.True);
        }
        finally { world.Free(); }
    }

    [Test]
    public void ReliableReplacementAdvancesGenerationAndRejectsStaleTopology()
    {
        var manager = new GameWorld();
        try
        {
            var first = new ShipSpawnRecord(1, Guid.NewGuid().ToString("N"), "fighter", Vector2.Zero, 0, Vector2.Zero);
            manager.Spawn(first);
            var replacement = first with { EntityId = Guid.NewGuid().ToString("N"), Position = Vector2.One };
            manager.ApplyTopology(new WorldTopology(new[] { new ParticipantBinding(1, 2, replacement.EntityId) }, new[] { EntitySpawnRecord.From(replacement) }));
            Assert.That(manager.Players[1].ControlGeneration, Is.EqualTo(2));
            Assert.That(manager.Players[1].ControlledShip!.EntityId, Is.EqualTo(replacement.EntityId));
            Assert.Throws<InvalidOperationException>(() => manager.ApplyTopology(
                new WorldTopology(new[] { new ParticipantBinding(1, 1, first.EntityId) }, new[] { EntitySpawnRecord.From(first) })));
            Assert.That(manager.Players[1].ControlledShip!.EntityId, Is.EqualTo(replacement.EntityId));
        }
        finally { manager.Free(); }
    }

    [Test]
    public void SpawnCaptureAndRestoreUseGlobalCoordinatesUnderTransformedParents()
    {
        var world = new Node2D { Position = new Vector2(300, 400), Rotation = .7f };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        var manager = new GameWorld();
        world.AddChild(manager);
        try
        {
            var record = new ShipSpawnRecord(1, Guid.NewGuid().ToString("N"), "fighter", new Vector2(50, 60), .2f, Vector2.One);
            manager.Spawn(record);
            var ship = manager.Players[1].ControlledShip!;
            Assert.That(ship.GlobalPosition.DistanceTo(record.Position), Is.LessThan(.001));
            ship.Restore(new Vector2(90, 100), .3f, Vector2.Zero, default);
            Assert.That(ship.State.Position.DistanceTo(ship.GlobalPosition), Is.LessThan(.001));
            Assert.That(ship.State.Rotation, Is.EqualTo(ship.GlobalRotation).Within(.001));
        }
        finally { world.Free(); }
    }
}
