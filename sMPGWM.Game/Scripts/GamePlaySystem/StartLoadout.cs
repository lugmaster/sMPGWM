using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class StartLoadout
{
    public static void ValidateDefinitions(DefinitionSet<ItemDefinition> items)
    {
        foreach (var slot in EquipmentSlotDefinition.Defaults)
        {
            var definitionId = GetStarterItem(slot);
            if (definitionId == null) continue;
            if (!items.TryGetById(definitionId, out var definition))
                throw new ArgumentException($"Starter loadout slot '{slot.Key}': missing required item '{definitionId}'.");
            if (slot.IsWeaponSlot ? WeaponSlotPolicy.GetCost(definition.CategoryId) == 0 : definition.CategoryId != slot.AcceptedCategory)
            {
                var message = $"Starter equipment '{definition.Id}', CategoryId: does not fit '{slot.Key}'.";
                if (items.SourceFor(definition.Id) is { } source) throw new CatalogueLoadException(source, message);
                throw new ArgumentException(message);
            }
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

    private static string? GetStarterItem(EquipmentSlotDefinition slot) => slot.Key switch
    {
        "engine" => "mock_standard_engine",
        "hull" => "mock_standard_hull",
        "shield_generator" => "mock_standard_shield_generator",
        "reactor" => "mock_standard_reactor",
        "battery" => "mock_standard_battery",
        "weapon_fixture" => "mock_standard_weapon_fixture",
        "weapon_1" => "vulcan_mk1",
        _ => null
    };

    private static int Find(ItemContainer inventory, string definition)
    {
        for (var i = 0; i < inventory.Entities.Length; i++)
            if (inventory.Entities[i]?.DefinitionId == definition) return i;
        return -1;
    }
}
