using System;
using Godot;
using sMPGWM.Scripts.Ui.MainMenu.Base;

namespace sMPGWM.Scripts.Provider;

public static class BaseSceneProvider
{
    private static readonly PackedScene MainGameScene =
        LoadScene("res://scenes/game/main_game.tscn");

    private static readonly PackedScene StartingScreenScene =
        LoadScene("res://scenes/starting_screen.tscn");

    public static PackedScene GetMainGameScene() => MainGameScene;

    public static PackedScene GetStartingScreen() => StartingScreenScene;

    public static TNode Instantiate<TNode>(PackedScene scene)
        where TNode : Node
    {
        ArgumentNullException.ThrowIfNull(scene);

        var node = scene.Instantiate<TNode>();

        return node ?? throw new InvalidOperationException(
            $"Could not instantiate scene as type: {typeof(TNode).Name}");
    }

    public static PackedScene LoadScene(string path)
    {
        var scene = GD.Load<PackedScene>(path);

        return scene ?? throw new InvalidOperationException(
            $"Could not load scene at path: {path}");
    }

    public static TControl LoadAndInstantiateControl<TControl>(string path)
        where TControl : BaseMenuScreen
    {
        return Instantiate<TControl>(LoadScene(path));
    }
}