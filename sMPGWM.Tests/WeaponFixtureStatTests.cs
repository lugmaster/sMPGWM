using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests;

public sealed class WeaponFixtureStatTests
{
    [Test]
    public void FixtureAddsMassButNeverEnergyDemand()
    {
        var fixture = new WeaponFixtureItemDefinition("fixture", "fixture", "weapon_fixture", "", false, 1, new IconDefinition { IconTexture = null! })
        {
            CategoryId = "weapon_fixture",
            Mass = 7, WeaponSlots = 9
        };
        var slots = new[] { new EquipmentSlotDefinition("weapon_fixture", "Weapon Fixture", "weapon_fixture") };
        var equipment = new ItemEntity?[] { new(ItemEntityId.FromNetwork(1), fixture, 1) };
        var stats = EquipmentStatCalculator.Calculate(new ShipStatModifiersDefinition(), 10, equipment, slots);
        Assert.That(stats.Mass, Is.EqualTo(17));
        Assert.That(stats.EnergyConsumption, Is.Zero);
        Assert.That(stats.GetEnergyConsumption(true), Is.Zero);
        Assert.That(stats.EnergyProduction, Is.Zero);
    }
}
