using FlatEarthUniverse.scripts.GameObjects.Ships;
using Godot;
using GCol = Godot.Collections;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public readonly struct MpControllerSpawningData(int owningPeerId, Vector2 position, float rotation, Vector2 velocity, ShipType shipType)
{
    public GCol.Array ToVariant()
    {
        var array = new GCol.Array
        {
            owningPeerId,
            position,
            rotation,
            velocity,
            (int)shipType
        };
        return array;
    }

    public static MpControllerSpawningData FromVariant(GCol.Array variant)
    {
        return new MpControllerSpawningData(
            variant[0].AsInt32(), 
            variant[1].AsVector2(),
            variant[2].AsInt64(), 
            variant[3].AsVector2(),
            (ShipType)variant[4].AsInt32());
    }

    public static MpControllerSpawningData CreateDefault(int peerId, Vector2 position, ShipType shipType)
    {
        return new MpControllerSpawningData(
            peerId,
            position,
            0f,
            Vector2.Zero,
            shipType);
    }
    
    public int GetPeerId() => owningPeerId;
    public Vector2 GetPosition() => position;
    public float GetRotation() => rotation;
    public Vector2 GetVelocity() => velocity;
    public ShipType GetShipType() => shipType;


    public override string ToString()
    {
        return $"MpControllerSpawningData: owningPeerId = {owningPeerId}, position = {position}, rotation = {rotation}, velocity = {velocity},  shipType = {shipType}";
    }
}