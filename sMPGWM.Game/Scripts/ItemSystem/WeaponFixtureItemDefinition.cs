using System;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record WeaponFixtureItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : ItemDefinition(id, displayName, categoryName, description, canStack, stackLimit, icon)
{
    private readonly int _weaponSlots;
    public required int WeaponSlots
    {
        get => _weaponSlots;
        init
        {
            if (value < 0 || value > WeaponSlotPolicy.MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(WeaponSlots));
            _weaponSlots = value;
        }
    }
}
