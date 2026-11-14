using sMPGWM.Scripts.GameplayDataSystem;


using System.Globalization;
using System.Text;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.UiSystem.Items;

internal static class ItemDisplayFormatter
{
    internal static IconDisplayData Create(ItemEntity? entity)
    {
        if (entity == null) return new IconDisplayData();
        return Create(entity.Definition, entity.Count);
    }

    internal static IconDisplayData Create(string definitionId, long? quantity)
    {
        return Create(GameplayDefinitions.Current.Items.GetById(definitionId), quantity);
    }

    private static IconDisplayData Create(ItemDefinition data, long? quantity)
    {
        var tooltip = new StringBuilder(data.DisplayName).Append('\n').Append(data.CategoryName);
        tooltip.Append('\n').Append(data.Description);
        AppendNumber(tooltip, nameof(data.Mass), data.Mass);
        switch (data)
        {
            case EngineItemDefinition engine:
                AppendNumber(tooltip, nameof(engine.Speed), engine.Speed);
                AppendNumber(tooltip, nameof(engine.AfterBurnerSpeed), engine.AfterBurnerSpeed);
                AppendNumber(tooltip, nameof(engine.RotationSpeed), engine.RotationSpeed);
                AppendNumber(tooltip, nameof(engine.Acceleration), engine.Acceleration);
                AppendNumber(tooltip, nameof(engine.AfterBurnerAcceleration), engine.AfterBurnerAcceleration);
                AppendNumber(tooltip, nameof(engine.EnergyConsumption), engine.EnergyConsumption, " /s");
                break;
            case HullItemDefinition hull:
                AppendNumber(tooltip, nameof(hull.Armor), hull.Armor);
                break;
            case ShieldGeneratorItemDefinition shield:
                AppendNumber(tooltip, nameof(shield.ShieldCapacity), shield.ShieldCapacity);
                AppendNumber(tooltip, nameof(shield.RechargeTime), shield.RechargeTime, " ms");
                AppendNumber(tooltip, nameof(shield.DownTime), shield.DownTime, " ms");
                AppendNumber(tooltip, nameof(shield.EnergyConsumption), shield.EnergyConsumption, " /s");
                break;
            case ReactorItemDefinition reactor:
                AppendNumber(tooltip, nameof(reactor.EnergyProduction), reactor.EnergyProduction, " /s");
                break;
            case BatteryItemDefinition battery:
                AppendNumber(tooltip, nameof(battery.EnergyStorage), battery.EnergyStorage);
                AppendNumber(tooltip, nameof(battery.EnergyConsumption), battery.EnergyConsumption, " /s");
                break;
            case WeaponFixtureItemDefinition fixture:
                AppendNumber(tooltip, nameof(fixture.WeaponSlots), fixture.WeaponSlots);
                break;
            case WeaponItemDefinition weapon:
                AppendText(tooltip, nameof(weapon.Archetype), weapon.Archetype);
                AppendText(tooltip, nameof(weapon.DeliveryType), weapon.DeliveryType.ToString());
                AppendText(tooltip, nameof(weapon.DamageSource), weapon.DamageSource);
                AppendNumber(tooltip, nameof(weapon.Damage), weapon.Damage);
                AppendNumber(tooltip, nameof(weapon.AttackSpeed), weapon.AttackSpeed, " /s");
                AppendNumber(tooltip, nameof(weapon.Range), weapon.Range);
                AppendNumber(tooltip, nameof(weapon.SpreadDegrees), weapon.SpreadDegrees, " degrees");
                AppendNumber(tooltip, nameof(weapon.EnergyConsumption), weapon.EnergyConsumption, " /s");
                switch (weapon.Supply)
                {
                    case AmmunitionSupplyDefinition ammunition:
                        AppendText(tooltip, "Supply", "Ammunition");
                        AppendNumber(tooltip, nameof(ammunition.AmmoCapacity), ammunition.AmmoCapacity);
                        AppendNumber(tooltip, "ReloadDuration", ammunition.ReloadDurationMs, " ms");
                        break;
                    case EnergySupplyDefinition energy:
                        AppendText(tooltip, "Supply", "Energy");
                        AppendNumber(tooltip, nameof(energy.EnergyPerShot), energy.EnergyPerShot);
                        break;
                }
                break;
        }
        if (data.CanStack)
        {
            var limit = data.StackLimit == int.MaxValue ? "Unlimited" : data.StackLimit.ToString(CultureInfo.InvariantCulture);
            AppendText(tooltip, "Quantity", quantity.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
            AppendText(tooltip, "Stack limit", limit);
        }
        return new IconDisplayData(tooltip.ToString(), data.CanStack ? quantity : null);
    }

    private static void AppendNumber(StringBuilder tooltip, string name, double value, string unit = "") =>
        tooltip.Append('\n').Append(name).Append(": ").Append(value.ToString(CultureInfo.InvariantCulture)).Append(unit);

    private static void AppendText(StringBuilder tooltip, string name, string value) =>
        tooltip.Append('\n').Append(name).Append(": ").Append(value);
}
