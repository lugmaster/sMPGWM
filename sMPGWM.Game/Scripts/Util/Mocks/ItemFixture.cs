using System;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;
using ItemLocation = sMPGWM.Scripts.Items.ItemLocation;

namespace sMPGWM.Scripts.Util.Mocks;

/// <summary>Arranges smoke fixtures through gameplay commands, including separate partial stacks.</summary>
internal static class ItemFixture
{
    internal static ItemEntity Place(ItemStore items, ItemContainer destination, int slot, string definitionId, int count = 1)
    {
        if (destination.Slots[slot] is not null) throw new InvalidOperationException("Fixture slot must be empty.");
        if (count <= 0 || count > ItemRegistry.GetById(definitionId).StackLimit)
            throw new ArgumentOutOfRangeException(nameof(count));
        var staging = items.CreateContainer(1);
        var creation = items.CreateItems(staging, definitionId, count);
        if (creation != InventoryResult.Success) throw new InvalidOperationException($"Fixture creation failed: {creation}.");
        var id = staging.Slots[0]!.Value;
        var transfer = items.TransferItem(id, items.GetLocation(id), new ItemLocation(destination.Id, slot),
            staging.Revision, destination.Revision);
        if (transfer != TransferResult.Success) throw new InvalidOperationException($"Fixture placement failed: {transfer}.");
        return items.GetItem(id);
    }

    internal static void MoveAway(ItemStore items, ItemContainer source, params int[] slots)
    {
        var storage = items.CreateContainer(slots.Length);
        for (var index = 0; index < slots.Length; index++)
        {
            if (source.Slots[slots[index]] is not { } id) continue;
            var transfer = items.TransferItem(id, items.GetLocation(id), new ItemLocation(storage.Id, index),
                source.Revision, storage.Revision);
            if (transfer != TransferResult.Success) throw new InvalidOperationException($"Fixture removal failed: {transfer}.");
        }
    }
}
