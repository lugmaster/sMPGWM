using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.World;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class ItemSnapshotCodec
{
    internal const int MaxInventorySlots = ItemContainer.MaxCapacity;

    internal static Array Encode(PlayerSession player) => new()
    {
        player.ControllingPeerId, player.ControlGeneration, player.ControlledShip?.EntityId ?? "",
        checked((long)player.Inventory.Container.Id.Value), checked((long)(player.ControlledShip?.Equipment.Container.Id.Value ?? 0)),
        player.Inventory.Revision, player.ControlledShip?.Equipment.Revision ?? 0,
        EncodeSlots(player.Inventory.Snapshot.Slots),
        player.ControlledShip is { } ship ? EncodeSlots(ship.Equipment.Snapshot.Slots) : new Array()
    };

    internal static bool IsOldBinding(Array rows, PlayerSession player) => rows.Count == 9 &&
        rows[1].VariantType == Variant.Type.Int && rows[1].AsInt64() < player.ControlGeneration;

    private static Array EncodeSlots(IReadOnlyList<ItemEntity?> slots)
    {
        var rows = new Array();
        foreach (var item in slots)
            rows.Add(item == null ? new Array() : new Array { checked((long)item.Id.Value), item.DefinitionId, item.Count });
        return rows;
    }

    internal static bool Apply(Array rows, PlayerSession player)
    {
        if (rows.Count != 9) return false;
        for (var i = 0; i < 7; i++)
            if (rows[i].VariantType != (i == 2 ? Variant.Type.String : Variant.Type.Int)) return false;
        if (rows[7].VariantType != Variant.Type.Array || rows[8].VariantType != Variant.Type.Array ||
            rows[0].AsInt64() != player.ControllingPeerId || rows[1].AsInt64() != player.ControlGeneration ||
            rows[2].AsString() != (player.ControlledShip?.EntityId ?? "") || rows[3].AsInt64() <= 0) return false;
        var inventoryIdentity = rows[3].AsInt64();
        var equipmentIdentity = rows[4].AsInt64();
        if (player.ReplicaInventoryIdentity != 0 && player.ReplicaInventoryIdentity != inventoryIdentity ||
            player.ReplicaGeneration == player.ControlGeneration && player.ReplicaEquipmentIdentity != equipmentIdentity) return false;
        var equipment = player.ControlledShip?.Equipment;
        if (equipment == null ? rows[4].AsInt64() != 0 : rows[4].AsInt64() <= 0 || rows[4].AsInt64() == rows[3].AsInt64()) return false;
        var inventoryRevision = rows[5].AsInt64();
        var equipmentRevision = rows[6].AsInt64();
        if (inventoryRevision < 0 || equipmentRevision < 0 || inventoryRevision == long.MaxValue || equipmentRevision == long.MaxValue)
            return false;
        var inventoryRows = rows[7].AsGodotArray();
        var equipmentRows = rows[8].AsGodotArray();
        if (inventoryRows.Count < InventoryController.InitialCapacity || inventoryRows.Count > MaxInventorySlots ||
            equipmentRows.Count != (equipment?.Definitions.Count ?? 0)) return false;
        var identities = new HashSet<long>();
        if (!DecodeSlots(inventoryRows, identities, out var inventorySlots) ||
            !DecodeSlots(equipmentRows, identities, out var equipmentSlots)) return false;
        if (equipment != null)
            for (var i = 0; i < equipmentSlots.Length; i++)
                if (equipmentSlots[i] is { } item && (item.Count != 1 || !equipment.CanPlace(equipment.Definitions[i].Key, item.DefinitionId)))
                    return false;
        if (inventoryRevision < player.Inventory.Revision || equipment != null && equipmentRevision < equipment.Revision) return true;
        var batch = equipment == null
            ? new[] { new ItemContainerConfirmation(player.Inventory.Container, inventorySlots, inventoryRevision) }
            : new[] { new ItemContainerConfirmation(player.Inventory.Container, inventorySlots, inventoryRevision),
                new ItemContainerConfirmation(equipment.Container, equipmentSlots, equipmentRevision) };
        if (!player.Inventory.Items.Confirm(batch)) return false;
        player.ReplicaInventoryIdentity = inventoryIdentity;
        player.ReplicaEquipmentIdentity = equipmentIdentity;
        player.ReplicaGeneration = player.ControlGeneration;
        return true;
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
