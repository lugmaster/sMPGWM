namespace sMPGWM.Scripts.ShipSystem;

public sealed record ShipStatModifiersDefinition
{
    public double ShieldCapacity { get; init; }
    public double Speed { get; init; }
    public double AfterBurnerSpeed { get; init; }
    public double RotationSpeed { get; init; }
    public double Acceleration { get; init; }
    public double AfterBurnerAcceleration { get; init; }
    public double Armor { get; init; }
    public double RechargeTime { get; init; }
    public double DownTime { get; init; }
    public double EnergyProduction { get; init; }
    public double EnergyStorage { get; init; }
}
