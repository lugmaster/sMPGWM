using System;
using Godot;
using sMPGWM.Scripts.UtilitySystem;

namespace sMPGWM.Scripts.GameFlowSystem;

public static class GameFlowSceneProvider
{
    public static PackedScene MainGameScene => SceneLoader.LoadScene("res://scenes/game/main_game.tscn");

    public static PackedScene StartMenuScene => SceneLoader.LoadScene("res://scenes/start_scene.tscn");
}
