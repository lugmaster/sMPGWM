using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ReplicationBoundaryTests
{

    [Test]
    public void ResourceAndEligibilitySnapshotsConverge()
    {
        var data = GameplayTestData.Load();
        var host = new ShipRuntime(100, 40);
        var client = new ShipRuntime(100, 40);
        var entity = Guid.NewGuid().ToString("N");
        host.SetShieldCurrent(17);
        host.SetFaction(2);
        host.SetCondition(data.Conditions.GetById("dead"), true);
        var rows = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) };
        Assert.That(GameplaySnapshotCodec.TryDecode(rows, data, out var entries), Is.True);
        var entry = entries[0];
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0, entry.Components), Is.True);
        Assert.That(client.Shield.Current, Is.EqualTo(17));
        Assert.That(client.IsAlive, Is.False);
        Assert.That(client.FactionId, Is.EqualTo(2));
        host.SetCondition(data.Conditions.GetById("dead"), false);
        var latest = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) };
        Assert.That(GameplaySnapshotCodec.TryDecode(latest, data, out var updated), Is.True);
        var next = updated[0];
        Assert.That(client.Confirm(next.Revision, next.Capacity, next.Current, next.Behaviours, next.FactionId, next.Conditions, next.Stats, next.EnergyCurrent, next.EnergyDepleted, next.ShieldProtectionAvailable, next.ShieldDowntimeDeadline, next.AuthoritativeTick, next.ConfigurationRevision, next.AfterburnerActive, 0, 0, next.Components), Is.True);
        Assert.That(client.IsAlive, Is.True);
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0, entry.Components), Is.False);
        foreach (var mode in new[] { "unknown", "duplicate", "faction", "tooMany" })
        {
            var malformed = rows.Duplicate(true);
            var row = malformed[0].AsGodotArray();
            var conditions = row[6].AsGodotArray();
            if (mode == "unknown") conditions[0] = "not-a-condition";
            else if (mode == "duplicate") conditions.Add("dead");
            else if (mode == "faction") row[5] = -1L;
            else for (var i = 0; i < ShipRuntime.MaxConditions; i++) conditions.Add("dead");
            Assert.That(GameplaySnapshotCodec.TryDecode(malformed, data, out var rejected), Is.False, mode);
            Assert.That(rejected, Is.Empty);
        }
    }

    [Test]
    public void InventoryCapacityFailureIsAtomic()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        var store = host.Store;
        Assert.Throws<ArgumentOutOfRangeException>(() => store.CreateContainer(ItemContainer.MaxCapacity + 1));
        Assert.That(store.CreateItems(host.Inventory.Container, "mock_standard_engine", ItemContainer.MaxCapacity), Is.EqualTo(InventoryResult.Success));
        var snapshot = host.Inventory.Snapshot;
        var revision = host.Inventory.Revision;
        Assert.That(snapshot.Capacity, Is.EqualTo(ItemSnapshotCodec.MaxInventorySlots));
        Assert.That(store.CreateItems(host.Inventory.Container, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.CapacityExceeded));
        Assert.That(host.Inventory.Revision, Is.EqualTo(revision));
        Assert.That(host.Inventory.Snapshot, Is.SameAs(snapshot));
        Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host.Player), client.Player), Is.True);
        Assert.That(client.Inventory.Capacity, Is.EqualTo(ItemContainer.MaxCapacity));
        Assert.That(client.Inventory.Snapshot.Slots[ItemContainer.MaxCapacity - 1]!.Id, Is.EqualTo(snapshot.Slots[ItemContainer.MaxCapacity - 1]!.Id));
    }

    [Test]
    public void EquipmentSourcesRequireCurrentIdentity()
    {
        using var items = new ShipItemTestContext();
        var ability = GameplayTestData.Load().Abilities.GetById("shieldCapacityBoost");
        var state = new ShipRuntime(100, 40);
        items.Store.CreateItems(items.Inventory.Container, "mock_standard_shield_generator", 2);
        var first = items.Inventory.Snapshot.Slots[0]!;
        var second = items.Inventory.Snapshot.Slots[1]!;
        Assert.That(items.Transfer(new(0, ItemCellAddress.Inventory(0), first.Id, ItemCellAddress.Equipment("shield_generator"), null,
            items.Inventory.Revision, items.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
        var source = AbilitySource.Equipment("shield_generator", (long)first.Id.Value);
        Assert.That(source.IsAvailable(ability, state, items.Equipment), Is.True);
        Assert.That(items.Transfer(new(0, ItemCellAddress.Inventory(1), second.Id, ItemCellAddress.Equipment("shield_generator"), first.Id,
            items.Inventory.Revision, items.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
        Assert.That(source.IsAvailable(ability, state, items.Equipment), Is.False);
        Assert.That(AbilitySource.Equipment("shield_generator", (long)second.Id.Value).IsAvailable(ability, state, items.Equipment), Is.True);
    }
}
