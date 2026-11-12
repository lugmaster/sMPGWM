using sMPGWM.Scripts.NetworkSystem; // TEST-SUPPORT: HEALTH-SNAPSHOT-001 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: HEALTH-SNAPSHOT-002 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically

namespace sMPGWM.Tests; // TEST-SUPPORT: HEALTH-SNAPSHOT-003 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically

[Category("Godot")] // TEST-SUPPORT: HEALTH-SNAPSHOT-004 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
public sealed class HealthSnapshotTests // TEST-SUPPORT: HEALTH-SNAPSHOT-005 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
{ // TEST-SUPPORT: HEALTH-SNAPSHOT-006 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
    [Test] // TEST-SUPPORT: HEALTH-SNAPSHOT-007 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering, sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
    public void HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering() // TEST-SUPPORT: HEALTH-SNAPSHOT-008 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
    { // TEST-SUPPORT: HEALTH-SNAPSHOT-009 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: HEALTH-SNAPSHOT-010 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        using var host = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-SNAPSHOT-011 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        using var client = new ShipRuntime(); // TEST-SUPPORT: HEALTH-SNAPSHOT-012 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        host.InitializeResources(); // TEST-SUPPORT: HEALTH-SNAPSHOT-013 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        host.SetHealthCurrent(87); // TEST-SUPPORT: HEALTH-SNAPSHOT-014 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        var entity = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: HEALTH-SNAPSHOT-015 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out var entries), Is.True); // TEST-SUPPORT: HEALTH-SNAPSHOT-016 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        var entry = entries[0]; // TEST-SUPPORT: HEALTH-SNAPSHOT-017 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(entry.HealthCapacity, Is.EqualTo(150)); // TEST-SUPPORT: HEALTH-SNAPSHOT-018 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(entry.HealthCurrent, Is.EqualTo(87)); // TEST-SUPPORT: HEALTH-SNAPSHOT-019 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: HEALTH-SNAPSHOT-020 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: HEALTH-SNAPSHOT-021 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, entry.HealthCapacity, entry.HealthCurrent, entry.Components), Is.True); // TEST-SUPPORT: HEALTH-SNAPSHOT-022 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Health, Is.EqualTo(host.Health)); // TEST-SUPPORT: HEALTH-SNAPSHOT-023 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        host.SetHealthCurrent(0); // TEST-SUPPORT: HEALTH-SNAPSHOT-024 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { GameplaySnapshotCodec.Encode(entity, host) }, definitions, out entries), Is.True); // TEST-SUPPORT: HEALTH-SNAPSHOT-025 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        var depleted = entries[0]; // TEST-SUPPORT: HEALTH-SNAPSHOT-026 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Confirm(depleted.Revision, depleted.Capacity, depleted.Current, depleted.Behaviours, depleted.FactionId, depleted.Conditions, // TEST-SUPPORT: HEALTH-SNAPSHOT-027 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
            depleted.Stats, depleted.EnergyCurrent, depleted.EnergyDepleted, depleted.ShieldProtectionAvailable, depleted.ShieldDowntimeDeadline, // TEST-SUPPORT: HEALTH-SNAPSHOT-028 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
            depleted.AuthoritativeTick, depleted.ConfigurationRevision, depleted.AfterburnerActive, depleted.HealthCapacity, depleted.HealthCurrent, depleted.Components), Is.True); // TEST-SUPPORT: HEALTH-SNAPSHOT-029 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Health.Current, Is.Zero); // TEST-SUPPORT: HEALTH-SNAPSHOT-030 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Health.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: HEALTH-SNAPSHOT-031 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: HEALTH-SNAPSHOT-032 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: HEALTH-SNAPSHOT-033 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, entry.HealthCapacity, entry.HealthCurrent, entry.Components), Is.False); // TEST-SUPPORT: HEALTH-SNAPSHOT-034 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
        Assert.That(client.Health.Current, Is.Zero); // TEST-SUPPORT: HEALTH-SNAPSHOT-035 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering
    } // TEST-SUPPORT: HEALTH-SNAPSHOT-036 | Tests: sMPGWM.Tests.HealthSnapshotTests.HealthRoundTripPreservesCapacityCurrentAndRevisionOrdering

    [Test] // TEST-SUPPORT: HEALTH-SNAPSHOT-037 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
    public void MalformedHealthSnapshotsAreRejectedAtomically() // TEST-SUPPORT: HEALTH-SNAPSHOT-038 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
    { // TEST-SUPPORT: HEALTH-SNAPSHOT-039 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: HEALTH-SNAPSHOT-040 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
        using var host = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-SNAPSHOT-041 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
        host.InitializeResources(); // TEST-SUPPORT: HEALTH-SNAPSHOT-042 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
        foreach (var mode in new[] { "capacityType", "currentType", "negativeCapacity", "negativeCurrent", "capacityNaN", "currentNaN", "infiniteCapacity", "infiniteCurrent", "overCapacity", "missing" }) // TEST-SUPPORT: HEALTH-SNAPSHOT-043 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
        { // TEST-SUPPORT: HEALTH-SNAPSHOT-044 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            var row = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host); // TEST-SUPPORT: HEALTH-SNAPSHOT-045 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            if (mode == "capacityType") row[17] = 150; // TEST-SUPPORT: HEALTH-SNAPSHOT-046 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "currentType") row[18] = "150"; // TEST-SUPPORT: HEALTH-SNAPSHOT-047 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "negativeCapacity") row[17] = -1d; // TEST-SUPPORT: HEALTH-SNAPSHOT-048 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "negativeCurrent") row[18] = -1d; // TEST-SUPPORT: HEALTH-SNAPSHOT-049 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "capacityNaN") row[17] = double.NaN; // TEST-SUPPORT: HEALTH-SNAPSHOT-050 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "currentNaN") row[18] = double.NaN; // TEST-SUPPORT: HEALTH-SNAPSHOT-051 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "infiniteCapacity") row[17] = double.PositiveInfinity; // TEST-SUPPORT: HEALTH-SNAPSHOT-052 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "infiniteCurrent") row[18] = double.PositiveInfinity; // TEST-SUPPORT: HEALTH-SNAPSHOT-053 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else if (mode == "overCapacity") row[18] = 151d; // TEST-SUPPORT: HEALTH-SNAPSHOT-054 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            else row.RemoveAt(18); // TEST-SUPPORT: HEALTH-SNAPSHOT-055 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            var rows = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), host), row }; // TEST-SUPPORT: HEALTH-SNAPSHOT-056 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            Assert.That(GameplaySnapshotCodec.TryDecode(rows, definitions, out var rejected), Is.False, mode); // TEST-SUPPORT: HEALTH-SNAPSHOT-057 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
            Assert.That(rejected, Is.Empty, mode); // TEST-SUPPORT: HEALTH-SNAPSHOT-058 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
        } // TEST-SUPPORT: HEALTH-SNAPSHOT-059 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
    } // TEST-SUPPORT: HEALTH-SNAPSHOT-060 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically
} // TEST-SUPPORT: HEALTH-SNAPSHOT-061 | Tests: sMPGWM.Tests.HealthSnapshotTests.MalformedHealthSnapshotsAreRejectedAtomically


