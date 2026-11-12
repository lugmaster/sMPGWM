using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.ItemSystem;

/// <summary>Item identity, storage and movement. Commands run on the game thread.</summary>
public sealed class ItemStore
{
    private readonly Dictionary<ItemEntityId, ItemEntity> _items = new();
    private readonly Dictionary<ItemEntityId, ItemLocation> _locations = new();
    private readonly Dictionary<ItemContainerId, ItemContainer> _containers = new();
    internal bool Publishing { get; private set; }

    public ItemEntity GetItem(ItemEntityId id) => _items[id];
    public ItemLocation GetLocation(ItemEntityId id) => _locations[id];

    public void DestroyContainer(ItemContainer container)
    {
        if (!Owns(container)) return;
        if (Publishing) throw new InvalidOperationException("Cannot destroy storage during publication.");
        foreach (var item in container.Entities)
        {
            if (item == null) continue;
            _items.Remove(item.Id);
            _locations.Remove(item.Id);
        }
        _containers.Remove(container.Id);
        container.Destroy();
    }

    public ItemContainer CreateContainer(int capacity, int growthChunk = 0,
        Func<int, ItemDefinition, int, bool>? accepts = null)
    {
        if (Publishing) throw new InvalidOperationException("Cannot create storage during publication.");
        if (capacity < 0 || capacity > ItemContainer.MaxCapacity || growthChunk < 0 || growthChunk > ItemContainer.MaxCapacity)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        var container = new ItemContainer(capacity, growthChunk, accepts);
        _containers.Add(container.Id, container);
        return container;
    }

    /// <summary>Creates stacks directly at their initial location; no registered item exists without one.</summary>
    public InventoryResult CreateItems(ItemContainer destination, string definitionId, long quantity)
    {
        // Multiplayer: send this command to the host; clients must not create authoritative items locally.
        if (!Owns(destination) || destination.Disposed) return InventoryResult.Disposed;
        if (Publishing) return InventoryResult.PublicationInProgress;
        if (quantity < 0) return InventoryResult.NegativeQuantity;
        if (quantity == 0) return InventoryResult.NoChange;
        if (!ItemRegistry.TryGetById(definitionId, out var definition)) return InventoryResult.UnknownDefinition;
        if (destination.Revision == long.MaxValue) return InventoryResult.CapacityExceeded;
        var remaining = quantity;
        foreach (var item in destination.Entities)
            if (item?.DefinitionId == definitionId) remaining -= Math.Min(remaining, definition.StackLimit - item.Count);
        var stacks = remaining / definition.StackLimit + (remaining % definition.StackLimit == 0 ? 0 : 1);
        var emptySlots = 0;
        foreach (var item in destination.Entities)
            if (item == null) emptySlots++;
        var extra = Math.Max(0, stacks - emptySlots);
        if (extra > 0 && destination.GrowthChunk == 0) return InventoryResult.CapacityExceeded;
        var chunks = extra == 0 ? 0 : (extra - 1) / destination.GrowthChunk + 1;
        if (chunks > (ItemContainer.MaxCapacity - destination.Capacity) / Math.Max(1, destination.GrowthChunk))
            return InventoryResult.CapacityExceeded;
        Dictionary<ItemContainer, ItemEntity?[]> changes;
        PreparedTransaction transaction;
        try
        {
            var slots = new ItemEntity?[checked(destination.Capacity + (int)chunks * destination.GrowthChunk)];
            Array.Copy(destination.Entities, slots, destination.Capacity);
            remaining = quantity;
            for (var index = 0; index < slots.Length && remaining > 0; index++)
            {
                if (slots[index] is not { } item || item.DefinitionId != definitionId) continue;
                var added = (int)Math.Min(remaining, definition.StackLimit - item.Count);
                if (added == 0) continue;
                slots[index] = new ItemEntity(item.Id, definition, item.Count + added);
                remaining -= added;
            }
            for (var index = 0; index < slots.Length && remaining > 0; index++)
            {
                if (slots[index] is not null) continue;
                var count = (int)Math.Min(remaining, definition.StackLimit);
                slots[index] = new ItemEntity(ItemEntityId.Create(), definition, count);
                remaining -= count;
            }
            if (!Valid(destination, slots)) return InventoryResult.CapacityExceeded;
            changes = new Dictionary<ItemContainer, ItemEntity?[]> { [destination] = slots };
            transaction = Prepare(changes);
        }
        catch (OutOfMemoryException) { return InventoryResult.CapacityExceeded; }
        catch (OverflowException) { return InventoryResult.CapacityExceeded; }
        catch (ArgumentException) { return InventoryResult.CapacityExceeded; }
        catch (InvalidOperationException) { return InventoryResult.CapacityExceeded; }
        transaction.Commit();
        Publish(changes.Keys);
        return InventoryResult.Success;
    }

    public TransferResult TransferItem(ItemEntityId itemId, ItemLocation expectedSource, ItemLocation destination,
        long sourceRevision, long destinationRevision, ItemEntityId? expectedDestination = null, bool commit = true)
    {
        // Multiplayer: request this transfer from the host and apply its confirmed result on clients.
        if (Publishing) return TransferResult.PublicationInProgress;
        if (!_containers.TryGetValue(expectedSource.ContainerId, out var sourceContainer) ||
            !_containers.TryGetValue(destination.ContainerId, out var targetContainer)) return TransferResult.InvalidLocation;
        if (sourceContainer.Disposed || targetContainer.Disposed) return TransferResult.Disposed;
        if (expectedSource.SlotIndex < 0 || expectedSource.SlotIndex >= sourceContainer.Capacity ||
            destination.SlotIndex < 0 || destination.SlotIndex >= targetContainer.Capacity) return TransferResult.InvalidLocation;
        if (sourceRevision != sourceContainer.Revision || destinationRevision != targetContainer.Revision)
            return TransferResult.StaleRevision;
        var target = targetContainer.Entities[destination.SlotIndex];
        if (!_locations.TryGetValue(itemId, out var actual) || actual != expectedSource || target?.Id != expectedDestination)
            return TransferResult.StaleEntity;
        if (expectedSource == destination) return TransferResult.NoChange;
        var source = _items[itemId];
        ItemEntity? nextSource = target;
        var nextTarget = source;
        var sourceCount = target?.Count ?? 0;
        var targetCount = source.Count;
        if (target is not null && target.DefinitionId == source.DefinitionId && source.Definition.CanStack)
        {
            var moved = Math.Min(source.Count, target.Definition.StackLimit - target.Count);
            if (moved == 0) return TransferResult.NoChange;
            sourceCount = source.Count - moved;
            targetCount = target.Count + moved;
            nextSource = sourceCount == 0 ? null : source;
            nextTarget = target;
        }
        if (nextSource is not null && !sourceContainer.Accepts(expectedSource.SlotIndex, nextSource.Definition, sourceCount) ||
            !targetContainer.Accepts(destination.SlotIndex, nextTarget.Definition, targetCount)) return TransferResult.IncompatiblePlacement;
        if (sourceContainer.Revision == long.MaxValue || targetContainer.Revision == long.MaxValue)
            return TransferResult.CapacityExceeded;
        if (!commit && sourceContainer.PrepareChange == null && targetContainer.PrepareChange == null)
            return TransferResult.Success;
        Dictionary<ItemContainer, ItemEntity?[]> changes;
        PreparedTransaction transaction;
        try
        {
            if (nextSource is not null && nextSource.Count != sourceCount)
                nextSource = new ItemEntity(nextSource.Id, nextSource.Definition, sourceCount);
            if (nextTarget.Count != targetCount)
                nextTarget = new ItemEntity(nextTarget.Id, nextTarget.Definition, targetCount);
            var sourceSlots = (ItemEntity?[])sourceContainer.Entities.Clone();
            var targetSlots = ReferenceEquals(sourceContainer, targetContainer) ? sourceSlots : (ItemEntity?[])targetContainer.Entities.Clone();
            sourceSlots[expectedSource.SlotIndex] = nextSource;
            targetSlots[destination.SlotIndex] = nextTarget;
            changes = new Dictionary<ItemContainer, ItemEntity?[]> { [sourceContainer] = sourceSlots, [targetContainer] = targetSlots };
            transaction = Prepare(changes);
        }
        catch (OutOfMemoryException) { return TransferResult.CapacityExceeded; }
        catch (OverflowException) { return TransferResult.CapacityExceeded; }
        catch (ArgumentException) { return TransferResult.IncompatiblePlacement; }
        catch (InvalidOperationException) { return TransferResult.IncompatiblePlacement; }
        if (!commit) return TransferResult.Success;
        transaction.Commit();
        Publish(changes.Keys);
        return TransferResult.Success;
    }

    private bool Owns(ItemContainer container) => _containers.TryGetValue(container.Id, out var registered) && ReferenceEquals(container, registered);

    internal bool Confirm(ItemContainer inventory, ItemEntity?[] inventorySlots, long inventoryRevision,
        ItemContainer equipment, ItemEntity?[] equipmentSlots, long equipmentRevision) =>
        Confirm(new[] { new ItemContainerConfirmation(inventory, inventorySlots, inventoryRevision),
            new ItemContainerConfirmation(equipment, equipmentSlots, equipmentRevision) });

    internal bool Confirm(IReadOnlyList<ItemContainerConfirmation> batch)
    {
        if (Publishing || batch.Count == 0 || batch.Count > 32) return false;
        var changes = new Dictionary<ItemContainer, ItemEntity?[]>();
        foreach (var entry in batch)
        {
            var container = entry.Container;
            if (!Owns(container) || container.Disposed || entry.Revision < container.Revision ||
                entry.Revision < 0 || !Valid(container, entry.Slots) || !changes.TryAdd(container, entry.Slots)) return false;
            if (entry.Revision == container.Revision && !SameItems(container.Entities, entry.Slots)) return false;
        }
        PreparedTransaction transaction;
        try { transaction = Prepare(changes); }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (OverflowException) { return false; }
        transaction.Commit();
        foreach (var entry in batch) entry.Container.Revision = entry.Revision;
        Publish(changes.Keys);
        return true;
    }

    private static bool SameItems(ItemEntity?[] left, ItemEntity?[] right)
    {
        if (left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++)
        {
            var a = left[i];
            var b = right[i];
            if (a?.Id != b?.Id || a?.DefinitionId != b?.DefinitionId || a?.Count != b?.Count) return false;
        }
        return true;
    }

    public void EvictReplica(ItemContainer container)
    {
        if (!Owns(container) || container.Disposed) return;
        if (Publishing) throw new InvalidOperationException("Cannot evict a replica during publication.");
        var changes = new Dictionary<ItemContainer, ItemEntity?[]> { [container] = new ItemEntity?[container.Capacity] };
        Prepare(changes).Commit();
        container.Revision = 0;
        Publish(changes.Keys);
    }

    private static bool Valid(ItemContainer container, ItemEntity?[] slots)
    {
        if (slots.Length > ItemContainer.MaxCapacity || container.GrowthChunk == 0 && slots.Length != container.Capacity) return false;
        for (var index = 0; index < slots.Length; index++)
            if (slots[index] is { } item && (item.Id == default || item.Count <= 0 ||
                item.Count > item.Definition.StackLimit || !container.Accepts(index, item.Definition, item.Count))) return false;
        return true;
    }

    private PreparedTransaction Prepare(Dictionary<ItemContainer, ItemEntity?[]> changes)
    {
        var identities = new HashSet<ItemEntityId>();
        var finalCount = _items.Count;
        foreach (var (container, slots) in changes)
        {
            foreach (var item in container.Entities)
                if (item != null) finalCount--;
            foreach (var item in slots)
            {
                if (item == null) continue;
                if (!identities.Add(item.Id) || _locations.TryGetValue(item.Id, out var location) &&
                    !changes.ContainsKey(_containers[location.ContainerId]))
                    throw new ArgumentException("Duplicate item identity across containers.");
                finalCount = checked(finalCount + 1);
            }
        }
        var states = new ItemContainer.PreparedState[changes.Count];
        var domainInstalls = new Action?[changes.Count];
        var index = 0;
        foreach (var (container, slots) in changes)
        {
            states[index] = container.Prepare(slots);
            domainInstalls[index] = container.PrepareChange?.Invoke(slots);
            index++;
        }
        // Reserve before committing so index updates cannot allocate midway through a transfer.
        _items.EnsureCapacity(finalCount);
        _locations.EnsureCapacity(finalCount);
        return new PreparedTransaction(this, states, domainInstalls);
    }

    private sealed class PreparedTransaction(ItemStore store, ItemContainer.PreparedState[] states, Action?[] domainInstalls)
    {
        internal void Commit()
        {
            foreach (var state in states)
                foreach (var item in state.Container.Entities)
                    if (item != null) { store._items.Remove(item.Id); store._locations.Remove(item.Id); }
            foreach (var state in states)
            {
                state.Install();
                var entities = state.Container.Entities;
                for (var index = 0; index < entities.Length; index++)
                {
                    if (entities[index] is not { } item) continue;
                    store._items.Add(item.Id, item);
                    store._locations.Add(item.Id, new ItemLocation(state.Container.Id, index));
                }
            }
            foreach (var install in domainInstalls) install?.Invoke();
        }
    }

    private void Publish(IEnumerable<ItemContainer> containers)
    {
        // Publication is outside command error handling: even overflow/OOM in a subscriber must escape.
        Publishing = true;
        try
        {
            foreach (var container in containers) container.PublishDomainChange?.Invoke();
            foreach (var container in containers) container.Publish();
        }
        finally { Publishing = false; }
    }
}
