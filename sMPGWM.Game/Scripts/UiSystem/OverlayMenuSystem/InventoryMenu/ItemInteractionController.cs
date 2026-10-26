#nullable enable

using System;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed record ItemPointerBinding(ItemLocation Location, ItemEntity? Entity, long Revision)
{
    internal static ItemPointerBinding? From(object? binding) => binding switch
    {
        InventoryCellBinding cell => new(ItemLocation.Inventory(cell.Address), cell.Entity, cell.Revision),
        EquipmentCellBinding cell => new(ItemLocation.Equipment(cell.SlotKey), cell.Entity, cell.Revision),
        _ => null
    };
}

internal sealed class ItemInteractionController(InventoryEquipmentTransferService transfers)
{
    private Guid _session;
    private long _inventoryRevision;
    private long _equipmentRevision;
    private long _mappingVersion;
    private Guid _dragToken;

    internal ItemPointerBinding? Source { get; private set; }
    internal bool IsCarrying => Source != null;
    internal TransferResult? Feedback { get; private set; }

    internal void SetSession(Guid session)
    {
        Cancel();
        _session = session;
    }

    internal void Cancel()
    {
        Source = null;
        Feedback = null;
        _dragToken = Guid.Empty;
    }

    internal Guid BeginDrag(ItemPointerBinding source, long inventoryRevision, long equipmentRevision, long mappingVersion)
    {
        Cancel();
        Click(source, inventoryRevision, equipmentRevision, mappingVersion);
        if (!IsCarrying) return Guid.Empty;
        _dragToken = Guid.NewGuid();
        return _dragToken;
    }

    internal TransferResult? ValidateDrag(Guid token, ItemPointerBinding target) =>
        token != Guid.Empty && token == _dragToken && Source != null ? transfers.Validate(Request(target)) : null;

    internal TransferResult? Drop(Guid token, ItemPointerBinding target)
    {
        if (token == Guid.Empty || token != _dragToken || Source == null) return null;
        try { return transfers.Transfer(Request(target)); }
        finally { Cancel(); }
    }

    internal void Refresh(long inventoryRevision, long equipmentRevision, long mappingVersion)
    {
        if (IsCarrying && (_inventoryRevision != inventoryRevision ||
            _equipmentRevision != equipmentRevision || _mappingVersion != mappingVersion)) Cancel();
    }

    internal void Click(ItemPointerBinding target, long inventoryRevision, long equipmentRevision, long mappingVersion)
    {
        var wasCarrying = IsCarrying;
        Refresh(inventoryRevision, equipmentRevision, mappingVersion);
        if (wasCarrying && !IsCarrying) return;
        if (_session == Guid.Empty) return;
        if (Source == null)
        {
            if (target.Entity == null) return;
            if (target.Revision != (target.Location.InventoryAddress.HasValue ? inventoryRevision : equipmentRevision)) return;
            Source = target;
            _inventoryRevision = inventoryRevision;
            _equipmentRevision = equipmentRevision;
            _mappingVersion = mappingVersion;
            return;
        }
        if (Source.Location == target.Location) { Cancel(); return; }
        var result = transfers.Transfer(Request(target));
        if (result == TransferResult.Success || IsStale(result)) Cancel();
        else Feedback = result;
    }

    internal TransferResult? Validate(ItemPointerBinding target)
    {
        if (Source == null) return null;
        var result = transfers.Validate(Request(target));
        if (IsStale(result)) Cancel();
        return result;
    }

    private TransferRequest Request(ItemPointerBinding target) => new(_session, Source!.Location, Source.Entity!.Id,
        target.Location, target.Entity?.Id,
        target.Location.InventoryAddress.HasValue ? target.Revision : _inventoryRevision,
        target.Location.EquipmentSlotKey != null ? target.Revision : _equipmentRevision);

    private static bool IsStale(TransferResult result) => result is TransferResult.InvalidSession or
        TransferResult.StaleRevision or TransferResult.StaleEntity or TransferResult.InvalidLocation or TransferResult.Disposed;
}
