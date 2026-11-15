using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class ReloadCommandProcessor(GameWorld world)
{
    private readonly Queue<PendingReload> _pending = new(32);
    public Action<int, long, ReloadResult>? Completed { get; set; }
    public int Count => _pending.Count;

    public bool TryEnqueue(PendingReload command)
    {
        if (_pending.Count == 32 || !ValidBinding(command)) return false;
        _pending.Enqueue(command with { Items = (ulong[])command.Items.Clone() });
        return true;
    }

    public bool ValidBinding(PendingReload command)
    {
        if (command.Request <= 0 || command.Request == long.MaxValue || command.Items == null || command.Items.Length is < 1 or > 9 ||
            !world.Players.TryGetValue(command.Peer, out var owner) || owner.ControlGeneration != command.Generation ||
            owner.ControlledShip is not { } ship || ship.EntityId != command.Entity || ship.Equipment.Revision != command.EquipmentRevision)
            return false;
        var at = 0;
        foreach (var item in ship.Equipment.Snapshot.Slots)
        {
            if (at == command.Items.Length) break;
            if (item?.Id.Value != command.Items[at]) continue;
            if (item.Definition is not WeaponItemDefinition ||
                item.Definition.Id is not ("vulcan_mk1" or "laser_mk1") || item.WeaponState == null) return false;
            at++;
        }
        return at == command.Items.Length;
    }

    public void Step(long tick, Func<int, bool> ready)
    {
        while (_pending.Count != 0)
        {
            var command = _pending.Dequeue();
            var result = ReloadResult.InvalidBinding;
            if (ready(command.Peer) && ValidBinding(command))
            {
                var ship = world.Players[command.Peer].ControlledShip!;
                result = ReloadResult.Unavailable;
                if (ship.Runtime.IsAlive && ship.Runtime.PoweredSystemsAvailable &&
                    (ship.Runtime.Components.Presence & ShipComponentKind.Weapons) != 0 && CanStartBatch(ship, command.Items, tick))
                {
                    var at = 0;
                    foreach (var item in ship.Equipment.Snapshot.Slots)
                    {
                        if (at == command.Items.Length) break;
                        if (item?.Id.Value != command.Items[at]) continue;
                        item.WeaponState!.TryStartReload(tick);
                        at++;
                    }
                    result = ReloadResult.Applied;
                }
            }
            Completed?.Invoke(command.Peer, command.Request, result);
        }
    }

    private static bool CanStartBatch(ShipController ship, ulong[] items, long tick)
    {
        var at = 0;
        foreach (var item in ship.Equipment.Snapshot.Slots)
        {
            if (at == items.Length) break;
            if (item?.Id.Value != items[at]) continue;
            var state = item.WeaponState!;
            at++;
            if (state.Capacity == 0 || state.Rounds == state.Capacity || state.IsReloading) continue;
            if (tick < 0 || tick > SnapshotEnvelope.MaxTick - state.ReloadDurationTicks || state.Revision >= long.MaxValue - 1)
                return false;
        }
        return true;
    }

    public void RemovePeer(int peer)
    {
        var count = _pending.Count;
        for (var i = 0; i < count; i++)
        {
            var command = _pending.Dequeue();
            if (command.Peer == peer) Completed?.Invoke(peer, command.Request, ReloadResult.Cancelled);
            else _pending.Enqueue(command);
        }
    }

    public void Clear() { _pending.Clear(); Completed = null; }
}
