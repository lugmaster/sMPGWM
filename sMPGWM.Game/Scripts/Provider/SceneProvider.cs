using System;
using Godot;

namespace sMPGWM.Scripts.Provider;

public static class SceneProvider
{
    private const string MainMenuFolder = "res://scenes/ui/screens/main_menu/";

    private static readonly PackedScene StartingScreenScene =
        LoadScene("res://scenes/starting_screen.tscn");

    private static readonly PackedScene SettingsScreenScene =
        LoadScene(MainMenuFolder + "settings_screen.tscn");

    private static readonly PackedScene JoinScreenScene =
        LoadScene(MainMenuFolder + "join_screen.tscn");

    private static readonly PackedScene HostScreenScene =
        LoadScene(MainMenuFolder + "host_screen.tscn");

    private static readonly PackedScene MainGameScene =
        LoadScene("res://scenes/game/main_game.tscn");

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

    public static PackedScene GetMainGameScene() => MainGameScene;
    
    public static PackedScene GetStartingScreen() => StartingScreenScene;

    private static TNode Instantiate<TNode>(PackedScene scene)
        where TNode : Node
    {
        ArgumentNullException.ThrowIfNull(scene);

        var node = scene.Instantiate<TNode>();

        return node ?? throw new InvalidOperationException(
            $"Could not instantiate scene as type: {typeof(TNode).Name}");
    }

    private static PackedScene LoadScene(string path)
    {
        var scene = GD.Load<PackedScene>(path);

        return scene ?? throw new InvalidOperationException(
            $"Could not load scene at path: {path}");
    }
}