using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;



namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ItemTransferBoundaryTests
{
    [Test]
    public void SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries()
    {
        using var items = new ShipItemTestContext();
        using var transfers = new InventoryEquipmentTransferService(items.Inventory, items.Equipment);
        Assert.That(items.Store.CreateItems(items.Inventory.Container, "mock_standard_shield_generator", 1),
            Is.EqualTo(InventoryResult.Success));
        var generator = items.Inventory.Snapshot.Slots[0]!;
        var request = new TransferRequest(0, ItemCellAddress.Inventory(0), generator.Id,
            ItemCellAddress.Equipment("shield_generator"), null, items.Inventory.Revision, items.Equipment.Revision);
        var calls = 0;
        transfers.SetAuthority(_ => { calls++; return TransferResult.Pending; });
        Assert.That(transfers.Transfer(request), Is.EqualTo(TransferResult.InvalidSession));
        Assert.That(calls, Is.Zero);
        var edit = transfers.OpenEditSession();
        foreach (var invalid in new[] { default(ItemCellAddress), ItemCellAddress.Inventory(-1),
            ItemCellAddress.Inventory(items.Inventory.Capacity), ItemCellAddress.Equipment("missing") })
        {
            var invalidSource = request with { EditSession = edit, Source = invalid };
            var invalidTarget = request with { EditSession = edit, Destination = invalid };
            Assert.That(items.Transfer(invalidSource), Is.EqualTo(TransferResult.InvalidLocation));
            Assert.That(transfers.Transfer(invalidSource), Is.EqualTo(TransferResult.InvalidLocation));
            Assert.That(items.Transfer(invalidTarget), Is.EqualTo(TransferResult.InvalidLocation));
            Assert.That(transfers.Transfer(invalidTarget), Is.EqualTo(TransferResult.InvalidLocation));
        }
        Assert.That(calls, Is.Zero);
        Assert.That(transfers.Transfer(request with { EditSession = edit }), Is.EqualTo(TransferResult.Pending));
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(items.Inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(generator.Id));
        transfers.RevokeEditSession();
        Assert.That(transfers.Transfer(request with { EditSession = edit }), Is.EqualTo(TransferResult.InvalidSession));
        Assert.That(items.Transfer(request), Is.EqualTo(TransferResult.Success));
        Assert.That(items.Provides("shield_generator", (long)generator.Id.Value, "shieldCapacityBoost"), Is.True);
        Assert.That(items.Transfer(request), Is.EqualTo(TransferResult.StaleRevision));
    }
}

