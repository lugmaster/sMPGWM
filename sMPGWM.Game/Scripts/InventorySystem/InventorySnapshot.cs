using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.InventorySystem;

public sealed class InventorySnapshot
{
    public long Revision { get; }
    public int Capacity => Slots.Count;
    public IReadOnlyList<ItemEntity?> Slots { get; }
    private IReadOnlyList<InventoryEntry>? _occupied;
    private IReadOnlyList<int>? _emptyAddresses;
    public IReadOnlyList<InventoryEntry> Occupied
    {
        get
        {
            if (_occupied != null) return _occupied;
            var count = 0;
            for (var i = 0; i < Slots.Count; i++)
                if (Slots[i] != null) count++;
            var entries = new InventoryEntry[count];
            var next = 0;
            for (var i = 0; i < Slots.Count; i++)
                if (Slots[i] is { } item) entries[next++] = new InventoryEntry(i, item);
            return _occupied = Array.AsReadOnly(entries);
        }
    }
    public IReadOnlyList<int> EmptyAddresses
    {
        get
        {
            if (_emptyAddresses != null) return _emptyAddresses;
            var count = 0;
            for (var i = 0; i < Slots.Count; i++)
                if (Slots[i] == null) count++;
            var addresses = new int[count];
            var next = 0;
            for (var i = 0; i < Slots.Count; i++)
                if (Slots[i] == null) addresses[next++] = i;
            return _emptyAddresses = Array.AsReadOnly(addresses);
        }
    }

    internal InventorySnapshot(long revision, ItemEntity?[] slots)
    {
        Revision = revision;
        Slots = Array.AsReadOnly((ItemEntity?[])slots.Clone());
    }
}
