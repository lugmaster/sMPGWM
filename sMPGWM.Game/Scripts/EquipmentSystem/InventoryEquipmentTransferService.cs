using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UtilitySystem;

namespace sMPGWM.Scripts.EquipmentSystem;

/// <summary>Adapts inventory UI edit sessions and cell addresses to the shared item system.</summary>
public sealed class InventoryEquipmentTransferService : IDisposable, IItemTransfers
{
    private InventoryController _inventoryImpl;
    private EquipmentController _equipmentImpl;
    private ulong _session;
    private bool _disposed;
    private Func<TransferRequest, TransferResult>? _authority;

    public void SetAuthority(Func<TransferRequest, TransferResult> authority) => _authority = authority;

    public InventoryEquipmentTransferService(InventoryController inventoryImpl, EquipmentController equipmentImpl)
    {
        ArgumentNullException.ThrowIfNull(inventoryImpl);
        ArgumentNullException.ThrowIfNull(equipmentImpl);
        if (!ReferenceEquals(inventoryImpl.Items, equipmentImpl.Items))
            throw new ArgumentException("Inventory and equipment must use the same ItemSystem.");
        _inventoryImpl = inventoryImpl;
        _equipmentImpl = equipmentImpl;
    }

    public bool Ready { get; set; } = true;

    public void Rebind(InventoryController inventory, EquipmentController equipment)
    {
        RevokeEditSession();
        if (!ReferenceEquals(inventory.Items, equipment.Items))
            throw new ArgumentException("Containers must share an item authority.");
        _inventoryImpl = inventory;
        _equipmentImpl = equipment;
    }

    public ulong OpenEditSession()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inventoryImpl.Items.Publishing) throw new InvalidOperationException("Cannot open an edit session during publication.");
        return _session = Ready ? RuntimeIds.Next() : 0UL;
    }

    public void RevokeEditSession() => _session = 0UL;
    public TransferResult Validate(TransferRequest request) => Execute(request, false);
    public TransferResult Transfer(TransferRequest request)
    {
        var validation = Execute(request, false);
        if (validation != TransferResult.Success) return validation;
        return _authority != null ? _authority(request) : Execute(request, true);
    }

    private TransferResult Execute(TransferRequest request, bool commit)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_disposed || _inventoryImpl.Availability == InventoryResult.Disposed ||
            _equipmentImpl.Availability == TransferResult.Disposed) return TransferResult.Disposed;
        if (_inventoryImpl.Availability == InventoryResult.PublicationInProgress ||
            _equipmentImpl.Availability == TransferResult.PublicationInProgress) return TransferResult.PublicationInProgress;
        if (!Ready || _session == 0UL || request.EditSession != _session) return TransferResult.InvalidSession;
        if (!ItemCellResolver.TryResolve(request.Source, _inventoryImpl, _equipmentImpl, out var source) || !ItemCellResolver.TryResolve(request.Destination, _inventoryImpl, _equipmentImpl, out var destination))
            return TransferResult.InvalidLocation;
        var sourceRevision = request.Source.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision;
        var destinationRevision = request.Destination.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision;
        return _inventoryImpl.Items.TransferItem(request.SourceEntity, source, destination,
            sourceRevision, destinationRevision, request.DestinationEntity, commit);
    }

    public void Dispose() { RevokeEditSession(); _disposed = true; }
}
