using System;
using System.Runtime.CompilerServices;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public static class WeaponDefinitionCompiler
{
    private static readonly ConditionalWeakTable<WeaponItemDefinition, WeaponExecutionPlan> Plans = new();

    public static WeaponExecutionPlan? Prepare(ItemDefinition definition)
    {
        if (definition is not WeaponItemDefinition weapon || weapon.DeliveryType != WeaponDeliveryType.HitScan) return null;
        return Plans.GetValue(weapon, Compile);
    }

    private static WeaponExecutionPlan Compile(WeaponItemDefinition weapon)
    {
        var profile = WeaponPresentationProfiles.IndexOf(weapon.PresentationId);
        if (profile < 0 || !double.IsFinite(weapon.AttackSpeed) || weapon.AttackSpeed <= 0 || weapon.AttackSpeed > 60 ||
            !double.IsFinite(weapon.Range) || weapon.Range <= 0 || weapon.Range > float.MaxValue ||
            !double.IsFinite(weapon.Damage) || weapon.Damage < 0 ||
            !double.IsFinite(weapon.SpreadDegrees) || weapon.SpreadDegrees < 0 || weapon.SpreadDegrees > 360 ||
            weapon.Supply is not (AmmunitionSupplyDefinition or EnergySupplyDefinition))
            throw new ArgumentException("Unsupported executable weapon definition: " + weapon.Id);
        return new(weapon, profile);
    }
}
