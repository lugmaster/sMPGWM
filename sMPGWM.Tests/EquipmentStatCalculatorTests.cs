using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests;

public sealed class EquipmentStatCalculatorTests
{
    [Test]
    public void InstalledComponentsReceiveModifiersAndWeaponsContributeOnlyMassAndDemand()
    {
        var engine = new ItemDefinition("engine", "engine", "engine", "", false, 1, new IconDefinition { IconTexture = null! })
        {
            Values = new Dictionary<string, object> { ["Speed"] = 200d, ["Mass"] = 2d, ["EnergyConsumption"] = 3d }
        };
        var weapon = new ItemDefinition("weapon", "weapon", "light_weapon", "", false, 1, new IconDefinition { IconTexture = null! })
        {
            Values = new Dictionary<string, object> { ["Speed"] = 900d, ["Mass"] = 4d, ["EnergyConsumption"] = 5d }
        };
        var slots = new[] { new EquipmentSlotDefinition("engine", "engine", "engine"), new EquipmentSlotDefinition("weapon", "weapon", "light_weapon") };
        var equipment = new EquipmentSnapshot(1, new ItemEntity?[] { new(ItemEntityId.FromNetwork(1), engine, 1), new(ItemEntityId.FromNetwork(2), weapon, 1) });
        var modifiers = new ShipStatModifiersDefinition { Speed = 50, ShieldCapacity = 100, EnergyProduction = 100, EnergyStorage = 100, Armor = 100 };
        var stats = EquipmentStatCalculator.Calculate(modifiers, 10, equipment, slots);
        Assert.That(stats.EngineEnergyConsumption, Is.EqualTo(3));
        Assert.That(stats.GetEnergyConsumption(true), Is.EqualTo(8.75));
        Assert.That(stats.GetEnergyConsumption(false), Is.EqualTo(8));
        Assert.That(stats.Speed, Is.EqualTo(250));
        Assert.That(stats.Mass, Is.EqualTo(16));
        Assert.That(stats.EnergyConsumption, Is.EqualTo(8));
        Assert.That(stats.ShieldCapacity, Is.Zero);
        Assert.That(stats.EnergyProduction, Is.Zero);
        Assert.That(stats.EnergyStorage, Is.Zero);
        Assert.That(stats.Armor, Is.Zero);
        Assert.That(StatValueCalculator.Calculate(200, 50, .2), Is.EqualTo(300));
        var missingEngine = new EquipmentSnapshot(2, new ItemEntity?[] { null, new(ItemEntityId.FromNetwork(2), weapon, 1) });
        var bare = EquipmentStatCalculator.Calculate(modifiers, 10, missingEngine, slots);
        Assert.That(bare.EngineEnergyConsumption, Is.Zero);
        Assert.That(bare.GetEnergyConsumption(true), Is.EqualTo(5));
        Assert.That(bare.Speed, Is.Zero);
        Assert.That(bare.Mass, Is.EqualTo(14));
    }

    [Test]
    public void InvalidTimingAndOverflowAreRejected()
    {
        var shield = new ItemDefinition("shield", "shield", "shield_generator", "", false, 1, new IconDefinition { IconTexture = null! })
        {
            Values = new Dictionary<string, object> { ["ShieldCapacity"] = 100d, ["RechargeTime"] = 1000d }
        };
        var slots = new[] { new EquipmentSlotDefinition("shield", "shield", "shield_generator") };
        var equipment = new EquipmentSnapshot(1, new ItemEntity?[] { new(ItemEntityId.FromNetwork(1), shield, 1) });
        Assert.Throws<ArgumentException>(() => EquipmentStatCalculator.Calculate(new ShipStatModifiersDefinition { RechargeTime = -1000 }, 1, equipment, slots));
        Assert.Throws<InvalidOperationException>(() => StatValueCalculator.Calculate(double.MaxValue, double.MaxValue, 0));
        Assert.Throws<InvalidOperationException>(() => StatValueCalculator.Calculate(100, 0, double.MaxValue));
        Assert.That(new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 100, double.Epsilon, 0, 0, 0, 0, false, false, true, false, false).IsValid, Is.False);
    }
}
