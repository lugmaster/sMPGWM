using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.StatSystem;

public static class EquipmentStatCalculator
{
    private static readonly ConditionalWeakTable<ItemDefinition, Values> Cache = new();

    private sealed class Values
    {
        internal readonly double Speed, AfterBurnerSpeed, RotationSpeed, Acceleration, AfterBurnerAcceleration;
        internal readonly double Mass, Armor, ShieldCapacity, RechargeTime, DownTime, EnergyProduction, EnergyConsumption, EnergyStorage;
        internal Values(ItemDefinition item)
        {
            Mass = item.Mass;
            switch (item)
            {
                case EngineItemDefinition engine:
                    Speed = engine.Speed;
                    AfterBurnerSpeed = engine.AfterBurnerSpeed;
                    RotationSpeed = engine.RotationSpeed;
                    Acceleration = engine.Acceleration;
                    AfterBurnerAcceleration = engine.AfterBurnerAcceleration;
                    EnergyConsumption = engine.EnergyConsumption;
                    break;
                case HullItemDefinition hull:
                    Armor = hull.Armor;
                    break;
                case ShieldGeneratorItemDefinition shield:
                    ShieldCapacity = shield.ShieldCapacity;
                    RechargeTime = shield.RechargeTime;
                    DownTime = shield.DownTime;
                    EnergyConsumption = shield.EnergyConsumption;
                    break;
                case ReactorItemDefinition reactor:
                    EnergyProduction = reactor.EnergyProduction;
                    break;
                case BatteryItemDefinition battery:
                    EnergyStorage = battery.EnergyStorage;
                    EnergyConsumption = battery.EnergyConsumption;
                    break;
                case WeaponItemDefinition weapon:
                    EnergyConsumption = weapon.EnergyConsumption;
                    break;
            }
        }
    }

    internal static void Precompile(ItemDefinition definition) => Cache.GetValue(definition, static item => new Values(item));

    public static ResolvedShipStats Calculate(ShipStatModifiersDefinition modifiers, double intrinsicMass,
        EquipmentSnapshot equipment, IReadOnlyList<EquipmentSlotDefinition> slots)
        => Calculate(modifiers, intrinsicMass, equipment.Slots, slots);

    public static ResolvedShipStats Calculate(ShipStatModifiersDefinition modifiers, double intrinsicMass,
        IReadOnlyList<ItemEntity?> equipment, IReadOnlyList<EquipmentSlotDefinition> slots)
    {
        if (!double.IsFinite(intrinsicMass) || intrinsicMass < 0) throw new ArgumentOutOfRangeException(nameof(intrinsicMass));
        if (equipment.Count != slots.Count) throw new ArgumentException("Equipment slot count mismatch.");
        Values? engine = null, hull = null, shield = null, reactor = null, battery = null;
        var mass = intrinsicMass;
        var demand = 0d;
        var engineDemand = 0d;
        for (var i = 0; i < slots.Count; i++)
        {
            if (equipment[i] is not { } item) continue;
            var value = Cache.GetValue(item.Definition, static item => new Values(item));
            mass += value.Mass;
            demand += value.EnergyConsumption;
            switch (slots[i].AcceptedCategory)
            {
                case "engine": engine = value; engineDemand += value.EnergyConsumption; break;
                case "hull": hull = value; break;
                case "shield_generator": shield = value; break;
                case "reactor": reactor = value; break;
                case "battery": battery = value; break;
            }
        }
        var result = new ResolvedShipStats(
            Movement(engine, engine?.Speed ?? 0, modifiers.Speed),
            Movement(engine, engine?.AfterBurnerSpeed ?? 0, modifiers.AfterBurnerSpeed),
            Movement(engine, engine?.RotationSpeed ?? 0, modifiers.RotationSpeed),
            Movement(engine, engine?.Acceleration ?? 0, modifiers.Acceleration),
            Movement(engine, engine?.AfterBurnerAcceleration ?? 0, modifiers.AfterBurnerAcceleration),
            mass, Resolve(hull, hull?.Armor ?? 0, modifiers.Armor),
            shield?.ShieldCapacity ?? 0,
            Resolve(shield, shield?.RechargeTime ?? 0, modifiers.RechargeTime),
            Resolve(shield, shield?.DownTime ?? 0, modifiers.DownTime),
            Resolve(reactor, reactor?.EnergyProduction ?? 0, modifiers.EnergyProduction), demand,
            Resolve(battery, battery?.EnergyStorage ?? 0, modifiers.EnergyStorage),
            engine != null, hull != null, shield != null, reactor != null, battery != null)
        {
            EngineEnergyConsumption = engineDemand,
            ShieldFlatModifier = shield == null ? 0 : modifiers.ShieldCapacity
        };
        if (!result.IsValid) throw new ArgumentException("Equipment resolves to invalid stats, recharge duration, or overflowing movement values.");
        return result;
    }

    private static double Resolve(Values? component, double baseline, double flat) =>
        component == null ? 0 : StatValueCalculator.Calculate(baseline, flat, 0);

    private static float Movement(Values? component, double baseline, double flat)
    {
        var value = Resolve(component, baseline, flat);
        if (value > float.MaxValue) throw new ArgumentException("Movement stat exceeds finite float range.");
        return (float)value;
    }
}
