using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.Items;

/// <summary>Storage shared by inventories, equipment and chests. Only ItemSystem may change it.</summary>
public sealed class ItemContainer
{
    public const int MaxCapacity = 4096;
    public ItemContainerId Id { get; } = new(RuntimeIds.Next());
    public int Capacity => Slots.Count;
    public long Revision { get; internal set; }
    public IReadOnlyList<ItemEntityId?> Slots { get; private set; }
    internal ItemEntity?[] Entities { get; private set; }
    internal int GrowthChunk { get; }
    internal Func<int, ItemDefinition, int, bool> Accepts { get; }
    internal bool Disposed { get; set; }
    public event Action? Changed;
    internal Func<ItemEntity?[], Action?>? PrepareChange { get; set; }
    internal Action? PublishDomainChange { get; set; }

    internal ItemContainer(int capacity, int growthChunk, Func<int, ItemDefinition, int, bool>? accepts)
    {
        Entities = new ItemEntity?[capacity];
        Slots = Array.AsReadOnly(new ItemEntityId?[capacity]);
        GrowthChunk = growthChunk;
        Accepts = accepts ?? ((_, _, _) => true);
    }

    internal Action Prepare(ItemEntity?[] entities)
    {
        var ids = new ItemEntityId?[entities.Length];
        for (var i = 0; i < entities.Length; i++) ids[i] = entities[i]?.Id;
        var slots = Array.AsReadOnly(ids);
        var revision = checked(Revision + 1);
        return () => { Entities = entities; Slots = slots; Revision = revision; };
    }

    internal void Publish() => Changed?.Invoke();

    internal void Destroy()
    {
        Disposed = true;
        Entities = Array.Empty<ItemEntity?>();
        Slots = Array.Empty<ItemEntityId?>();
        Changed = null;
        PrepareChange = null;
        PublishDomainChange = null;
    }

    internal void Subscribe(Action handler) => Changed += handler;
    internal void Unsubscribe(Action handler) => Changed -= handler;
}
