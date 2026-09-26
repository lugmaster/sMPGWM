using Godot;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.StartMenuSystem;

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
        return SceneLoader.Instantiate<Control>(SettingsMenuScene);
    }

    public static Control CreateJoinGameMenu()
    {
        return SceneLoader.Instantiate<Control>(JoinGameMenuScene);
    }

    public static Control CreateHostGameMenu()
    {
        return SceneLoader.Instantiate<Control>(HostGameMenuScene);
    }

    private static PackedScene LoadScene(string path)
    {
        return SceneLoader.LoadScene(path);
    }
}