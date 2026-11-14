using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class ResourceSnapshotTests
{
    [Test]
    public void DrainDowntimeAndMalformedStateRoundTrip()
    {
        var definitions = GameplayTestData.Load();
        var host = new ShipRuntime(0, 0);
        var stats = new ResolvedShipStats(200, 300, 4, 100, 200, 10, 20, 100, 1000, 1000, 5, 10, 100, true, true, true, true, true);
        host.ApplyEquipment(stats, 1);
        host.InitializeResources();
        host.AdvanceResources(1);
        Assert.That(host.Shield.Current, Is.GreaterThan(0));
        Assert.That(host.ShieldProtectionAvailable, Is.False);
        var entity = Guid.NewGuid().ToString("N");
        var rows = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) };
        Assert.That(GameplaySnapshotCodec.TryDecode(rows, definitions, out var decoded), Is.True);
        var entry = decoded[0];
        var client = new ShipRuntime(0, 0);
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline,
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0), Is.True);
        Assert.That(client.Shield, Is.EqualTo(host.Shield));
        Assert.That(client.Energy, Is.EqualTo(host.Energy));
        Assert.That(client.MovementAvailable, Is.False);
        for (var tick = 2; tick <= 62; tick++) host.AdvanceResources(tick);
        Assert.That(host.Shield.Current, Is.Zero);
        Assert.That(host.ShieldDowntimeDeadline, Is.GreaterThan(host.AuthoritativeTick));
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out decoded), Is.True);
        Assert.That(decoded[0].ShieldDowntimeDeadline, Is.EqualTo(host.ShieldDowntimeDeadline));
        foreach (var mode in new[] { "energy", "protection", "presence", "numeric", "revision", "condition", "rate" })
        {
            var row = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host);
            if (mode == "energy") row[9] = 101d;
            else if (mode == "protection") row[14] = true;
            else if (mode == "presence") row[8] = 0;
            else if (mode == "revision") row[10] = -1L;
            else if (mode == "rate") { var rateValues = row[7].AsFloat64Array(); rateValues[8] = 1e-100; row[7] = rateValues; row[2] = double.MaxValue; }
            else if (mode == "condition") row[6] = new Godot.Collections.Array();
            else
            {
                var values = row[7].AsFloat64Array();
                values[0] = double.PositiveInfinity;
                row[7] = values;
            }
            Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host), row }, definitions, out var rejected), Is.False);
            Assert.That(rejected, Is.Empty);
        }
    }

    [Test]
    public void AfterburnerDemandAndStateRoundTripAndRejectMalformedValues()
    {
        var definitions = GameplayTestData.Load();
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 8.5, 8, 0, true, false, false, true, false) { EngineEnergyConsumption = 4 };
        var host = new ShipRuntime();
        host.ApplyEquipment(stats, 0);
        host.SetAfterburner(true);
        host.AdvanceResources(1);
        var entity = Guid.NewGuid().ToString("N");
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out var decoded), Is.True);
        var entry = decoded[0];
        Assert.That(entry.AfterburnerActive, Is.True);
        Assert.That(entry.Stats, Is.EqualTo(stats));
        Assert.That(entry.EnergyDepleted, Is.True);
        var client = new ShipRuntime();
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline,
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0), Is.True);
        Assert.That(client.AfterburnerActive, Is.True);
        Assert.That(client.EnergyConsumption, Is.EqualTo(9));
        Assert.That(client.EnergyDepleted, Is.True);
        Assert.That(client.Stats, Is.EqualTo(stats));
        Assert.Throws<ArgumentException>(() => new ShipRuntime().Confirm(entry.Revision, entry.Capacity, entry.Current,
            entry.Behaviours, entry.FactionId, entry.Conditions, entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted,
            entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, false, 0, 0));
        foreach (var mode in new[] { "flagType", "flagDemand", "demand", "negative", "nonfinite", "noEngine", "overflow", "statsLength", "rowLength" })
        {
            var row = GameplaySnapshotCodec.Encode(entity, host);
            var values = row[7].AsFloat64Array();
            if (mode == "flagType") row[15] = 1;
            else if (mode == "flagDemand") row[15] = false;
            else if (mode == "demand") values[13] = 9;
            else if (mode == "negative") values[13] = -1;
            else if (mode == "nonfinite") values[13] = double.NaN;
            else if (mode == "noEngine") { values[13] = 0; row[8] = 8; }
            else if (mode == "overflow") { values[11] = double.MaxValue; values[13] = double.MaxValue; }
            else if (mode == "statsLength") values = values[..13];
            else if (mode == "rowLength") row.RemoveAt(15);
            row[7] = values;
            Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, definitions, out var rejected), Is.False, mode);
            Assert.That(rejected, Is.Empty, mode);
        }
        host.SetAfterburner(false);
        host.AdvanceResources(2);
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out decoded), Is.True);
        entry = decoded[0];
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline,
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0), Is.True);
        Assert.That(client.AfterburnerActive, Is.False);
        Assert.That(client.EnergyConsumption, Is.EqualTo(8));
        Assert.That(client.EnergyDepleted, Is.False);
        Assert.That(client.Stats, Is.EqualTo(stats));
    }
}
