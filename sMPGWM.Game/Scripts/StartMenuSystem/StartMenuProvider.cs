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
    
    private static readonly PackedScene JoinLanGameMenuScene =
        LoadScene(MainMenuFolder + "join_lan_game_menu.tscn");
    
    private static readonly PackedScene JoinSteamGameMenuScene =
        LoadScene(MainMenuFolder + "join_steam_game_menu.tscn");

    private static readonly PackedScene HostGameMenuScene =
        LoadScene(MainMenuFolder + "host_game_menu.tscn");
    
    private static readonly PackedScene HostLanGameMenuScene =
        LoadScene(MainMenuFolder + "host_lan_game_menu.tscn");
    
    private static readonly PackedScene HostSteamGameMenuScene =
        LoadScene(MainMenuFolder + "host_steam_game_menu.tscn");

    

    public static Control CreateSettingsMenu()
    {
        return SceneLoader.Instantiate<Control>(SettingsMenuScene);
    }

    public static Control CreateJoinGameMenu()
    {
        return SceneLoader.Instantiate<Control>(JoinGameMenuScene);
    }
    
    public static Control CreateJoinLanGameMenu()
    {
        return SceneLoader.Instantiate<Control>(JoinLanGameMenuScene);
    }
    
    public static Control CreateJoinSteamGameMenu()
    {
        return SceneLoader.Instantiate<Control>(JoinSteamGameMenuScene);
    }

    public static Control CreateHostGameMenu()
    {
        return SceneLoader.Instantiate<Control>(HostGameMenuScene);
    }
    
    public static Control CreateHostLanGameMenu()
    {
        return SceneLoader.Instantiate<Control>(HostLanGameMenuScene);
    }
    
    public static Control CreateHostSteamGameMenu()
    {
        return SceneLoader.Instantiate<Control>(HostSteamGameMenuScene);
    }

    private static PackedScene LoadScene(string path)
    {
        return SceneLoader.LoadScene(path);
    }
}