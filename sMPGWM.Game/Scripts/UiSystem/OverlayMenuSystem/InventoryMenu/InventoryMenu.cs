#nullable enable

using sMPGWM.Scripts.Util.UiComponents;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.EquipmentSystem;
using System;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.HudSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class InventoryMenu : GameOverlayMenu
{
    protected override string ScreenTitle => "InventoryMenu";

    private InventoryImpl? _inventory;
    private EquipmentImpl? _equipment;
    private InventoryViewState _viewState = null!;
    private InventoryContainer _inventoryView = null!;
    private EquipmentContainer _equipmentView = null!;
    private ItemVisuals _visuals = null!;
    private ItemInteractionController? _interaction;
    private long _mappingVersion;
    private ItemPointerBinding? _pressedTarget;
    private long _pressedVersion;
    private bool _consumeRelease;
    private TextureRect? _preview;
    private Label? _feedback;
    private ClickableIcon? _sourceIcon;
    private ClickableIcon? _hoverIcon;
    private bool _nativeDrag;
    private bool _dropping;
    private ItemHotbarState? _hotbar;
    private ItemHotbarAdapter? _hotbarAdapter;
    private HotbarControl? _hotbarView;
    private Guid _session;
    private HotbarCellBinding? _hotbarSource;
    private HotbarCellBinding? _pressedHotbar;
    private Guid _hotbarDragToken;
    private long _sourceHotbarRevision;
    internal bool IsCarrying => _interaction?.IsCarrying == true || _hotbarSource != null;

    internal void BindHotbar(ItemHotbarState? state, ItemHotbarAdapter? adapter, HotbarControl view, Guid session)
    {
        if (_hotbar != null) _hotbar.Changed -= OnHotbarChanged;
        CancelInteraction();
        _hotbar = state;
        _hotbarAdapter = adapter;
        _hotbarView = view;
        _session = session;
        if (_hotbar != null) _hotbar.Changed += OnHotbarChanged;
        if (IsNodeReady()) BindDragAdapters();
    }

    private void OnHotbarChanged()
    {
        OnViewChanging();
        BindDragAdapters();
    }

    internal void SetInteraction(InventoryEquipmentTransferService? transfers, Guid session)
    {
        CancelInteraction();
        _pressedTarget = null;
        _consumeRelease = false;
        _interaction = transfers == null ? null : new ItemInteractionController(transfers);
        _interaction?.SetSession(session);
    }

    internal void Bind(InventoryImpl inventoryImpl, EquipmentImpl equipmentImpl,
        InventoryViewState state)
    {
        Unbind();
        _inventory = inventoryImpl;
        _equipment = equipmentImpl;
        _visuals = new ItemVisuals(inventoryImpl.Catalog);
        _viewState = state;
        if (IsNodeReady()) BindViews();
    }

    protected override void OnReady()
    {
        base.OnReady();
        _inventoryView = GetNode<InventoryContainer>("%InventoryContainer");
        _equipmentView = GetNode<EquipmentContainer>("%EquipmentContainer");
        _preview = new TextureRect { Name = "ItemPointerPreview", MouseFilter = MouseFilterEnum.Ignore, TopLevel = true,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Size = new Vector2(40, 40), ZIndex = 100 };
        _feedback = new Label { Name = "ItemPointerFeedback", MouseFilter = MouseFilterEnum.Ignore, TopLevel = true, ZIndex = 100 };
        AddChild(_preview);
        AddChild(_feedback);
        _preview.Hide();
        _feedback.Hide();
        if (_inventory != null) BindViews();
    }

    private void BindViews()
    {
        _inventoryView.Bind(_viewState);
        _inventoryView.ViewChanging += OnViewChanging;
        _inventory!.Changed += RefreshInventory;
        _equipment!.Changed += RefreshEquipment;
        RefreshInventory(_inventory.Snapshot);
        RefreshEquipment(_equipment.Snapshot);
    }

    internal void Unbind()
    {
        DismissTooltips();
        SetInteraction(null, Guid.Empty);
        if (_hotbar != null) _hotbar.Changed -= OnHotbarChanged;
        if (_hotbarView != null)
            foreach (var icon in _hotbarView.Icons)
                icon.SetDragForwarding(default, default, default);
        _hotbar = null;
        _hotbarAdapter = null;
        _hotbarView = null;
        _session = Guid.Empty;
        if (_inventory != null) _inventory.Changed -= RefreshInventory;
        if (_equipment != null) _equipment.Changed -= RefreshEquipment;
        if (_inventoryView != null)
        {
            _inventoryView.ViewChanging -= OnViewChanging;
            _inventoryView.Unbind();
        }
        _inventory = null;
        _equipment = null;
    }

    internal void DismissTooltips()
    {
        if (!IsNodeReady()) return;
        foreach (var icon in FindChildren("*", "Button", true, false))
            if (icon is ClickableIcon cell) cell.DismissTooltip();
    }

    public override void _ExitTree()
    {
        Unbind();
        base._ExitTree();
    }

    private void RefreshInventory(InventorySnapshot snapshot)
    {
        OnViewChanging();
        _inventoryView.Render(snapshot, _visuals);
        BindDragAdapters();
    }

    private void RefreshEquipment(EquipmentSnapshot snapshot)
    {
        OnViewChanging();
        _equipmentView.Render(_equipment!.Definitions, snapshot, _visuals);
        BindDragAdapters();
    }

    private void BindDragAdapters()
    {
        foreach (var node in FindChildren("*", "Button", true, false))
        {
            if (node is not ClickableIcon icon || ItemPointerBinding.From(icon.Binding) == null) continue;
            icon.SetDragForwarding(
                Callable.From<Vector2, Variant>(_ => BeginNativeDrag(icon)),
                Callable.From<Vector2, Variant, bool>((_, data) => CanDrop(icon, data)),
                Callable.From<Vector2, Variant>((_, data) => Drop(icon, data)));
        }
        if (_hotbarView == null) return;
        foreach (var icon in _hotbarView.Icons)
            icon.SetDragForwarding(
                Callable.From<Vector2, Variant>(_ => BeginNativeDrag(icon)),
                Callable.From<Vector2, Variant, bool>((_, data) => CanDrop(icon, data)),
                Callable.From<Vector2, Variant>((_, data) => Drop(icon, data)));
    }

    private Variant BeginNativeDrag(ClickableIcon icon)
    {
        if (icon.Binding is HotbarCellBinding hotbar)
        {
            if (_session == Guid.Empty || _hotbar == null || _pressedHotbar != hotbar ||
                _pressedVersion != _mappingVersion || _hotbar.Entries[hotbar.Position] == null) return default;
            CancelInteraction();
            _hotbarSource = hotbar;
            _hotbarDragToken = Guid.NewGuid();
            _nativeDrag = true;
            _consumeRelease = true;
            icon.SetDragPreview(new TextureRect { Texture = icon.IconTexture, MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(40, 40) });
            return _hotbarDragToken.ToString();
        }
        var source = ItemPointerBinding.From(icon.Binding);
        if (_interaction == null || _inventory == null || _equipment == null || source == null ||
            _pressedVersion != _mappingVersion || _pressedTarget?.Location != source.Location) return default;
        var token = _interaction.BeginDrag(source, _inventory.Revision, _equipment.Revision, _mappingVersion);
        if (token == Guid.Empty) return default;
        DismissTooltips();
        _pressedTarget = null;
        _consumeRelease = true;
        _nativeDrag = true;
        _sourceHotbarRevision = _hotbar?.Revision ?? 0;
        icon.SetDragPreview(new TextureRect { Texture = icon.IconTexture, MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(40, 40),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        return token.ToString();
    }

    private static Guid DragToken(Variant data) =>
        data.VariantType == Variant.Type.String && Guid.TryParse(data.AsString(), out var token) ? token : Guid.Empty;

    private bool CanDrop(ClickableIcon icon, Variant data)
    {
        if (icon.Binding is HotbarCellBinding hotbar)
            return DragToken(data) != Guid.Empty && ValidateHotbar(hotbar, DragToken(data)) == HotbarResult.Success;
        var target = ItemPointerBinding.From(icon.Binding);
        return target != null && _interaction?.ValidateDrag(DragToken(data), target) == TransferResult.Success;
    }

    private void Drop(ClickableIcon icon, Variant data)
    {
        if (icon.Binding is HotbarCellBinding hotbar)
        {
            if (DragToken(data) == Guid.Empty) return;
            _dropping = true;
            try { PlaceHotbar(hotbar, DragToken(data)); }
            finally { _dropping = false; CancelInteraction(); }
            return;
        }
        var target = ItemPointerBinding.From(icon.Binding);
        if (target == null) return;
        _dropping = true;
        try { _interaction?.Drop(DragToken(data), target); }
        finally { _dropping = false; }
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd || !_nativeDrag) return;
        _nativeDrag = false;
        _pressedTarget = null;
        CancelInteraction();
    }

    private void OnViewChanging()
    {
        _mappingVersion++;
        CancelInteraction();
    }

    private void CancelInteraction()
    {
        _interaction?.Cancel();
        _hotbarSource = null;
        _hotbarDragToken = Guid.Empty;
        ClearHighlights();
        _preview?.Hide();
        _feedback?.Hide();
        if (_nativeDrag && !_dropping && IsInsideTree()) GetViewport().GuiCancelDrag();
    }

    private void ClearHighlights()
    {
        RestoreHighlight(_sourceIcon);
        RestoreHighlight(_hoverIcon);
        _sourceIcon = null;
        _hoverIcon = null;
    }

    private void RestoreHighlight(ClickableIcon? icon)
    {
        if (!GodotObject.IsInstanceValid(icon)) return;
        icon!.Modulate = icon.Binding is HotbarCellBinding hotbar && _hotbarAdapter?.Resolve(hotbar.Position).Available == false
            ? new Color(0.6f, 0.6f, 0.6f) : Colors.White;
    }

    public override void _Input(InputEvent @event)
    {
        if (_interaction == null || _inventory == null || _equipment == null) return;
        if (@event is not InputEventMouseButton mouse) return;
        if (mouse.ButtonIndex == MouseButton.Right)
        {
            var rightTarget = (GetViewport().GuiGetHoveredControl() as ClickableIcon)?.Binding as HotbarCellBinding;
            if (!IsCarrying && !_consumeRelease && (rightTarget == null || _session == Guid.Empty)) return;
            CancelInteraction();
            if (mouse.Pressed && rightTarget != null)
                _hotbar?.Clear(_session, rightTarget.Revision, rightTarget.Position);
            _pressedTarget = null;
            _consumeRelease = mouse.Pressed;
            GetViewport().SetInputAsHandled();
            return;
        }
        if (mouse.ButtonIndex != MouseButton.Left) return;
        if (_nativeDrag)
        {
            if (!mouse.Pressed) { _pressedTarget = null; _consumeRelease = false; }
            return;
        }
        var hovered = GetViewport().GuiGetHoveredControl();
        var icon = hovered as ClickableIcon;
        var target = icon != null && IsAncestorOf(icon) ? ItemPointerBinding.From(icon.Binding) : null;
        var hotbarTarget = icon?.Binding as HotbarCellBinding;
        if (mouse.Pressed)
        {
            _consumeRelease = false;
            _pressedTarget = target;
            _pressedHotbar = hotbarTarget;
            _pressedVersion = _mappingVersion;
            if (!IsCarrying && hotbarTarget == null) return;
            if (!IsCarrying && _session == Guid.Empty) return;
            if (!IsCarrying && hotbarTarget != null) return;
            if (hovered != null && _inventoryView.IsToolbarControl(hovered))
            {
                CancelInteraction();
                _pressedTarget = null;
                return;
            }
            _consumeRelease = true;
            GetViewport().SetInputAsHandled();
            return;
        }
        var consume = _consumeRelease;
        _consumeRelease = false;
        var captured = _pressedTarget;
        var capturedHotbar = _pressedHotbar;
        _pressedTarget = null;
        _pressedHotbar = null;
        if (_pressedVersion != _mappingVersion) { if (consume) GetViewport().SetInputAsHandled(); return; }
        if (hotbarTarget != null && hotbarTarget == capturedHotbar && _session != Guid.Empty)
        {
            if (IsCarrying) PlaceHotbar(hotbarTarget);
            else if (_hotbar?.Entries[hotbarTarget.Position] != null) _hotbarSource = hotbarTarget;
            GetViewport().SetInputAsHandled();
            UpdatePointerVisuals();
            return;
        }
        if (_hotbarSource != null)
        {
            if (target == null) CancelInteraction();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_interaction.IsCarrying && (captured == null || target?.Location != captured.Location)) CancelInteraction();
        else if (captured != null && target?.Location == captured.Location)
        {
            DismissTooltips();
            _interaction.Click(captured, _inventory.Revision, _equipment.Revision, _mappingVersion);
            _sourceHotbarRevision = _hotbar?.Revision ?? 0;
        }
        if (consume) GetViewport().SetInputAsHandled();
        UpdatePointerVisuals();
    }

    public override void _Process(double delta) => UpdatePointerVisuals();

    private HotbarResult ValidateHotbar(HotbarCellBinding target, Guid token = default)
    {
        if (_hotbar == null || _hotbarAdapter == null || _inventory == null || _equipment == null)
            return HotbarResult.InvalidSession;
        if (_hotbarSource != null)
        {
            if (token != Guid.Empty && token != _hotbarDragToken) return HotbarResult.InvalidSession;
            return _hotbar.ValidateEdit(_session, _hotbarSource.Revision, target.Position);
        }
        var source = _interaction?.Source;
        if (source?.Entity == null) return HotbarResult.IneligibleSource;
        if (token != Guid.Empty && !_interaction!.OwnsDrag(token)) return HotbarResult.InvalidSession;
        return _hotbarAdapter.Assign(_session, _sourceHotbarRevision, target.Position, source.Location, source.Entity.Id,
            source.Location.InventoryAddress.HasValue ? source.Revision : _inventory.Revision,
            source.Location.EquipmentSlotKey != null ? source.Revision : _equipment.Revision, false);
    }

    private void PlaceHotbar(HotbarCellBinding target, Guid token = default)
    {
        if (ValidateHotbar(target, token) != HotbarResult.Success) return;
        if (_hotbarSource != null)
            _hotbar!.Move(_session, _hotbarSource.Revision, _hotbarSource.Position, target.Position);
        else
        {
            var source = _interaction!.Source!;
            _hotbarAdapter!.Assign(_session, _sourceHotbarRevision, target.Position, source.Location, source.Entity!.Id,
                _inventory!.Revision, _equipment!.Revision);
        }
        CancelInteraction();
    }

    private void UpdatePointerVisuals()
    {
        ClearHighlights();
        if (_hotbarSource != null && _hotbarView != null && _preview != null && _feedback != null)
        {
            _sourceIcon = _hotbarView.Icons[_hotbarSource.Position];
            _sourceIcon.Modulate = new Color(1f, 0.85f, 0.4f);
            _preview.Texture = _sourceIcon.IconTexture;
            _preview.GlobalPosition = GetGlobalMousePosition() + new Vector2(16, 16);
            _preview.Visible = !_nativeDrag;
            _feedback.GlobalPosition = _preview.GlobalPosition + new Vector2(0, 44);
            _feedback.Text = "Choose a hotbar position • Right-click to cancel";
            var hoveredIcon = GetViewport().GuiGetHoveredControl() as ClickableIcon;
            if (hoveredIcon != null)
            {
                var hotbar = hoveredIcon.Binding as HotbarCellBinding;
                var valid = hotbar != null && ValidateHotbar(hotbar) == HotbarResult.Success;
                _feedback.Text = valid ? hotbar!.Position == _hotbarSource.Position ? "Cancel placement" : "Move or swap hotbar entry"
                    : "Hotbar entries only fit hotbar positions";
                if (hoveredIcon != _sourceIcon)
                {
                    _hoverIcon = hoveredIcon;
                    hoveredIcon.Modulate = valid ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.6f, 0.4f);
                }
            }
            _feedback.Show();
            return;
        }
        if (_interaction?.Source == null || _preview == null || _feedback == null)
        {
            _preview?.Hide();
            _feedback?.Hide();
            return;
        }
        var source = _interaction.Source;
        foreach (var node in FindChildren("*", "Button", true, false))
            if (node is ClickableIcon cell && ItemPointerBinding.From(cell.Binding)?.Location == source.Location)
            {
                _sourceIcon = cell;
                cell.Modulate = new Color(1f, 0.85f, 0.4f);
                _preview.Texture = cell.IconTexture;
                break;
            }
        var hovered = GetViewport().GuiGetHoveredControl() as ClickableIcon;
        var target = hovered != null && IsAncestorOf(hovered) ? ItemPointerBinding.From(hovered.Binding) : null;
        var hotbarTarget = hovered?.Binding as HotbarCellBinding;
        var result = target == null ? _interaction.Feedback : _interaction.Validate(target);
        if (!_interaction.IsCarrying) { CancelInteraction(); return; }
        if (target != null && hovered != _sourceIcon)
        {
            _hoverIcon = hovered;
            hovered!.Modulate = result == TransferResult.Success ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.6f, 0.4f);
        }
        _preview.GlobalPosition = GetGlobalMousePosition() + new Vector2(16, 16);
        _feedback.GlobalPosition = _preview.GlobalPosition + new Vector2(0, 44);
        _feedback.Text = result switch
        {
            TransferResult.Success => "Place item",
            TransferResult.IncompatiblePlacement => "Item does not fit this slot",
            TransferResult.NoChange => target?.Location == source.Location ? "Cancel placement" : "Stack is full",
            null => "Choose a slot • Right-click to cancel",
            _ => "Placement unavailable"
        };
        if (hotbarTarget != null)
        {
            var valid = ValidateHotbar(hotbarTarget) == HotbarResult.Success;
            _hoverIcon = hovered;
            hovered!.Modulate = valid ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.6f, 0.4f);
            _feedback.Text = valid ? "Assign hotbar link" : "Item cannot be assigned here";
        }
        _preview.Visible = !_nativeDrag;
        _feedback.Show();
    }
}
