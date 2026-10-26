#nullable enable

using Godot;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class ClickableIcon : Button
{
    internal object? Binding { get; set; }
    private Texture2D? _iconTexture;
    private IconSize _iconSize = IconSize.Medium;

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
    }

    private void ApplyTexture()
    {
        Icon = _iconTexture;
    }

    private void ApplySize()
    {
        var size = _iconSize.Vector();

        CustomMinimumSize = size;
    }
}
