using Godot; // TEST-SUPPORT: RESOURCE-SNAPSHOT-001 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: RESOURCE-SNAPSHOT-002 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: RESOURCE-SNAPSHOT-003 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: RESOURCE-SNAPSHOT-004 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues

namespace sMPGWM.Tests; // TEST-SUPPORT: RESOURCE-SNAPSHOT-005 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues

[Category("Godot")] // TEST-SUPPORT: RESOURCE-SNAPSHOT-006 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
public sealed class ResourceSnapshotTests // TEST-SUPPORT: RESOURCE-SNAPSHOT-007 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip, sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
{
    [Test] // TEST-SUPPORT: RESOURCE-SNAPSHOT-008 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
    public void DrainDowntimeAndMalformedStateRoundTrip() // TEST-SUPPORT: RESOURCE-SNAPSHOT-009 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
    {
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: RESOURCE-SNAPSHOT-010 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var host = new ShipRuntime(0, 0); // TEST-SUPPORT: RESOURCE-SNAPSHOT-011 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var stats = new ResolvedShipStats(200, 300, 4, 100, 200, 10, 20, 100, 1000, 1000, 5, 10, 100, true, true, true, true, true); // TEST-SUPPORT: RESOURCE-SNAPSHOT-012 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        host.ApplyEquipment(stats, 1); // TEST-SUPPORT: RESOURCE-SNAPSHOT-013 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        host.InitializeResources(); // TEST-SUPPORT: RESOURCE-SNAPSHOT-014 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        host.AdvanceResources(1); // TEST-SUPPORT: RESOURCE-SNAPSHOT-015 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(host.Shield.Current, Is.GreaterThan(0)); // TEST-SUPPORT: RESOURCE-SNAPSHOT-016 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(host.ShieldProtectionAvailable, Is.False); // TEST-SUPPORT: RESOURCE-SNAPSHOT-017 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var entity = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: RESOURCE-SNAPSHOT-018 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var rows = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) }; // TEST-SUPPORT: RESOURCE-SNAPSHOT-019 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(GameplaySnapshotCodec.TryDecode(rows, definitions, out var decoded), Is.True); // TEST-SUPPORT: RESOURCE-SNAPSHOT-020 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var entry = decoded[0]; // TEST-SUPPORT: RESOURCE-SNAPSHOT-021 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var client = new ShipRuntime(0, 0); // TEST-SUPPORT: RESOURCE-SNAPSHOT-022 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: RESOURCE-SNAPSHOT-023 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: RESOURCE-SNAPSHOT-024 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0), Is.True); // TEST-SUPPORT: RESOURCE-SNAPSHOT-025 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(client.Shield, Is.EqualTo(host.Shield)); // TEST-SUPPORT: RESOURCE-SNAPSHOT-026 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(client.Energy, Is.EqualTo(host.Energy)); // TEST-SUPPORT: RESOURCE-SNAPSHOT-027 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(client.MovementAvailable, Is.False); // TEST-SUPPORT: RESOURCE-SNAPSHOT-028 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        for (var tick = 2; tick <= 62; tick++) host.AdvanceResources(tick); // TEST-SUPPORT: RESOURCE-SNAPSHOT-029 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(host.Shield.Current, Is.Zero); // TEST-SUPPORT: RESOURCE-SNAPSHOT-030 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(host.ShieldDowntimeDeadline, Is.GreaterThan(host.AuthoritativeTick)); // TEST-SUPPORT: RESOURCE-SNAPSHOT-031 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out decoded), Is.True); // TEST-SUPPORT: RESOURCE-SNAPSHOT-032 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(decoded[0].ShieldDowntimeDeadline, Is.EqualTo(host.ShieldDowntimeDeadline)); // TEST-SUPPORT: RESOURCE-SNAPSHOT-033 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        foreach (var mode in new[] { "energy", "protection", "presence", "numeric", "revision", "condition", "rate" }) // TEST-SUPPORT: RESOURCE-SNAPSHOT-034 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        {
            var row = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host); // TEST-SUPPORT: RESOURCE-SNAPSHOT-035 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            if (mode == "energy") row[9] = 101d; // TEST-SUPPORT: RESOURCE-SNAPSHOT-036 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            else if (mode == "protection") row[14] = true; // TEST-SUPPORT: RESOURCE-SNAPSHOT-037 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            else if (mode == "presence") row[8] = 0; // TEST-SUPPORT: RESOURCE-SNAPSHOT-038 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            else if (mode == "revision") row[10] = -1L; // TEST-SUPPORT: RESOURCE-SNAPSHOT-039 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            else if (mode == "rate") { var rateValues = row[7].AsFloat64Array(); rateValues[8] = 1e-100; row[7] = rateValues; row[2] = double.MaxValue; } // TEST-SUPPORT: RESOURCE-SNAPSHOT-RATE-001 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            else if (mode == "condition") row[6] = new Godot.Collections.Array(); // TEST-SUPPORT: RESOURCE-SNAPSHOT-040 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            else // TEST-SUPPORT: RESOURCE-SNAPSHOT-041 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            {
                var values = row[7].AsFloat64Array(); // TEST-SUPPORT: RESOURCE-SNAPSHOT-042 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
                values[0] = double.PositiveInfinity; // TEST-SUPPORT: RESOURCE-SNAPSHOT-043 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
                row[7] = values; // TEST-SUPPORT: RESOURCE-SNAPSHOT-044 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            }
            Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host), row }, definitions, out var rejected), Is.False); // TEST-SUPPORT: RESOURCE-SNAPSHOT-045 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            Assert.That(rejected, Is.Empty); // TEST-SUPPORT: RESOURCE-SNAPSHOT-046 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        }
    }

    [Test] // TEST-SUPPORT: AFTERBURN-SNAPSHOT-002 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
    public void AfterburnerDemandAndStateRoundTripAndRejectMalformedValues() // TEST-SUPPORT: AFTERBURN-SNAPSHOT-003 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
    { // TEST-SUPPORT: AFTERBURN-SNAPSHOT-004 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-005 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        var stats = new ResolvedShipStats(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 8.5, 8, 0, true, false, false, true, false) { EngineEnergyConsumption = 4 }; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-006 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        var host = new ShipRuntime(); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-007 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        host.ApplyEquipment(stats, 0); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-008 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        host.SetAfterburner(true); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-009 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        host.AdvanceResources(1); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-010 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        var entity = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-011 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out var decoded), Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-012 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        var entry = decoded[0]; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-013 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(entry.AfterburnerActive, Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-014 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(entry.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-015 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(entry.EnergyDepleted, Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-016 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        var client = new ShipRuntime(); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-017 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: AFTERBURN-SNAPSHOT-018 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: AFTERBURN-SNAPSHOT-019 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0), Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-020 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.AfterburnerActive, Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-021 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.EnergyConsumption, Is.EqualTo(9)); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-022 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.EnergyDepleted, Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-023 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-024 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.Throws<ArgumentException>(() => new ShipRuntime().Confirm(entry.Revision, entry.Capacity, entry.Current, // TEST-SUPPORT: AFTERBURN-SNAPSHOT-025 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            entry.Behaviours, entry.FactionId, entry.Conditions, entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, // TEST-SUPPORT: AFTERBURN-SNAPSHOT-026 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, false, 0, 0)); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-027 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        foreach (var mode in new[] { "flagType", "flagDemand", "demand", "negative", "nonfinite", "noEngine", "overflow", "statsLength", "rowLength" }) // TEST-SUPPORT: AFTERBURN-SNAPSHOT-028 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        { // TEST-SUPPORT: AFTERBURN-SNAPSHOT-029 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            var row = GameplaySnapshotCodec.Encode(entity, host); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-030 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            var values = row[7].AsFloat64Array(); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-031 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            if (mode == "flagType") row[15] = 1; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-032 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "flagDemand") row[15] = false; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-033 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "demand") values[13] = 9; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-034 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "negative") values[13] = -1; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-035 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "nonfinite") values[13] = double.NaN; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-036 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "noEngine") { values[13] = 0; row[8] = 8; } // TEST-SUPPORT: AFTERBURN-SNAPSHOT-037 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "overflow") { values[11] = double.MaxValue; values[13] = double.MaxValue; } // TEST-SUPPORT: AFTERBURN-SNAPSHOT-038 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "statsLength") values = values[..13]; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-039 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            else if (mode == "rowLength") row.RemoveAt(15); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-040 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            row[7] = values; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-041 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, definitions, out var rejected), Is.False, mode); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-042 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            Assert.That(rejected, Is.Empty, mode); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-043 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        } // TEST-SUPPORT: AFTERBURN-SNAPSHOT-044 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        host.SetAfterburner(false); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-045 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        host.AdvanceResources(2); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-046 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out decoded), Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-047 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        entry = decoded[0]; // TEST-SUPPORT: AFTERBURN-SNAPSHOT-048 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: AFTERBURN-SNAPSHOT-049 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: AFTERBURN-SNAPSHOT-050 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0), Is.True); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-051 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.AfterburnerActive, Is.False); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-052 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.EnergyConsumption, Is.EqualTo(8)); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-053 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.EnergyDepleted, Is.False); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-054 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
        Assert.That(client.Stats, Is.EqualTo(stats)); // TEST-SUPPORT: AFTERBURN-SNAPSHOT-055 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
    } // TEST-SUPPORT: AFTERBURN-SNAPSHOT-056 | Tests: sMPGWM.Tests.ResourceSnapshotTests.AfterburnerDemandAndStateRoundTripAndRejectMalformedValues
}
