using System;
using Godot;

namespace sMPGWM.Scripts.Util;

public static class SceneLoader
{
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

    public static TNode LoadAndInstantiate<TNode>(string path) where TNode : Node
    {
        var scene = LoadScene(path);
        return Instantiate<TNode>(scene);
    }
}