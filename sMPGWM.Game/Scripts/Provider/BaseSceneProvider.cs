using System;
using Godot;

namespace sMPGWM.Scripts.Provider;

public static class BaseSceneProvider
{
    private static readonly PackedScene MainGameScene =
        LoadScene("res://scenes/game/main_game.tscn");

    private static readonly PackedScene StartMenuScene =
        LoadScene("res://scenes/ui/menu/start_menu/start_menu.tscn");

    public static PackedScene GetMainGameScene() => MainGameScene;

    public static PackedScene GetStartMenuScene() => StartMenuScene;

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
}