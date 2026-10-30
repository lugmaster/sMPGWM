using sMPGWM.Scripts.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sMPGWM.Scripts.ItemSystem;

public readonly record struct ItemContainerId(ulong Value);

/// <summary>Storage shared by inventories, equipment and chests. Only ItemSystem may change it.</summary>
public sealed class ItemContainer
{
    public ItemContainerId Id { get; } = new(RuntimeIds.Next());
    public int Capacity => Slots.Count;
    public long Revision { get; internal set; }
    public IReadOnlyList<ItemEntityId?> Slots { get; private set; }
    internal ItemEntity?[] Entities { get; private set; }
    internal int GrowthChunk { get; }
    internal Func<int, ItemDefinition, int, bool> Accepts { get; }
    internal bool Disposed { get; set; }
    public event Action? Changed;

    internal ItemContainer(int capacity, int growthChunk, Func<int, ItemDefinition, int, bool>? accepts)
    {
        Entities = new ItemEntity?[capacity];
        Slots = Array.AsReadOnly(new ItemEntityId?[capacity]);
        GrowthChunk = growthChunk;
        Accepts = accepts ?? ((_, _, _) => true);
    }

    internal Action Prepare(ItemEntity?[] entities)
    {
        var slots = Array.AsReadOnly(entities.Select(item => item?.Id).ToArray());
        var revision = checked(Revision + 1);
        return () => { Entities = entities; Slots = slots; Revision = revision; };
    }

    internal void Publish() => Changed?.Invoke();

    internal void Subscribe(Action handler) => Changed += handler;
    internal void Unsubscribe(Action handler) => Changed -= handler;
}
