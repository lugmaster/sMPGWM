using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.ItemSystem;

/// <summary>An immutable in-memory storage snapshot for authoritative world restoration.</summary>
public sealed class ItemContainerState
{
    public ItemContainerId Id { get; }
    public long Revision { get; }
    public int GrowthChunk { get; }
    public IReadOnlyList<ItemEntity?> Slots { get; }

    public ItemContainerState(ItemContainerId id, long revision, int growthChunk, IReadOnlyList<ItemEntity?> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        if (slots.Count > ItemContainer.MaxCapacity) throw new ArgumentOutOfRangeException(nameof(slots));
        Id = id;
        Revision = revision;
        GrowthChunk = growthChunk;
        var copy = new ItemEntity?[slots.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = slots[i];
        Slots = Array.AsReadOnly(copy);
    }
}
