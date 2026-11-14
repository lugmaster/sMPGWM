using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.UtilitySystem.Mocks;
using Array = Godot.Collections.Array;

namespace sMPGWM.Tests.NetworkSystem;

[Category("Godot")]
public sealed class WeaponFixtureReplicationTests
{
    [Test]
    public void MixedWeaponsReplicateAndFixtureRemovalClearsHotbarWithoutSubscribers()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        var fixtureSlot = host.Equipment.FindSlot("weapon_fixture");
        var mediumSlot = host.Equipment.FindSlot("weapon_2");
        var fixture = ItemFixture.Place(host.Store, host.Equipment.Container, fixtureSlot, "mock_standard_weapon_fixture");
        ItemFixture.Place(host.Store, host.Equipment.Container, host.Equipment.FindSlot("weapon_1"), "vulcan_mk1");
        var medium = ItemFixture.Place(host.Store, host.Equipment.Container, mediumSlot, "mock_standard_medium_weapon");
        ItemFixture.Place(host.Store, host.Equipment.Container, host.Equipment.FindSlot("weapon_3"), "mock_standard_heavy_weapon");
        using var initial = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(initial, client.Player), Is.True);
        Assert.That(client.Equipment.Snapshot.MaxWeaponSlots, Is.EqualTo(9));
        Assert.That(client.Equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(6));
        Assert.That(client.Equipment.Snapshot.FreeWeaponSlots, Is.EqualTo(3));
        Assert.That(client.Equipment.Snapshot.Slots[mediumSlot]!.Id, Is.EqualTo(medium.Id));
        using var transfers = new InventoryEquipmentTransferService(client.Inventory, client.Equipment);
        using var hotbar = new ItemHotbarController(client.Inventory, client.Equipment, transfers);
        var token = transfers.OpenEditSession();
        hotbar.SetEditSession(token);
        Assert.That(hotbar.Assign(new(token, hotbar.Revision, 0, ItemCellAddress.Equipment("weapon_2"), medium.Id,
            client.Inventory.Revision, client.Equipment.Revision)), Is.EqualTo(HotbarResult.Success));
        Assert.That(hotbar.Resolve(0).Available, Is.True);
        var equipmentBefore = client.Equipment.Snapshot;
        var inventoryBefore = client.Inventory.Snapshot;
        using var malformed = initial.Duplicate(true);
        malformed[5] = host.Inventory.Revision + 1;
        malformed[6] = host.Equipment.Revision + 1;
        var badEquipment = malformed[8].AsGodotArray();
        malformed[7].AsGodotArray()[0] = badEquipment[fixtureSlot];
        badEquipment[fixtureSlot] = new Array();
        Assert.That(ItemSnapshotCodec.Apply(malformed, client.Player), Is.False);
        Assert.That(client.Equipment.Snapshot, Is.SameAs(equipmentBefore));
        Assert.That(client.Inventory.Snapshot, Is.SameAs(inventoryBefore));
        Assert.That(hotbar.Entries[0], Is.TypeOf<WeaponHotbarBinding>());
        Assert.That(host.Transfer(new(0, ItemCellAddress.Equipment("weapon_fixture"), fixture.Id,
            ItemCellAddress.Inventory(0), null, host.Inventory.Revision, host.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
        using var removed = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(removed, client.Player), Is.True);
        Assert.That(client.Equipment.Snapshot.MaxWeaponSlots, Is.Zero);
        Assert.That(client.Equipment.Snapshot.UsedWeaponSlots, Is.Zero);
        Assert.That(hotbar.Entries[0], Is.Null);
        Assert.That(hotbar.Resolve(0).Available, Is.False);
        Assert.That(client.Store.GetLocation(medium.Id).ContainerId, Is.EqualTo(client.Inventory.Container.Id));
        Assert.That(client.Store.GetLocation(fixture.Id).ContainerId, Is.EqualTo(client.Inventory.Container.Id));
        Assert.That(ItemSnapshotCodec.Apply(removed, client.Player), Is.True);
        Assert.That(hotbar.Entries[0], Is.Null);
    }

    [Test]
    public void FixtureRemovalClearsWeaponAbilityBindingsInOneHotbarChange()
    {
        using var host = new ShipItemTestContext();
        var fixture = ItemFixture.Place(host.Store, host.Equipment.Container, host.Equipment.FindSlot("weapon_fixture"), "mock_standard_weapon_fixture");
        ItemFixture.Place(host.Store, host.Equipment.Container, host.Equipment.FindSlot("weapon_1"), "vulcan_mk1");
        using var transfers = new InventoryEquipmentTransferService(host.Inventory, host.Equipment);
        using var state = new ItemHotbarState(new HotbarBinding[]
        {
            new WeaponHotbarBinding("weapon_1"),
            new EquipmentAbilityHotbarBinding("weapon_1", "shieldCapacityBoost"),
            new ActionHotbarBinding("retained")
        });
        using var hotbar = new ItemHotbarController(state, host.Inventory, host.Equipment, transfers);
        var changes = 0;
        state.Changed += () => changes++;
        Assert.That(host.Transfer(new(0, ItemCellAddress.Equipment("weapon_fixture"), fixture.Id,
            ItemCellAddress.Inventory(0), null, host.Inventory.Revision, host.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
        Assert.That(hotbar.Entries[0], Is.Null);
        Assert.That(hotbar.Entries[1], Is.Null);
        Assert.That(hotbar.Entries[2], Is.EqualTo(new ActionHotbarBinding("retained")));
        Assert.That(changes, Is.EqualTo(1));
    }
}
