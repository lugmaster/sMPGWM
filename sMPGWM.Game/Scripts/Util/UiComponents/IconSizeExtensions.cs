using System;
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public static class IconSizeExtensions
{
    public static int Pixels(this IconSize size)
    {
        return size switch
        {
            IconSize.Small => 24,
            IconSize.Medium => 40,
            IconSize.Large => 56,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    public static Vector2 Vector(this IconSize size)
    {
        var pixels = size.Pixels();
        return new Vector2(pixels, pixels);
    }
}
