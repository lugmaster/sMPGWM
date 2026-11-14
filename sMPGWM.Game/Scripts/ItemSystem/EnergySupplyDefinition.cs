using System;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record EnergySupplyDefinition : WeaponSupplyDefinition
{
    public double EnergyPerShot { get; }

    public EnergySupplyDefinition(double energyPerShot)
    {
        if (!double.IsFinite(energyPerShot) || energyPerShot <= 0)
            throw new ArgumentOutOfRangeException(nameof(energyPerShot));
        EnergyPerShot = energyPerShot;
    }
}
