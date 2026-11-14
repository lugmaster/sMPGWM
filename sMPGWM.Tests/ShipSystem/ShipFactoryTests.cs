using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.ShipSystem;

[Category("Godot")]
public class ShipFactoryTests
{
    [Test]
    public void LoadoutRequiresExplicitConfiguration()
    {
        var ship = new ShipController();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(ship);
        var store = new ItemStore();
        try
        {
            Assert.Throws<InvalidOperationException>(() => _ = ship.Loadout);
            Assert.Throws<InvalidOperationException>(() => _ = ship.Equipment);
            ship.ConfigureLoadout(store, false);
            var loadout = ship.Loadout;
            Assert.That(loadout.Store, Is.SameAs(store));
            ship.ConfigureLoadout(store, false);
            Assert.That(ship.Loadout, Is.SameAs(loadout));
            Assert.Throws<InvalidOperationException>(() => ship.ConfigureLoadout(new ItemStore(), false));
        }
        finally { ship.Free(); }
        Assert.That(store.IsEmpty, Is.True);
    }

    [Test]
    public void FailureBeforeLoadoutPreservesCauseAndReleasesNode()
    {
        GamePlayProvider.CreateShip().Free();
        var data = ShipRegistry.GetById("fighter");
        var store = new ItemStore();
        using var inventory = new InventoryController(store);
        InvalidOperationException? failure = null;
        inventory.Changed += _ => failure = Assert.Throws<InvalidOperationException>(() =>
            ShipFactory.Create(data, "failed-before-loadout", Vector2.Zero, 0, Vector2.Zero, store, false));
        var orphanCount = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
        Assert.That(store.CreateItems(inventory.Container, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success));
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(failure.InnerException!.Message, Is.EqualTo("Cannot create storage during publication."));
        Assert.That(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount), Is.EqualTo(orphanCount));
        inventory.Dispose();
        Assert.That(store.IsEmpty, Is.True);
    }

    [Test]
    public void FailureAfterLoadoutReleasesStoreAndAllowsRetry()
    {
        GamePlayProvider.CreateShip().Free();
        var data = ShipRegistry.GetById("fighter");
        var invalid = new ShipData(new ShipDefinition { Name = "invalid-texture", Health = data.Health, TexturePath = "res://scenes/game/game_objects/ship.tscn" }, Array.Empty<Vector2>(), data.Stats);
        var store = new ItemStore();
        var orphanCount = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);
        var failure = Assert.Throws<InvalidOperationException>(() => ShipFactory.Create(invalid, "failed-after-loadout", Vector2.Zero, 0, Vector2.Zero, store, false));
        Assert.That(failure!.InnerException, Is.TypeOf<InvalidCastException>());
        Assert.That(store.IsEmpty, Is.True);
        Assert.That(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount), Is.EqualTo(orphanCount));
        var ship = ShipFactory.Create(data, "retry", Vector2.Zero, 0, Vector2.Zero, store, false);
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(ship);
        try { Assert.That(ship.Loadout.Store, Is.SameAs(store)); }
        finally { ship.Free(); }
        Assert.That(store.IsEmpty, Is.True);
    }
}
