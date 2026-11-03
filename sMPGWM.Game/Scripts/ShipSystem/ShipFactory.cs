using System;
using System.Linq;
using Godot;
namespace sMPGWM.Scripts.ShipSystem;
public static class ShipFactory
{
    public static ShipController Create(ShipData data, string entityId, Vector2 position, float rotation, Vector2 velocity)
    {
        ShipController? ship = null;
        try
        {
            // TODO #ShipStory create a GamePlayProvider
            ship = ResourceLoader.Load<PackedScene>("res://scenes/game/ship.tscn").Instantiate<ShipController>();
            ship.EntityId = entityId; ship.Data = data;
            ship.Position = position; ship.Rotation = rotation; ship.Velocity = velocity;
            ship.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon = data.ColliderPoints.ToArray();
            ship.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture = ResourceLoader.Load<Texture2D>(data.TexturePath)
                ?? throw new InvalidOperationException("Texture unavailable: " + data.TexturePath);
            ship.SetLocalControl(false); ship.CaptureState();
            return ship;
        }
        catch (Exception e)
        {
            ship?.Free();
            throw new InvalidOperationException($"Constructing ship '{data.Name}' entity '{entityId}' failed: {e.Message}", e);
        }
    }
}
