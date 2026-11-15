using System;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;
using sMPGWM.Scripts.CollisionSystem;

namespace sMPGWM.Scripts.WeaponSystem;

/// <summary>Runs after authoritative movement, never from prediction replay.</summary>
public sealed class WeaponSimulation : IDisposable
{
    private readonly GameWorld _world;
    private readonly HitscanQuery _query = new();
    private readonly ShipController?[] _shooters = new ShipController[10];
    private readonly PendingHit[] _hits = new PendingHit[90];
    private int _hitCount;
    public ReloadCommandProcessor ReloadCommands { get; }
    public Func<string, int>? EntityIndex { get; set; }
    public Action<AcceptedShot>? Accepted { get; set; }
    public Func<Guid>? Session { get; set; }
    public long AcceptedCount { get; private set; }

    public WeaponSimulation(GameWorld world)
    {
        _world = world;
        ReloadCommands = new(world);
    }

    public void MaintainReloads(long tick, Func<int, bool> ready)
    {
        foreach (var ship in _world.Entities.Values)
        {
            ship.Weapons.Refresh();
            CancelUnavailableReloads(ship);
        }
        ReloadCommands.Step(tick, ready);
        foreach (var ship in _world.Entities.Values)
        {
            if (!ship.Runtime.WeaponsAvailable) continue;
            for (var i = 0; i < ship.Weapons.Count; i++)
                ship.Weapons.GetBinding(i).Item.WeaponState!.CompleteReload(tick);
        }
    }

    private static void CancelUnavailableReloads(ShipController ship)
    {
        if (ship.Runtime.WeaponsAvailable) return;
        for (var i = 0; i < ship.Weapons.Count; i++)
            ship.Weapons.GetBinding(i).Item.WeaponState!.CancelReload();
    }

    public void Step(long tick, Func<int, bool> ready)
    {
        if (EntityIndex == null || Session == null) return;
        var session = Session();
        if (session == Guid.Empty) return;
        var count = 0;
        foreach (var entity in _world.Entities.Values) entity.ForceUpdateTransform();
        foreach (var player in _world.Players.Values)
        {
            var ship = player.ControlledShip;
            if (ship == null || !ready(player.ControllingPeerId)) continue;
            if (count == _shooters.Length) throw new InvalidOperationException("Weapon shooter capacity exceeded.");
            var at = count++;
            while (at > 0 && string.CompareOrdinal(_shooters[at - 1]!.EntityId, ship.EntityId) > 0)
            { _shooters[at] = _shooters[at - 1]; at--; }
            _shooters[at] = ship;
        }
        _hitCount = 0;
        for (var i = 0; i < count; i++) Fire(_shooters[i]!, tick, session);
        // Admit every shot before damage so lethal hits cannot suppress same-tick trades.
        for (var i = 0; i < _hitCount; i++)
        {
            var pending = _hits[i];
            var damage = pending.Target?.Runtime.ResolveDamage(pending.Damage) ?? default;
            _hits[i] = pending with { Shot = pending.Shot with { ShieldContact = damage.ShieldContact } };
        }
        for (var i = 0; i < _hitCount; i++) Accepted?.Invoke(_hits[i].Shot);
        Array.Clear(_hits, 0, _hitCount);
        Array.Clear(_shooters);
    }

    private void Fire(ShipController ship, long tick, Guid session)
    {
        var weapons = ship.Weapons;
        weapons.Refresh();
        var input = ship.InputState;
        var enabled = input.WeaponRevision == ship.Equipment.Revision;
        _query.Prepare(_world, ship);
        var fixtures = ship.Runtime.WeaponFixturePoints;
        for (var i = 0; i < weapons.Count; i++)
        {
            var binding = weapons.GetBinding(i);
            var state = binding.Item.WeaponState!;
            var eligible = enabled && ship.Runtime.WeaponsAvailable &&
                (input.FireMask & binding.Groups) != 0 && fixtures.Count != 0;
            if (eligible && binding.Definition.Supply is AmmunitionSupplyDefinition && state.Rounds == 0)
                state.TryStartReload(tick);
            if (!eligible || !state.CanAccept(tick, binding.Definition.AttackSpeed))
            { state.TryAccept(tick, binding.Definition.AttackSpeed, eligible); continue; }
            state.ValidateAcceptance(tick, binding.Definition.AttackSpeed);
            if (binding.Definition.Supply is EnergySupplyDefinition energy && ship.Runtime.Energy.Current < energy.EnergyPerShot)
            { state.TryAccept(tick, binding.Definition.AttackSpeed, false); continue; }
            var sequence = checked(state.Sequence + 1);
            var muzzle = (int)((sequence - 1) % fixtures.Count);
            var origin = ship.GlobalTransform * fixtures[muzzle];
            var direction = ship.GlobalTransform.X.Normalized().Rotated(Spread(binding.Item.Id.Value, sequence, binding.Definition.SpreadDegrees));
            var contact = _query.Cast(ship, origin, origin + direction * (float)binding.Definition.Range);
            var target = contact.Target;
            if (binding.Definition.Supply is EnergySupplyDefinition payment && !ship.Runtime.TrySpendEnergy(payment.EnergyPerShot)) continue;
            if (!state.TryAccept(tick, binding.Definition.AttackSpeed, true)) continue;
            CancelUnavailableReloads(ship);
            ship.RefreshMovementConfiguration();
            AcceptedCount++;
            _hits[_hitCount++] = new(new(session, tick, binding.Item.Id.Value, EntityIndex!(ship.EntityId),
                ship.ControllingPlayer!.ControlGeneration, sequence, binding.Profile, muzzle, origin, contact.Position, contact.Normal, contact.Hit, false),
                target != null && !ship.Runtime.IsAllyOf(target.Runtime) ? target : null, binding.Definition.Damage);
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
        Array.Clear(_hits);
        ReloadCommands.Clear();
        _query.Dispose();
    }

    private readonly record struct PendingHit(AcceptedShot Shot, ShipController? Target, double Damage);
}


