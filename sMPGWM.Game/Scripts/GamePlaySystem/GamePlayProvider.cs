using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class GamePlayProvider
{
    private const string ShipScenePath = "res://scenes/game/game_objects/ship.tscn";

    public static ShipController CreateShip() =>
        SceneLoader.LoadAndInstantiate<ShipController>(ShipScenePath);

    public static Texture2D CreateSpaceBackgroundStars()
    {
        // TODO return Texture2D for background form sMPGWM.Game/assets/world/backgrounds/pixel_stars.png
        return null;
    }
    public static Texture2D CreateSpaceBackgroundNebulah()
    {
        // TODO return Texture2D for background form sMPGWM.Game/assets/world/backgrounds/Blue_Nebula_1_1024x1024.png
        return null;
    }
    
        
}
