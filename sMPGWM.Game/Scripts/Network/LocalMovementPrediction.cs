using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.World;

namespace sMPGWM.Scripts.Network;

/// <summary>Bounded local command/state history and replay. Never samples input or sends RPCs.</summary>
public sealed class LocalMovementPrediction
{
    private readonly SortedDictionary<long, (MovementCommand Command, MovementSample State)> _history = new();
    private MovementSample? _identity;
    public long Tick { get; private set; }
    public bool AtCapacity => _history.Count >= 128;
    public void Reset(long tick) { Tick = tick; _history.Clear(); _identity = null; }
    public void ApplyBaseline(long tick, MovementSample state, ShipController ship)
    {
        Reset(tick); _identity = state;
        MovementStateAdapter.Restore(ship, state); ship.Presentation.Snap();
    }
    public void Step(int peer, PlayerSession owner)
    {
        var ship = owner.ControlledShip!;
        var command = new MovementCommand(++Tick, ship.InputState);
        ship.Simulate(1.0 / 60);
        _history[Tick] = (command, MovementStateAdapter.Capture(peer, owner, Tick));
    }
    public MovementCommand[] RecentCommands() => _history.Values.TakeLast(6).Select(h => h.Command).ToArray();
    public bool Reconcile(int peer, PlayerSession owner, long tick, MovementSample authoritative)
    {
        if ((_identity is { } identity && (identity.Entity != authoritative.Entity ||
            identity.Generation != authoritative.Generation || identity.Revision != authoritative.Revision)) ||
            !_history.TryGetValue(tick, out var predicted) || Tick - tick > 64) return false;
        // Validate replay continuity before changing the collision body.
        for (long t = tick + 1; t <= Tick; t++) if (!_history.ContainsKey(t)) return false;
        _identity = authoritative;
        var state = predicted.State;
        bool differs = state.Position.DistanceTo(authoritative.Position) > .1f ||
            state.Velocity.DistanceTo(authoritative.Velocity) > .1f ||
            Math.Abs(Mathf.AngleDifference(state.Rotation, authoritative.Rotation)) > .001f ||
            state.Input != authoritative.Input || state.AppliedFlags != authoritative.AppliedFlags;
        if (differs)
        {
            var ship = owner.ControlledShip!;
            var displayed = ship.Presentation.Position;
            float angle = ship.Presentation.Rotation;
            MovementStateAdapter.Restore(ship, authoritative);
            for (long t = tick + 1; t <= Tick; t++)
            {
                var saved = _history[t];
                ship.SetInput(saved.Command.Input); ship.Simulate(1.0 / 60);
                _history[t] = (saved.Command, MovementStateAdapter.Capture(peer, owner, t));
            }
            ship.Presentation.CorrectFrom(displayed, angle);
        }
        foreach (long key in _history.Keys.TakeWhile(t => t <= tick).ToArray()) _history.Remove(key);
        return true;
    }
}
