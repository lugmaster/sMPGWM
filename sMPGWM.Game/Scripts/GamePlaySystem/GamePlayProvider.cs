using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class GamePlayProvider
{
    private const string ShipScenePath = "res://scenes/game/game_objects/ship.tscn";
    private const string BackgroundStarsTexturePath = "res://assets/world/backgrounds/pixel_stars.png";
    private const string BackgroundNebulaTexturePath = "res://assets/world/backgrounds/Blue_Nebula_1_1024x1024.png";

    public static ShipController CreateShip() =>
        SceneLoader.LoadAndInstantiate<ShipController>(ShipScenePath);

    public static Texture2D CreateSpaceBackgroundStars()
    {
       return LoadTexture(BackgroundStarsTexturePath);
    }
    public static Texture2D CreateSpaceBackgroundNebula()
    {
        return LoadTexture(BackgroundNebulaTexturePath);
    }

    private static Texture2D LoadTexture(string path)
    {
        return ResourceLoader.Load<Texture2D>(path)
               ?? throw new InvalidOperationException("Texture unavailable: " + path);
    }
}
