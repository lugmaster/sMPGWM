using System;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.ShieldSystem;
public sealed class ShieldSystem
{
    internal double Advance(ResourcePool shield, ResolvedShipStats stats, bool powered, long tick, long deadline)
    {
        if (!stats.HasShieldGenerator || shield.Capacity <= 0) return shield.Current;
        var amount = shield.Capacity * Math.Min(1, 1000d / ShipRuntime.PhysicsStepsPerSecond / stats.RechargeTime);
        if (!powered) return Math.Max(0, shield.Current - amount);
        return tick >= deadline ? shield.Current + Math.Min(amount, shield.Capacity - shield.Current) : shield.Current;
    }
}
