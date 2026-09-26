using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public sealed class IconDefinition
{
    public required Texture2D IconTexture { get; init; }

    public IconSize IconSize { get; init; } = IconSize.Medium;
}