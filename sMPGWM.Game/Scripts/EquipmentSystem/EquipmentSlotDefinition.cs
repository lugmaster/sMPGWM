using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed record EquipmentSlotDefinition(string Key, string DisplayName, string AcceptedCategory)
{
    public bool IsWeaponSlot => AcceptedCategory == "weapon" || WeaponSlotPolicy.GetCost(AcceptedCategory) != 0;
    public bool Accepts(string categoryId) => AcceptedCategory == "weapon"
        ? WeaponSlotPolicy.GetCost(categoryId) != 0 : AcceptedCategory == categoryId;

    public static IReadOnlyList<EquipmentSlotDefinition> Defaults { get; } =
        Array.AsReadOnly(new[]
        {
            new EquipmentSlotDefinition("engine", "Engine", "engine"),
            new EquipmentSlotDefinition("hull", "Hull", "hull"),
            new EquipmentSlotDefinition("shield_generator", "Shield Generator", "shield_generator"),
            new EquipmentSlotDefinition("reactor", "Reactor", "reactor"),
            new EquipmentSlotDefinition("battery", "Battery", "battery"),
            new EquipmentSlotDefinition("weapon_fixture", "Weapon Fixture", "weapon_fixture"),
            new EquipmentSlotDefinition("weapon_1", "Weapon 1", "weapon"),
            new EquipmentSlotDefinition("weapon_2", "Weapon 2", "weapon"),
            new EquipmentSlotDefinition("weapon_3", "Weapon 3", "weapon"),
            new EquipmentSlotDefinition("weapon_4", "Weapon 4", "weapon"),
            new EquipmentSlotDefinition("weapon_5", "Weapon 5", "weapon"),
            new EquipmentSlotDefinition("weapon_6", "Weapon 6", "weapon"),
            new EquipmentSlotDefinition("weapon_7", "Weapon 7", "weapon"),
            new EquipmentSlotDefinition("weapon_8", "Weapon 8", "weapon"),
            new EquipmentSlotDefinition("weapon_9", "Weapon 9", "weapon")
        });
}
