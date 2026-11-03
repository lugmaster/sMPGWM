using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class GamePlayProvider
{
    private const string ShipScenePath = "res://scenes/game/ship.tscn";

    public static ShipController CreateShip() =>
        SceneLoader.LoadAndInstantiate<ShipController>(ShipScenePath);
}
