using System;
using System.Collections.Generic;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ResourcePool
{
    private readonly SortedDictionary<long, (double Flat, double Fraction)> _contributions = new();
    public double BaseCapacity { get; private set; }
    public double BaselineFlat { get; private set; }
    public bool Installed { get; private set; } = true;
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

    public void SetBaseline(double capacity, bool installed = true, double flat = 0)
    {
        if (!double.IsFinite(capacity) || capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (!double.IsFinite(flat)) throw new ArgumentOutOfRangeException(nameof(flat));
        var previous = BaseCapacity;
        var previousFlat = BaselineFlat;
        var wasInstalled = Installed;
        BaseCapacity = capacity;
        BaselineFlat = flat;
        Installed = installed;
        try { Recompute(); }
        catch { BaseCapacity = previous; BaselineFlat = previousFlat; Installed = wasInstalled; throw; }
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

    public void Confirm(double baseline, double capacity, double current, bool installed, double flat = 0)
    {
        if (!double.IsFinite(flat) || !double.IsFinite(baseline) || baseline < 0 || !installed && (baseline != 0 || flat != 0 || capacity != 0 || current != 0))
            throw new ArgumentOutOfRangeException(nameof(baseline));
        Confirm(capacity, current);
        BaseCapacity = baseline;
        BaselineFlat = flat;
        Installed = installed;
    }

    public void SetCurrent(double current)
    {
        if (!double.IsFinite(current) || current < 0 || current > Capacity)
            throw new ArgumentOutOfRangeException(nameof(current));
        Current = current;
    }

    private void Recompute()
    {
        Capacity = PreviewCapacity(BaseCapacity, Installed, BaselineFlat);
        Current = Math.Min(Current, Capacity);
    }

    public double PreviewCapacity(double baseline, bool installed, double baselineFlat = 0)
    {
        if (!double.IsFinite(baseline) || baseline < 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        if (!double.IsFinite(baselineFlat)) throw new ArgumentOutOfRangeException(nameof(baselineFlat));
        if (!installed) return 0;
        if (_contributions.Count == 0) return StatValueCalculator.Calculate(baseline, baselineFlat, 0);
        var flat = baselineFlat;
        var fraction = 0d;
        foreach (var contribution in _contributions.Values)
        {
            flat += contribution.Flat;
            fraction += contribution.Fraction;
        }
        var capacity = StatValueCalculator.Calculate(baseline, flat, fraction);
        return installed ? capacity : 0;
    }
}
