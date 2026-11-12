using System;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.EnergySystem;
public sealed class PowerSystem
{
    internal void Advance(ResourcePool energy, double production, double consumption)
    {
        var delta = (production - consumption) / ShipRuntime.PhysicsStepsPerSecond;
        energy.SetCurrent(delta >= 0 ? energy.Current + Math.Min(delta, energy.Capacity - energy.Current) : Math.Max(0, energy.Current + delta));
    }
}
