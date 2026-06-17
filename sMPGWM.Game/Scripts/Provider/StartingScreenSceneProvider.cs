using System;
using Godot;

namespace sMPGWM.Scripts.Provider;

public static class StartingScreenSceneProvider
{
    private const string MainMenuFolder = "res://scenes/ui/screens/main_menu/";

    private static readonly PackedScene SettingsScreenScene =
        LoadScene(MainMenuFolder + "settings_screen.tscn");

    private static readonly PackedScene JoinScreenScene =
        LoadScene(MainMenuFolder + "join_screen.tscn");

    private static readonly PackedScene HostScreenScene =
        LoadScene(MainMenuFolder + "host_screen.tscn");

    

    public static Control CreateSettingsScreen()
    {
        return Instantiate<Control>(SettingsScreenScene);
    }

    public static Control CreateJoinScreen()
    {
        return Instantiate<Control>(JoinScreenScene);
    }

    public static Control CreateHostScreen()
    {
        return Instantiate<Control>(HostScreenScene);
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