#nullable enable

using sMPGWM.Scripts.Util.UiComponents;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.EquipmentSystem;
using System;
using Godot;

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
    internal bool IsCarrying => _interaction?.IsCarrying == true;

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
    }

    private Variant BeginNativeDrag(ClickableIcon icon)
    {
        var source = ItemPointerBinding.From(icon.Binding);
        if (_interaction == null || _inventory == null || _equipment == null || source == null ||
            _pressedVersion != _mappingVersion || _pressedTarget?.Location != source.Location) return default;
        var token = _interaction.BeginDrag(source, _inventory.Revision, _equipment.Revision, _mappingVersion);
        if (token == Guid.Empty) return default;
        DismissTooltips();
        _pressedTarget = null;
        _consumeRelease = true;
        _nativeDrag = true;
        icon.SetDragPreview(new TextureRect { Texture = icon.IconTexture, MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(40, 40),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        return token.ToString();
    }

    private static Guid DragToken(Variant data) =>
        data.VariantType == Variant.Type.String && Guid.TryParse(data.AsString(), out var token) ? token : Guid.Empty;

    private bool CanDrop(ClickableIcon icon, Variant data)
    {
        var target = ItemPointerBinding.From(icon.Binding);
        return target != null && _interaction?.ValidateDrag(DragToken(data), target) == TransferResult.Success;
    }

    private void Drop(ClickableIcon icon, Variant data)
    {
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
        ClearHighlights();
        _preview?.Hide();
        _feedback?.Hide();
        if (_nativeDrag && !_dropping && IsInsideTree()) GetViewport().GuiCancelDrag();
    }

    private void ClearHighlights()
    {
        if (GodotObject.IsInstanceValid(_sourceIcon)) _sourceIcon!.Modulate = Colors.White;
        if (GodotObject.IsInstanceValid(_hoverIcon)) _hoverIcon!.Modulate = Colors.White;
        _sourceIcon = null;
        _hoverIcon = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (_interaction == null || _inventory == null || _equipment == null) return;
        if (@event is not InputEventMouseButton mouse) return;
        if (mouse.ButtonIndex == MouseButton.Right)
        {
            if (!_interaction.IsCarrying && !_consumeRelease) return;
            CancelInteraction();
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
        if (mouse.Pressed)
        {
            _consumeRelease = false;
            _pressedTarget = target;
            _pressedVersion = _mappingVersion;
            if (!_interaction.IsCarrying) return;
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
        _pressedTarget = null;
        if (_pressedVersion != _mappingVersion) { if (consume) GetViewport().SetInputAsHandled(); return; }
        if (_interaction.IsCarrying && (captured == null || target?.Location != captured.Location)) CancelInteraction();
        else if (captured != null && target?.Location == captured.Location)
        {
            DismissTooltips();
            _interaction.Click(captured, _inventory.Revision, _equipment.Revision, _mappingVersion);
        }
        if (consume) GetViewport().SetInputAsHandled();
        UpdatePointerVisuals();
    }

    public override void _Process(double delta) => UpdatePointerVisuals();

    private void UpdatePointerVisuals()
    {
        ClearHighlights();
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
        _preview.Visible = !_nativeDrag;
        _feedback.Show();
    }
}
