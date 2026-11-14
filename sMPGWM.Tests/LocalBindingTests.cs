using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;

namespace sMPGWM.Tests;
[Category("Godot")]
public sealed class LocalBindingTests
{
    [Test]
    public void EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits()
    {
        var store = new ItemStore();
        using var inventory = new InventoryController(store);
        using var first = new EquipmentController(store);
        using var second = new EquipmentController(store);
        sMPGWM.Scripts.UtilitySystem.Mocks.ItemFixture.Place(store, first.Container, first.FindSlot("weapon_fixture"), "mock_standard_weapon_fixture");
        sMPGWM.Scripts.UtilitySystem.Mocks.ItemFixture.Place(store, second.Container, second.FindSlot("weapon_fixture"), "mock_standard_weapon_fixture");
        store.CreateItems(inventory.Container, "mock_standard_light_weapon", 1);
        var a = inventory.Snapshot.Slots[0]!;
        var slot = first.FindSlot("weapon_1");
        Assert.That(store.TransferItem(a.Id, store.GetLocation(a.Id), new(first.Container.Id, slot), inventory.Revision, first.Revision), Is.EqualTo(TransferResult.Success));
        store.CreateItems(inventory.Container, "mock_advanced_light_weapon", 1);
        var b = inventory.Snapshot.Slots[0]!;
        Assert.That(store.TransferItem(b.Id, store.GetLocation(b.Id), new(second.Container.Id, slot), inventory.Revision, second.Revision), Is.EqualTo(TransferResult.Success));
        Assert.That(first.Revision, Is.EqualTo(second.Revision));
        using var transfers = new InventoryEquipmentTransferService(inventory, first);
        using var hotbar = new ItemHotbarController(inventory, first, transfers);
        var token = transfers.OpenEditSession();
        hotbar.SetEditSession(token);
        Assert.That(hotbar.Assign(new(token, hotbar.Revision, 0, ItemCellAddress.Equipment("weapon_1"), a.Id, inventory.Revision, first.Revision)), Is.EqualTo(HotbarResult.Success));
        Assert.That(hotbar.Resolve(0).DefinitionId, Is.EqualTo(a.DefinitionId));
        transfers.Rebind(inventory, second);
        hotbar.Rebind(inventory, second);
        Assert.That(hotbar.Resolve(0).DefinitionId, Is.EqualTo(b.DefinitionId));
        Assert.That(hotbar.Entries[0], Is.EqualTo(new WeaponHotbarBinding("weapon_1")));
        Assert.That(transfers.Transfer(new(token, ItemCellAddress.Equipment("weapon_1"), b.Id, ItemCellAddress.Inventory(0), null, inventory.Revision, second.Revision)), Is.EqualTo(TransferResult.InvalidSession));
    }
}
