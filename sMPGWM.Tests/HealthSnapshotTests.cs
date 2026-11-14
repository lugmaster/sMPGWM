using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class HealthSnapshotTests
{
    [Test]
    public void HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering()
    {
        var definitions = GameplayTestData.Load();
        using var host = new ShipRuntime(150);
        using var client = new ShipRuntime();
        host.InitializeResources();
        host.SetHealthCurrent(87);
        var entity = Guid.NewGuid().ToString("N");
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out var entries), Is.True);
        var entry = entries[0];
        Assert.That(entry.HealthCapacity, Is.EqualTo(150));
        Assert.That(entry.HealthCurrent, Is.EqualTo(87));
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline,
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, entry.HealthCapacity, entry.HealthCurrent, entry.Components), Is.True);
        Assert.That(client.Health, Is.EqualTo(host.Health));
        host.SetHealthCurrent(0);
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out entries), Is.True);
        var depleted = entries[0];
        Assert.That(client.Confirm(depleted.Revision, depleted.Capacity, depleted.Current, depleted.Behaviours, depleted.FactionId, depleted.Conditions,
            depleted.Stats, depleted.EnergyCurrent, depleted.EnergyDepleted, depleted.ShieldProtectionAvailable, depleted.ShieldDowntimeDeadline,
            depleted.AuthoritativeTick, depleted.ConfigurationRevision, depleted.AfterburnerActive, depleted.HealthCapacity, depleted.HealthCurrent, depleted.Components), Is.True);
        Assert.That(client.Health.Current, Is.Zero);
        Assert.That(client.Health.Capacity, Is.EqualTo(150));
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline,
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, entry.HealthCapacity, entry.HealthCurrent, entry.Components), Is.False);
        Assert.That(client.Health.Current, Is.Zero);
    }

    [Test]
    public void MalformedHealthSnapshotsAreRejectedAtomically()
    {
        var definitions = GameplayTestData.Load();
        using var host = new ShipRuntime(150);
        host.InitializeResources();
        foreach (var mode in new[] { "capacityType", "currentType", "negativeCapacity", "negativeCurrent", "capacityNaN", "currentNaN", "infiniteCapacity", "infiniteCurrent", "overCapacity", "missing" })
        {
            var row = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host);
            if (mode == "capacityType") row[17] = 150;
            else if (mode == "currentType") row[18] = "150";
            else if (mode == "negativeCapacity") row[17] = -1d;
            else if (mode == "negativeCurrent") row[18] = -1d;
            else if (mode == "capacityNaN") row[17] = double.NaN;
            else if (mode == "currentNaN") row[18] = double.NaN;
            else if (mode == "infiniteCapacity") row[17] = double.PositiveInfinity;
            else if (mode == "infiniteCurrent") row[18] = double.PositiveInfinity;
            else if (mode == "overCapacity") row[18] = 151d;
            else row.RemoveAt(18);
            var rows = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host), row };
            Assert.That(GameplaySnapshotCodec.TryDecode(rows, definitions, out var rejected), Is.False, mode);
            Assert.That(rejected, Is.Empty, mode);
        }
    }
}


