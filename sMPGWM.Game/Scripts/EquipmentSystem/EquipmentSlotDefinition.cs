using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed record EquipmentSlotDefinition(string Key, string DisplayName, ItemCategoryId AcceptedCategory)
{
    public static IReadOnlyList<EquipmentSlotDefinition> Defaults { get; } =
        Array.AsReadOnly(new[]
        {
            new EquipmentSlotDefinition("engine", "Engine", ItemCategories.Engine),
            new EquipmentSlotDefinition("hull", "Hull", ItemCategories.Hull),
            new EquipmentSlotDefinition("shield_generator", "Shield Generator", ItemCategories.ShieldGenerator),
            new EquipmentSlotDefinition("reactor", "Reactor", ItemCategories.Reactor),
            new EquipmentSlotDefinition("light_weapon_1", "Light Weapon Slot 1", ItemCategories.LightWeapon),
            new EquipmentSlotDefinition("light_weapon_2", "Light Weapon Slot 2", ItemCategories.LightWeapon),
            new EquipmentSlotDefinition("heavy_weapon_1", "Heavy Weapon Slot 1", ItemCategories.HeavyWeapon),
            new EquipmentSlotDefinition("heavy_weapon_2", "Heavy Weapon Slot 2", ItemCategories.HeavyWeapon)
        });
}
