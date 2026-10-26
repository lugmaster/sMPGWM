#nullable enable

using Godot;
using System;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class ClickableIcon : Button
{
    internal object? Binding { get; set; }
    internal Color DisplayModulate { get; set; } = Colors.White;
    internal event Action<ClickableIcon, InputEvent>? InputReceived;
    internal event Action<ClickableIcon, bool>? HoverChanged;
    internal event Action<ClickableIcon>? Exiting;
    internal Func<ClickableIcon, Variant>? DragRequested { get; set; }
    internal Func<ClickableIcon, Variant, bool>? DropValidated { get; set; }
    internal Action<ClickableIcon, Variant>? DropRequested { get; set; }

    public override void _GuiInput(InputEvent @event) => InputReceived?.Invoke(this, @event);
    public override Variant _GetDragData(Vector2 atPosition) => DragRequested?.Invoke(this) ?? default;
    public override bool _CanDropData(Vector2 atPosition, Variant data) => DropValidated?.Invoke(this, data) == true;
    public override void _DropData(Vector2 atPosition, Variant data) => DropRequested?.Invoke(this, data);
    public override void _ExitTree() => Exiting?.Invoke(this);
    private Texture2D? _iconTexture;
    private IconSize _iconSize = IconSize.Medium;
    private IconDisplayData _display = new();
    private Label? _tooltipLabel;

    public void SetDisplay(Texture2D? texture, IconDisplayData display)
    {
        DismissTooltip();
        _display = display;
        IconTexture = texture;
        TooltipText = display.Tooltip;
        if (IsNodeReady()) ApplyDisplay();
    }

    internal void DismissTooltip()
    {
        TooltipText = "";
        if (GodotObject.IsInstanceValid(_tooltipLabel) && _tooltipLabel!.IsInsideTree())
            _tooltipLabel.GetWindow().Hide();
    }

    public override GodotObject _MakeCustomTooltip(string forText)
    {
        if (string.IsNullOrEmpty(forText)) return null!;
        _tooltipLabel = new Label
        {
            Text = forText,
            ThemeTypeVariation = "TooltipLabel",
            MouseFilter = MouseFilterEnum.Ignore,
            FocusMode = FocusModeEnum.None
        };
        return _tooltipLabel;
    }

    private void ApplyDisplay()
    {
        var quantity = GetNode<Label>("QuantityLabel");
        quantity.Text = _display.Quantity.HasValue ? IconDisplayData.FormatQuantity(_display.Quantity.Value) : "";
        quantity.Visible = _display.Quantity.HasValue;
        quantity.MouseFilter = MouseFilterEnum.Ignore;
        quantity.FocusMode = FocusModeEnum.None;
        TooltipText = _display.Tooltip;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
            DismissTooltip();
        if (what == NotificationMouseEnter)
        {
            TooltipText = _display.Tooltip;
            HoverChanged?.Invoke(this, true);
        }
        else if (what == NotificationMouseExit) HoverChanged?.Invoke(this, false);
    }

    public Texture2D? IconTexture
    {
        get => _iconTexture;
        set
        {
            _iconTexture = value;

            if (IsNodeReady())
                ApplyTexture();
        }
    }

    [Export]
    public IconSize IconSize
    {
        get => _iconSize;
        set
        {
            _iconSize = value;

            if (IsNodeReady())
                ApplySize();
        }
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(ClickableIcon)}.{nameof(_Ready)} called.");
        if (_iconTexture == null && Icon != null)
            _iconTexture = Icon;

        ApplySize();
        ApplyTexture();
        ApplyDisplay();
    }

    private void ApplyTexture()
    {
        Icon = _iconTexture;
    }

    private void ApplySize()
    {
        ExpandIcon = true;
        CustomMinimumSize = _iconSize.Vector() + GetThemeStylebox("normal").GetMinimumSize();
    }
}
