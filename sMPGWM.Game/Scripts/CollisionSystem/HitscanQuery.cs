using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.CollisionSystem;

public sealed class HitscanQuery : System.IDisposable
{
    private readonly bool _friendlyFire;
    // Capture the rule once; direct callers default to ally pass-through.
    public HitscanQuery(bool friendlyFire = false) => _friendlyFire = friendlyFire;

    private readonly PhysicsRayQueryParameters2D _query = new()
    {
        CollisionMask = (1u << 0) | (1u << 1) | (1u << 3) | (1u << 4),
        CollideWithAreas = false, CollideWithBodies = true, HitFromInside = true
    };
    private readonly Godot.Collections.Array<Rid> _excluded = new();
    public void Dispose() { _query.Dispose(); _excluded.Clear(); }

    public ShipController? Cast(GameWorld world, ShipController shooter, Vector2 origin, Vector2 endpoint,
        out Vector2 end, out Vector2 normal, out bool blocked)
    {
        _excluded.Clear();
        foreach (var ship in world.Entities.Values)
            if (ship == shooter || !_friendlyFire && ship.Runtime.IsAlive && shooter.Runtime.IsAllyOf(ship.Runtime)) _excluded.Add(ship.GetRid());
        _query.From = origin; _query.To = endpoint; _query.Exclude = _excluded;
        var hit = shooter.GetWorld2D().DirectSpaceState.IntersectRay(_query);
        blocked = hit.Count != 0;
        end = blocked ? hit["position"].AsVector2() : endpoint;
        normal = blocked ? hit["normal"].AsVector2() : Vector2.Zero;
        return blocked ? hit["collider"].AsGodotObject() as ShipController : null;
    }
}
