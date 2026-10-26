#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.InventorySystem;

public sealed class InventorySystem : IDisposable
{
    public const int InitialCapacity = 128;
    public const int GrowthChunk = 64;

    private readonly ItemCatalog _catalog;
    private readonly Action<Exception>? _reportObserverError;
    private bool _publishing;
    private bool _disposed;

    public InventorySnapshot Snapshot { get; private set; } = new(0, new ItemEntity?[InitialCapacity]);
    public int Capacity => Snapshot.Capacity;
    public long Revision => Snapshot.Revision;
    public event Action<InventorySnapshot>? Changed;
    internal ItemCatalog Catalog => _catalog;
    internal InventoryResult Availability => _disposed ? InventoryResult.Disposed :
        _publishing ? InventoryResult.PublicationInProgress : InventoryResult.Success;

    public InventorySystem(ItemCatalog catalog, Action<Exception>? reportObserverError = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        _reportObserverError = reportObserverError;
    }

    public InventoryResult Add(ItemDefinitionId definitionId, long quantity)
    {
        if (_disposed) return InventoryResult.Disposed;
        if (_publishing) return InventoryResult.PublicationInProgress;
        if (quantity < 0) return InventoryResult.NegativeQuantity;
        if (!_catalog.Definitions.Any(definition => definition.Id == definitionId))
            return InventoryResult.UnknownDefinition;
        if (quantity == 0) return InventoryResult.NoChange;
        if (Revision == long.MaxValue) return InventoryResult.CapacityExceeded;

        var limit = _catalog.GetStackLimit(definitionId);
        var remaining = quantity;
        foreach (var entry in Snapshot.Occupied)
        {
            if (entry.Entity.DefinitionId == definitionId)
                remaining -= Math.Min(remaining, (long)limit - entry.Entity.Count);
        }

        var newStacks = remaining / limit + (remaining % limit == 0 ? 0 : 1);
        var additionalCells = Math.Max(0, newStacks - Snapshot.EmptyAddresses.Count);
        var chunks = additionalCells / GrowthChunk + (additionalCells % GrowthChunk == 0 ? 0 : 1);
        if (chunks > (Array.MaxLength - Capacity) / GrowthChunk)
            return InventoryResult.CapacityExceeded;
        var nextCapacity = Capacity + chunks * GrowthChunk;

        InventoryChangePlan plan;
        try
        {
            var slots = new ItemEntity?[(int)nextCapacity];
            for (var address = 0; address < Capacity; address++)
                slots[address] = Snapshot.Slots[address];

            remaining = quantity;
            foreach (var entry in Snapshot.Occupied)
            {
                var entity = entry.Entity;
                if (entity.DefinitionId != definitionId) continue;
                var added = (int)Math.Min(remaining, (long)limit - entity.Count);
                if (added == 0) continue;
                slots[entry.Address] = new ItemEntity(entity.Id, definitionId, entity.Count + added);
                remaining -= added;
            }

            for (var address = 0; remaining > 0 && address < slots.Length; address++)
            {
                if (slots[address] is not null) continue;
                var count = (int)Math.Min(remaining, limit);
                slots[address] = _catalog.CreateEntity(definitionId, count);
                remaining -= count;
            }

            plan = PrepareChange(slots);
        }
        catch (OutOfMemoryException)
        {
            return InventoryResult.CapacityExceeded;
        }

        var result = Install(plan);
        if (result == InventoryResult.Success) Publish();
        return result;
    }

    internal InventoryChangePlan PrepareChange(ItemEntity?[] slots)
    {
        if (slots.Length < Capacity || (slots.Length - InitialCapacity) % GrowthChunk != 0)
            throw new ArgumentException("Inventory capacity cannot shrink and must grow in chunks.", nameof(slots));
        var identities = new HashSet<ItemEntityId>();
        foreach (var entity in slots)
        {
            if (entity is null) continue;
            if (entity.Id == default || !identities.Add(entity.Id) || entity.Count <= 0 ||
                entity.Count > _catalog.GetStackLimit(entity.DefinitionId))
                throw new ArgumentException("Invalid inventory entity or duplicate identity.", nameof(slots));
        }

        return new InventoryChangePlan(this, Snapshot, new InventorySnapshot(checked(Revision + 1), slots));
    }

    internal InventoryResult Validate(InventoryChangePlan plan)
    {
        if (_disposed) return InventoryResult.Disposed;
        if (_publishing) return InventoryResult.PublicationInProgress;
        if (!ReferenceEquals(plan.Owner, this) || !ReferenceEquals(plan.Before, Snapshot))
            return InventoryResult.StaleRevision;
        return Snapshot.Slots.SequenceEqual(plan.After.Slots) ? InventoryResult.NoChange : InventoryResult.Success;
    }

    internal InventoryResult Install(InventoryChangePlan plan)
    {
        var result = Validate(plan);
        if (result == InventoryResult.Success) Snapshot = plan.After;
        return result;
    }

    internal void Publish()
    {
        _publishing = true;
        try
        {
            NotifyObservers();
        }
        finally
        {
            _publishing = false;
        }
    }

    internal void SetPublicationInProgress(bool publishing) => _publishing = publishing;

    internal void NotifyObservers()
    {
        if (Changed is not { } handlers) return;
        foreach (Action<InventorySnapshot> observer in handlers.GetInvocationList())
        {
            try { observer(Snapshot); }
            catch (Exception exception)
            {
                try { _reportObserverError?.Invoke(exception); }
                catch (Exception) { }
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Changed = null;
    }
}
