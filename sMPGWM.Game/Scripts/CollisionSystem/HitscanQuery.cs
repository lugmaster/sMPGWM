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

    public void Prepare(GameWorld world, ShipController shooter)
    {
        _excluded.Clear();
        _excluded.Add(shooter.GetRid());
        foreach (var other in world.Ships)
            if (other != shooter && other.Runtime.IsAlive && shooter.Runtime.IsAllyOf(other.Runtime))
                _excluded.Add(other.GetRid());
        _query.Exclude = _excluded;
    }

    public HitscanContact Cast(ShipController shooter, Vector2 origin, Vector2 endpoint)
    {
        if (!origin.IsFinite() || !endpoint.IsFinite()) throw new ArgumentException("Invalid hitscan segment.");
        _query.From = origin;
        _query.To = endpoint;
        using var hit = shooter.GetWorld2D().DirectSpaceState.IntersectRay(_query);
        if (hit.Count == 0) return new(false, endpoint, Vector2.Zero, null);
        var normal = hit["normal"].AsVector2();
        if (normal.IsZeroApprox()) normal = (origin - endpoint).Normalized();
        return new(true, hit["position"].AsVector2(), normal, hit["collider"].AsGodotObject() as ShipController);
    }

    public void Dispose()
    {
        _query.Dispose();
        _excluded.Clear();
    }
}
