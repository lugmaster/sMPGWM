using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

public sealed class CommandQueue
{
    private const int Capacity = 32;
    private readonly MovementCommand[] _pending = new MovementCommand[Capacity];
    private uint _occupied;
    private ShipInputState _last;
    private long _applied = -100;
    public long LatestArrival { get; private set; }

    public bool Accept(MovementCommand c, long completed)
    {
        if (!MovementWire.Valid(c.Input) || c.Tick <= completed || (ulong)c.Tick - (ulong)completed > Capacity) return false;
        var index = (int)(c.Tick & (Capacity - 1));
        var bit = 1u << index;
        if ((_occupied & bit) != 0)
        {
            var existing = _pending[index];
            if (existing.Tick == c.Tick) return existing.Input == c.Input;
            if (existing.Tick > completed) return false;
        }
        _pending[index] = c;
        _occupied |= bit;
        LatestArrival = completed;
        return true;
    }
    public ShipInputState Consume(long tick)
    {
        var index = (int)(tick & (Capacity - 1));
        var bit = 1u << index;
        if ((_occupied & bit) != 0 && _pending[index].Tick == tick)
        {
            _last = _pending[index].Input;
            _applied = tick;
            _occupied &= ~bit;
        }
        else
        {
            if (tick - _applied > 6) _last = new(Vector2.Zero, 0, false, false, false, _last.AutomaticBraking);
        }
        return _last;
    }
}
