using sMPGWM.Scripts.Ui.Base;

namespace sMPGWM.Scripts.Provider;

public static class GameOverlayProvider
{
    private const string GameOverlayMenuPath = "res://scenes/ui/menu/game_overlay_menu/";
    private const string MainMenuPath = GameOverlayMenuPath + "main_menu.tscn";
    private const string InventoryMenuPath = GameOverlayMenuPath + "inventory_menu.tscn";
    private const string MapMenuPath = GameOverlayMenuPath + "map_menu.tscn";

    public static GameOverlayMenu CreateMainMenu()
    {
        return BaseSceneProvider.LoadAndInstantiate<GameOverlayMenu>(MainMenuPath);
    }

    public static GameOverlayMenu CreateInventoryMenu()
    {
        return BaseSceneProvider.LoadAndInstantiate<GameOverlayMenu>(InventoryMenuPath);
    }

    public static GameOverlayMenu CreateMapMenu()
    {
        return BaseSceneProvider.LoadAndInstantiate<GameOverlayMenu>(MapMenuPath);
    }
}