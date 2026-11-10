using System.Reflection; // TEST-SUPPORT: GRANT-LIFECYCLE-001 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using Godot; // TEST-SUPPORT: GRANT-LIFECYCLE-002 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: GRANT-LIFECYCLE-003 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.Items; // TEST-SUPPORT: GRANT-LIFECYCLE-004 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.Items.Equipment; // TEST-SUPPORT: GRANT-LIFECYCLE-005 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.Items.Inventory; // TEST-SUPPORT: GRANT-LIFECYCLE-006 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: GRANT-LIFECYCLE-007 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
using sMPGWM.Scripts.World; // TEST-SUPPORT: GRANT-LIFECYCLE-008 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp

namespace sMPGWM.Tests; // TEST-SUPPORT: GRANT-LIFECYCLE-009 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp

[Category("Godot")] // TEST-SUPPORT: GRANT-LIFECYCLE-010 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
public sealed class EquipmentGrantLifecycleTests // TEST-SUPPORT: GRANT-LIFECYCLE-011 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
{
    [Test] // TEST-SUPPORT: GRANT-LIFECYCLE-012 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
    public void UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp() // TEST-SUPPORT: GRANT-LIFECYCLE-013 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
    {
        var world = new GameWorld(); // TEST-SUPPORT: GRANT-LIFECYCLE-014 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: GRANT-LIFECYCLE-015 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        using var host = new PlayerItemState(); // TEST-SUPPORT: GRANT-LIFECYCLE-016 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        using var client = new PlayerItemState(); // TEST-SUPPORT: GRANT-LIFECYCLE-017 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        try // TEST-SUPPORT: GRANT-LIFECYCLE-018 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        {
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: GRANT-LIFECYCLE-019 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var owner = world.Players[1]; // TEST-SUPPORT: GRANT-LIFECYCLE-020 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var ship = owner.ControlledShip!; // TEST-SUPPORT: GRANT-LIFECYCLE-021 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            client.Bind(owner); // TEST-SUPPORT: GRANT-LIFECYCLE-022 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            ship.Gameplay.Grants.SetSource("skill:pilot", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: GRANT-LIFECYCLE-023 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(host.Store.CreateItems(host.Inventory.Container, "mock_standard_shield_generator", 1), // TEST-SUPPORT: GRANT-LIFECYCLE-024 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: GRANT-LIFECYCLE-025 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var generator = host.Inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: GRANT-LIFECYCLE-026 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var slot = ItemCellAddress.Equipment("shield_generator"); // TEST-SUPPORT: GRANT-LIFECYCLE-027 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(host.Transfer(new(0, ItemCellAddress.Inventory(0), generator.Id, slot, null, // TEST-SUPPORT: GRANT-LIFECYCLE-028 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                host.Inventory.Revision, host.Equipment.Revision)), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: GRANT-LIFECYCLE-029 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host), client), Is.True); // TEST-SUPPORT: GRANT-LIFECYCLE-030 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var source = "item:" + generator.Id; // TEST-SUPPORT: GRANT-LIFECYCLE-031 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var grants = (Dictionary<string, string[]>)typeof(AbilityGrants) // TEST-SUPPORT: GRANT-LIFECYCLE-032 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                .GetField("_sources", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ship.Gameplay.Grants)!; // TEST-SUPPORT: GRANT-LIFECYCLE-033 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var original = grants[source]; // TEST-SUPPORT: GRANT-LIFECYCLE-034 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(host.Store.CreateItems(host.Inventory.Container, "mock_standard_consumable", 7), // TEST-SUPPORT: GRANT-LIFECYCLE-035 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: GRANT-LIFECYCLE-036 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host), client), Is.True); // TEST-SUPPORT: GRANT-LIFECYCLE-037 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(grants[source], Is.SameAs(original)); // TEST-SUPPORT: GRANT-LIFECYCLE-038 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            client.Equipment.NotifyObservers(); // TEST-SUPPORT: GRANT-LIFECYCLE-039 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var allocated = GC.GetAllocatedBytesForCurrentThread(); // TEST-SUPPORT: GRANT-LIFECYCLE-040 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            for (var i = 0; i < 120; i++) client.Equipment.NotifyObservers(); // TEST-SUPPORT: GRANT-LIFECYCLE-041 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            var unchangedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated; // TEST-SUPPORT: GRANT-LIFECYCLE-042 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(grants[source], Is.SameAs(original)); // TEST-SUPPORT: GRANT-LIFECYCLE-043 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(unchangedBytes, Is.Zero); // TEST-SUPPORT: GRANT-LIFECYCLE-044 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp

            var replacement = world.SpawnEntity(new(Guid.NewGuid().ToString("N"), "fighter", Vector2.Zero, 0, Vector2.Zero)); // TEST-SUPPORT: GRANT-LIFECYCLE-045 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            owner.AssignShip(replacement); // TEST-SUPPORT: GRANT-LIFECYCLE-046 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Gameplay.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False); // TEST-SUPPORT: GRANT-LIFECYCLE-047 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Gameplay.Grants.ContainsSource("skill:pilot", "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-LIFECYCLE-048 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(replacement.Gameplay.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-LIFECYCLE-049 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(host.Transfer(new(0, slot, generator.Id, ItemCellAddress.Inventory(1), null, // TEST-SUPPORT: GRANT-LIFECYCLE-050 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                host.Inventory.Revision, host.Equipment.Revision)), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: GRANT-LIFECYCLE-051 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host), client), Is.True); // TEST-SUPPORT: GRANT-LIFECYCLE-052 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(replacement.Gameplay.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False); // TEST-SUPPORT: GRANT-LIFECYCLE-053 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(host.Transfer(new(0, ItemCellAddress.Inventory(1), generator.Id, slot, null, // TEST-SUPPORT: GRANT-LIFECYCLE-054 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
                host.Inventory.Revision, host.Equipment.Revision)), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: GRANT-LIFECYCLE-055 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host), client), Is.True); // TEST-SUPPORT: GRANT-LIFECYCLE-056 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            client.Dispose(); // TEST-SUPPORT: GRANT-LIFECYCLE-057 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(replacement.Gameplay.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False); // TEST-SUPPORT: GRANT-LIFECYCLE-058 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            owner.AssignShip(ship); // TEST-SUPPORT: GRANT-LIFECYCLE-059 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            Assert.That(ship.Gameplay.Grants.ContainsSource(source, "shieldCapacityBoost"), Is.False); // TEST-SUPPORT: GRANT-LIFECYCLE-060 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        }
        finally // TEST-SUPPORT: GRANT-LIFECYCLE-061 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        {
            client.Dispose(); // TEST-SUPPORT: GRANT-LIFECYCLE-062 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
            world.Free(); // TEST-SUPPORT: GRANT-LIFECYCLE-063 | Tests: sMPGWM.Tests.EquipmentGrantLifecycleTests.UnchangedSnapshotsPreserveGrantsAndOwnershipChangesCleanUp
        }
    }
}

