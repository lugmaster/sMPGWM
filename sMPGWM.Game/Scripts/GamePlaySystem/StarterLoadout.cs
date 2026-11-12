using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class StarterLoadout
{
    public static void Equip(ShipLoadout loadout, InventoryController inventory)
    {
        var store = loadout.Store;
        var equipment = loadout.Equipment;
        if (!ReferenceEquals(store, inventory.Items)) throw new ArgumentException("Starter inventory belongs to a different store.");
        for (var index = 0; index < equipment.Definitions.Count; index++)
        {
            if (equipment.Container.Entities[index] != null) continue;
            var definition = "mock_standard_" + equipment.Definitions[index].AcceptedCategory;
            var sourceIndex = Find(inventory.Container, definition);
            if (sourceIndex < 0)
            {
                if (store.CreateItems(inventory.Container, definition, 1) != InventoryResult.Success)
                    throw new InvalidOperationException("Could not create starter equipment: " + definition);
                sourceIndex = Find(inventory.Container, definition);
            }
            var item = inventory.Container.Entities[sourceIndex]!;
            if (store.TransferItem(item.Id, new(inventory.Container.Id, sourceIndex), new(equipment.Container.Id, index),
                    inventory.Container.Revision, equipment.Container.Revision) != TransferResult.Success)
                throw new InvalidOperationException("Could not equip starter item: " + definition);
        }
    }

    private static int Find(ItemContainer inventory, string definition)
    {
        for (var i = 0; i < inventory.Entities.Length; i++)
            if (inventory.Entities[i]?.DefinitionId == definition) return i;
        return -1;
    }
}
