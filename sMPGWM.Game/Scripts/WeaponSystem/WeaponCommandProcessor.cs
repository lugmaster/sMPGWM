using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class WeaponCommandProcessor
{
    private const int Capacity = 32;
    private readonly Queue<PendingCommand> _queue = new(Capacity);
    private readonly PendingCommand[] _completed = new PendingCommand[Capacity];
    private readonly WeaponCommandResult[] _results = new WeaponCommandResult[Capacity];
    private int _completedCount;

    public bool Enqueue(int peer, string entity, long generation, long revision, WeaponCommandKind kind,
        string slot, ItemEntityId item, byte group, Action<WeaponCommandResult> complete)
    {
        if (_queue.Count >= Capacity) return false;
        _queue.Enqueue(new(peer, entity, generation, revision, kind, slot, item, group, complete));
        return true;
    }

    public void Step(long tick, GameWorld world, Func<int, bool> ready)
    {
        if (_completedCount != 0) throw new InvalidOperationException("Weapon command results must be published before the next step.");
        var count = _queue.Count;
        for (var i = 0; i < count; i++)
        {
            var command = _queue.Dequeue();
            _completed[i] = command;
            _results[i] = Execute(command, tick, world, ready);
            _completedCount++;
        }
    }

    private static WeaponCommandResult Execute(PendingCommand command, long tick, GameWorld world, Func<int, bool> ready)
    {
        if (!ready(command.Peer) || !world.Players.TryGetValue(command.Peer, out var owner) ||
            owner.ControlGeneration != command.Generation || owner.ControlledShip is not { } ship ||
            ship.EntityId != command.Entity || !ship.Runtime.IsAlive ||
            ship.Loadout.Weapons.ConfigurationRevision != command.Revision)
            return WeaponCommandResult.Rejected;

        switch (command.Kind)
        {
            case WeaponCommandKind.Reload:
                ship.Loadout.Weapons.ReloadAll(tick);
                return WeaponCommandResult.Accepted;
            case WeaponCommandKind.AssignGroup:
                return ship.Loadout.Weapons.AssignGroup(ship.Equipment.FindSlot(command.Slot), command.Item,
                    command.Revision, command.Group) ? WeaponCommandResult.Accepted : WeaponCommandResult.Rejected;
            default:
                return WeaponCommandResult.Rejected;
        }
    }

    public void PublishResults()
    {
        for (var i = 0; i < _completedCount; i++)
        {
            var command = _completed[i];
            _completed[i] = default;
            command.Complete(_results[i]);
        }
        _completedCount = 0;
    }

    public void RemovePeer(int peer)
    {
        var count = _queue.Count;
        for (var i = 0; i < count; i++)
        {
            var command = _queue.Dequeue();
            if (command.Peer != peer) _queue.Enqueue(command);
        }
    }

    public void Clear()
    {
        _queue.Clear();
        Array.Clear(_completed);
        _completedCount = 0;
    }

    private readonly record struct PendingCommand(int Peer, string Entity, long Generation, long Revision,
        WeaponCommandKind Kind, string Slot, ItemEntityId Item, byte Group, Action<WeaponCommandResult> Complete);
}
