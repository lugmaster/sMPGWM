using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Items.Equipment;

public sealed class EquipmentSnapshot
{
    public long Revision { get; }
    public IReadOnlyList<ItemEntity?> Slots { get; }

    internal EquipmentSnapshot(long revision, ItemEntity?[] slots)
    {
        Revision = revision;
        Slots = Array.AsReadOnly((ItemEntity?[])slots.Clone());
    }
}
