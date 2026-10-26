#nullable enable

using System;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed class ItemInteractionController(InventoryEquipmentTransferService transfers)
{
    internal enum PlacementMode { Idle, Click, NativeDrag }

    private Guid _session;
    private long _inventoryRevision;
    private long _equipmentRevision;
    private long _hotbarRevision;
    private long _mappingVersion;
    private Guid _dragToken;
    private ItemHotbarState? _hotbar;
    private ItemHotbarAdapter? _adapter;

    internal ItemCellBinding? Source { get; private set; }
    internal PlacementMode Mode { get; private set; }
    internal bool IsCarrying => Mode != PlacementMode.Idle;
    internal bool OwnsDrag(Guid token) => token != Guid.Empty && token == _dragToken && Mode == PlacementMode.NativeDrag;
    internal TransferResult? Feedback { get; private set; }

    internal void BindHotbar(ItemHotbarState? hotbar, ItemHotbarAdapter? adapter)
    {
        Cancel();
        _hotbar = hotbar;
        _adapter = adapter;
    }

    internal void SetSession(Guid session)
    {
        Cancel();
        _session = session;
    }

    internal void Cancel()
    {
        Source = null;
        Mode = PlacementMode.Idle;
        Feedback = null;
        _dragToken = Guid.Empty;
    }

    internal Guid BeginDrag(ItemCellBinding source, long inventoryRevision, long equipmentRevision, long mappingVersion)
    {
        Cancel();
        Click(source, inventoryRevision, equipmentRevision, mappingVersion);
        if (!IsCarrying) return Guid.Empty;
        Mode = PlacementMode.NativeDrag;
        return _dragToken = Guid.NewGuid();
    }

    internal TransferResult? ValidateDrag(Guid token, ItemCellBinding target) =>
        OwnsDrag(token) ? Evaluate(target, false) : null;

    internal TransferResult? Drop(Guid token, ItemCellBinding target)
    {
        if (!OwnsDrag(token)) return null;
        try { return Evaluate(target, true); }
        finally { Cancel(); }
    }

    internal void Refresh(long inventoryRevision, long equipmentRevision, long mappingVersion)
    {
        if (IsCarrying && (_inventoryRevision != inventoryRevision || _equipmentRevision != equipmentRevision ||
            _mappingVersion != mappingVersion || _hotbarRevision != (_hotbar?.Revision ?? 0))) Cancel();
    }

    internal void Click(ItemCellBinding target, long inventoryRevision, long equipmentRevision, long mappingVersion)
    {
        var wasCarrying = IsCarrying;
        Refresh(inventoryRevision, equipmentRevision, mappingVersion);
        if (wasCarrying && !IsCarrying || _session == Guid.Empty) return;
        if (Source == null)
        {
            if (target.IsHotbar)
            {
                if (_hotbar == null || _hotbar.ValidateEdit(_session, target.Revision, target.HotbarPosition!.Value) != HotbarResult.Success ||
                    _hotbar.Entries[target.HotbarPosition.Value] == null) return;
            }
            else if (target.Entity == null || target.Revision !=
                (target.Location.InventoryAddress.HasValue ? inventoryRevision : equipmentRevision)) return;
            Source = target;
            Mode = PlacementMode.Click;
            _inventoryRevision = inventoryRevision;
            _equipmentRevision = equipmentRevision;
            _hotbarRevision = _hotbar?.Revision ?? 0;
            _mappingVersion = mappingVersion;
            return;
        }
        if (SameCell(Source, target)) { Cancel(); return; }
        var result = Evaluate(target, true);
        if (result == TransferResult.Success || IsStale(result)) Cancel();
        else Feedback = result;
    }

    internal TransferResult? Validate(ItemCellBinding target)
    {
        if (Source == null) return null;
        var result = Evaluate(target, false);
        if (IsStale(result)) Cancel();
        return result;
    }

    internal HotbarResult Clear(ItemCellBinding target) => !target.IsHotbar || _hotbar == null
        ? HotbarResult.IneligibleSource : _hotbar.Clear(_session, target.Revision, target.HotbarPosition!.Value);

    internal static bool SameCell(ItemCellBinding left, ItemCellBinding right) =>
        left.IsHotbar == right.IsHotbar && (left.IsHotbar ? left.HotbarPosition == right.HotbarPosition : left.Location == right.Location);

    private TransferResult Evaluate(ItemCellBinding target, bool commit)
    {
        var source = Source!;
        if (!target.IsHotbar)
        {
            if (source.IsHotbar) return TransferResult.IncompatiblePlacement;
            if (target.Revision != (target.Location.InventoryAddress.HasValue ? _inventoryRevision : _equipmentRevision))
                return TransferResult.StaleRevision;
            return commit ? transfers.Transfer(Request(target)) : transfers.Validate(Request(target));
        }
        if (_hotbar == null || _adapter == null) return TransferResult.InvalidSession;
        var validation = _hotbar.ValidateEdit(_session, _hotbarRevision, target.HotbarPosition!.Value);
        if (validation != HotbarResult.Success) return Convert(validation);
        if (target.Revision != _hotbarRevision) return TransferResult.StaleRevision;
        if (source.IsHotbar)
            return commit ? Convert(_hotbar.Move(_session, _hotbarRevision, source.HotbarPosition!.Value, target.HotbarPosition.Value)) :
                Convert(_hotbar.ValidateMove(_session, _hotbarRevision, source.HotbarPosition!.Value, target.HotbarPosition.Value));
        return Convert(_adapter.Assign(_session, _hotbarRevision, target.HotbarPosition.Value, source.Location, source.Entity!.Id,
            _inventoryRevision, _equipmentRevision, commit));
    }

    private TransferRequest Request(ItemCellBinding target) => new(_session, Source!.Location, Source.Entity!.Id,
        target.Location, target.Entity?.Id,
        target.Location.InventoryAddress.HasValue ? target.Revision : _inventoryRevision,
        target.Location.EquipmentSlotKey != null ? target.Revision : _equipmentRevision);

    private static TransferResult Convert(HotbarResult result) => result switch
    {
        HotbarResult.Success => TransferResult.Success,
        HotbarResult.NoChange => TransferResult.NoChange,
        HotbarResult.StaleRevision => TransferResult.StaleRevision,
        HotbarResult.InvalidSession => TransferResult.InvalidSession,
        HotbarResult.InvalidLocation or HotbarResult.InvalidPosition => TransferResult.InvalidLocation,
        HotbarResult.Disposed => TransferResult.Disposed,
        HotbarResult.PublicationInProgress => TransferResult.PublicationInProgress,
        HotbarResult.IneligibleSource or HotbarResult.Unavailable => TransferResult.IncompatiblePlacement,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
    };

    private static bool IsStale(TransferResult result) => result is TransferResult.InvalidSession or
        TransferResult.StaleRevision or TransferResult.StaleEntity or TransferResult.InvalidLocation or TransferResult.Disposed;
}
