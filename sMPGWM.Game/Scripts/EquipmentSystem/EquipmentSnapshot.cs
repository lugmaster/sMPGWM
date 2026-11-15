using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed class EquipmentSnapshot
{
    public long Revision { get; }
    public WeaponGroupConfiguration WeaponGroups { get; }
    public IReadOnlyList<ItemEntity?> Slots { get; }
    public int MaxWeaponSlots { get; }
    public int UsedWeaponSlots { get; }
    public int FreeWeaponSlots => MaxWeaponSlots - UsedWeaponSlots;

    internal EquipmentSnapshot(long revision, ItemEntity?[] slots) : this(revision, slots, WeaponGroupConfiguration.Empty) { }

    internal EquipmentSnapshot(long revision, ItemEntity?[] slots, WeaponGroupConfiguration weaponGroups)
    {
        WeaponGroups = weaponGroups;
        Revision = revision;
        Slots = Array.AsReadOnly((ItemEntity?[])slots.Clone());
        MaxWeaponSlots = WeaponSlotPolicy.Capacity(slots);
        UsedWeaponSlots = WeaponSlotPolicy.Used(slots);
    }
}
