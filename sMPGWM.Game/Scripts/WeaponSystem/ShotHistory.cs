using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class ShotHistory
{
    private readonly int _capacity;
    private readonly Dictionary<ItemEntityId, Entry> _entries;

    public ShotHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _entries = new(capacity);
    }

    public bool TryAccept(ItemEntityId item, ulong sequence, long tick)
    {
        if (item == default || sequence == 0) return false;
        if (_entries.TryGetValue(item, out var entry))
        {
            if (!entry.Window.TryAccept(sequence)) return false;
            entry.LastTick = Math.Max(entry.LastTick, tick);
            _entries[item] = entry;
            return true;
        }

        if (_entries.Count == _capacity)
        {
            var oldestItem = default(ItemEntityId);
            var oldestTick = long.MaxValue;
            foreach (var pair in _entries)
                if (oldestItem == default || pair.Value.LastTick < oldestTick)
                {
                    oldestItem = pair.Key;
                    oldestTick = pair.Value.LastTick;
                }
            // Retain recent weapons while allowing new items throughout a session.
            _entries.Remove(oldestItem);
        }

        entry.Window.TryAccept(sequence);
        entry.LastTick = tick;
        _entries.Add(item, entry);
        return true;
    }

    public void Clear() => _entries.Clear();

    private struct Entry
    {
        public ShotSequenceWindow Window;
        public long LastTick;
    }
}
