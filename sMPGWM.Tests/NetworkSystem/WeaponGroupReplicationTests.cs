using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class WeaponGroupReplicationTests
{
    [Test]
    public void OwnerBaselineRestoresCadenceWithoutChangingEquipmentRevisionAndRejectsMalformedStateAtomically()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        Equip(host, "mock_standard_weapon_fixture", "weapon_fixture");
        var weapon = Equip(host, "vulcan_mk1", "weapon_1");
        var slot = host.Equipment.FindSlot("weapon_1");
        using var initial = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(initial, client.Player), Is.True);
        var revision = host.Equipment.Revision;
        Assert.That(weapon.WeaponState!.TryAccept(100, 15, true), Is.True);
        using var updated = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(updated, client.Player), Is.True);
        var replica = client.Equipment.Snapshot.Slots[slot]!.WeaponState!;
        Assert.That(replica.Sequence, Is.EqualTo(1));
        Assert.That(replica.NextEligibleTick, Is.EqualTo(104));
        Assert.That(replica.LastAcceptedTick, Is.EqualTo(100));
        Assert.That(client.Equipment.Revision, Is.EqualTo(revision));
        using var malformed = ItemSnapshotCodec.Encode(host.Player);
        malformed[8].AsGodotArray()[slot].AsGodotArray()[3] = double.NaN;
        Assert.That(ItemSnapshotCodec.Apply(malformed, client.Player), Is.False);
        Assert.That(client.Equipment.Snapshot.Slots[slot]!.WeaponState, Is.SameAs(replica));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void OwnerSnapshotSeparatesGroupConfigurationAndPublishesAcceptedMembership(bool primary, bool secondary)
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        Equip(host, "mock_standard_weapon_fixture", "weapon_fixture");
        var weapon = Equip(host, "vulcan_mk1", "weapon_1");
        var slot = host.Equipment.FindSlot("weapon_1");
        Assert.That(host.Store.CreateItems(host.Inventory.Container, "mock_standard_resource", 7), Is.EqualTo(InventoryResult.Success));
        Assert.That(host.Equipment.SetWeaponGroups(slot, weapon.Id, host.Equipment.Revision, primary, secondary),
            Is.AnyOf(TransferResult.Success, TransferResult.NoChange));
        using var payload = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(payload.Count, Is.EqualTo(10));
        AssertItemRowsHaveNoGroupFields(payload[7].AsGodotArray());
        AssertItemRowsHaveNoGroupFields(payload[8].AsGodotArray());
        var groups = payload[9].AsGodotArray();
        Assert.That(groups.Count, Is.EqualTo(1));
        var group = groups[0].AsGodotArray();
        Assert.That(group.Count, Is.EqualTo(4));
        Assert.That(group[0].AsInt32(), Is.EqualTo(slot));
        Assert.That(group[1].AsInt64(), Is.EqualTo((long)weapon.Id.Value));
        Assert.That(group[2].AsBool(), Is.EqualTo(primary));
        Assert.That(group[3].AsBool(), Is.EqualTo(secondary));
        var published = 0;
        client.Equipment.Changed += snapshot =>
        {
            Assert.That(snapshot.WeaponGroups.TryGetBinding(slot, weapon.Id, out var binding), Is.True);
            Assert.That(binding.Primary, Is.EqualTo(primary));
            Assert.That(binding.Secondary, Is.EqualTo(secondary));
            published++;
        };
        Assert.That(ItemSnapshotCodec.Apply(payload, client.Player), Is.True);
        Assert.That(published, Is.EqualTo(1));
        Assert.That(client.Equipment.Snapshot.WeaponGroups.Bindings, Is.EqualTo(host.Equipment.Snapshot.WeaponGroups.Bindings));
        Assert.That(client.Equipment.Revision, Is.EqualTo(host.Equipment.Revision));
        Assert.That(ItemSnapshotCodec.Apply(payload, client.Player), Is.True);
        Assert.That(client.Equipment.Snapshot.WeaponGroups.Bindings, Is.EqualTo(host.Equipment.Snapshot.WeaponGroups.Bindings));
    }

    [Test]
    public void IdenticalEquippedWeaponsRetainIndependentBindings()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        Equip(host, "mock_standard_weapon_fixture", "weapon_fixture");
        for (var i = 0; i < 4; i++)
        {
            var key = "weapon_" + (i + 1);
            var item = Equip(host, "vulcan_mk1", key);
            Assert.That(host.Equipment.SetWeaponGroups(host.Equipment.FindSlot(key), item.Id, host.Equipment.Revision,
                (i & 1) != 0, (i & 2) != 0), Is.AnyOf(TransferResult.Success, TransferResult.NoChange));
        }
        using var payload = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(payload, client.Player), Is.True);
        Assert.That(client.Equipment.Snapshot.WeaponGroups.Bindings.Count, Is.EqualTo(4));
        Assert.That(client.Equipment.Snapshot.WeaponGroups.Bindings, Is.EqualTo(host.Equipment.Snapshot.WeaponGroups.Bindings));
    }

    [TestCase("missing-field")]
    [TestCase("missing-binding")]
    [TestCase("wrong-item")]
    [TestCase("empty-slot")]
    [TestCase("duplicate-binding")]
    [TestCase("invalid-flag")]
    [TestCase("truncated-binding")]
    public void InvalidConfigurationDoesNotMutateItemsRevisionsOrPublish(string invalid)
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        Equip(host, "mock_standard_weapon_fixture", "weapon_fixture");
        var weapon = Equip(host, "vulcan_mk1", "weapon_1");
        using var initial = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(initial, client.Player), Is.True);
        var equipmentBefore = client.Equipment.Snapshot;
        var inventoryBefore = client.Inventory.Snapshot;
        var published = 0;
        client.Equipment.Changed += _ => published++;
        client.Inventory.Changed += _ => published++;
        Assert.That(host.Equipment.SetWeaponGroups(host.Equipment.FindSlot("weapon_1"), weapon.Id,
            host.Equipment.Revision, true, true), Is.EqualTo(TransferResult.Success));
        Assert.That(host.Store.CreateItems(host.Inventory.Container, "mock_standard_resource", 7), Is.EqualTo(InventoryResult.Success));
        using var malformed = ItemSnapshotCodec.Encode(host.Player);
        var groups = malformed[9].AsGodotArray();
        var group = groups[0].AsGodotArray();
        switch (invalid)
        {
            case "missing-field": malformed.RemoveAt(9); break;
            case "missing-binding": groups.Clear(); break;
            case "wrong-item": group[1] = (long)host.Equipment.Snapshot.Slots[host.Equipment.FindSlot("weapon_fixture")]!.Id.Value; break;
            case "empty-slot": group[0] = host.Equipment.FindSlot("weapon_2"); break;
            case "duplicate-binding": groups.Add(group.Duplicate()); break;
            case "invalid-flag": group[2] = 1; break;
            case "truncated-binding": group.RemoveAt(3); break;
        }
        Assert.That(ItemSnapshotCodec.Apply(malformed, client.Player), Is.False);
        Assert.That(client.Equipment.Snapshot, Is.SameAs(equipmentBefore));
        Assert.That(client.Inventory.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(client.Equipment.Revision, Is.EqualTo(equipmentBefore.Revision));
        Assert.That(client.Inventory.Revision, Is.EqualTo(inventoryBefore.Revision));
        Assert.That(published, Is.Zero);
        using var valid = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(valid, client.Player), Is.True);
    }

    private static void AssertItemRowsHaveNoGroupFields(Array rows)
    {
        foreach (var value in rows)
        {
            var row = value.AsGodotArray();
            Assert.That(row.Count, Is.AnyOf(0, 6));
        }
    }

    private static ItemEntity Equip(ShipItemTestContext context, string definition, string key)
    {
        Assert.That(context.Store.CreateItems(context.Inventory.Container, definition, 1), Is.EqualTo(InventoryResult.Success));
        for (var i = 0; i < context.Inventory.Container.Entities.Length; i++)
        {
            if (context.Inventory.Container.Entities[i] is not { } item || item.DefinitionId != definition) continue;
            Assert.That(context.Store.TransferItem(item.Id, new(context.Inventory.Container.Id, i),
                new(context.Equipment.Container.Id, context.Equipment.FindSlot(key)), context.Inventory.Revision,
                context.Equipment.Revision), Is.EqualTo(TransferResult.Success));
            return item;
        }
        throw new InvalidOperationException("Missing created item.");
    }
}
