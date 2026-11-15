using System;

namespace sMPGWM.Scripts.ShipSystem;

public sealed partial class ShipRuntime
{
    public bool ValidResources(ShipResourceUpdate update)
    {
        var dead = !IsAlive;
        return update.Revision > 0 && update.ConfigurationRevision > 0 && update.Tick >= 0 && update.ShieldDeadline >= 0 &&
            double.IsFinite(update.Shield) && update.Shield >= 0 && update.Shield <= _shield.Capacity &&
            double.IsFinite(update.Energy) && update.Energy >= 0 && update.Energy <= _energy.Capacity &&
            double.IsFinite(update.Health) && update.Health >= 0 && update.Health <= _health.Capacity &&
            (_health.Capacity == 0 || dead == (update.Health == 0)) &&
            (!update.Afterburner || !dead && Stats.HasEngine && Components.Has(ShipComponentKind.Movement)) &&
            update.Depleted == (Stats.EnergyProduction < Stats.GetEnergyConsumption(update.Afterburner) && update.Energy == 0) &&
            _conditions.Contains("energyDepleted") == update.Depleted &&
            update.Protected == (!dead && Components.Has(ShipComponentKind.Power | ShipComponentKind.Shield) &&
                Stats.HasShieldGenerator && update.Shield > 0 && !update.Depleted && update.Tick >= update.ShieldDeadline);
    }

    public bool ConfirmResources(ShipResourceUpdate update)
    {
        if (_disposed || update.Revision <= Revision || update.Tick < AuthoritativeTick ||
            update.ConfigurationRevision < ConfigurationRevision) return false;
        if (!ValidResources(update)) throw new ArgumentException("Invalid resource update.");
        _shield.SetCurrent(update.Shield);
        _energy.SetCurrent(update.Energy);
        _health.SetCurrent(update.Health);
        ConfigurationRevision = update.ConfigurationRevision;
        AuthoritativeTick = update.Tick;
        ShieldDowntimeDeadline = update.ShieldDeadline;
        EnergyDepleted = update.Depleted;
        AfterburnerActive = update.Afterburner;
        Revision = update.Revision;
        PublishChanges();
        return true;
    }
}
