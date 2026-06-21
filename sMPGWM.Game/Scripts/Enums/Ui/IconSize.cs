using System;
using Godot;

namespace sMPGWM.Scripts.Enums.Ui;

public enum IconSize
{
    Small,
    Medium,
    Large
}

public static class IconSizeExtensions
{
    public static int Pixels(this IconSize size)
    {
        return size switch
        {
            IconSize.Small => 32,
            IconSize.Medium => 48,
            IconSize.Large => 64,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    public static Vector2 Vector(this IconSize size)
    {
        var pixels = size.Pixels();
        return new Vector2(pixels, pixels);
    }
}