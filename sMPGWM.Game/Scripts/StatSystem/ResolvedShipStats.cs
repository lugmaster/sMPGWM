using System;

namespace sMPGWM.Scripts.StatSystem;

public readonly record struct ResolvedShipStats(
    float Speed, float AfterBurnerSpeed, float RotationSpeed, float Acceleration,
    float AfterBurnerAcceleration, double Mass, double Armor, double ShieldCapacity,
    double RechargeTime, double DownTime, double EnergyProduction, double EnergyConsumption,
    double EnergyStorage, bool HasEngine, bool HasHull, bool HasShieldGenerator,
    bool HasReactor, bool HasBattery)
{
    public double ShieldFlatModifier { get; init; }
    public double EngineEnergyConsumption { get; init; }

    public double GetEnergyConsumption(bool afterburnerActive) =>
        EnergyConsumption + (afterburnerActive ? EngineEnergyConsumption * 0.25 : 0);

    public bool IsValid =>
        float.IsFinite(Speed) && Speed >= 0 && float.IsFinite(AfterBurnerSpeed) && AfterBurnerSpeed >= 0 &&
        float.IsFinite(RotationSpeed) && RotationSpeed >= 0 && float.IsFinite(Acceleration) && Acceleration >= 0 &&
        float.IsFinite(AfterBurnerAcceleration) && AfterBurnerAcceleration >= 0 &&
        double.IsFinite(Mass) && Mass >= 0 && Mass <= float.MaxValue / 2 &&
        double.IsFinite(Armor) && Armor >= 0 && double.IsFinite(ShieldCapacity) && ShieldCapacity >= 0 &&
        double.IsFinite(ShieldFlatModifier) && double.IsFinite(ShieldCapacity + ShieldFlatModifier) &&
        double.IsFinite(RechargeTime) && RechargeTime >= 0 && (!HasShieldGenerator || RechargeTime > 0) &&
        (!HasShieldGenerator || double.IsFinite(Math.Max(0, ShieldCapacity + ShieldFlatModifier) * (1000d / 60 / RechargeTime))) &&
        double.IsFinite(DownTime) && DownTime >= 0 && DownTime <= long.MaxValue / 60d &&
        double.IsFinite(EnergyProduction) && EnergyProduction >= 0 &&
        double.IsFinite(EnergyConsumption) && EnergyConsumption >= 0 &&
        double.IsFinite(EngineEnergyConsumption) && EngineEnergyConsumption >= 0 &&
        EngineEnergyConsumption <= EnergyConsumption && (HasEngine || EngineEnergyConsumption == 0) &&
        double.IsFinite(GetEnergyConsumption(true)) &&
        double.IsFinite(EnergyStorage) && EnergyStorage >= 0 &&
        (HasEngine || Speed == 0 && AfterBurnerSpeed == 0 && RotationSpeed == 0 && Acceleration == 0 && AfterBurnerAcceleration == 0) &&
        (HasHull || Armor == 0) && (HasShieldGenerator || ShieldCapacity == 0 && ShieldFlatModifier == 0 && RechargeTime == 0 && DownTime == 0) &&
        (HasReactor || EnergyProduction == 0) && (HasBattery || EnergyStorage == 0);
}
