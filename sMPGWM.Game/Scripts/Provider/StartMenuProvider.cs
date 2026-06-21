using Godot;

namespace sMPGWM.Scripts.Provider;

public static class StartMenuProvider
{
    private const string MainMenuFolder = "res://scenes/ui/menu/start_menu/";

    private static readonly PackedScene SettingsMenuScene =
        LoadScene(MainMenuFolder + "settings_menu.tscn");

    private static readonly PackedScene JoinGameMenuScene =
        LoadScene(MainMenuFolder + "join_game_menu.tscn");

    private static readonly PackedScene HostGameMenuScene =
        LoadScene(MainMenuFolder + "host_game_menu.tscn");

    

    public static Control CreateSettingsMenu()
    {
        return BaseSceneProvider.Instantiate<Control>(SettingsMenuScene);
    }

    public static Control CreateJoinGameMenu()
    {
        return BaseSceneProvider.Instantiate<Control>(JoinGameMenuScene);
    }

    public static Control CreateHostGameMenu()
    {
        return BaseSceneProvider.Instantiate<Control>(HostGameMenuScene);
    }

    private static PackedScene LoadScene(string path)
    {
        return BaseSceneProvider.LoadScene(path);
    }
}