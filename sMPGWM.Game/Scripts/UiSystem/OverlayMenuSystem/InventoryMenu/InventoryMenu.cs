using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.HudSystem.HotbarSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class InventoryMenu : GameOverlayMenu
{
    protected override string ScreenTitle => "InventoryMenu";
    private readonly HashSet<ClickableIcon> _cells = new();
    private IInventoryView? _inventory;
    private IEquipmentView? _equipment;
    private InventoryViewState _viewState = null!;
    private InventoryContainer _inventoryView = null!;
    private EquipmentContainer _equipmentView = null!;
    private ItemVisuals _visuals = null!;
    private ItemInteractionController? _interaction;
    private ItemHotbarState? _hotbar;
    private ItemPointerOverlay? _pointerOverlay;
    private ClickableIcon? _sourceIcon;
    private ClickableIcon? _hoverIcon;
    private ClickableIcon? _pressedIcon;
    private ItemCellBinding? _pressedBinding;
    private long _mappingVersion;
    private long _pressedVersion;
    private bool _nativeDrag;
    private bool _dropping;
    internal bool IsCarrying => _interaction?.IsCarrying == true;

    internal void SetPointerOverlay(ItemPointerOverlay overlay)
    {
        CancelInteraction();
        _pointerOverlay = overlay;
    }

    internal void BindHotbar(ItemHotbarState? state, ItemHotbarAdapter? adapter, HotbarControl view, ulong session)
    {
        if (_hotbar != null) _hotbar.Changed -= OnViewChanging;
        CancelInteraction();
        _hotbar = state;
        _interaction?.BindHotbar(state, adapter);
        if (_hotbar != null) _hotbar.Changed += OnViewChanging;
        foreach (var icon in view.Icons) RegisterCell(icon);
    }

    internal void SetInteraction(IItemTransfers? transfers, ulong session)
    {
        CancelInteraction();
        _interaction = transfers == null ? null : new ItemInteractionController(transfers);
        _interaction?.SetSession(session);
        if (_interaction != null)
        {
            _interaction.Placement = PlaceVisual;
            _interaction.Placed = (source, target) =>
            {
                if (source.Address < 0 && target.VisualSlot is { } slot && _viewState.CanArrange)
                {
                    _viewState.Reconcile(_inventory!.Snapshot);
                    _viewState.Place(source.Entity!.Id, slot);
                    RefreshView();
                }
            };
        }
    }

    private TransferResult? PlaceVisual(ItemCellBinding source, ItemCellBinding target, bool commit)
    {
        if (source.Address < 0 || target.Address < 0) return null;
        if (!_viewState.CanArrange || target.VisualSlot == null) return TransferResult.IncompatiblePlacement;
        if (target.Entity != null && source.Entity!.DefinitionId == target.Entity.DefinitionId && source.Entity.Definition.CanStack)
            return null;
        if (commit)
        {
            _viewState.Place(source.Entity!.Id, target.VisualSlot.Value);
            RefreshView();
        }
        return TransferResult.Success;
    }

    internal void Bind(IInventoryView inventoryImpl, IEquipmentView equipmentImpl, InventoryViewState state)
    {
        Unbind();
        _inventory = inventoryImpl;
        _equipment = equipmentImpl;
        _visuals = new ItemVisuals();
        _viewState = state;
        if (IsNodeReady()) BindViews();
    }

    protected override void OnReady()
    {
        base.OnReady();
        _inventoryView = GetNode<InventoryContainer>("%InventoryContainer");
        _equipmentView = GetNode<EquipmentContainer>("%EquipmentContainer");
        if (_inventory != null) BindViews();
    }

    private void BindViews()
    {
        _inventoryView.Bind(_viewState);
        _inventoryView.CellCreated += RegisterCell;
        _equipmentView.CellCreated += RegisterCell;
        foreach (var cell in _inventoryView.Cells) RegisterCell(cell);
        foreach (var cell in _equipmentView.Cells) RegisterCell(cell);
        _inventoryView.ViewChanging += OnViewChanging;
        _inventoryView.ViewChanged += RefreshView;
        _inventory!.Changed += RefreshInventory;
        _equipment!.Changed += RefreshEquipment;
        RefreshInventory(_inventory.Snapshot);
        RefreshEquipment(_equipment.Snapshot);
    }

    internal void Unbind()
    {
        DismissTooltips();
        SetInteraction(null, 0UL);
        if (_hotbar != null) _hotbar.Changed -= OnViewChanging;
        _hotbar = null;
        if (_inventory != null) _inventory.Changed -= RefreshInventory;
        if (_equipment != null) _equipment.Changed -= RefreshEquipment;
        if (_inventoryView != null)
        {
            _inventoryView.CellCreated -= RegisterCell;
            _equipmentView.CellCreated -= RegisterCell;
            _inventoryView.ViewChanging -= OnViewChanging;
            _inventoryView.ViewChanged -= RefreshView;
            _inventoryView.Unbind();
        }
        foreach (var cell in new List<ClickableIcon>(_cells)) UnregisterCell(cell);
        _inventory = null;
        _equipment = null;
    }

    private void RegisterCell(ClickableIcon cell)
    {
        if (!_cells.Add(cell)) return;
        cell.InputReceived += OnCellInput;
        cell.HoverChanged += OnCellHoverChanged;
        cell.Exiting += UnregisterCell;
        cell.DragRequested = BeginNativeDrag;
        cell.DropValidated = CanDrop;
        cell.DropRequested = Drop;
    }

    private void UnregisterCell(ClickableIcon cell)
    {
        if (!_cells.Remove(cell)) return;
        cell.InputReceived -= OnCellInput;
        cell.HoverChanged -= OnCellHoverChanged;
        cell.Exiting -= UnregisterCell;
        cell.DragRequested = null;
        cell.DropValidated = null;
        cell.DropRequested = null;
    }

    internal void DismissTooltips()
    {
        foreach (var cell in _cells) cell.DismissTooltip();
    }

    public override void _ExitTree() { Unbind(); base._ExitTree(); }
    private void RefreshInventory(InventorySnapshot snapshot) { OnViewChanging(); _inventoryView.Render(snapshot, _visuals); }
    private void RefreshEquipment(EquipmentSnapshot snapshot) { OnViewChanging(); _equipmentView.Render(_equipment!.Definitions, snapshot, _visuals); }
    private void RefreshView() => RefreshInventory(_inventory!.Snapshot);
    private void OnViewChanging() { _mappingVersion++; CancelInteraction(); }

    private Variant BeginNativeDrag(ClickableIcon icon)
    {
        if (_interaction == null || _inventory == null || _equipment == null || icon != _pressedIcon ||
            _pressedVersion != _mappingVersion || icon.Binding is not ItemCellBinding binding || binding != _pressedBinding) return default;
        var token = _interaction.BeginDrag(binding, _inventory.Snapshot.Revision, _equipment.Snapshot.Revision, _mappingVersion);
        if (token == 0UL) return default;
        _nativeDrag = true;
        _pressedIcon = null;
        DismissTooltips();
        _sourceIcon = icon;
        _pointerOverlay?.SetDragPreviewFor(icon, icon.IconTexture);
        UpdateFeedback(GetViewport().GuiGetHoveredControl() as ClickableIcon);
        return token.ToString();
    }

    private static ulong DragToken(Variant data) =>
        data.VariantType == Variant.Type.String && ulong.TryParse(data.AsString(), out var token) ? token : 0UL;

    private bool CanDrop(ClickableIcon icon, Variant data) => icon.Binding is ItemCellBinding binding &&
        _interaction?.ValidateDrag(DragToken(data), binding) == TransferResult.Success;

    private void Drop(ClickableIcon icon, Variant data)
    {
        if (icon.Binding is not ItemCellBinding binding) return;
        _dropping = true;
        try { _interaction?.Drop(DragToken(data), binding); }
        finally { _dropping = false; CancelInteraction(); }
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd || !_nativeDrag) return;
        _nativeDrag = false;
        CancelInteraction();
    }

    internal void CancelInteraction()
    {
        _interaction?.Cancel();
        // Button focus outlives the carried item unless explicitly released.
        // Leave toolbar focus alone (for example, when typing in search).
        if (IsInsideTree() && GetViewport().GuiGetFocusOwner() is ClickableIcon focused && _cells.Contains(focused))
            focused.ReleaseFocus();
        _pressedIcon = null;
        _pressedBinding = null;
        RestoreHighlight(_sourceIcon);
        RestoreHighlight(_hoverIcon);
        _sourceIcon = null;
        _hoverIcon = null;
        if (IsInstanceValid(_pointerOverlay)) _pointerOverlay!.Clear();
        if (_nativeDrag && !_dropping && IsInsideTree()) GetViewport().GuiCancelDrag();
    }

    private static void RestoreHighlight(ClickableIcon? icon)
    {
        if (IsInstanceValid(icon)) icon!.Modulate = icon.DisplayModulate;
    }

    public override void _Input(InputEvent @event)
    {
        if (_interaction == null || @event is not InputEventMouseButton mouse || !mouse.Pressed) return;
        if (_nativeDrag)
        {
            if (mouse.ButtonIndex == MouseButton.Right) { CancelInteraction(); GetViewport().SetInputAsHandled(); }
            return;
        }
        var hovered = GetViewport().GuiGetHoveredControl();
        if (hovered is ClickableIcon icon && _cells.Contains(icon)) return;
        for (var ancestor = hovered; ancestor != null; ancestor = ancestor.GetParent() as Control)
            if (ancestor is MenuToolbarControl)
            {
                CancelInteraction();
                return;
            }
        if (!IsCarrying) return;
        CancelInteraction();
        if (hovered == null || !_inventoryView.IsToolbarControl(hovered)) GetViewport().SetInputAsHandled();
    }

    private void OnCellInput(ClickableIcon icon, InputEvent @event)
    {
        if (_interaction == null || _inventory == null || _equipment == null ||
            @event is not InputEventMouseButton mouse || icon.Binding is not ItemCellBinding binding) return;
        if (mouse.ButtonIndex == MouseButton.Right)
        {
            if (mouse.Pressed)
            {
                CancelInteraction();
                _interaction.Clear(binding);
            }
            icon.AcceptEvent();
            return;
        }
        if (mouse.ButtonIndex != MouseButton.Left || _nativeDrag) return;
        if (mouse.Pressed)
        {
            _pressedIcon = icon;
            _pressedBinding = binding;
            _pressedVersion = _mappingVersion;
            return;
        }
        if (_pressedIcon != icon || _pressedBinding != binding || _pressedVersion != _mappingVersion)
        {
            if (IsCarrying) CancelInteraction();
            return;
        }
        _pressedIcon = null;
        DismissTooltips();
        _interaction.Click(binding, _inventory.Snapshot.Revision, _equipment.Snapshot.Revision, _mappingVersion);
        if (_interaction.Source == binding) _sourceIcon = icon;
        if (!IsCarrying) CancelInteraction();
        else UpdateFeedback(icon);
        icon.AcceptEvent();
    }

    private void OnCellHoverChanged(ClickableIcon icon, bool entered) => UpdateFeedback(entered ? icon : null);

    private void UpdateFeedback(ClickableIcon? hovered)
    {
        RestoreHighlight(_hoverIcon);
        _hoverIcon = null;
        if (!IsCarrying || _pointerOverlay == null) return;
        if (_sourceIcon != null)
        {
            _sourceIcon.Modulate = new Color(1f, 0.85f, 0.4f);
        }
        var target = hovered != null && _cells.Contains(hovered) ? hovered.Binding as ItemCellBinding : null;
        var result = target == null ? null : _interaction!.Validate(target);
        if (!IsCarrying) { CancelInteraction(); return; }
        if (target != null && hovered != _sourceIcon)
        {
            _hoverIcon = hovered;
            hovered!.Modulate = result == TransferResult.Success ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.6f, 0.4f);
        }
        var feedback = result switch
        {
            TransferResult.Success => target!.IsHotbar ? "Assign or move hotbar link" : "Place item",
            TransferResult.IncompatiblePlacement => "Item does not fit here",
            TransferResult.NoChange when target != null && ItemInteractionController.SameCell(_interaction!.Source!, target)
                => "Cancel placement",
            TransferResult.NoChange when target?.IsHotbar == true => _interaction!.Source!.IsHotbar
                ? "These slots already have the same assignment" : "Already assigned here",
            TransferResult.NoChange => "Stack is full",
            null => "Choose a slot - Right-click to cancel",
            _ => "Placement unavailable"
        };
        _pointerOverlay.UpdateDisplay(_sourceIcon?.IconTexture, feedback, !_nativeDrag);
    }
}
