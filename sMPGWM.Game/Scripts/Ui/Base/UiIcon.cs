using Godot;
using sMPGWM.Scripts.Enums.Ui;

namespace sMPGWM.Scripts.Ui.Base;

[Tool]
public partial class UiIcon : Control
{
    private TextureRect? _textureRect;

    private Texture2D? _iconTexture;

    public IconSize Size { get; set; } = IconSize.Medium;

    public override void _Ready()
    {
        ApplySize();
        UpdateTexture();
    }
    
    private void UpdateTexture()
    {
        _textureRect ??= GetNodeOrNull<TextureRect>("IconTexture");

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