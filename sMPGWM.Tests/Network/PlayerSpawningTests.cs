using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.Network;

namespace sMPGWM.Tests.Network;

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
        var manager = new MpManager();
        try
        {
            var records = Enumerable.Range(1, 3).Select(peer => new ShipSpawnRecord(peer,
                Guid.NewGuid().ToString("N"), "fighter", new Vector2(peer * 140, 300), 0, Vector2.Zero)).ToArray();
            foreach (var record in records) manager.Spawn(record);
            var ship = manager.MpControllers[2].ControlledShip!;
            ship.Position = new Vector2(900, 600);
            manager.Spawn(records[1]);
            Assert.That(manager.MpControllers, Has.Count.EqualTo(3));
            Assert.That(ship.Position, Is.EqualTo(new Vector2(900, 600)));
            Assert.That(ship.Data, Is.SameAs(manager.MpControllers[1].ControlledShip!.Data));
            Assert.That(ship.State, Is.Not.SameAs(manager.MpControllers[1].ControlledShip!.State));
            Assert.That(ship.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon, Has.Length.EqualTo(20));
            Assert.That(ship.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture, Is.Not.Null);
            Assert.That(ship.LocallyControlled, Is.False);
            Assert.Throws<InvalidOperationException>(() => manager.Spawn(records[1] with { EntityId = Guid.NewGuid().ToString("N") }));
            manager.Despawn(2); manager.Despawn(2);
            Assert.That(manager.MpControllers, Has.Count.EqualTo(2));
            manager.Clear(); manager.Clear();
            Assert.That(manager.MpControllers, Is.Empty);
        }
        finally { manager.Free(); }
    }
    [Test]
    public void ShipTransferClearsOldAssociationAndSurvivesOldOwnerRemoval()
    {
        var world = new Node2D();
        var first = new MpController { ControllingPeerId = 1 };
        var second = new MpController { ControllingPeerId = 2 };
        world.AddChild(first); world.AddChild(second);
        var ship = ShipFactory.Create(ShipRegistry.GetById("fighter"), Guid.NewGuid().ToString("N"), new Vector2(20, 30), 0, Vector2.Zero);
        var changes = 0;
        first.ControlledShipChanged += (_, _) => changes++;
        try
        {
            first.AssignShip(ship); first.AssignShip(ship);
            Assert.That(changes, Is.EqualTo(1));
            second.AssignShip(ship);
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(first.ControlledShip, Is.Null);
            Assert.That(ship.GetParent(), Is.SameAs(second));
            first.Free();
            Assert.That(GodotObject.IsInstanceValid(ship), Is.True);
        }
        finally { world.Free(); }
    }
}
