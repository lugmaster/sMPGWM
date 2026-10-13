using System;
using Godot;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.GameFlowSystem;

public static class GameFlowSceneProvider
{
    public static readonly PackedScene MainGameScene = SceneLoader.LoadScene("res://scenes/game/main_game.tscn");

    public static readonly PackedScene StartMenuScene = SceneLoader.LoadScene("res://scenes/start_scene.tscn");
}
