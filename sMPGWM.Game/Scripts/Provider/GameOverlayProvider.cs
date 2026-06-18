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
        return LoadAndInstantiate(MainMenuPath);
    }

    public static GameOverlayMenu CreateInventoryMenu()
    {
        return LoadAndInstantiate(InventoryMenuPath);
    }

    public static GameOverlayMenu CreateMapMenu()
    {
        return LoadAndInstantiate(MapMenuPath);
    }

    private static GameOverlayMenu LoadAndInstantiate(string path)
    {
        var scene = BaseSceneProvider.LoadScene(path);
        return BaseSceneProvider.Instantiate<GameOverlayMenu>(scene);
    }
}