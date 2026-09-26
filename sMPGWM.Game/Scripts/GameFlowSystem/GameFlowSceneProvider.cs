using System;
using Godot;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.GameFlowSystem;

public static class GameFlowSceneProvider
{
    private static readonly PackedScene MainGameScene = SceneLoader.LoadScene("res://scenes/game/main_game.tscn");

    private static readonly PackedScene StartMenuScene = SceneLoader.LoadScene("res://scenes/start_scene.tscn");

    public static PackedScene GetMainGameScene() => MainGameScene;

    public static PackedScene GetStartMenuScene() => StartMenuScene;
}
