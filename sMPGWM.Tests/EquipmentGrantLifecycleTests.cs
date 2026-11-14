using sMPGWM.Scripts.StatSystem;
using System.Reflection;
using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;
namespace sMPGWM.Tests;
[Category("Godot")]
public sealed class EquipmentGrantLifecycleTests
{
    [Test]
    public void UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp()
    {
        var world = new GameWorld { AuthoritativeItems = true };
        var clientWorld = new GameWorld();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(world);
        root.AddChild(clientWorld);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            world.ApproveSpawn(2, new("fighter"));
            clientWorld.ApplyTopology(world.CaptureTopology());
            var owner = world.Players[1];
            var other = world.Players[2];
            var ship = owner.ControlledShip!;
            var replacement = other.ControlledShip!;
            var client = clientWorld.Players[1];
            var clientShip = client.ControlledShip!;
            ship.Runtime.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" });
            Assert.That(world.Items.CreateItems(owner.Inventory.Container, "mock_standard_shield_generator", 1), Is.EqualTo(InventoryResult.Success));
            var generator = owner.Inventory.Snapshot.Slots[0]!;
            var slot = ItemCellAddress.Equipment("shield_generator");
            Assert.That(owner.Transfer(new(0, ItemCellAddress.Inventory(0), generator.Id, slot, null,
                owner.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
            ship.Loadout.InitializeResources();
            ship.Runtime.SetShieldCurrent(17);
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(owner), client), Is.True);
            var source = "item:" + generator.Id;
            var grants = (Dictionary<StatContributionSource, string[]>)typeof(AbilityGrants)
                .GetField("_ownedSources", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(clientShip.Runtime.Grants)!;
            var typedSource = new StatContributionSource(StatContributionKind.Equipment, (long)generator.Id.Value);
            var original = grants[typedSource];
            world.Items.CreateItems(owner.Inventory.Container, "mock_standard_consumable", 7);
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(owner), client), Is.True);
            Assert.That(grants[typedSource], Is.SameAs(original));
            clientShip.Equipment.NotifyObservers();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 120; i++) clientShip.Equipment.NotifyObservers();
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - allocated, Is.Zero);
            var personal = owner.Inventory.Snapshot;
            var stats = ship.Stats;
            owner.AssignShip(replacement);
            other.AssignShip(ship);
            Assert.That(owner.Inventory.Snapshot, Is.SameAs(personal));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("shield_generator")]!.Id, Is.EqualTo(generator.Id));
            Assert.That(ship.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.True);
            Assert.That(ship.Runtime.Grants.ContainsSource("skill:pilot", "shieldCapacityBoost"), Is.True);
            Assert.That(replacement.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False);
            Assert.That(ship.Stats, Is.EqualTo(stats));
            Assert.That(ship.Resources.Shield.Current, Is.EqualTo(17));
            other.AssignShip(null);
            other.AssignShip(ship);
            Assert.That(ship.Resources.Shield.Current, Is.EqualTo(17));
            owner.Dispose();
            Assert.That(ship.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.True);
            ship.Loadout.Dispose();
            Assert.That(ship.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False);
            Assert.That(ship.Runtime.Grants.ContainsSource("skill:pilot", "shieldCapacityBoost"), Is.True);
            Assert.Throws<KeyNotFoundException>(() => world.Items.GetItem(generator.Id));
        }
        finally
        {
            world.Free();
            clientWorld.Free();
        }
    }
}
