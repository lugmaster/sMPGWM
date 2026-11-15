using System;

namespace sMPGWM.Scripts.VfxSystem;

/// <summary>Fixed identity table. An entry is only recycled after its entire admissible event window expires.</summary>
public sealed class ShotSequenceWindow
{
    public const int Capacity = 512;
    public const int RetentionTicks = 180;
    private readonly ulong[] _items = new ulong[Capacity];
    private readonly long[] _sequences = new long[Capacity];
    private readonly ulong[] _windows = new ulong[Capacity];
    private readonly long[] _seenTicks = new long[Capacity];
    public long CapacityDrops { get; private set; }

    public bool Remember(ulong item, long sequence, long tick, double now)
    {
        if (item == 0 || sequence <= 0) return false;
        var replacement = -1;
        for (var i = 0; i < Capacity; i++)
        {
            if (_items[i] == item && _sequences[i] != 0)
            {
                if (sequence > _sequences[i])
                {
                    var delta = sequence - _sequences[i];
                    _windows[i] = delta >= 64 ? 1UL : (_windows[i] << (int)delta) | 1;
                    _sequences[i] = sequence;
                }
                else
                {
                    var delta = _sequences[i] - sequence;
                    if (delta >= 64 || (_windows[i] & (1UL << (int)delta)) != 0) return false;
                    _windows[i] |= 1UL << (int)delta;
                }
                _seenTicks[i] = Math.Max(_seenTicks[i], tick);
                return true;
            }
            if (replacement < 0 && (_sequences[i] == 0 || _seenTicks[i] < now - RetentionTicks)) replacement = i;
        }
        if (replacement < 0) { CapacityDrops++; return false; }
        _items[replacement] = item;
        _sequences[replacement] = sequence;
        _windows[replacement] = 1;
        _seenTicks[replacement] = tick;
        return true;
    }

    public void Clear()
    {
        Array.Clear(_items);
        Array.Clear(_sequences);
        Array.Clear(_windows);
        Array.Clear(_seenTicks);
        CapacityDrops = 0;
    }
}
