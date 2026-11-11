namespace sMPGWM.Scripts.ShipSystem;

public sealed record ShipStatModifiersDefinition
{
    public double ShieldCapacity { get; set; }
    public double Speed { get; set; }
    public double AfterBurnerSpeed { get; set; }
    public double RotationSpeed { get; set; }
    public double Acceleration { get; set; }
    public double AfterBurnerAcceleration { get; set; }
    public double Armor { get; set; }
    public double RechargeTime { get; set; }
    public double DownTime { get; set; }
    public double EnergyProduction { get; set; }
    public double EnergyStorage { get; set; }
}
