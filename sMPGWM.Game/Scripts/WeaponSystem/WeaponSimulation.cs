using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.WeaponSystem;

/// <summary>Runs after authoritative movement, never from prediction replay.</summary>
public sealed class WeaponSimulation : IDisposable
{
    private readonly GameWorld _world;
    private readonly PhysicsRayQueryParameters2D _query = new() { CollideWithBodies = true, CollideWithAreas = false, HitFromInside = true };
    private readonly Godot.Collections.Array<Rid> _excluded = new();
    private readonly ShipController?[] _shooters = new ShipController[10];
    public Func<string, int>? EntityIndex { get; set; }
    public Action<AcceptedShot>? Accepted { get; set; }
    public Func<Guid>? Session { get; set; }
    public long AcceptedCount { get; private set; }

    public WeaponSimulation(GameWorld world) => _world = world;

    public void Step(long tick, Func<int, bool> ready)
    {
        if (Accepted == null || EntityIndex == null || Session == null) return;
        var session = Session();
        if (session == Guid.Empty) return;
        var count = 0;
        foreach (var player in _world.Players.Values)
        {
            var ship = player.ControlledShip;
            if (ship == null || !ready(player.ControllingPeerId)) continue;
            var at = count++;
            while (at > 0 && string.CompareOrdinal(_shooters[at - 1]!.EntityId, ship.EntityId) > 0)
            { _shooters[at] = _shooters[at - 1]; at--; }
            _shooters[at] = ship;
        }
        for (var i = 0; i < count; i++) Fire(_shooters[i]!, tick, session);
        Array.Clear(_shooters);
    }

    private void Fire(ShipController ship, long tick, Guid session)
    {
        var weapons = ship.Weapons;
        weapons.Refresh();
        var input = ship.InputState;
        var enabled = ship.Runtime.IsAlive && ship.Runtime.PoweredSystemsAvailable &&
            input.WeaponRevision == ship.Equipment.Revision;
        _excluded.Clear();
        _excluded.Add(ship.GetRid());
        foreach (var other in _world.Entities.Values)
            if (other != ship && other.Runtime.IsAlive && other.Runtime.FactionId == ship.Runtime.FactionId)
                _excluded.Add(other.GetRid());
        _query.Exclude = _excluded;
        var fixtures = ship.Runtime.WeaponFixturePoints;
        for (var i = 0; i < weapons.Count; i++)
        {
            var binding = weapons.GetBinding(i);
            var state = binding.Item.WeaponState!;
            var eligible = enabled && (input.FireMask & binding.Groups) != 0 && fixtures.Count != 0;
            if (!eligible || tick + 1e-9 < state.NextEligibleTick || tick <= state.LastAcceptedTick)
            { state.TryAccept(tick, binding.Definition.AttackSpeed, eligible); continue; }
            var sequence = checked(state.Sequence + 1);
            var muzzle = (int)((sequence - 1) % fixtures.Count);
            var origin = ship.GlobalTransform * fixtures[muzzle];
            var direction = ship.GlobalTransform.X.Normalized().Rotated(Spread(binding.Item.Id.Value, sequence, binding.Definition.SpreadDegrees));
            _query.From = origin;
            _query.To = origin + direction * (float)binding.Definition.Range;
            var collision = ship.GetWorld2D().DirectSpaceState.IntersectRay(_query);
            var hit = collision.Count != 0;
            var endpoint = hit ? collision["position"].AsVector2() : _query.To;
            var normal = hit ? collision["normal"].AsVector2() : Vector2.Zero;
            if (hit && normal.IsZeroApprox()) normal = -direction;
            var shield = hit && collision["collider"].AsGodotObject() is ShipController target && target.Runtime.ShieldProtectionAvailable;
            if (!state.TryAccept(tick, binding.Definition.AttackSpeed, true)) continue;
            AcceptedCount++;
            Accepted!(new(session, tick, binding.Item.Id.Value, EntityIndex!(ship.EntityId),
                ship.ControllingPlayer!.ControlGeneration, sequence, binding.Profile, muzzle, origin, endpoint, normal, hit, shield));
        }
    }

    public static float Spread(ulong item, long sequence, double degrees)
    {
        // SplitMix64 is stable across hosts, runtimes and restores.
        var value = unchecked(item + (ulong)sequence * 0x9E3779B97F4A7C15UL);
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;
        return (float)(((value >> 11) * (1.0 / 9007199254740992.0) - .5) * degrees * Math.PI / 180);
    }

    public void Dispose()
    {
        Accepted = null; EntityIndex = null; Session = null;
        Array.Clear(_shooters);
        _excluded.Clear();
        _query.Dispose();
    }
}


