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

    public IReadOnlyList<ItemDefinition> GetTestItems(ItemCategories categories)
    {
        var definitions = new List<ItemDefinition>();
        foreach (var category in categories.All)
        {
            definitions.Add(new ItemDefinition(new($"mock_standard_{category.Id.Value}"),
                $"Standard {category.DisplayName}", category.Id,
                "PlayerMock example. Try moving, merging, equipping, or assigning eligible items to the hotbar.",
                maximumStackSize: category.CanStack ? 10 : null));
            definitions.Add(new ItemDefinition(new($"mock_advanced_{category.Id.Value}"),
                $"Advanced {category.DisplayName}", category.Id,
                "PlayerMock alternate example for equipment swaps and separate item stacks.",
                maximumStackSize: category.CanStack ? 1000 : null));
        }
        return definitions.AsReadOnly();
    }

    public void PopulateInventory(InventoryImpl inventory)
    {
        foreach (var definition in inventory.Catalog.Definitions)
        {
            var quantity = inventory.Catalog.Categories.Get(definition.CategoryId).CanStack
                ? definition.MaximumStackSize == 10 ? 27 : 1250
                : 2;
            var result = inventory.Add(definition.Id, quantity);
            if (result != InventoryResult.Success)
                throw new InvalidOperationException($"Could not add mock item {definition.Id}: {result}.");
        }
    }

    public void OnHotbarSlotPressed(int slotIndex)
    {
        Logger.Debug($"{nameof(PlayerMock)}.{nameof(OnHotbarSlotPressed)} requested skill launch from hotbar slot {slotIndex}.");

        // Later:
        // SkillController.RequestLaunchSkill(slotIndex);
    }
}
