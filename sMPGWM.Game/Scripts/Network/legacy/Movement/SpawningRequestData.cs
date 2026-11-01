using FlatEarthUniverse.scripts.GameObjects.Ships;
using GCol = Godot.Collections;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public readonly struct SpawningRequestData(int peerId, ShipType shipType)
{
    public GCol.Array ToVariant()
    {
        var array = new GCol.Array
        {
            peerId,
            (int)shipType
        };
        return array;
    }

    public static SpawningRequestData FromVariant(GCol.Array variant)
    {
        return new SpawningRequestData(
            variant[0].AsInt32(),
            (ShipType)variant[1].AsInt32());
    }

    public int GetPeerId() => peerId;
    public ShipType GetShipType() => shipType;


    public override string ToString()
    {
        return
            $"SpawningRequestData: peerId = {peerId},  shipType = {shipType}";
    }
}

    
