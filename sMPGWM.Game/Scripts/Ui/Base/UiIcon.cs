using Godot;
using sMPGWM.Scripts.Enums.Ui;
using Logger = sMPGWM.Scripts.Singletons.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Base;

[Tool]
public partial class UiIcon : Control
{
    private TextureRect? _textureRect;

    private Texture2D? _iconTexture;

    public IconSize Size { get; set; } = IconSize.Medium;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(UiIcon)}.{nameof(_EnterTree)} called.");
        _textureRect = GetNodeOrNull<TextureRect>("IconTexture");
        if (_textureRect == null)
            Logger.Warning($"{nameof(UiIcon)}.{nameof(_EnterTree)} could not find optional node {nameof(TextureRect)} IconTexture.");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(UiIcon)}.{nameof(_Ready)} called.");
        ApplySize();
        UpdateTexture();
    }
    
    private void UpdateTexture()
    {
        if (_textureRect == null)
            return;

        _textureRect.Texture = _iconTexture;
    }

    protected void ApplySize()
    {
        var size = Size.Vector();
        CustomMinimumSize = size;
    }
    
    
}
