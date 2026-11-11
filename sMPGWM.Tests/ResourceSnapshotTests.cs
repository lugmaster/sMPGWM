using Godot; // TEST-SUPPORT: RESOURCE-SNAPSHOT-001 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: RESOURCE-SNAPSHOT-002 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: RESOURCE-SNAPSHOT-003 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: RESOURCE-SNAPSHOT-004 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip

namespace sMPGWM.Tests; // TEST-SUPPORT: RESOURCE-SNAPSHOT-005 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip

[Category("Godot")] // TEST-SUPPORT: RESOURCE-SNAPSHOT-006 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
public sealed class ResourceSnapshotTests // TEST-SUPPORT: RESOURCE-SNAPSHOT-007 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
{
    [Test] // TEST-SUPPORT: RESOURCE-SNAPSHOT-008 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
    public void DrainDowntimeAndMalformedStateRoundTrip() // TEST-SUPPORT: RESOURCE-SNAPSHOT-009 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
    {
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: RESOURCE-SNAPSHOT-010 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        var host = new ShipGameplayState(0, 0); // TEST-SUPPORT: RESOURCE-SNAPSHOT-011 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
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
        var client = new ShipGameplayState(0, 0); // TEST-SUPPORT: RESOURCE-SNAPSHOT-022 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: RESOURCE-SNAPSHOT-023 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: RESOURCE-SNAPSHOT-024 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
            entry.AuthoritativeTick, entry.ConfigurationRevision), Is.True); // TEST-SUPPORT: RESOURCE-SNAPSHOT-025 | Tests: sMPGWM.Tests.ResourceSnapshotTests.DrainDowntimeAndMalformedStateRoundTrip
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
}
