using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Items.Equipment;

public sealed record EquipmentSlotDefinition(string Key, string DisplayName, string AcceptedCategory)
{
    public static IReadOnlyList<EquipmentSlotDefinition> Defaults { get; } =
        Array.AsReadOnly(new[]
        {
            new EquipmentSlotDefinition("engine", "Engine", "engine"),
            new EquipmentSlotDefinition("hull", "Hull", "hull"),
            new EquipmentSlotDefinition("shield_generator", "Shield Generator", "shield_generator"),
            new EquipmentSlotDefinition("reactor", "Reactor", "reactor"),
            new EquipmentSlotDefinition("light_weapon_1", "Light Weapon Slot 1", "light_weapon"),
            new EquipmentSlotDefinition("light_weapon_2", "Light Weapon Slot 2", "light_weapon"),
            new EquipmentSlotDefinition("heavy_weapon_1", "Heavy Weapon Slot 1", "heavy_weapon"),
            new EquipmentSlotDefinition("heavy_weapon_2", "Heavy Weapon Slot 2", "heavy_weapon")
        });
}
