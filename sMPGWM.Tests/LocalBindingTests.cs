using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
// TEST-SUPPORT: LOCAL-REBIND-0 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
// TEST-SUPPORT: LOCAL-REBIND-1 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
// TEST-SUPPORT: LOCAL-REBIND-2 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits

// TEST-SUPPORT: LOCAL-REBIND-3 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
namespace sMPGWM.Tests; // TEST-SUPPORT: LOCAL-REBIND-4 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
[Category("Godot")] // TEST-SUPPORT: LOCAL-REBIND-5 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
public sealed class LocalBindingTests // TEST-SUPPORT: LOCAL-REBIND-6 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
{
    [Test] // TEST-SUPPORT: LOCAL-REBIND-8 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
    public void EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits() // TEST-SUPPORT: LOCAL-REBIND-9 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
    {
        var store = new ItemStore(); // TEST-SUPPORT: LOCAL-REBIND-11 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        using var inventory = new InventoryController(store); // TEST-SUPPORT: LOCAL-REBIND-12 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        using var first = new EquipmentController(store); // TEST-SUPPORT: LOCAL-REBIND-13 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        using var second = new EquipmentController(store); // TEST-SUPPORT: LOCAL-REBIND-14 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        store.CreateItems(inventory.Container, "mock_standard_light_weapon", 1); // TEST-SUPPORT: LOCAL-REBIND-15 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        var a = inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: LOCAL-REBIND-16 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        var slot = first.FindSlot("light_weapon_1"); // TEST-SUPPORT: LOCAL-REBIND-17 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(store.TransferItem(a.Id, store.GetLocation(a.Id), new(first.Container.Id, slot), inventory.Revision, first.Revision), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: LOCAL-REBIND-18 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        store.CreateItems(inventory.Container, "mock_advanced_light_weapon", 1); // TEST-SUPPORT: LOCAL-REBIND-19 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        var b = inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: LOCAL-REBIND-20 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(store.TransferItem(b.Id, store.GetLocation(b.Id), new(second.Container.Id, slot), inventory.Revision, second.Revision), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: LOCAL-REBIND-21 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(first.Revision, Is.EqualTo(second.Revision)); // TEST-SUPPORT: LOCAL-REBIND-22 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        using var transfers = new InventoryEquipmentTransferService(inventory, first); // TEST-SUPPORT: LOCAL-REBIND-23 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        using var hotbar = new ItemHotbarController(inventory, first, transfers); // TEST-SUPPORT: LOCAL-REBIND-24 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        var token = transfers.OpenEditSession(); // TEST-SUPPORT: LOCAL-REBIND-25 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        hotbar.SetEditSession(token); // TEST-SUPPORT: LOCAL-REBIND-26 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(hotbar.Assign(new(token, hotbar.Revision, 0, ItemCellAddress.Equipment("light_weapon_1"), a.Id, inventory.Revision, first.Revision)), Is.EqualTo(HotbarResult.Success)); // TEST-SUPPORT: LOCAL-REBIND-27 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(hotbar.Resolve(0).DefinitionId, Is.EqualTo(a.DefinitionId)); // TEST-SUPPORT: LOCAL-REBIND-28 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        transfers.Rebind(inventory, second); // TEST-SUPPORT: LOCAL-REBIND-29 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        hotbar.Rebind(inventory, second); // TEST-SUPPORT: LOCAL-REBIND-30 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(hotbar.Resolve(0).DefinitionId, Is.EqualTo(b.DefinitionId)); // TEST-SUPPORT: LOCAL-REBIND-31 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(hotbar.Entries[0], Is.EqualTo(new WeaponHotbarBinding("light_weapon_1"))); // TEST-SUPPORT: LOCAL-REBIND-32 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
        Assert.That(transfers.Transfer(new(token, ItemCellAddress.Equipment("light_weapon_1"), b.Id, ItemCellAddress.Inventory(0), null, inventory.Revision, second.Revision)), Is.EqualTo(TransferResult.InvalidSession)); // TEST-SUPPORT: LOCAL-REBIND-33 | Tests: sMPGWM.Tests.LocalBindingTests.EqualRevisionRebindPreservesAssignmentsAndRevokesOldEdits
    }
}
