using System;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record AmmunitionSupplyDefinition : WeaponSupplyDefinition
{
    public int AmmoCapacity { get; }
    public double ReloadDurationMs { get; }

    public AmmunitionSupplyDefinition(int ammoCapacity, double reloadDurationMs)
    {
        if (ammoCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(ammoCapacity));
        if (!double.IsFinite(reloadDurationMs) || reloadDurationMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(reloadDurationMs));
        AmmoCapacity = ammoCapacity;
        ReloadDurationMs = reloadDurationMs;
    }
}
