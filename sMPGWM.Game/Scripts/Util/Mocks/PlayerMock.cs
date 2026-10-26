using System;
using System.Collections.Generic;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class PlayerMock: AbstractSingleton<PlayerMock>
{
    
    // public readonly LivingEntityState LivingEntityState = new LivingEntityState(StateEngine.Config.PlayerActiveEffectCapacity)
    // {
    //     Condition = LivingEntityCondition.None,
    //     Health = new Stat(100,15),
    //     Shield = new Stat(175,168),
    //     EnergyStorage = new Stat(120, 77),
    //     Speed = new Stat(0),
    //     Acceleration = new Stat(0),
    //     LivingEntityType = LivingEntityType.MpController,
    //     LivingEntityId = 1
    // };

    public IReadOnlyList<IconDefinition> GetHotBarSkills()
    {
        return Array.Empty<IconDefinition>();
    }

    private static readonly (ItemDefinitionId Id, long Quantity)[] InventoryExamples =
    [
        (new("mock_standard_engine"), 2),
        (new("mock_advanced_engine"), 2),
        (new("mock_standard_hull"), 2),
        (new("mock_advanced_hull"), 2),
        (new("mock_standard_shield_generator"), 2),
        (new("mock_advanced_shield_generator"), 2),
        (new("mock_standard_reactor"), 2),
        (new("mock_advanced_reactor"), 2),
        (new("mock_standard_light_weapon"), 2),
        (new("mock_advanced_light_weapon"), 2),
        (new("mock_standard_heavy_weapon"), 2),
        (new("mock_advanced_heavy_weapon"), 2),
        (new("mock_standard_consumable"), 27),
        (new("mock_advanced_consumable"), 1250),
        (new("mock_standard_resource"), 27),
        (new("mock_advanced_resource"), 1250),
    ];

    internal static void ValidateInventoryDefinitions(ItemCatalog catalog)
    {
        foreach (var (id, _) in InventoryExamples)
            catalog.Get(id);
    }

    public void PopulateInventory(InventoryImpl inventory)
    {
        // Resolve every reference before the first addition, so missing definitions cannot partially seed a player.
        ValidateInventoryDefinitions(inventory.Catalog);
        foreach (var (id, quantity) in InventoryExamples)
        {
            var result = inventory.Add(id, quantity);
            if (result != InventoryResult.Success)
                throw new InvalidOperationException($"Could not add mock item {id}: {result}.");
        }
    }

    public void OnHotbarSlotPressed(int slotIndex)
    {
        Logger.Debug($"{nameof(PlayerMock)}.{nameof(OnHotbarSlotPressed)} requested skill launch from hotbar slot {slotIndex}.");

        // Later:
        // SkillController.RequestLaunchSkill(slotIndex);
    }
}
