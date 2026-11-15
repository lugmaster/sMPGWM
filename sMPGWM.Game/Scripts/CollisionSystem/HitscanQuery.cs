using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.CollisionSystem;

public sealed class HitscanQuery : IDisposable
{
    // Shields currently protect the hull surface; no independent shield geometry is authored.
    private readonly PhysicsRayQueryParameters2D _query = new()
    {
        CollisionMask = CollisionLayers.Hitscan,
        CollideWithBodies = true, CollideWithAreas = false, HitFromInside = true
    };
    private readonly Godot.Collections.Array<Rid> _excluded = new();
    private readonly RayCast2D _ray = new()
    {
        Enabled = false, Visible = false, TopLevel = true, ExcludeParent = false,
        CollisionMask = CollisionLayers.Hitscan,
        CollideWithBodies = true, CollideWithAreas = false, HitFromInside = true
    };

    public void Prepare(GameWorld world, ShipController shooter)
    {
        if (_ray.GetParent() == null) world.AddChild(_ray);
        _ray.ClearExceptions();
        _excluded.Clear();
        _excluded.Add(shooter.GetRid());
        _ray.AddExceptionRid(shooter.GetRid());
        foreach (var other in world.Ships)
            if (other != shooter && other.Runtime.IsAlive && shooter.Runtime.IsAllyOf(other.Runtime))
            {
                _excluded.Add(other.GetRid());
                _ray.AddExceptionRid(other.GetRid());
            }
    }

    public HitscanContact Cast(ShipController shooter, Vector2 origin, Vector2 endpoint)
    {
        if (!origin.IsFinite() || !endpoint.IsFinite()) throw new ArgumentException("Invalid hitscan segment.");
        // RayCast2D substitutes a short vertical ray for a zero-length segment.
        if (origin == endpoint) return CastPoint(shooter, origin);
        _ray.GlobalTransform = new Transform2D(0, origin);
        _ray.TargetPosition = endpoint - origin;
        _ray.ForceRaycastUpdate();
        if (!_ray.IsColliding()) return new(false, endpoint, Vector2.Zero, null);
        var normal = _ray.GetCollisionNormal();
        if (normal.IsZeroApprox()) normal = (origin - endpoint).Normalized();
        return new(true, _ray.GetCollisionPoint(), normal, _ray.GetCollider() as ShipController);
    }

    private HitscanContact CastPoint(ShipController shooter, Vector2 origin)
    {
        _query.From = _query.To = origin;
        _query.Exclude = _excluded;
        using var hit = shooter.GetWorld2D().DirectSpaceState.IntersectRay(_query);
        return hit.Count == 0 ? new(false, origin, Vector2.Zero, null) :
            new(true, hit["position"].AsVector2(), hit["normal"].AsVector2(), hit["collider"].AsGodotObject() as ShipController);
    }

    public void Dispose()
    {
        _query.Dispose();
        _excluded.Clear();
        if (GodotObject.IsInstanceValid(_ray)) _ray.Free();
    }
}
