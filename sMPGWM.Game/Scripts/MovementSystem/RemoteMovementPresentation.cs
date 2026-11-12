using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.MovementSystem;

public sealed class RemoteMovementPresentation
{
    private readonly Dictionary<int, List<(long Tick, MovementSample State)>> _buffers = new();
    private double _cursor;
    public void Reset(long tick) { _buffers.Clear(); _cursor = tick; }
    public void Remove(int peer) => _buffers.Remove(peer);
    public void Apply(int peer, ShipController ship, long tick, MovementSample sample, bool baseline)
    {
        var oldPosition = ship.Presentation.Position;
        float oldAngle = ship.Presentation.Rotation;
        MovementStateAdapter.Restore(ship, sample);
        if (!_buffers.TryGetValue(peer, out var buffer)) _buffers[peer] = buffer = new();
        bool reset = baseline || buffer.Count == 0 || buffer[^1].State.Revision != sample.Revision ||
            buffer[^1].State.Generation != sample.Generation || buffer[^1].State.Entity != sample.Entity;
        if (reset) buffer.Clear();
        buffer.Add((tick, sample));
        if (buffer.Count > 64) buffer.RemoveAt(0);
        if (reset) ship.Presentation.Snap();
        else ship.Presentation.Present(oldPosition, oldAngle);
    }
    public void Render(double delta, double target, IReadOnlyDictionary<int, PlayerSession> owners)
    {
        _cursor += Math.Clamp(target - _cursor, 0, delta * 66);
        foreach (var (peer, buffer) in _buffers)
        {
            if (buffer.Count == 0 || !owners.TryGetValue(peer, out var owner) || owner.ControlledShip == null) continue;
            while (buffer.Count > 2 && buffer[1].Tick <= _cursor) buffer.RemoveAt(0);
            var lower = buffer[0]; var upper = buffer.Count > 1 ? buffer[1] : lower;
            var presented = MovementPresentation.Sample(lower.Tick, lower.State, upper.Tick, upper.State, _cursor);
            owner.ControlledShip.Presentation.MovementFlags = presented.AppliedFlags;
            owner.ControlledShip.Presentation.Present(presented.Position, presented.Rotation);
        }
    }
}
