using System;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ResourcePool
{
    public double Capacity { get; private set; }
    public double Current { get; private set; }
    public ResourcePool(double capacity, double current) => Confirm(capacity, current);
    public void SetCapacity(double capacity)
    {
        if (!double.IsFinite(capacity) || capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        Current = Math.Min(Current, capacity);
    }
    public void Confirm(double capacity, double current)
    {
        if (!double.IsFinite(capacity) || !double.IsFinite(current) || current < 0 || capacity < current)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        Current = current;
    }
    public void SetCurrent(double current)
    {
        if (!double.IsFinite(current) || current < 0 || current > Capacity) throw new ArgumentOutOfRangeException(nameof(current));
        Current = current;
    }
}
