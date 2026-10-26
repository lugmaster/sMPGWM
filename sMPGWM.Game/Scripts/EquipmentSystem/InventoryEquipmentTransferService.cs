#nullable enable

using System;
using System.Collections.Generic;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed class InventoryEquipmentTransferService : IDisposable
{
    private readonly InventoryImpl _inventoryImpl;
    private readonly EquipmentImpl _equipmentImpl;
    private Guid _session;
    private bool _disposed;
    private bool _publishing;

    public InventoryEquipmentTransferService(InventoryImpl inventoryImpl, EquipmentImpl equipmentImpl)
    {
        ArgumentNullException.ThrowIfNull(inventoryImpl);
        ArgumentNullException.ThrowIfNull(equipmentImpl);
        if (!ReferenceEquals(inventoryImpl.Catalog, equipmentImpl.Catalog))
            throw new ArgumentException("State owners must share their item catalog.", nameof(equipmentImpl));
        _inventoryImpl = inventoryImpl;
        _equipmentImpl = equipmentImpl;
    }

    public Guid OpenEditSession()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_publishing) throw new InvalidOperationException("Cannot open an edit session during publication.");
        return _session = Guid.NewGuid();
    }

    public void RevokeEditSession() => _session = Guid.Empty;

    public TransferResult Validate(TransferRequest request) => Plan(request, out _, out _);

    public TransferResult Transfer(TransferRequest request)
    {
        var result = Plan(request, out var inventorySlots, out var equipmentSlots);
        if (result != TransferResult.Success) return result;

        InventoryChangePlan? inventoryPlan;
        EquipmentSnapshot? equipmentPlan;
        try
        {
            inventoryPlan = inventorySlots is null ? null : _inventoryImpl.PrepareChange(inventorySlots);
            equipmentPlan = equipmentSlots is null ? null : _equipmentImpl.PrepareChange(equipmentSlots);
        }
        catch (OutOfMemoryException) { return TransferResult.CapacityExceeded; }
        catch (OverflowException) { return TransferResult.CapacityExceeded; }

        if (inventoryPlan is not null) _inventoryImpl.Install(inventoryPlan);
        if (equipmentPlan is not null) _equipmentImpl.Install(equipmentPlan);
        _publishing = true;
        _inventoryImpl.SetPublicationInProgress(true);
        _equipmentImpl.SetPublicationInProgress(true);
        try
        {
            if (inventoryPlan is not null) _inventoryImpl.NotifyObservers();
            if (equipmentPlan is not null) _equipmentImpl.NotifyObservers();
        }
        finally
        {
            _equipmentImpl.SetPublicationInProgress(false);
            _inventoryImpl.SetPublicationInProgress(false);
            _publishing = false;
        }
        return TransferResult.Success;
    }

    private TransferResult Plan(TransferRequest request, out ItemEntity?[]? inventorySlots,
        out ItemEntity?[]? equipmentSlots)
    {
        ArgumentNullException.ThrowIfNull(request);
        inventorySlots = null;
        equipmentSlots = null;
        if (_disposed || _inventoryImpl.Availability == InventoryResult.Disposed ||
            _equipmentImpl.Availability == TransferResult.Disposed) return TransferResult.Disposed;
        if (_publishing || _inventoryImpl.Availability == InventoryResult.PublicationInProgress ||
            _equipmentImpl.Availability == TransferResult.PublicationInProgress) return TransferResult.PublicationInProgress;
        if (_session == Guid.Empty || request.EditSession != _session) return TransferResult.InvalidSession;
        if (!TryResolve(request.Source, out var sourceIndex, out var source) ||
            !TryResolve(request.Destination, out var destinationIndex, out var destination))
            return TransferResult.InvalidLocation;
        var usesInventory = request.Source.InventoryAddress.HasValue || request.Destination.InventoryAddress.HasValue;
        var usesEquipment = request.Source.EquipmentSlotKey is not null || request.Destination.EquipmentSlotKey is not null;
        if ((usesInventory && request.InventoryRevision != _inventoryImpl.Revision) ||
            (usesEquipment && request.EquipmentRevision != _equipmentImpl.Revision)) return TransferResult.StaleRevision;
        if (source is null || source.Id != request.SourceEntity || destination?.Id != request.DestinationEntity)
            return TransferResult.StaleEntity;
        if (request.Source == request.Destination) return TransferResult.NoChange;

        ItemEntity? nextSource = destination;
        var nextDestination = source;
        if (destination is not null && _inventoryImpl.Catalog.CanStackTogether(source.DefinitionId, destination.DefinitionId))
        {
            var moved = Math.Min(source.Count, _inventoryImpl.Catalog.GetStackLimit(destination.DefinitionId) - destination.Count);
            if (moved == 0) return TransferResult.NoChange;
            nextSource = moved == source.Count ? null : new ItemEntity(source.Id, source.DefinitionId, source.Count - moved);
            nextDestination = new ItemEntity(destination.Id, destination.DefinitionId, destination.Count + moved);
        }
        if (!CanPlace(request.Source, nextSource) || !CanPlace(request.Destination, nextDestination))
            return TransferResult.IncompatiblePlacement;
        if ((usesInventory && _inventoryImpl.Revision == long.MaxValue) ||
            (usesEquipment && _equipmentImpl.Revision == long.MaxValue)) return TransferResult.CapacityExceeded;

        try
        {
            if (usesInventory) inventorySlots = Copy(_inventoryImpl.Snapshot.Slots);
            if (usesEquipment) equipmentSlots = Copy(_equipmentImpl.Snapshot.Slots);
            (request.Source.InventoryAddress.HasValue ? inventorySlots! : equipmentSlots!)[sourceIndex] = nextSource;
            (request.Destination.InventoryAddress.HasValue ? inventorySlots! : equipmentSlots!)[destinationIndex] = nextDestination;
            var identities = new HashSet<ItemEntityId>();
            foreach (var entity in inventorySlots ?? _inventoryImpl.Snapshot.Slots)
                if (entity is not null && !identities.Add(entity.Id)) return TransferResult.StaleEntity;
            foreach (var entity in equipmentSlots ?? _equipmentImpl.Snapshot.Slots)
                if (entity is not null && !identities.Add(entity.Id)) return TransferResult.StaleEntity;
        }
        catch (OutOfMemoryException) { return TransferResult.CapacityExceeded; }
        return TransferResult.Success;
    }

    private bool TryResolve(ItemLocation location, out int index, out ItemEntity? entity)
    {
        index = location.InventoryAddress ?? _equipmentImpl.FindSlot(location.EquipmentSlotKey!);
        var slots = location.InventoryAddress.HasValue ? _inventoryImpl.Snapshot.Slots : _equipmentImpl.Snapshot.Slots;
        entity = index >= 0 && index < slots.Count ? slots[index] : null;
        return index >= 0 && index < slots.Count;
    }

    private bool CanPlace(ItemLocation location, ItemEntity? entity) => entity is null ||
        location.InventoryAddress.HasValue || (entity.Count == 1 && _equipmentImpl.CanPlace(location.EquipmentSlotKey!, entity.DefinitionId));

    private static ItemEntity?[] Copy(IReadOnlyList<ItemEntity?> slots)
    {
        var copy = new ItemEntity?[slots.Count];
        for (var index = 0; index < slots.Count; index++) copy[index] = slots[index];
        return copy;
    }

    public void Dispose()
    {
        RevokeEditSession();
        _disposed = true;
    }
}
