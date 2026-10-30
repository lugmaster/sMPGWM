using sMPGWM.Scripts.Util;
using System;
using sMPGWM.Scripts.InventorySystem;

namespace sMPGWM.Scripts.EquipmentSystem;

/// <summary>Adapts inventory UI edit sessions and cell addresses to the shared item system.</summary>
public sealed class InventoryEquipmentTransferService : IDisposable, IItemTransfers
{
    private readonly InventoryController _inventoryImpl;
    private readonly EquipmentController _equipmentImpl;
    private ulong _session;
    private bool _disposed;

    public InventoryEquipmentTransferService(InventoryController inventoryImpl, EquipmentController equipmentImpl)
    {
        ArgumentNullException.ThrowIfNull(inventoryImpl);
        ArgumentNullException.ThrowIfNull(equipmentImpl);
        if (!ReferenceEquals(inventoryImpl.Items, equipmentImpl.Items))
            throw new ArgumentException("Inventory and equipment must use the same ItemSystem.");
        _inventoryImpl = inventoryImpl;
        _equipmentImpl = equipmentImpl;
    }

    public ulong OpenEditSession()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inventoryImpl.Items.Publishing) throw new InvalidOperationException("Cannot open an edit session during publication.");
        return _session = RuntimeIds.Next();
    }

    public void RevokeEditSession() => _session = 0UL;
    public TransferResult Validate(TransferRequest request) => Execute(request, false);
    public TransferResult Transfer(TransferRequest request) => Execute(request, true);

    private TransferResult Execute(TransferRequest request, bool commit)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_disposed || _inventoryImpl.Availability == InventoryResult.Disposed ||
            _equipmentImpl.Availability == TransferResult.Disposed) return TransferResult.Disposed;
        if (_inventoryImpl.Availability == InventoryResult.PublicationInProgress ||
            _equipmentImpl.Availability == TransferResult.PublicationInProgress) return TransferResult.PublicationInProgress;
        if (_session == 0UL || request.EditSession != _session) return TransferResult.InvalidSession;
        if (!TryResolve(request.Source, out var source) || !TryResolve(request.Destination, out var destination))
            return TransferResult.InvalidLocation;
        var sourceRevision = request.Source.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision;
        var destinationRevision = request.Destination.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision;
        return _inventoryImpl.Items.TransferItem(request.SourceEntity, source, destination,
            sourceRevision, destinationRevision, request.DestinationEntity, commit);
    }

    private bool TryResolve(ItemLocation cell, out sMPGWM.Scripts.ItemSystem.ItemLocation location)
    {
        var inventory = cell.InventoryAddress.HasValue;
        var index = inventory ? cell.InventoryAddress!.Value : _equipmentImpl.FindSlot(cell.EquipmentSlotKey!);
        var container = inventory ? _inventoryImpl.Container : _equipmentImpl.Container;
        location = new sMPGWM.Scripts.ItemSystem.ItemLocation(container.Id, index);
        return index >= 0 && index < container.Capacity;
    }

    public void Dispose() { RevokeEditSession(); _disposed = true; }
}
