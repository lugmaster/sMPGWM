using System;
using Godot;

namespace sMPGWM.Scripts.UtilitySystem.UiComponents;

public sealed class IconDefinition
{
    private readonly Texture2D? _texture;
    private readonly Func<IconDefinition>? _resolve;

    public IconDefinition() { }

    internal IconDefinition(Func<IconDefinition> resolve) => _resolve = resolve;

    public Texture2D IconTexture
    {
        get => _resolve != null ? _resolve().IconTexture : _texture!;
        init => _texture = value;
    }

    public IconSize IconSize { get; init; } = IconSize.Medium;
}
