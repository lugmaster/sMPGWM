using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class AbilitySnapshotTests
{
    [Test]
    public void MultipleBehavioursRoundTripAndMalformedSnapshotsAreRejected()
    {
        var data = GameplayTestData.Load(variants: true);
        using var runtime = new AbilityRuntime(data);
        var host = new ShipRuntime(100, 40);
        host.Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "shieldVariant" });
        var context = new AbilityActivationContext(host, host, 0);
        runtime.Activate("shieldCapacityBoost", context, 0);
        runtime.Activate("shieldVariant", context, 0);
        var entity = Guid.NewGuid().ToString("N");
        var baseline = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) };
        Assert.That(GameplaySnapshotCodec.TryDecode(baseline, data, out var entries), Is.True);
        var late = new ShipRuntime(100, 40);
        var entry = entries[0];
        Assert.That(late.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0, entry.Components), Is.True);
        Assert.That(late.ActiveBehaviours, Is.EqualTo(host.ActiveBehaviours));
        Assert.That(late.Shield.Capacity, Is.EqualTo(175));
        Assert.That(late.Shield.Current, Is.EqualTo(40));
        Assert.That(late.ActiveBehaviours[1].Deadline, Is.EqualTo(600));
        host.Expire(600);
        var update = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host) };
        Assert.That(GameplaySnapshotCodec.TryDecode(update, data, out var updated), Is.True);
        Assert.That(late.Confirm(updated[0].Revision, updated[0].Capacity, updated[0].Current, updated[0].Behaviours, updated[0].FactionId, updated[0].Conditions, updated[0].Stats, updated[0].EnergyCurrent, updated[0].EnergyDepleted, updated[0].ShieldProtectionAvailable, updated[0].ShieldDowntimeDeadline, updated[0].AuthoritativeTick, updated[0].ConfigurationRevision, updated[0].AfterburnerActive, 0, 0, updated[0].Components), Is.True);
        Assert.That(late.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0, entry.Components), Is.False);
        Assert.That(late.ActiveBehaviours.Count, Is.EqualTo(1));
        Assert.That(late.Shield.Capacity, Is.EqualTo(150));

        var invalid = new Godot.Collections.Array { GameplaySnapshotCodec.Encode(entity, host), GameplaySnapshotCodec.Encode(entity, host) };
        Assert.That(GameplaySnapshotCodec.TryDecode(invalid, data, out var rejected), Is.False);
        Assert.That(rejected, Is.Empty);
        foreach (var mode in new[] { "unknown", "duplicateDefinition", "duplicateInstance", "negativeDeadline", "nan", "tooMany" })
        {
            var row = GameplaySnapshotCodec.Encode(entity, host);
            var behaviours = row[4].AsGodotArray();
            if (mode == "unknown") behaviours[0].AsGodotArray()[0] = "missing";
            else if (mode == "duplicateDefinition") behaviours.Add(new Godot.Collections.Array { "shieldCapacityBoost", 999L, 1800L });
            else if (mode == "duplicateInstance") behaviours.Add(new Godot.Collections.Array { "shieldVariant", host.ActiveBehaviours[0].RuntimeId, 1800L });
            else if (mode == "negativeDeadline") behaviours[0].AsGodotArray()[2] = -1L;
            else if (mode == "nan") row[2] = double.NaN;
            else for (var i = 0; i <= ShipRuntime.MaxBehaviours; i++) behaviours.Add(new Godot.Collections.Array());
            Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, data, out rejected), Is.False, mode);
            Assert.That(rejected, Is.Empty);
        }
        var tenShips = new Godot.Collections.Array();
        for (var i = 0; i < 10; i++)
        {
            var state = new ShipRuntime(100, 40);
            state.Grants.SetSource("fixture", new[] { "shieldCapacityBoost", "shieldVariant" });
            runtime.Activate("shieldCapacityBoost", new(state, state, 0), 0);
            runtime.Activate("shieldVariant", new(state, state, 0), 0);
            tenShips.Add(GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), state));
        }
        Assert.That(GameplaySnapshotCodec.TryDecode(tenShips, data, out entries), Is.True);
        Assert.That(entries.Length, Is.EqualTo(10));
        var bytes = GD.VarToBytes(tenShips).Length;
        TestContext.Out.WriteLine($"Gameplay snapshot: ten ships, two behaviours each, {bytes} bytes.");
        Assert.That(bytes, Is.LessThan(4096));
    }
}
