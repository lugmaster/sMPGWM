using System;
using System.Collections.Generic;
using System.Linq;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;

namespace sMPGWM.Scripts.Items;

/// <summary>Item identity, storage and movement. Commands run on the game thread.</summary>
public sealed class ItemStore
{
    private Dictionary<ItemEntityId, ItemEntity> _items = new();
    private Dictionary<ItemEntityId, ItemLocation> _locations = new();
    private readonly Dictionary<ItemContainerId, ItemContainer> _containers = new();
    internal bool Publishing { get; private set; }

    public ItemEntity GetItem(ItemEntityId id) => _items[id];
    public ItemLocation GetLocation(ItemEntityId id) => _locations[id];

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
        var extra = Math.Max(0, stacks - destination.Entities.Count(item => item is null));
        if (extra > 0 && destination.GrowthChunk == 0) return InventoryResult.CapacityExceeded;
        var chunks = extra == 0 ? 0 : (extra - 1) / destination.GrowthChunk + 1;
        if (chunks > (ItemContainer.MaxCapacity - destination.Capacity) / Math.Max(1, destination.GrowthChunk))
            return InventoryResult.CapacityExceeded;
        Dictionary<ItemContainer, ItemEntity?[]> changes;
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
            Commit(changes);
        }
        catch (OutOfMemoryException) { return InventoryResult.CapacityExceeded; }
        catch (OverflowException) { return InventoryResult.CapacityExceeded; }
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
        if (!commit) return TransferResult.Success;
        Dictionary<ItemContainer, ItemEntity?[]> changes;
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
            Commit(changes);
        }
        catch (OutOfMemoryException) { return TransferResult.CapacityExceeded; }
        catch (OverflowException) { return TransferResult.CapacityExceeded; }
        Publish(changes.Keys);
        return TransferResult.Success;
    }

    private bool Owns(ItemContainer container) => _containers.TryGetValue(container.Id, out var registered) && ReferenceEquals(container, registered);

    internal bool Confirm(ItemContainer inventory, ItemEntity?[] inventorySlots, long inventoryRevision,
        ItemContainer equipment, ItemEntity?[] equipmentSlots, long equipmentRevision)
    {
        if (Publishing || !Owns(inventory) || !Owns(equipment) || inventory.Disposed || equipment.Disposed ||
            inventoryRevision < inventory.Revision || equipmentRevision < equipment.Revision ||
            !Valid(inventory, inventorySlots) || !Valid(equipment, equipmentSlots)) return false;
        var identities = new HashSet<ItemEntityId>();
        foreach (var item in inventorySlots)
            if (item != null && !identities.Add(item.Id)) return false;
        foreach (var item in equipmentSlots)
            if (item != null && !identities.Add(item.Id)) return false;
        var changes = new Dictionary<ItemContainer, ItemEntity?[]> { [inventory] = inventorySlots, [equipment] = equipmentSlots };
        Commit(changes);
        inventory.Revision = inventoryRevision;
        equipment.Revision = equipmentRevision;
        Publish(changes.Keys);
        return true;
    }
    private static bool Valid(ItemContainer container, ItemEntity?[] slots)
    {
        if (slots.Length > ItemContainer.MaxCapacity) return false;
        for (var index = 0; index < slots.Length; index++)
            if (slots[index] is { } item && (item.Id == default || item.Count <= 0 ||
                item.Count > item.Definition.StackLimit || !container.Accepts(index, item.Definition, item.Count))) return false;
        return true;
    }

    private void Commit(Dictionary<ItemContainer, ItemEntity?[]> changes)
    {
        // Prepare every allocation and index before installing state. No observers run until all locations agree.
        var items = new Dictionary<ItemEntityId, ItemEntity>(_items);
        var locations = new Dictionary<ItemEntityId, ItemLocation>(_locations);
        foreach (var container in changes.Keys)
            foreach (var item in container.Entities)
                if (item is not null) { items.Remove(item.Id); locations.Remove(item.Id); }
        var installs = new List<Action>();
        foreach (var (container, slots) in changes)
        {
            for (var index = 0; index < slots.Length; index++)
                if (slots[index] is { } item)
                {
                    if (!items.TryAdd(item.Id, item)) throw new ArgumentException("Duplicate item identity across containers.");
                    locations.Add(item.Id, new ItemLocation(container.Id, index));
                }
            installs.Add(container.Prepare(slots));
        }
        foreach (var install in installs) install();
        _items = items;
        _locations = locations;
    }

    private void Publish(IEnumerable<ItemContainer> containers)
    {
        // Publication is outside command error handling: even overflow/OOM in a subscriber must escape.
        Publishing = true;
        try { foreach (var container in containers) container.Publish(); }
        finally { Publishing = false; }
    }
}
