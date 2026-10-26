using sMPGWM.Scripts.UiSystem.MessageStackSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

internal static class GameOverlayProvider
{
    private const string GameOverlayMenuPath = "res://scenes/ui/menu/game_overlay_menu/";
    private const string MainMenuPath = GameOverlayMenuPath + "main_menu.tscn";
    private const string InventoryMenuPath = GameOverlayMenuPath + "inventory_menu.tscn";
    private const string MapMenuPath = GameOverlayMenuPath + "map_menu.tscn";
    private const string MessageStackPath = GameOverlayMenuPath + "message_stack.tscn";
    private const string MessageEntryPath = GameOverlayMenuPath + "message_entry.tscn";

    public static GameOverlayMenu CreateMainMenu()
    {
        return SceneLoader.LoadAndInstantiate<GameOverlayMenu>(MainMenuPath);
    }

    public static GameOverlayMenu CreateInventoryMenu()
    {
        return SceneLoader.LoadAndInstantiate<GameOverlayMenu>(InventoryMenuPath);
    }

    public static GameOverlayMenu CreateMapMenu()
    {
        return SceneLoader.LoadAndInstantiate<GameOverlayMenu>(MapMenuPath);
    }

    public static MessageEntry CreateMessageStack()
    {
        return SceneLoader.LoadAndInstantiate<MessageEntry>(MessageEntryPath);
    }
}
