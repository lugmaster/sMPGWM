using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.EquipmentSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class StarterLoadout
{
    public static void ValidateDefinitions()
    {
        foreach (var slot in EquipmentSlotDefinition.Defaults)
        {
            var definitionId = GetStarterItem(slot);
            if (definitionId == null) continue;
            var definition = ItemRegistry.GetById(definitionId);
            if (slot.IsWeaponSlot ? WeaponSlotPolicy.GetCost(definition.CategoryId) == 0 : definition.CategoryId != slot.AcceptedCategory)
                throw new InvalidOperationException($"Starter equipment '{definition.Id}' does not fit '{slot.Key}'.");
        }
    }

    public static void Equip(ShipLoadout loadout, InventoryController inventory)
    {
        var store = loadout.Store;
        var equipment = loadout.Equipment;
        if (!ReferenceEquals(store, inventory.Items)) throw new ArgumentException("Starter inventory belongs to a different store.");
        for (var pass = 0; pass < 2; pass++)
        {
            for (var index = 0; index < equipment.Definitions.Count; index++)
            {
                var slot = equipment.Definitions[index];
                if (slot.IsWeaponSlot != (pass == 1) || equipment.Container.Entities[index] != null) continue;
                var definition = GetStarterItem(slot);
                if (definition == null) continue;
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
    }

    private static string? GetStarterItem(EquipmentSlotDefinition slot) => slot.IsWeaponSlot
        ? slot.Key switch
        {
            "weapon_1" => "mock_standard_light_weapon",
            "weapon_2" => "mock_standard_medium_weapon",
            "weapon_3" => "mock_standard_heavy_weapon",
            _ => null
        }
        : "mock_standard_" + slot.AcceptedCategory;

    private static int Find(ItemContainer inventory, string definition)
    {
        for (var i = 0; i < inventory.Entities.Length; i++)
            if (inventory.Entities[i]?.DefinitionId == definition) return i;
        return -1;
    }
}
