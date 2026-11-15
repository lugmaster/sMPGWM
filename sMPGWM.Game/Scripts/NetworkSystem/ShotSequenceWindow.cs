using System;

namespace sMPGWM.Scripts.NetworkSystem;

public sealed class ShotSequenceWindow
{
    public const int Capacity = 512;
    public const long RetentionTicks = 120;
    private readonly ulong[] _items = new ulong[Capacity];
    private readonly long[] _sequences = new long[Capacity];
    private readonly ulong[] _windows = new ulong[Capacity];
    private readonly long[] _seen = new long[Capacity];
    private Guid _session;
    private long _cutoff = -1;
    public long CapacityDrops { get; private set; }
    public int HighWater { get; private set; }

    public void Clear(Guid session, long cutoff)
    {
        _session = session; _cutoff = cutoff;
        Array.Clear(_items); Array.Clear(_sequences); Array.Clear(_windows); Array.Clear(_seen);
    }

    public bool Accept(Guid session, ulong item, long sequence, long tick, long now)
    {
        if (session != _session || item == 0 || sequence <= 0 || tick <= _cutoff || tick < now - RetentionTicks || tick > now + 32) return false;
        var free = -1;
        var active = 0;
        for (var i = 0; i < Capacity; i++)
        {
            if (_items[i] != 0 && _seen[i] < now - RetentionTicks) _items[i] = 0;
            if (_items[i] == item)
            {
                if (sequence > _sequences[i])
                {
                    var distance = sequence - _sequences[i];
                    _windows[i] = distance >= 64 ? 1 : (_windows[i] << (int)distance) | 1;
                    _sequences[i] = sequence;
                }
                else
                {
                    var distance = _sequences[i] - sequence;
                    if (distance >= 64 || (_windows[i] & (1UL << (int)distance)) != 0) return false;
                    _windows[i] |= 1UL << (int)distance;
                }
                _seen[i] = Math.Max(_seen[i], tick);
                return true;
            }
            if (_items[i] == 0) { if (free < 0) free = i; }
            else active++;
        }
        if (free < 0) { CapacityDrops++; return false; }
        _items[free] = item; _sequences[free] = sequence; _windows[free] = 1; _seen[free] = tick;
        HighWater = Math.Max(HighWater, active + 1);
        return true;
    }
}
