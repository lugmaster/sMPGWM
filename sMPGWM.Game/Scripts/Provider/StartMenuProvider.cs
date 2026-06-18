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
        return Instantiate<Control>(SettingsMenuScene);
    }

    public static Control CreateJoinGameMenu()
    {
        return Instantiate<Control>(JoinGameMenuScene);
    }

    public static Control CreateHostGameMenu()
    {
        return Instantiate<Control>(HostGameMenuScene);
    }

    private static TNode Instantiate<TNode>(PackedScene scene)
        where TNode : Node
    {
        return BaseSceneProvider.Instantiate<TNode>(scene);
    }

    private static PackedScene LoadScene(string path)
    {
        return BaseSceneProvider.LoadScene(path);
    }
}