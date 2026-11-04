using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

public sealed class CommandQueue
{
    private readonly SortedDictionary<long, ShipInputState> _pending = new();
    private ShipInputState _last;
    private long _applied = -100;
    public long LatestArrival { get; private set; }

    public bool Accept(MovementCommand c, long completed)
    {
        if (!MovementWire.Valid(c.Input) || c.Tick <= completed || c.Tick > completed + 32) return false;
        if (_pending.TryGetValue(c.Tick, out var existing)) return existing == c.Input;
        if (_pending.Count >= 32) return false;
        _pending.Add(c.Tick, c.Input); LatestArrival = completed;
        return true;
    }
    public ShipInputState Consume(long tick)
    {
        if (_pending.Remove(tick, out var input)) { _last = input; _applied = tick; }
        else
        {
            if (tick - _applied > 6) _last = new(Vector2.Zero, 0, false, false, false, _last.AutomaticBraking);
        }
        return _last;
    }
}
