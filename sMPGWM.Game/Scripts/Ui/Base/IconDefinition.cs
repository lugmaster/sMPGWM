using Godot;
using sMPGWM.Scripts.Enums.Ui;

namespace sMPGWM.Scripts.Ui.Base;

public sealed class IconDefinition
{
    public required Texture2D IconTexture { get; init; }

    public IconSize IconSize { get; init; } = IconSize.Medium;
}