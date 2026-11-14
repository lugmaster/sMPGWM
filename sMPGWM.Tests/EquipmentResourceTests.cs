using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class EquipmentResourceTests
{
    [Test]
    public void EnergySupplyAndBatteryIdentityFollowTheEquipment()
    {
        var state = new ShipRuntime(0, 0);
        var stats = new ResolvedShipStats(200, 300, 2, 100, 150, 10, 20, 100, 1000, 100, 120, 60, 100, true, true, true, true, true);
        state.ApplyEquipment(stats, 1);
        state.InitializeResources();
        Assert.That(state.Shield.Current, Is.EqualTo(100));
        Assert.That(state.Energy.Current, Is.Zero);
        for (var tick = 1; tick <= 60; tick++) state.AdvanceResources(tick);
        Assert.That(state.Energy.Current, Is.EqualTo(60));
        state.ApplyEquipment(stats with { EnergyStorage = 200 }, 1);
        Assert.That(state.Energy.Current, Is.EqualTo(60));
        state.ApplyEquipment(stats with { EnergyProduction = 0, HasReactor = false }, 1);
        for (var tick = 61; tick <= 120; tick++) state.AdvanceResources(tick);
        Assert.That(state.Energy.Current, Is.Zero);
        Assert.That(state.EnergyDepleted, Is.True);
        Assert.That(state.MovementAvailable, Is.False);
        Assert.That(state.ShieldProtectionAvailable, Is.False);
        Assert.That(state.Shield.Current, Is.LessThan(100).And.GreaterThan(0));
        state.ApplyEquipment(stats, 1);
        state.AdvanceResources(121);
        Assert.That(state.PoweredSystemsAvailable, Is.True);
        state.ApplyEquipment(stats, 2);
        Assert.That(state.Energy.Current, Is.Zero);
        state.InitializeResources();
        Assert.That(state.Energy.Current, Is.Zero);
    }

    [Test]
    public void EqualityWithoutBatteryPowersSystemsAndDeficitNeverOscillates()
    {
        var state = new ShipRuntime(0, 0);
        var stats = new ResolvedShipStats(200, 300, 2, 100, 150, 10, 0, 100, 1000, 100, 60, 60, 0, true, false, true, true, false);
        state.ApplyEquipment(stats, 0);
        state.InitializeResources();
        state.AdvanceResources(1);
        Assert.That(state.PoweredSystemsAvailable, Is.True);
        state.ApplyEquipment(stats with { EnergyProduction = 30 }, 0);
        for (var tick = 2; tick < 100; tick++)
        {
            state.AdvanceResources(tick);
            Assert.That(state.EnergyDepleted, Is.True);
            Assert.That(state.Stats.EnergyConsumption, Is.EqualTo(60));
        }
        state.ApplyEquipment(stats, 0);
        Assert.That(state.PoweredSystemsAvailable, Is.True);
    }

    [Test]
    public void ShieldDowntimeSurvivesReplacementAndStartsOnlyOnTransition()
    {
        var state = new ShipRuntime(0, 0);
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 60, 1000, 100, 0, 60, 0, false, false, true, false, false);
        state.ApplyEquipment(stats, 0);
        state.InitializeResources();
        for (var tick = 1; tick <= 60; tick++) state.AdvanceResources(tick);
        Assert.That(state.Shield.Current, Is.Zero);
        Assert.That(state.ShieldDowntimeDeadline, Is.EqualTo(66));
        state.AdvanceResources(61);
        Assert.That(state.ShieldDowntimeDeadline, Is.EqualTo(66));
        stats = stats with { EnergyProduction = 60, HasReactor = true, ShieldCapacity = 120, DownTime = 0 };
        state.ApplyEquipment(stats, 0);
        for (var tick = 62; tick <= 65; tick++) state.AdvanceResources(tick);
        Assert.That(state.Shield.Current, Is.Zero);
        state.AdvanceResources(66);
        Assert.That(state.Shield.Current, Is.EqualTo(2));
        Assert.That(state.ShieldProtectionAvailable, Is.True);
        Assert.That(state.AdvanceResources(66), Is.False);
        state.ApplyEquipment(stats with { ShieldCapacity = 0, RechargeTime = 0, HasShieldGenerator = false }, 0);
        Assert.That(state.Shield.Current, Is.Zero);
        state.ApplyEquipment(stats, 0);
        Assert.That(state.Shield.Current, Is.Zero);
    }

    [Test]
    public void OwnedModifiersSurviveMissingSystemAndUseAllFlatValues()
    {
        var pool = new ShipStatState();
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 200, 5000, 0, 0, 0, 0, false, false, true, false, false));
        pool.SetContribution(new(StatContributionKind.Effect, 1), 50, .2);
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(300));
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, false, false, false) { ShieldFlatModifier = 0 });
        Assert.That(pool.Effective.ShieldCapacity, Is.Zero);
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false) { ShieldFlatModifier = 0 });
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(180));
        Assert.That(pool.EquipmentInputs.ShieldCapacity, Is.EqualTo(100));
        pool.RemoveSource(new(StatContributionKind.Effect, 1));
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(100));
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false) { ShieldFlatModifier = -150 });
        Assert.That(pool.Effective.ShieldCapacity, Is.Zero);
        pool.SetContribution(new(StatContributionKind.Effect, 2), 100, 0);
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(50));
    }

    [TestCase(5)]
    [TestCase(10)]
    public void SustainedResourceSimulationMeasuresTimingAndAllocations(int count)
    {
        var states = new ShipRuntime[count];
        var stats = new ResolvedShipStats(200, 300, 2, 100, 150, 10, 0, 100, 1000, 500, 120, 60, 100, true, false, true, true, true);
        for (var i = 0; i < count; i++)
        {
            states[i] = new(0, 0);
            states[i].ApplyEquipment(stats, 1);
            states[i].InitializeResources();
        }
        for (var tick = 1; tick <= 600; tick++) foreach (var state in states) state.AdvanceResources(tick);
        var clock = new System.Diagnostics.Stopwatch();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        clock.Start();
        for (var tick = 601; tick <= 36600; tick++)
        {
            foreach (var state in states)
            {
                if (tick % 600 == 0) state.ApplyEquipment(stats with { EnergyProduction = tick % 1200 == 0 ? 0 : 120 }, 1);
                state.AdvanceResources(tick);
            }
        }
        clock.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Out.WriteLine($"Resources: {count} ships, 36000 steps, {clock.Elapsed.TotalMilliseconds:F3} ms total, {clock.Elapsed.TotalMilliseconds / 36000:F6} ms/step, {allocated} allocated bytes, GC counts {GC.CollectionCount(0) - gen0}/{GC.CollectionCount(1) - gen1}/{GC.CollectionCount(2) - gen2}");
        Assert.That(allocated, Is.LessThan(1024));
        Assert.That(states[0].Energy.Current, Is.InRange(0, 100));
    }
}
