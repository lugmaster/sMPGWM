using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public static class WeaponSlotPolicy
{
    public const int MaxSlots = 9;

    public static int GetCost(string categoryId) => categoryId switch
    {
        "light_weapon" => 1,
        "medium_weapon" => 2,
        "heavy_weapon" => 3,
        _ => 0
    };

    public static int GetCapacity(ItemDefinition fixture)
    {
        if (fixture is not WeaponFixtureItemDefinition definition)
            throw new ArgumentException($"Item '{fixture.Id}' is not a weapon fixture.");
        return definition.WeaponSlots;
    }

    public static int Capacity(IReadOnlyList<ItemEntity?> slots)
    {
        foreach (var item in slots)
            if (item?.Definition.CategoryId == "weapon_fixture") return GetCapacity(item.Definition);
        return 0;
    }

    public static int Used(IReadOnlyList<ItemEntity?> slots)
    {
        var used = 0;
        foreach (var item in slots)
            if (item != null) used += GetCost(item.Definition.CategoryId);
        return used;
    }
}
