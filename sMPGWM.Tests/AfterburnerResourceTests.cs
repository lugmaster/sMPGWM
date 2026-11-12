using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: AFTERBURN-RESOURCE-001 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats, sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests, sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected, sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: AFTERBURN-RESOURCE-002 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats, sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests, sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected, sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats

namespace sMPGWM.Tests; // TEST-SUPPORT: AFTERBURN-RESOURCE-003 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats, sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests, sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected, sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats

public sealed class AfterburnerResourceTests // TEST-SUPPORT: AFTERBURN-RESOURCE-004 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats, sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests, sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected, sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
{ // TEST-SUPPORT: AFTERBURN-RESOURCE-005 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats, sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests, sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected, sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
    [Test] // TEST-SUPPORT: AFTERBURN-RESOURCE-006 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
    public void BatteryDrainAndRecoveryPreserveEquipmentStats() // TEST-SUPPORT: AFTERBURN-RESOURCE-007 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
    { // TEST-SUPPORT: AFTERBURN-RESOURCE-008 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 9, 8, 10, true, false, false, true, true) { EngineEnergyConsumption = 8 }; // TEST-SUPPORT: AFTERBURN-RESOURCE-009 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        var state = new ShipGameplayState(); // TEST-SUPPORT: AFTERBURN-RESOURCE-010 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        state.ApplyEquipment(stats, 1); // TEST-SUPPORT: AFTERBURN-RESOURCE-011 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        state.InitializeResources(); // TEST-SUPPORT: AFTERBURN-RESOURCE-012 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        for (var tick = 1; tick <= 60; tick++) state.AdvanceResources(tick); // TEST-SUPPORT: AFTERBURN-RESOURCE-013 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Energy.Current, Is.EqualTo(1).Within(1e-10)); // TEST-SUPPORT: AFTERBURN-RESOURCE-014 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        state.SetAfterburner(true); // TEST-SUPPORT: AFTERBURN-RESOURCE-015 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: AFTERBURN-RESOURCE-016 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.EnergyConsumption, Is.EqualTo(10)); // TEST-SUPPORT: AFTERBURN-RESOURCE-017 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Energy.Current, Is.EqualTo(1).Within(1e-10)); // TEST-SUPPORT: AFTERBURN-RESOURCE-018 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        for (var tick = 61; tick <= 90; tick++) state.AdvanceResources(tick); // TEST-SUPPORT: AFTERBURN-RESOURCE-019 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Energy.Current, Is.EqualTo(0.5).Within(1e-10)); // TEST-SUPPORT: AFTERBURN-RESOURCE-020 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        for (var tick = 91; tick <= 180; tick++) state.AdvanceResources(tick); // TEST-SUPPORT: AFTERBURN-RESOURCE-021 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.EnergyDepleted, Is.True); // TEST-SUPPORT: AFTERBURN-RESOURCE-022 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.MovementAvailable, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-023 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        var depletedRevision = state.Revision; // TEST-SUPPORT: AFTERBURN-RESOURCE-024 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        for (var tick = 181; tick <= 240; tick++) // TEST-SUPPORT: AFTERBURN-RESOURCE-025 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        { // TEST-SUPPORT: AFTERBURN-RESOURCE-026 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
            state.SetAfterburner(true); // TEST-SUPPORT: AFTERBURN-RESOURCE-027 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
            state.AdvanceResources(tick); // TEST-SUPPORT: AFTERBURN-RESOURCE-028 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
            Assert.That(state.EnergyDepleted, Is.True); // TEST-SUPPORT: AFTERBURN-RESOURCE-029 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        } // TEST-SUPPORT: AFTERBURN-RESOURCE-030 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Revision, Is.EqualTo(depletedRevision)); // TEST-SUPPORT: AFTERBURN-RESOURCE-031 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        state.SetAfterburner(false); // TEST-SUPPORT: AFTERBURN-RESOURCE-032 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.EnergyConsumption, Is.EqualTo(8)); // TEST-SUPPORT: AFTERBURN-RESOURCE-033 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.EnergyDepleted, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-034 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.MovementAvailable, Is.True); // TEST-SUPPORT: AFTERBURN-RESOURCE-035 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        for (var tick = 241; tick <= 300; tick++) state.AdvanceResources(tick); // TEST-SUPPORT: AFTERBURN-RESOURCE-036 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Energy.Current, Is.EqualTo(1).Within(1e-10)); // TEST-SUPPORT: AFTERBURN-RESOURCE-037 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
        Assert.That(state.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: AFTERBURN-RESOURCE-038 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats
    } // TEST-SUPPORT: AFTERBURN-RESOURCE-039 | Tests: sMPGWM.Tests.AfterburnerResourceTests.BatteryDrainAndRecoveryPreserveEquipmentStats

    [Test] // TEST-SUPPORT: AFTERBURN-RESOURCE-040 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
    public void RemovingEngineClearsAfterburnerAndRejectsFurtherRequests() // TEST-SUPPORT: AFTERBURN-RESOURCE-041 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
    { // TEST-SUPPORT: AFTERBURN-RESOURCE-042 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 8.5, 8, 0, true, false, false, true, false) { EngineEnergyConsumption = 4 }; // TEST-SUPPORT: AFTERBURN-RESOURCE-043 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        var state = new ShipGameplayState(); // TEST-SUPPORT: AFTERBURN-RESOURCE-044 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        state.ApplyEquipment(stats, 0); // TEST-SUPPORT: AFTERBURN-RESOURCE-045 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        state.SetAfterburner(true); // TEST-SUPPORT: AFTERBURN-RESOURCE-046 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.AfterburnerActive, Is.True); // TEST-SUPPORT: AFTERBURN-RESOURCE-047 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.EnergyDepleted, Is.True); // TEST-SUPPORT: AFTERBURN-RESOURCE-048 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        var withoutEngine = stats with { HasEngine = false, EngineEnergyConsumption = 0, EnergyConsumption = 4 }; // TEST-SUPPORT: AFTERBURN-RESOURCE-049 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        state.ApplyEquipment(withoutEngine, 0); // TEST-SUPPORT: AFTERBURN-RESOURCE-050 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.AfterburnerActive, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-051 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.EnergyConsumption, Is.EqualTo(4)); // TEST-SUPPORT: AFTERBURN-RESOURCE-052 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.EnergyDepleted, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-053 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        var revision = state.Revision; // TEST-SUPPORT: AFTERBURN-RESOURCE-054 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        state.SetAfterburner(true); // TEST-SUPPORT: AFTERBURN-RESOURCE-055 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.AfterburnerActive, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-056 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
        Assert.That(state.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: AFTERBURN-RESOURCE-057 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests
    } // TEST-SUPPORT: AFTERBURN-RESOURCE-058 | Tests: sMPGWM.Tests.AfterburnerResourceTests.RemovingEngineClearsAfterburnerAndRejectsFurtherRequests

    [Test] // TEST-SUPPORT: AFTERBURN-RESOURCE-059 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
    public void InvalidEngineConsumptionIsRejected() // TEST-SUPPORT: AFTERBURN-RESOURCE-060 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
    { // TEST-SUPPORT: AFTERBURN-RESOURCE-061 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
        var baseline = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 8, 0, true, false, false, false, false) { EngineEnergyConsumption = 4 }; // TEST-SUPPORT: AFTERBURN-RESOURCE-062 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
        foreach (var demand in new[] { -1d, 9d, double.NaN, double.PositiveInfinity }) // TEST-SUPPORT: AFTERBURN-RESOURCE-063 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
        { // TEST-SUPPORT: AFTERBURN-RESOURCE-064 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
            var invalid = baseline with { EngineEnergyConsumption = demand }; // TEST-SUPPORT: AFTERBURN-RESOURCE-065 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
            Assert.That(invalid.IsValid, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-066 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
            Assert.Throws<ArgumentException>(() => new ShipGameplayState().ApplyEquipment(invalid, 0)); // TEST-SUPPORT: AFTERBURN-RESOURCE-067 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
        } // TEST-SUPPORT: AFTERBURN-RESOURCE-068 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
        Assert.That((baseline with { HasEngine = false }).IsValid, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-069 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
        Assert.That((baseline with { EnergyConsumption = double.MaxValue, EngineEnergyConsumption = double.MaxValue }).IsValid, Is.False); // TEST-SUPPORT: AFTERBURN-RESOURCE-070 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected
    } // TEST-SUPPORT: AFTERBURN-RESOURCE-071 | Tests: sMPGWM.Tests.AfterburnerResourceTests.InvalidEngineConsumptionIsRejected

    [Test] // TEST-SUPPORT: AFTERBURN-RESOURCE-072 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
    public void TogglingAtFullBatteryPublishesWithoutChangingStats() // TEST-SUPPORT: AFTERBURN-RESOURCE-073 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
    { // TEST-SUPPORT: AFTERBURN-RESOURCE-074 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 20, 8, 1, true, false, false, true, true) { EngineEnergyConsumption = 4 }; // TEST-SUPPORT: AFTERBURN-RESOURCE-075 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        var state = new ShipGameplayState(); // TEST-SUPPORT: AFTERBURN-RESOURCE-076 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        state.ApplyEquipment(stats, 1); // TEST-SUPPORT: AFTERBURN-RESOURCE-077 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        state.InitializeResources(); // TEST-SUPPORT: AFTERBURN-RESOURCE-078 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        for (var tick = 1; tick <= 60; tick++) state.AdvanceResources(tick); // TEST-SUPPORT: AFTERBURN-RESOURCE-079 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        var revision = state.Revision; // TEST-SUPPORT: AFTERBURN-RESOURCE-080 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        var configurationRevision = state.ConfigurationRevision; // TEST-SUPPORT: AFTERBURN-RESOURCE-081 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        state.SetAfterburner(true); // TEST-SUPPORT: AFTERBURN-RESOURCE-082 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        Assert.That(state.Revision, Is.EqualTo(revision + 1)); // TEST-SUPPORT: AFTERBURN-RESOURCE-083 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        state.AdvanceResources(61); // TEST-SUPPORT: AFTERBURN-RESOURCE-084 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        Assert.That(state.Energy.Current, Is.EqualTo(1)); // TEST-SUPPORT: AFTERBURN-RESOURCE-085 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        Assert.That(state.Revision, Is.EqualTo(revision + 1)); // TEST-SUPPORT: AFTERBURN-RESOURCE-086 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        state.SetAfterburner(false); // TEST-SUPPORT: AFTERBURN-RESOURCE-087 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        Assert.That(state.Revision, Is.EqualTo(revision + 2)); // TEST-SUPPORT: AFTERBURN-RESOURCE-088 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        Assert.That(state.ConfigurationRevision, Is.EqualTo(configurationRevision)); // TEST-SUPPORT: AFTERBURN-RESOURCE-089 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
        Assert.That(state.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: AFTERBURN-RESOURCE-090 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
    } // TEST-SUPPORT: AFTERBURN-RESOURCE-091 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
} // TEST-SUPPORT: AFTERBURN-RESOURCE-092 | Tests: sMPGWM.Tests.AfterburnerResourceTests.TogglingAtFullBatteryPublishesWithoutChangingStats
