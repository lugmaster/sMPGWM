using sMPGWM.Scripts.EquipmentSystem; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-001 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-002 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-003 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-004 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
using sMPGWM.Scripts.UtilitySystem.UiComponents; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-005 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand

namespace sMPGWM.Tests; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-007 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand

public sealed class WeaponFixtureStatTests // TEST-SUPPORT: WEAPON-FIXTURE-STAT-009 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
{ // TEST-SUPPORT: WEAPON-FIXTURE-STAT-010 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
    [Test] // TEST-SUPPORT: WEAPON-FIXTURE-STAT-011 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
    public void FixtureAddsMassButNeverEnergyDemand() // TEST-SUPPORT: WEAPON-FIXTURE-STAT-012 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
    { // TEST-SUPPORT: WEAPON-FIXTURE-STAT-013 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        var fixture = new ItemDefinition("fixture", "fixture", "weapon_fixture", "", false, 1, new IconDefinition { IconTexture = null! }) // TEST-SUPPORT: WEAPON-FIXTURE-STAT-014 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        { // TEST-SUPPORT: WEAPON-FIXTURE-STAT-015 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
            CategoryId = "weapon_fixture", // TEST-SUPPORT: WEAPON-FIXTURE-STAT-027 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
            Values = new Dictionary<string, object> { ["Mass"] = 7d, ["WeaponSlots"] = 9, ["EnergyConsumption"] = 500d } // TEST-SUPPORT: WEAPON-FIXTURE-STAT-016 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        }; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-017 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        var slots = new[] { new EquipmentSlotDefinition("weapon_fixture", "Weapon Fixture", "weapon_fixture") }; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-018 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        var equipment = new ItemEntity?[] { new(ItemEntityId.FromNetwork(1), fixture, 1) }; // TEST-SUPPORT: WEAPON-FIXTURE-STAT-019 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        var stats = EquipmentStatCalculator.Calculate(new ShipStatModifiersDefinition(), 10, equipment, slots); // TEST-SUPPORT: WEAPON-FIXTURE-STAT-020 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        Assert.That(stats.Mass, Is.EqualTo(17)); // TEST-SUPPORT: WEAPON-FIXTURE-STAT-021 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        Assert.That(stats.EnergyConsumption, Is.Zero); // TEST-SUPPORT: WEAPON-FIXTURE-STAT-022 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        Assert.That(stats.GetEnergyConsumption(true), Is.Zero); // TEST-SUPPORT: WEAPON-FIXTURE-STAT-023 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
        Assert.That(stats.EnergyProduction, Is.Zero); // TEST-SUPPORT: WEAPON-FIXTURE-STAT-024 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
    } // TEST-SUPPORT: WEAPON-FIXTURE-STAT-025 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
} // TEST-SUPPORT: WEAPON-FIXTURE-STAT-026 | Tests: sMPGWM.Tests.WeaponFixtureStatTests.FixtureAddsMassButNeverEnergyDemand
