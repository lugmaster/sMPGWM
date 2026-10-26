#nullable enable

using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

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
