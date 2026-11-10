using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Inventory;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class ItemSnapshotCodec
{
    internal const int MaxInventorySlots = ItemContainer.MaxCapacity;

    internal static Array Encode(PlayerItemState items) => new()
    {
        items.Inventory.Revision, items.Equipment.Revision,
        EncodeSlots(items.Inventory.Snapshot.Slots), EncodeSlots(items.Equipment.Snapshot.Slots)
    };

    private static Array EncodeSlots(IReadOnlyList<ItemEntity?> slots)
    {
        var rows = new Array();
        foreach (var item in slots)
            rows.Add(item == null ? new Array() : new Array { checked((long)item.Id.Value), item.DefinitionId, item.Count });
        return rows;
    }

    internal static bool Apply(Array rows, PlayerItemState items)
    {
        if (rows.Count != 4 || rows[0].VariantType != Variant.Type.Int || rows[1].VariantType != Variant.Type.Int ||
            rows[2].VariantType != Variant.Type.Array || rows[3].VariantType != Variant.Type.Array) return false;
        var inventoryRevision = rows[0].AsInt64();
        var equipmentRevision = rows[1].AsInt64();
        if (inventoryRevision < 0 || equipmentRevision < 0 || inventoryRevision == long.MaxValue || equipmentRevision == long.MaxValue)
            return false;
        var inventoryRows = rows[2].AsGodotArray();
        var equipmentRows = rows[3].AsGodotArray();
        if (inventoryRows.Count < InventoryController.InitialCapacity || inventoryRows.Count > MaxInventorySlots ||
            equipmentRows.Count != items.Equipment.Definitions.Count) return false;
        var identities = new HashSet<long>();
        if (!DecodeSlots(inventoryRows, identities, out var inventory) ||
            !DecodeSlots(equipmentRows, identities, out var equipment)) return false;
        for (var i = 0; i < equipment.Length; i++)
            if (equipment[i] is { } item && (item.Count != 1 || !items.Equipment.CanPlace(items.Equipment.Definitions[i].Key, item.DefinitionId)))
                return false;
        if (inventoryRevision < items.Inventory.Revision || equipmentRevision < items.Equipment.Revision) return true;
        return items.Store.Confirm(items.Inventory.Container, inventory, inventoryRevision,
            items.Equipment.Container, equipment, equipmentRevision);
    }

    private static bool DecodeSlots(Array rows, HashSet<long> identities, out ItemEntity?[] slots)
    {
        slots = new ItemEntity?[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].VariantType != Variant.Type.Array) return false;
            var row = rows[i].AsGodotArray();
            if (row.Count == 0) continue;
            if (row.Count != 3 || row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.String ||
                row[2].VariantType != Variant.Type.Int) return false;
            var id = row[0].AsInt64();
            var definitionId = row[1].AsString();
            var count = row[2].AsInt64();
            if (id <= 0 || !identities.Add(id) || definitionId.Length > 128 ||
                !ItemRegistry.TryGetById(definitionId, out var definition) || count <= 0 || count > definition.StackLimit) return false;
            slots[i] = new(ItemEntityId.FromNetwork(id), definition, (int)count);
        }
        return true;
    }
}
