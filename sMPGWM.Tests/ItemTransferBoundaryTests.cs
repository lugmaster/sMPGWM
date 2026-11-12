using sMPGWM.Scripts.Items; // TEST-SUPPORT: TRANSFER-BOUNDARY-001 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
using sMPGWM.Scripts.Items.Equipment; // TEST-SUPPORT: TRANSFER-BOUNDARY-002 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
using sMPGWM.Scripts.Items.Inventory; // TEST-SUPPORT: TRANSFER-BOUNDARY-003 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries

namespace sMPGWM.Tests; // TEST-SUPPORT: TRANSFER-BOUNDARY-004 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries

[Category("Godot")] // TEST-SUPPORT: TRANSFER-BOUNDARY-005 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
public sealed class ItemTransferBoundaryTests // TEST-SUPPORT: TRANSFER-BOUNDARY-006 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
{
    [Test] // TEST-SUPPORT: TRANSFER-BOUNDARY-007 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
    public void SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries() // TEST-SUPPORT: TRANSFER-BOUNDARY-008 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
    {
        using var items = new ShipItemTestContext(); // TEST-SUPPORT: TRANSFER-BOUNDARY-009 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        using var transfers = new InventoryEquipmentTransferService(items.Inventory, items.Equipment); // TEST-SUPPORT: TRANSFER-BOUNDARY-010 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(items.Store.CreateItems(items.Inventory.Container, "mock_standard_shield_generator", 1), // TEST-SUPPORT: TRANSFER-BOUNDARY-011 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: TRANSFER-BOUNDARY-012 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        var generator = items.Inventory.Snapshot.Slots[0]!; // TEST-SUPPORT: TRANSFER-BOUNDARY-013 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        var request = new TransferRequest(0, ItemCellAddress.Inventory(0), generator.Id, // TEST-SUPPORT: TRANSFER-BOUNDARY-014 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            ItemCellAddress.Equipment("shield_generator"), null, items.Inventory.Revision, items.Equipment.Revision); // TEST-SUPPORT: TRANSFER-BOUNDARY-015 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        var calls = 0; // TEST-SUPPORT: TRANSFER-BOUNDARY-016 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        transfers.SetAuthority(_ => { calls++; return TransferResult.Pending; }); // TEST-SUPPORT: TRANSFER-BOUNDARY-017 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(transfers.Transfer(request), Is.EqualTo(TransferResult.InvalidSession)); // TEST-SUPPORT: TRANSFER-BOUNDARY-018 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(calls, Is.Zero); // TEST-SUPPORT: TRANSFER-BOUNDARY-019 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        var edit = transfers.OpenEditSession(); // TEST-SUPPORT: TRANSFER-BOUNDARY-020 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        foreach (var invalid in new[] { default(ItemCellAddress), ItemCellAddress.Inventory(-1), // TEST-SUPPORT: TRANSFER-BOUNDARY-021 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            ItemCellAddress.Inventory(items.Inventory.Capacity), ItemCellAddress.Equipment("missing") }) // TEST-SUPPORT: TRANSFER-BOUNDARY-022 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        {
            var invalidSource = request with { EditSession = edit, Source = invalid }; // TEST-SUPPORT: TRANSFER-BOUNDARY-023 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            var invalidTarget = request with { EditSession = edit, Destination = invalid }; // TEST-SUPPORT: TRANSFER-BOUNDARY-024 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            Assert.That(items.Transfer(invalidSource), Is.EqualTo(TransferResult.InvalidLocation)); // TEST-SUPPORT: TRANSFER-BOUNDARY-025 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            Assert.That(transfers.Transfer(invalidSource), Is.EqualTo(TransferResult.InvalidLocation)); // TEST-SUPPORT: TRANSFER-BOUNDARY-026 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            Assert.That(items.Transfer(invalidTarget), Is.EqualTo(TransferResult.InvalidLocation)); // TEST-SUPPORT: TRANSFER-BOUNDARY-027 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
            Assert.That(transfers.Transfer(invalidTarget), Is.EqualTo(TransferResult.InvalidLocation)); // TEST-SUPPORT: TRANSFER-BOUNDARY-028 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        }
        Assert.That(calls, Is.Zero); // TEST-SUPPORT: TRANSFER-BOUNDARY-029 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(transfers.Transfer(request with { EditSession = edit }), Is.EqualTo(TransferResult.Pending)); // TEST-SUPPORT: TRANSFER-BOUNDARY-030 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(calls, Is.EqualTo(1)); // TEST-SUPPORT: TRANSFER-BOUNDARY-031 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(items.Inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(generator.Id)); // TEST-SUPPORT: TRANSFER-BOUNDARY-032 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        transfers.RevokeEditSession(); // TEST-SUPPORT: TRANSFER-BOUNDARY-033 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(transfers.Transfer(request with { EditSession = edit }), Is.EqualTo(TransferResult.InvalidSession)); // TEST-SUPPORT: TRANSFER-BOUNDARY-034 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(items.Transfer(request), Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: TRANSFER-BOUNDARY-035 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(items.Provides("shield_generator", (long)generator.Id.Value, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: TRANSFER-BOUNDARY-036 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
        Assert.That(items.Transfer(request), Is.EqualTo(TransferResult.StaleRevision)); // TEST-SUPPORT: TRANSFER-BOUNDARY-037 | Tests: sMPGWM.Tests.ItemTransferBoundaryTests.SharedCellResolutionPreservesEditSessionAndAuthorityBoundaries
    }
}

