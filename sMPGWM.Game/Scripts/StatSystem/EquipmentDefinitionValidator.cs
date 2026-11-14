using System;
using System.Collections.Generic;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.StatSystem;

internal static class EquipmentDefinitionValidator
{
    internal static void Validate(IReadOnlyList<ShipData> ships, IReadOnlyList<ItemDefinition> items)
    {
        // Production uses this layout; custom layouts still validate their resolved runtime stats.
        var slots = EquipmentSlotDefinition.Defaults;
        var equipment = new ItemEntity?[slots.Count];
        var masses = new double[slots.Count];
        var demands = new double[slots.Count];
        var weaponTotals = new double[WeaponSlotPolicy.MaxSlots + 1];
        var capacity = 0;

        foreach (var item in items)
        {
            if (item is WeaponFixtureItemDefinition fixture) capacity = Math.Max(capacity, fixture.WeaponSlots);
            for (var index = 0; index < slots.Count; index++)
            {
                if (!slots[index].Accepts(item.CategoryId)) continue;
                if (!slots[index].IsWeaponSlot)
                {
                    masses[index] = Math.Max(masses[index], item.Mass);
                    demands[index] = Math.Max(demands[index], Demand(item));
                }
                // Reuse runtime calculations to validate every ship/component combination before play.
                equipment[index] = new ItemEntity(default, item, 1);
                foreach (var ship in ships)
                {
                    try { EquipmentStatCalculator.Calculate(ship.StatModifiers, ship.Mass, equipment, slots); }
                    catch (Exception error) when (error is ArgumentException or InvalidOperationException)
                    {
                        throw new ArgumentException($"Ships.yml: ship '{ship.Id}', item '{item.Id}': invalid equipment stats. {error.Message}", error);
                    }
                }
                equipment[index] = null;
                break;
            }
        }

        foreach (var ship in ships)
        {
            var mass = ship.Mass;
            var demand = 0d;
            var engineDemand = 0d;
            for (var index = 0; index < slots.Count; index++)
            {
                if (slots[index].AcceptedCategory != "weapon_fixture") mass += masses[index];
                demand += demands[index];
                if (slots[index].AcceptedCategory == "engine") engineDemand = demands[index];
            }
            demand = MaximumWeaponTotal(items, weaponTotals, capacity, demand, false) + engineDemand * 0.25;
            Require(double.IsFinite(demand), ship, "combined energy demand with afterburner");
            Require(double.IsFinite(mass) && mass <= float.MaxValue / 2, ship, "combined equipment mass");
            foreach (var item in items)
            {
                if (item is not WeaponFixtureItemDefinition fixture) continue;
                var total = MaximumWeaponTotal(items, weaponTotals, fixture.WeaponSlots, mass + fixture.Mass, true);
                Require(double.IsFinite(total) && total <= float.MaxValue / 2, ship,
                    $"combined equipment mass with fixture '{fixture.Id}'");
            }
        }
    }

    private static double MaximumWeaponTotal(IReadOnlyList<ItemDefinition> items, double[] totals, int capacity,
        double initial, bool mass)
    {
        // Start with component totals so floating-point additions follow runtime equipment order.
        totals[0] = initial;
        for (var budget = 1; budget <= capacity; budget++)
        {
            totals[budget] = totals[budget - 1];
            foreach (var item in items)
            {
                if (item is not WeaponItemDefinition weapon) continue;
                var cost = WeaponSlotPolicy.GetCost(weapon.CategoryId);
                if (cost <= 0 || cost > budget) continue;
                totals[budget] = Math.Max(totals[budget], totals[budget - cost] +
                    (mass ? weapon.Mass : weapon.EnergyConsumption));
            }
        }
        return totals[capacity];
    }

    private static double Demand(ItemDefinition item) => item switch
    {
        EngineItemDefinition engine => engine.EnergyConsumption,
        ShieldGeneratorItemDefinition shield => shield.EnergyConsumption,
        BatteryItemDefinition battery => battery.EnergyConsumption,
        _ => 0
    };

    private static void Require(bool valid, ShipData ship, string field)
    {
        if (!valid) throw new ArgumentException($"Ships.yml: ship '{ship.Id}': {field} exceeds runtime numeric bounds.");
    }
}
