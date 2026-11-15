using System;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class DevelopmentLoadout
{
    private static readonly (string Id, long Quantity)[] InventoryExamples =
    [
        ("mock_standard_engine", 2),
        ("mock_advanced_engine", 2),
        ("mock_standard_hull", 2),
        ("mock_advanced_hull", 2),
        ("mock_standard_shield_generator", 2),
        ("mock_advanced_shield_generator", 2),
        ("mock_standard_reactor", 2),
        ("mock_advanced_reactor", 2),
        ("mock_standard_weapon_fixture", 2),
        ("vulcan_mk1", 2),
        ("laser_mk1", 2),
        ("mock_standard_medium_weapon", 2),
        ("mock_advanced_medium_weapon", 2),
        ("mock_standard_heavy_weapon", 2),
        ("mock_advanced_heavy_weapon", 2),
        ("mock_standard_consumable", 27),
        ("mock_advanced_consumable", 1250),
        ("mock_standard_resource", 27),
        ("mock_advanced_resource", 1250)
    ];

    public static void Equip(ShipLoadout loadout, InventoryController inventory)
    {
        foreach (var (id, _) in InventoryExamples)
            GameplayDefinitions.Current.Items.GetById(id);
        var store = loadout.Store;
        foreach (var (id, quantity) in InventoryExamples)
        {
            var result = store.CreateItems(inventory.Container, id, quantity);
            if (result != InventoryResult.Success)
                throw new InvalidOperationException($"Could not create development item '{id}': {result}.");
        }
        StartLoadout.Equip(loadout, inventory);
        var equipment = loadout.Equipment;
        for (var index = 0; index < inventory.Container.Entities.Length; index++)
        {
            if (inventory.Container.Entities[index] is not { } item) continue;
            var slot = item.DefinitionId switch
            {
                "laser_mk1" => "weapon_2",
                "mock_standard_heavy_weapon" => "weapon_3",
                _ => null
            };
            if (slot == null) continue;
            var targetIndex = equipment.FindSlot(slot);
            if (equipment.Container.Entities[targetIndex] != null) continue;
            if (store.TransferItem(item.Id, new(inventory.Container.Id, index), new(equipment.Container.Id, targetIndex),
                    inventory.Container.Revision, equipment.Container.Revision) != TransferResult.Success)
                throw new InvalidOperationException("Could not equip development weapon: " + item.DefinitionId);
        }
        equipment.ApplyWeaponGroupDefinition(["weapon_1", "weapon_3"], ["weapon_2"]);
    }
}
