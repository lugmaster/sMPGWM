using Godot;

namespace sMPGWM.Scripts.UtilitySystem.UiComponents;

public static class Cursors
{
    private static CursorTextureProvider? _provider;
    public static Texture2D RedTargetCursor => Provider.Red;
    public static Texture2D GreenTargetCursor => Provider.Green;
    public static Texture2D BlueTargetCursor => Provider.Blue;
    private static CursorTextureProvider Provider => _provider ?? throw new System.InvalidOperationException("Cursor textures are not initialized.");

    internal static void Initialize() => _provider ??= new("res://assets/ui/cursors/crosshair.png");
    internal static void Release()
    {
        _provider?.Dispose();
        _provider = null;
    }
}
