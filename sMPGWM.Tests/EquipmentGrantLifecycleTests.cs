using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: OWNERSHIP-TYPED-0 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using System.Reflection; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-0 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using Godot; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-1 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-2 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
// TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-3 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
// TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-4 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
// TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-5 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-6 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-7 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
namespace sMPGWM.Tests; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-8 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
[Category("Godot")] // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-9 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
public sealed class EquipmentGrantLifecycleTests // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-10 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
{
    [Test] // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-12 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
    public void UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp() // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-13 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
    {
        var world = new GameWorld { AuthoritativeItems = true }; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-15 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        var clientWorld = new GameWorld(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-16 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-17 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        root.AddChild(world); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-18 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        root.AddChild(clientWorld); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-19 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        try // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-20 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        {
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-22 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            world.ApproveSpawn(2, new("fighter")); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-23 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            clientWorld.ApplyTopology(world.CaptureTopology()); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-24 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var owner = world.Players[1]; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-25 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var other = world.Players[2]; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-26 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var ship = owner.ControlledShip!; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-27 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var replacement = other.ControlledShip!; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-28 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var client = clientWorld.Players[1]; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-29 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var clientShip = client.ControlledShip!; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-30 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            ship.Runtime.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-31 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(world.Items.CreateItems(owner.Inventory.Container, "mock_standard_shield_generator", 1), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-32 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var generator = owner.Inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-33 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var slot = ItemCellAddress.Equipment("shield_generator"); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-34 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(owner.Transfer(new(0, ItemCellAddress.Inventory(0), generator.Id, slot, null, // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-35 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                owner.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-36 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            ship.Loadout.InitializeResources(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-37 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            ship.Runtime.SetShieldCurrent(17); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-38 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(owner), client), Is.True); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-39 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var source = "item:" + generator.Id; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-40 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var grants = (Dictionary<StatContributionSource, string[]>)typeof(AbilityGrants) // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-41 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                .GetField("_ownedSources", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(clientShip.Runtime.Grants)!; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-42 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var typedSource = new StatContributionSource(StatContributionKind.Equipment, (long)generator.Id.Value); // TEST-SUPPORT: OWNERSHIP-TYPED-44 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var original = grants[typedSource]; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-43 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            world.Items.CreateItems(owner.Inventory.Container, "mock_standard_consumable", 7); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-44 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(owner), client), Is.True); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-45 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(grants[typedSource], Is.SameAs(original)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-46 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            clientShip.Equipment.NotifyObservers(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-47 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var allocated = GC.GetAllocatedBytesForCurrentThread(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-48 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            for (var i = 0; i < 120; i++) clientShip.Equipment.NotifyObservers(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-49 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - allocated, Is.Zero); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-50 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var personal = owner.Inventory.Snapshot; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-51 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var stats = ship.Stats; // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-52 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            owner.AssignShip(replacement); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-53 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            other.AssignShip(ship); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-54 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(owner.Inventory.Snapshot, Is.SameAs(personal)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-55 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("shield_generator")]!.Id, Is.EqualTo(generator.Id)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-56 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-57 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Runtime.Grants.ContainsSource("skill:pilot", "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-58 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(replacement.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-59 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-60 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Resources.Shield.Current, Is.EqualTo(17)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-61 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            other.AssignShip(null); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-62 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            other.AssignShip(ship); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-63 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Resources.Shield.Current, Is.EqualTo(17)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-64 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            owner.Dispose(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-65 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-66 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            ship.Loadout.Dispose(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-67 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Runtime.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-68 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Runtime.Grants.ContainsSource("skill:pilot", "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-69 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.Throws<KeyNotFoundException>(() => world.Items.GetItem(generator.Id)); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-70 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        }
        finally // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-72 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        {
            world.Free(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-74 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            clientWorld.Free(); // TEST-SUPPORT: SHIP-OWNERSHIP-LIFECYCLE-75 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        }
    }
}
