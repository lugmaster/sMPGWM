using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ResourcePool
{
    private readonly SortedDictionary<long, (double Flat, double Fraction)> _contributions = new();
    public double BaseCapacity { get; }
    public double Capacity { get; private set; }
    public double Current { get; private set; }

    public ResourcePool(double capacity, double current)
    {
        if (!double.IsFinite(capacity) || capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (!double.IsFinite(current) || current < 0 || current > capacity) throw new ArgumentOutOfRangeException(nameof(current));
        BaseCapacity = Capacity = capacity;
        Current = current;
    }

    public void SetContribution(long owner, double flat, double baselineFraction)
    {
        if (owner <= 0) throw new ArgumentOutOfRangeException(nameof(owner));
        if (!double.IsFinite(flat) || !double.IsFinite(baselineFraction)) throw new ArgumentOutOfRangeException(nameof(flat));
        var existed = _contributions.TryGetValue(owner, out var previous);
        _contributions[owner] = (flat, baselineFraction);
        try { Recompute(); }
        catch
        {
            if (existed) _contributions[owner] = previous;
            else _contributions.Remove(owner);
            throw;
        }
    }

    public bool RemoveOwner(long owner)
    {
        if (!_contributions.TryGetValue(owner, out var previous)) return false;
        _contributions.Remove(owner);
        try { Recompute(); }
        catch { _contributions.Add(owner, previous); throw; }
        return true;
    }

    public void Confirm(double capacity, double current)
    {
        if (!double.IsFinite(capacity) || !double.IsFinite(current) || current < 0 || capacity < current)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity; Current = current;
    }

    public void SetCurrent(double current)
    {
        if (!double.IsFinite(current) || current < 0 || current > Capacity)
            throw new ArgumentOutOfRangeException(nameof(current));
        Current = current;
    }

    private void Recompute()
    {
        var flat = 0d;
        var fraction = 0d;
        foreach (var contribution in _contributions.Values)
        {
            flat += contribution.Flat;
            fraction += contribution.Fraction;
        }
        var capacity = BaseCapacity + flat + BaseCapacity * fraction;
        if (!double.IsFinite(capacity)) throw new InvalidOperationException("Resource capacity overflow.");
        Capacity = Math.Max(0, capacity);
        Current = Math.Min(Current, Capacity);
    }
}
