#nullable enable

using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.InventorySystem;

public sealed class InventorySnapshot
{
    public long Revision { get; }
    public int Capacity => Slots.Count;
    public IReadOnlyList<ItemEntity?> Slots { get; }
    public IReadOnlyList<InventoryEntry> Occupied { get; }
    public IReadOnlyList<int> EmptyAddresses { get; }

    internal InventorySnapshot(long revision, ItemEntity?[] slots)
    {
        Revision = revision;
        Slots = Array.AsReadOnly((ItemEntity?[])slots.Clone());
        var occupied = new List<InventoryEntry>();
        var empty = new List<int>();
        for (var address = 0; address < slots.Length; address++)
        {
            if (slots[address] is { } entity)
                occupied.Add(new InventoryEntry(address, entity));
            else
                empty.Add(address);
        }

        Occupied = occupied.AsReadOnly();
        EmptyAddresses = empty.AsReadOnly();
    }
}
