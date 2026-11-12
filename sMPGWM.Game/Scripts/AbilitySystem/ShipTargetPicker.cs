using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Scripts.AbilitySystem;

internal static class ShipTargetPicker
{
    internal static ShipController? Pick(GameWorld world, Vector2 position)
    {
        ShipController? nearest = null;
        var distance = float.PositiveInfinity;
        foreach (var ship in world.Entities.Values)
        {
            if (!ship.Runtime.IsAlive || ship.GetNodeOrNull<CollisionPolygon2D>("CollisionPolygon2D") is not { Disabled: false } collision)
                continue;
            var visuals = ship.GetNode<Node2D>("ShipVisuals");
            var transform = visuals.GlobalTransform * ship.GlobalTransform.AffineInverse() * collision.GlobalTransform;
            var local = transform.AffineInverse() * position;
            var inside = Geometry2D.IsPointInPolygon(local, collision.Polygon);
            if (!inside) continue;
            var candidateDistance = position.DistanceSquaredTo(ship.DisplayedPosition);
            if (candidateDistance < distance || candidateDistance == distance && string.CompareOrdinal(ship.EntityId, nearest?.EntityId) < 0)
            {
                nearest = ship;
                distance = candidateDistance;
            }
        }
        return nearest;
    }
}
