using System;
using Godot;
using sMPGWM.Scripts.CollisionSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class WeaponSystem : IDisposable
{
    public const int MaxShooters = 10;
    public const int MaxShotsPerTick = MaxShooters * 9;
    private readonly ShipController?[] _shooters = new ShipController?[MaxShooters];
    private readonly AcceptedShot[] _shots = new AcceptedShot[MaxShotsPerTick];
    private readonly ShipController?[] _targets = new ShipController?[MaxShotsPerTick];
    private readonly double[] _damage = new double[MaxShotsPerTick];
    private readonly HitscanQuery _query;
    // Omitted rules preserve friendly-fire-disabled behavior for direct callers.
    public WeaponSystem(bool friendlyFire = false) => _query = new(friendlyFire);
    public event Action<AcceptedShot>? ShotProduced;
    public void Dispose()
    {
        ShotProduced = null;
        Array.Clear(_shooters); Array.Clear(_shots); Array.Clear(_targets); Array.Clear(_damage);
        _query.Dispose();
    }

    public void Step(long tick, GameWorld world, Func<int, bool> ready)
    {
        var count = CollectShooters(tick, world, ready);
        var accepted = ResolveShots(tick, world, count);
        ApplyDamage(tick, accepted);
        RefreshShooters(count);
        PublishShots(accepted);
    }

    private int CollectShooters(long tick, GameWorld world, Func<int, bool> ready)
    {
        var count = 0;
        foreach (var ship in world.Entities.Values)
        {
            ship.ForceUpdateTransform();
            var controller = ship.Loadout.Weapons;
            controller.AdvanceReloads(tick);
            var owner = ship.ControllingPlayer;
            if (owner == null || !ready(owner.ControllingPeerId)) { controller.ClearInput(); continue; }
            if (count == MaxShooters) throw new InvalidOperationException("Weapon shooter capacity exceeded.");
            var index = count++;
            while (index > 0 && string.CompareOrdinal(_shooters[index - 1]!.EntityId, ship.EntityId) > 0)
            { _shooters[index] = _shooters[index - 1]; index--; }
            _shooters[index] = ship;
        }
        return count;
    }

    private int ResolveShots(long tick, GameWorld world, int count)
    {
        var accepted = 0;
        for (var i = 0; i < count; i++)
        {
            var ship = _shooters[i]!;
            var controller = ship.Loadout.Weapons;
            controller.SetInput(ship.InputState.FireMask, ship.InputState.WeaponConfigurationRevision);
            foreach (var binding in controller.Bindings)
            {
                if (binding == null || !controller.Available) continue;
                var group = controller.GetGroup(binding.Slot);
                if (group == 0 || (controller.HeldMask & (1 << (group - 1))) == 0) continue;
                var state = binding.State;
                var definition = binding.Definition;
                var stats = definition.Stats;
                var ammunition = definition.SupplyMode == WeaponSupplyMode.Ammunition;
                if (ammunition && state.Rounds == 0) { state.StartReload(tick, stats); continue; }
                if (state.Reloading || !ammunition && ship.Runtime.Energy.Current < stats.EnergyPerShot || !state.Ready(tick)) continue;
                var sequence = checked(state.ShotSequence + 1);
                var muzzle = (byte)(state.ShotSequence & 1);
                if (ship.Data.MuzzleOffsets.Count != 2) throw new InvalidOperationException("Firing ship needs two muzzle offsets.");
                var origin = ship.GlobalTransform * ship.Data.MuzzleOffsets[muzzle];
                var spread = Spread(binding.ItemId.Value, sequence) * stats.SpreadDegrees * Math.PI / 180d;
                var direction = ship.GlobalTransform.X.Normalized().Rotated((float)spread);
                var endpoint = origin + direction * (float)stats.Range;
                if (!origin.IsFinite() || !endpoint.IsFinite()) continue;
                ShipController? target;
                Vector2 end;
                Vector2 normal;
                bool blocked;
                try { target = _query.Cast(world, ship, origin, endpoint, out end, out normal, out blocked); }
                catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
                {
                    GD.PushError("Hitscan query failed: " + exception.Message);
                    continue;
                }
                if (!ammunition && !ship.Runtime.TrySpendEnergy(stats.EnergyPerShot)) continue;
                state.Accept(tick, stats, ammunition);
                var flags = blocked ? target == null ? ShotResultFlags.World : !target.Runtime.IsAlive ? ShotResultFlags.Wreck : ShotResultFlags.Miss : ShotResultFlags.Miss;
                if (target is { ShieldColliderActive: true }) flags |= ShotResultFlags.ShieldContact;
                _shots[accepted] = new(tick, binding.ItemId, sequence, ship.EntityId, ship.ControllingPlayer!.ControlGeneration,
                    definition.PresentationId, muzzle, origin, end, normal, flags, target?.EntityId ?? "");
                _targets[accepted] = target;
                _damage[accepted++] = stats.Damage;
                if (!controller.Available) controller.AdvanceReloads(tick);
            }
        }
        return accepted;
    }

    private void ApplyDamage(long tick, int accepted)
    {
        for (var i = 0; i < accepted; i++)
        {
            var shot = _shots[i];
            if (_targets[i] is { } target)
            {
                var result = target.Runtime.ApplyDamage(_damage[i]);
                var flags = shot.Flags;
                if (result.ShieldDamage > 0) flags |= ShotResultFlags.Shield;
                if (result.HealthDamage > 0) flags |= ShotResultFlags.Hull;
                if (!target.Runtime.IsAlive && result.ShieldDamage == 0 && result.HealthDamage == 0) flags |= ShotResultFlags.Wreck;
                shot = shot with { Flags = flags };
                if (!target.Runtime.IsAlive)
                {
                    target.ClearInput(); target.Velocity = Vector2.Zero;
                    target.Loadout.Weapons.ClearInput(); target.Loadout.Weapons.AdvanceReloads(tick);
                }
                target.RefreshMovementConfiguration();
                target.CaptureState();
            }
            _targets[i] = null;
            _shots[i] = shot;
        }
    }

    private void RefreshShooters(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _shooters[i]!.RefreshMovementConfiguration();
            _shooters[i] = null;
        }
    }

    private void PublishShots(int accepted)
    {
        for (var i = 0; i < accepted; i++) ShotProduced?.Invoke(_shots[i]);
    }

    public static double Spread(ulong item, ulong sequence)
    {
        var value = unchecked(item * 0x9E3779B97F4A7C15UL + sequence);
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;
        return (value >> 11) * (1d / 9007199254740992d) - .5d;
    }
}
