using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class AfterburnerResourceTests
{
    [Test]
    public void BatteryDrainAndRecoveryPreserveEquipmentStats()
    {
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 9, 8, 10, true, false, false, true, true) { EngineEnergyConsumption = 8 };
        var state = new ShipRuntime();
        state.ApplyEquipment(stats, 1);
        state.InitializeResources();
        for (var tick = 1; tick <= 60; tick++) state.AdvanceResources(tick);
        Assert.That(state.Energy.Current, Is.EqualTo(1).Within(1e-10));
        state.SetAfterburner(true);
        Assert.That(state.Stats, Is.EqualTo(stats));
        Assert.That(state.EnergyConsumption, Is.EqualTo(10));
        Assert.That(state.Energy.Current, Is.EqualTo(1).Within(1e-10));
        for (var tick = 61; tick <= 90; tick++) state.AdvanceResources(tick);
        Assert.That(state.Energy.Current, Is.EqualTo(0.5).Within(1e-10));
        for (var tick = 91; tick <= 180; tick++) state.AdvanceResources(tick);
        Assert.That(state.EnergyDepleted, Is.True);
        Assert.That(state.MovementAvailable, Is.False);
        var depletedRevision = state.Revision;
        for (var tick = 181; tick <= 240; tick++)
        {
            state.SetAfterburner(true);
            state.AdvanceResources(tick);
            Assert.That(state.EnergyDepleted, Is.True);
        }
        Assert.That(state.Revision, Is.EqualTo(depletedRevision));
        state.SetAfterburner(false);
        Assert.That(state.EnergyConsumption, Is.EqualTo(8));
        Assert.That(state.EnergyDepleted, Is.False);
        Assert.That(state.MovementAvailable, Is.True);
        for (var tick = 241; tick <= 300; tick++) state.AdvanceResources(tick);
        Assert.That(state.Energy.Current, Is.EqualTo(1).Within(1e-10));
        Assert.That(state.Stats, Is.EqualTo(stats));
    }

    [Test]
    public void RemovingEngineClearsAfterburnerAndRejectsFurtherRequests()
    {
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 8.5, 8, 0, true, false, false, true, false) { EngineEnergyConsumption = 4 };
        var state = new ShipRuntime();
        state.ApplyEquipment(stats, 0);
        state.SetAfterburner(true);
        Assert.That(state.AfterburnerActive, Is.True);
        Assert.That(state.EnergyDepleted, Is.True);
        var withoutEngine = stats with { HasEngine = false, EngineEnergyConsumption = 0, EnergyConsumption = 4 };
        state.ApplyEquipment(withoutEngine, 0);
        Assert.That(state.AfterburnerActive, Is.False);
        Assert.That(state.EnergyConsumption, Is.EqualTo(4));
        Assert.That(state.EnergyDepleted, Is.False);
        var revision = state.Revision;
        state.SetAfterburner(true);
        Assert.That(state.AfterburnerActive, Is.False);
        Assert.That(state.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void InvalidEngineConsumptionIsRejected()
    {
        var baseline = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 8, 0, true, false, false, false, false) { EngineEnergyConsumption = 4 };
        foreach (var demand in new[] { -1d, 9d, double.NaN, double.PositiveInfinity })
        {
            var invalid = baseline with { EngineEnergyConsumption = demand };
            Assert.That(invalid.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => new ShipRuntime().ApplyEquipment(invalid, 0));
        }
        Assert.That((baseline with { HasEngine = false }).IsValid, Is.False);
        Assert.That((baseline with { EnergyConsumption = double.MaxValue, EngineEnergyConsumption = double.MaxValue }).IsValid, Is.False);
    }

    [Test]
    public void TogglingAtFullBatteryPublishesWithoutChangingStats()
    {
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 20, 8, 1, true, false, false, true, true) { EngineEnergyConsumption = 4 };
        var state = new ShipRuntime();
        state.ApplyEquipment(stats, 1);
        state.InitializeResources();
        for (var tick = 1; tick <= 60; tick++) state.AdvanceResources(tick);
        var revision = state.Revision;
        var configurationRevision = state.ConfigurationRevision;
        state.SetAfterburner(true);
        Assert.That(state.Revision, Is.EqualTo(revision + 1));
        state.AdvanceResources(61);
        Assert.That(state.Energy.Current, Is.EqualTo(1));
        Assert.That(state.Revision, Is.EqualTo(revision + 1));
        state.SetAfterburner(false);
        Assert.That(state.Revision, Is.EqualTo(revision + 2));
        Assert.That(state.ConfigurationRevision, Is.EqualTo(configurationRevision));
        Assert.That(state.Stats, Is.EqualTo(stats));
    }
}
