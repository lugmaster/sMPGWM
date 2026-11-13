using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed class EquipmentSnapshot
{
    public long Revision { get; }
    public IReadOnlyList<ItemEntity?> Slots { get; }
    public int MaxWeaponSlots { get; }
    public int UsedWeaponSlots { get; }
    public int FreeWeaponSlots => MaxWeaponSlots - UsedWeaponSlots;

    internal EquipmentSnapshot(long revision, ItemEntity?[] slots)
    {
        Revision = revision;
        Slots = Array.AsReadOnly((ItemEntity?[])slots.Clone());
        MaxWeaponSlots = WeaponSlotPolicy.Capacity(slots);
        UsedWeaponSlots = WeaponSlotPolicy.Used(slots);
    }
}
