using System;
using Godot;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.MovementSystem;

/// <summary>Bounded local command/state history and replay. Never samples input or sends RPCs.</summary>
public sealed class LocalMovementPrediction
{
    private const int Capacity = 128;
    private readonly (MovementCommand Command, MovementSample State)[] _history = new (MovementCommand, MovementSample)[Capacity];
    private long _oldest;
    private int _count;
    private MovementSample? _identity;
    public long Tick { get; private set; }
    public bool AtCapacity => _count == Capacity;
    public void Reset(long tick) { Tick = tick; Array.Clear(_history); _oldest = tick + 1; _count = 0; _identity = null; }
    public void ApplyBaseline(long tick, MovementSample state, ShipController ship)
    {
        Reset(tick); _identity = state;
        MovementStateAdapter.Restore(ship, state); ship.Presentation.Snap();
    }
    public void Step(int peer, PlayerSession owner)
    {
        if (AtCapacity) throw new InvalidOperationException("Prediction history capacity exceeded.");
        var ship = owner.ControlledShip!;
        var command = new MovementCommand(++Tick, ship.InputState);
        ship.Simulate(1.0 / 60);
        _history[(int)(Tick % Capacity)] = (command, MovementStateAdapter.Capture(peer, owner, Tick));
        if (_count++ == 0) _oldest = Tick;
    }
    public int CopyRecentCommands(Span<MovementCommand> destination)
    {
        var count = Math.Min(MovementWire.MaxCommands, _count);
        if (destination.Length < count) throw new ArgumentException("Command buffer is too small.", nameof(destination));
        for (var i = 0; i < count; i++)
            destination[i] = _history[(int)((Tick - count + 1 + i) % Capacity)].Command;
        return count;
    }
    public bool Reconcile(int peer, PlayerSession owner, long tick, MovementSample authoritative)
    {
        var controlled = owner.ControlledShip!;
        if (authoritative.ConfigurationRevision < controlled.MovementConfigurationRevision) return true;
        if (authoritative.ConfigurationRevision != controlled.MovementConfigurationRevision)
        {
            ApplyBaseline(tick, authoritative, controlled);
            return true;
        }
        if ((_identity is { } identity && (identity.Entity != authoritative.Entity ||
            identity.Generation != authoritative.Generation || identity.Revision != authoritative.Revision)) ||
            _count == 0 || tick < _oldest || tick > Tick || Tick - tick > 64) return false;
        var predicted = _history[(int)(tick % Capacity)];
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
                var saved = _history[(int)(t % Capacity)];
                ship.SetInput(saved.Command.Input); ship.Simulate(1.0 / 60);
                _history[(int)(t % Capacity)] = (saved.Command, MovementStateAdapter.Capture(peer, owner, t));
            }
            ship.Presentation.CorrectFrom(displayed, angle);
        }
        for (var acknowledged = _oldest; acknowledged <= tick; acknowledged++)
            _history[(int)(acknowledged % Capacity)] = default;
        _count -= (int)(tick - _oldest + 1);
        _oldest = tick + 1;
        return true;
    }
}
