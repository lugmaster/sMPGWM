using System;
using sMPGWM.Scripts.Items;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
namespace sMPGWM.Scripts.ShipSystem;
public static class ShipFactory
{
    public static ShipController Create(ShipData data, string entityId, Vector2 position, float rotation, Vector2 velocity)
        => Create(data, entityId, position, rotation, velocity, new ItemStore(), false);

    public static ShipController Create(ShipData data, string entityId, Vector2 position, float rotation, Vector2 velocity,
        ItemStore store, bool authoritative)
    {
        ShipController? ship = null;
        try
        {
            ship = GamePlayProvider.CreateShip();
            ship.EntityId = entityId; ship.Data = data;
            ship.ConfigureLoadout(store, authoritative);
            ship.Position = position; ship.Rotation = rotation; ship.Velocity = velocity;
            var collider = new Vector2[data.ColliderPoints.Count];
            for (var i = 0; i < collider.Length; i++) collider[i] = data.ColliderPoints[i];
            ship.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon = collider;
            ship.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture = ResourceLoader.Load<Texture2D>(data.TexturePath)
                ?? throw new InvalidOperationException("Texture unavailable: " + data.TexturePath);
            ship.SetLocalControl(false); ship.CaptureState();
            return ship;
        }
        catch (Exception e)
        {
            if (ship != null) { ship.Loadout.Dispose(); ship.Runtime.Dispose(); ship.Free(); }
            throw new InvalidOperationException($"Constructing ship '{data.Name}' entity '{entityId}' failed: {e.Message}", e);
        }
    }
}
