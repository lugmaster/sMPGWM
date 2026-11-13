namespace sMPGWM.Scripts.WeaponSystem;

public readonly record struct ResolvedWeaponStats(string DamageSourceId, double Damage, double AttackSpeed,
    double Range, double SpreadDegrees, double EnergyConsumption, double EnergyPerShot,
    int MagazineCapacity, long ReloadTicks)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(DamageSourceId) && DamageSourceId.Length <= 64 &&
        double.IsFinite(Damage) && Damage >= 0 && double.IsFinite(AttackSpeed) && AttackSpeed > 0 && AttackSpeed <= 60 &&
        double.IsFinite(Range) && Range > 0 && Range <= float.MaxValue / 4 &&
        double.IsFinite(SpreadDegrees) && SpreadDegrees >= 0 && SpreadDegrees <= 180 &&
        double.IsFinite(EnergyConsumption) && EnergyConsumption >= 0 &&
        double.IsFinite(EnergyPerShot) && EnergyPerShot >= 0 && MagazineCapacity >= 0 && MagazineCapacity <= ushort.MaxValue && ReloadTicks >= 0;
}
