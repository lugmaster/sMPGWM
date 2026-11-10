using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Tests; // TEST-SUPPORT: ABILITY-SNAPSHOT-001 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected

[Category("Godot")] // TEST-SUPPORT: ABILITY-SNAPSHOT-002 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
public sealed class AbilitySnapshotTests // TEST-SUPPORT: ABILITY-SNAPSHOT-003 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
{
    [Test] // TEST-SUPPORT: ABILITY-SNAPSHOT-004 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
    public void MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected() // TEST-SUPPORT: ABILITY-SNAPSHOT-005 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
    {
        var data = GameplayTestData.Load(variants: true); // TEST-SUPPORT: ABILITY-SNAPSHOT-006 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        using var runtime = new AbilityRuntime(data); // TEST-SUPPORT: ABILITY-SNAPSHOT-007 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var host = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-SNAPSHOT-008 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        host.Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "shieldVariant" }); // TEST-SUPPORT: ABILITY-SNAPSHOT-009 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var context = new AbilityActivationContext(host, host, true, 0, Array.Empty<string>()); // TEST-SUPPORT: ABILITY-SNAPSHOT-010 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        runtime.Activate("shieldCapacityBoost", context, 0); // TEST-SUPPORT: ABILITY-SNAPSHOT-011 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        runtime.Activate("shieldVariant", context, 0); // TEST-SUPPORT: ABILITY-SNAPSHOT-012 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var entity = Guid.NewGuid().ToString("N"); // TEST-SUPPORT: ABILITY-SNAPSHOT-013 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var baseline = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) }; // TEST-SUPPORT: ABILITY-SNAPSHOT-014 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(GameplaySnapshotCodec.TryDecode(baseline, data, out var entries), Is.True); // TEST-SUPPORT: ABILITY-SNAPSHOT-015 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var late = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-SNAPSHOT-016 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var entry = entries[0]; // TEST-SUPPORT: ABILITY-SNAPSHOT-017 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours), Is.True); // TEST-SUPPORT: ABILITY-SNAPSHOT-018 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.ActiveBehaviours, Is.EqualTo(host.ActiveBehaviours)); // TEST-SUPPORT: ABILITY-SNAPSHOT-019 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.Shield.Capacity, Is.EqualTo(175)); // TEST-SUPPORT: ABILITY-SNAPSHOT-020 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: ABILITY-SNAPSHOT-021 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.ActiveBehaviours[1].Deadline, Is.EqualTo(600)); // TEST-SUPPORT: ABILITY-SNAPSHOT-022 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        host.Expire(600); // TEST-SUPPORT: ABILITY-SNAPSHOT-023 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var update = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) }; // TEST-SUPPORT: ABILITY-SNAPSHOT-024 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(GameplaySnapshotCodec.TryDecode(update, data, out var updated), Is.True); // TEST-SUPPORT: ABILITY-SNAPSHOT-025 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.Confirm(updated[0].Revision, updated[0].Capacity, updated[0].Current, updated[0].Behaviours), Is.True); // TEST-SUPPORT: ABILITY-SNAPSHOT-026 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours), Is.False); // TEST-SUPPORT: ABILITY-SNAPSHOT-027 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.ActiveBehaviours.Count, Is.EqualTo(1)); // TEST-SUPPORT: ABILITY-SNAPSHOT-028 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(late.Shield.Capacity, Is.EqualTo(150)); // TEST-SUPPORT: ABILITY-SNAPSHOT-029 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected

        var invalid = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host), GameplaySnapshotCodec.Encode(entity, host) }; // TEST-SUPPORT: ABILITY-SNAPSHOT-030 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(GameplaySnapshotCodec.TryDecode(invalid, data, out var rejected), Is.False); // TEST-SUPPORT: ABILITY-SNAPSHOT-031 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(rejected, Is.Empty); // TEST-SUPPORT: ABILITY-SNAPSHOT-032 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        foreach (var mode in new[] { "unknown", "duplicateDefinition", "duplicateInstance", "negativeDeadline", "nan", "tooMany" }) // TEST-SUPPORT: ABILITY-SNAPSHOT-033 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        {
            var row = GameplaySnapshotCodec.Encode(entity, host); // TEST-SUPPORT: ABILITY-SNAPSHOT-034 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            var behaviours = row[4].AsGodotArray(); // TEST-SUPPORT: ABILITY-SNAPSHOT-035 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            if (mode == "unknown") behaviours[0].AsGodotArray()[0] = "missing"; // TEST-SUPPORT: ABILITY-SNAPSHOT-036 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            else if (mode == "duplicateDefinition") behaviours.Add(new Godot.Collections.Array { "shieldCapacityBoost", 999L, 1800L }); // TEST-SUPPORT: ABILITY-SNAPSHOT-037 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            else if (mode == "duplicateInstance") behaviours.Add(new Godot.Collections.Array { "shieldVariant", host.ActiveBehaviours[0].RuntimeId, 1800L }); // TEST-SUPPORT: ABILITY-SNAPSHOT-038 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            else if (mode == "negativeDeadline") behaviours[0].AsGodotArray()[2] = -1L; // TEST-SUPPORT: ABILITY-SNAPSHOT-039 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            else if (mode == "nan") row[2] = double.NaN; // TEST-SUPPORT: ABILITY-SNAPSHOT-040 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            else for (var i = 0; i <= ShipGameplayState.MaxBehaviours; i++) behaviours.Add(new Godot.Collections.Array()); // TEST-SUPPORT: ABILITY-SNAPSHOT-041 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, data, out rejected), Is.False, mode); // TEST-SUPPORT: ABILITY-SNAPSHOT-042 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            Assert.That(rejected, Is.Empty); // TEST-SUPPORT: ABILITY-SNAPSHOT-043 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        }
        var tenShips = new Godot.Collections.Array(); // TEST-SUPPORT: ABILITY-SNAPSHOT-044 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        for (var i = 0; i < 10; i++) // TEST-SUPPORT: ABILITY-SNAPSHOT-045 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        {
            var state = new ShipGameplayState(100, 40); // TEST-SUPPORT: ABILITY-SNAPSHOT-046 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            state.Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "shieldVariant" }); // TEST-SUPPORT: ABILITY-SNAPSHOT-047 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            runtime.Activate("shieldCapacityBoost", new(state, state, true, 0, Array.Empty<string>()), 0); // TEST-SUPPORT: ABILITY-SNAPSHOT-048 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            runtime.Activate("shieldVariant", new(state, state, true, 0, Array.Empty<string>()), 0); // TEST-SUPPORT: ABILITY-SNAPSHOT-049 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
            tenShips.Add(GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), state)); // TEST-SUPPORT: ABILITY-SNAPSHOT-050 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        }
        Assert.That(GameplaySnapshotCodec.TryDecode(tenShips, data, out entries), Is.True); // TEST-SUPPORT: ABILITY-SNAPSHOT-051 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(entries.Length, Is.EqualTo(10)); // TEST-SUPPORT: ABILITY-SNAPSHOT-052 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        var bytes = GD.VarToBytes(tenShips).Length; // TEST-SUPPORT: ABILITY-SNAPSHOT-053 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        TestContext.Out.WriteLine($"Gameplay snapshot: ten ships, two behaviours each, {bytes} bytes."); // TEST-SUPPORT: ABILITY-SNAPSHOT-054 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
        Assert.That(bytes, Is.LessThan(4096)); // TEST-SUPPORT: ABILITY-SNAPSHOT-055 | Tests: sMPGWM.Tests.AbilitySnapshotTests.MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected
    }
}
