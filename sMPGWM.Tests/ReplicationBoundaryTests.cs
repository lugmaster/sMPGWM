using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: REPLICATION-BOUNDARY-001 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: REPLICATION-BOUNDARY-002 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
using sMPGWM.Scripts.Items; // TEST-SUPPORT: REPLICATION-BOUNDARY-003 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
using sMPGWM.Scripts.Items.Equipment; // TEST-SUPPORT: REPLICATION-BOUNDARY-004 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
using sMPGWM.Scripts.Items.Inventory; // TEST-SUPPORT: REPLICATION-BOUNDARY-005 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: REPLICATION-BOUNDARY-006 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity

namespace sMPGWM.Tests; // TEST-SUPPORT: REPLICATION-BOUNDARY-007 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity

[Category("Godot")] // TEST-SUPPORT: REPLICATION-BOUNDARY-008 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
public sealed class ReplicationBoundaryTests // TEST-SUPPORT: REPLICATION-BOUNDARY-009 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge, sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic, sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
{

    [Test] // TEST-SUPPORT: REPLICATION-BOUNDARY-010 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
    public void ResourceAndEligibilitySnapshotsConverge() // TEST-SUPPORT: REPLICATION-BOUNDARY-011 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
    {
        var data = GameplayTestData.Load(); // TEST-SUPPORT: REPLICATION-BOUNDARY-012 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var host = new ShipGameplayState(100, 40); // TEST-SUPPORT: REPLICATION-BOUNDARY-013 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var client = new ShipGameplayState(100, 40); // TEST-SUPPORT: REPLICATION-BOUNDARY-014 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var entity = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: REPLICATION-BOUNDARY-015 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        host.SetShieldCurrent(17); // TEST-SUPPORT: REPLICATION-BOUNDARY-016 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        host.SetFaction(2); // TEST-SUPPORT: REPLICATION-BOUNDARY-017 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        host.SetCondition(data.Conditions.GetById("dead"), true); // TEST-SUPPORT: REPLICATION-BOUNDARY-018 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var rows = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) }; // TEST-SUPPORT: REPLICATION-BOUNDARY-019 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(GameplaySnapshotCodec.TryDecode(rows, data, out var entries), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-020 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var entry = entries[0]; // TEST-SUPPORT: REPLICATION-BOUNDARY-021 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-022 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.Shield.Current, Is.EqualTo(17)); // TEST-SUPPORT: REPLICATION-BOUNDARY-023 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.IsAlive, Is.False); // TEST-SUPPORT: REPLICATION-BOUNDARY-024 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.FactionId, Is.EqualTo(2)); // TEST-SUPPORT: REPLICATION-BOUNDARY-025 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        host.SetCondition(data.Conditions.GetById("dead"), false); // TEST-SUPPORT: REPLICATION-BOUNDARY-026 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var latest = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) }; // TEST-SUPPORT: REPLICATION-BOUNDARY-027 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(GameplaySnapshotCodec.TryDecode(latest, data, out var updated), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-028 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        var next = updated[0]; // TEST-SUPPORT: REPLICATION-BOUNDARY-029 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.Confirm(next.Revision, next.Capacity, next.Current, next.Behaviours, next.FactionId, next.Conditions), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-030 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.IsAlive, Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-031 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions), Is.False); // TEST-SUPPORT: REPLICATION-BOUNDARY-032 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        foreach (var mode in new[] { "unknown", "duplicate", "faction", "tooMany" }) // TEST-SUPPORT: REPLICATION-BOUNDARY-033 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        {
            var malformed = rows.Duplicate(true); // TEST-SUPPORT: REPLICATION-BOUNDARY-034 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            var row = malformed[0].AsGodotArray(); // TEST-SUPPORT: REPLICATION-BOUNDARY-035 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            var conditions = row[6].AsGodotArray(); // TEST-SUPPORT: REPLICATION-BOUNDARY-036 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            if (mode == "unknown") conditions[0] = "not-a-condition"; // TEST-SUPPORT: REPLICATION-BOUNDARY-037 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            else if (mode == "duplicate") conditions.Add("dead"); // TEST-SUPPORT: REPLICATION-BOUNDARY-038 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            else if (mode == "faction") row[5] = -1L; // TEST-SUPPORT: REPLICATION-BOUNDARY-039 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            else for (var i = 0; i < ShipGameplayState.MaxConditions; i++) conditions.Add("dead"); // TEST-SUPPORT: REPLICATION-BOUNDARY-040 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            Assert.That(GameplaySnapshotCodec.TryDecode(malformed, data, out var rejected), Is.False, mode); // TEST-SUPPORT: REPLICATION-BOUNDARY-041 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
            Assert.That(rejected, Is.Empty); // TEST-SUPPORT: REPLICATION-BOUNDARY-042 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.ResourceAndEligibilitySnapshotsConverge
        }
    }

    [Test] // TEST-SUPPORT: REPLICATION-BOUNDARY-043 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
    public void InventoryCapacityFailureIsAtomic() // TEST-SUPPORT: REPLICATION-BOUNDARY-044 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
    {
        using var host = new PlayerItemState(); // TEST-SUPPORT: REPLICATION-BOUNDARY-045 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        using var client = new PlayerItemState(); // TEST-SUPPORT: REPLICATION-BOUNDARY-046 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        var store = host.Store; // TEST-SUPPORT: REPLICATION-BOUNDARY-047 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.Throws<ArgumentOutOfRangeException>(() => store.CreateContainer(ItemContainer.MaxCapacity + 1)); // TEST-SUPPORT: REPLICATION-BOUNDARY-048 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(store.CreateItems(host.Inventory.Container, "mock_standard_engine", ItemContainer.MaxCapacity), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: REPLICATION-BOUNDARY-049 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        var snapshot = host.Inventory.Snapshot; // TEST-SUPPORT: REPLICATION-BOUNDARY-050 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        var revision = host.Inventory.Revision; // TEST-SUPPORT: REPLICATION-BOUNDARY-051 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(snapshot.Capacity, Is.EqualTo(ItemSnapshotCodec.MaxInventorySlots)); // TEST-SUPPORT: REPLICATION-BOUNDARY-052 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(store.CreateItems(host.Inventory.Container, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.CapacityExceeded)); // TEST-SUPPORT: REPLICATION-BOUNDARY-053 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(host.Inventory.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: REPLICATION-BOUNDARY-054 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(host.Inventory.Snapshot, Is.SameAs(snapshot)); // TEST-SUPPORT: REPLICATION-BOUNDARY-055 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host), client), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-056 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(client.Inventory.Capacity, Is.EqualTo(ItemContainer.MaxCapacity)); // TEST-SUPPORT: REPLICATION-BOUNDARY-057 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
        Assert.That(client.Inventory.Snapshot.Slots[ItemContainer.MaxCapacity - 1]!.Id, Is.EqualTo(snapshot.Slots[ItemContainer.MaxCapacity - 1]!.Id)); // TEST-SUPPORT: REPLICATION-BOUNDARY-058 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.InventoryCapacityFailureIsAtomic
    }

    [Test] // TEST-SUPPORT: REPLICATION-BOUNDARY-059 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
    public void EquipmentSourcesRequireCurrentIdentity() // TEST-SUPPORT: REPLICATION-BOUNDARY-060 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
    {
        using var items = new PlayerItemState(); // TEST-SUPPORT: REPLICATION-BOUNDARY-061 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        var ability = GameplayTestData.Load().Abilities.GetById("shieldCapacityBoost"); // TEST-SUPPORT: REPLICATION-BOUNDARY-062 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: REPLICATION-BOUNDARY-063 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        items.Store.CreateItems(items.Inventory.Container, "mock_standard_shield_generator", 2); // TEST-SUPPORT: REPLICATION-BOUNDARY-064 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        var first = items.Inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: REPLICATION-BOUNDARY-065 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        var second = items.Inventory.Snapshot.Slots[1]!; // TEST-SUPPORT: REPLICATION-BOUNDARY-066 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        Assert.That(items.Transfer(new(0, ItemCellAddress.Inventory(0), first.Id, ItemCellAddress.Equipment("shield_generator"), null, // TEST-SUPPORT: REPLICATION-BOUNDARY-067 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
            items.Inventory.Revision, items.Equipment.Revision)), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: REPLICATION-BOUNDARY-068 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        var source = AbilitySource.Equipment("shield_generator", (long)first.Id.Value); // TEST-SUPPORT: REPLICATION-BOUNDARY-069 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        Assert.That(source.IsAvailable(ability, state, items), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-070 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        Assert.That(items.Transfer(new(0, ItemCellAddress.Inventory(1), second.Id, ItemCellAddress.Equipment("shield_generator"), first.Id, // TEST-SUPPORT: REPLICATION-BOUNDARY-071 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
            items.Inventory.Revision, items.Equipment.Revision)), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: REPLICATION-BOUNDARY-072 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        Assert.That(source.IsAvailable(ability, state, items), Is.False); // TEST-SUPPORT: REPLICATION-BOUNDARY-073 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
        Assert.That(AbilitySource.Equipment("shield_generator", (long)second.Id.Value).IsAvailable(ability, state, items), Is.True); // TEST-SUPPORT: REPLICATION-BOUNDARY-074 | Tests: sMPGWM.Tests.ReplicationBoundaryTests.EquipmentSourcesRequireCurrentIdentity
    }
}
