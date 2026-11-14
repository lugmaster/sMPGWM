using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class RestoredRuntimeTests
{
    [Test]
    public void RestoredRuntimePreservesResourcesEffectsAndTickContinuity()
    {
        var definitions = GameplayTestData.Load();
        using var abilities = new AbilityRuntime(definitions);
        using var source = new ShipRuntime();
        var inputs = new ResolvedShipStats(200, 300, 2, 100, 150, 10, 20, 100, 1000, 100, 120, 60, 100, true, true, true, true, true);
        source.ApplyEquipment(inputs, 23);
        source.InitializeResources();
        source.SetShieldCurrent(30);
        source.AdvanceResources(10);
        source.Grants.SetSource("player", new[] { "shieldCapacityBoost" });
        Assert.That(abilities.Activate("shieldCapacityBoost", new(source, source, 0), 10), Is.EqualTo(AbilityActivationResult.Accepted));
        source.StageComponentChange(ShipComponentKind.Effects, false);
        var saved = source.CaptureState();
        using var restored = new ShipRuntime();
        restored.ApplyEquipment(inputs, 23);
        restored.RestoreState(saved, definitions);
        Assert.That(restored.Shield, Is.EqualTo(source.Shield));
        Assert.That(restored.Energy, Is.EqualTo(source.Energy));
        Assert.That(restored.Revision, Is.EqualTo(source.Revision));
        Assert.That(restored.ConfigurationRevision, Is.EqualTo(source.ConfigurationRevision));
        Assert.That(restored.ActiveBehaviours, Is.EqualTo(source.ActiveBehaviours));
        Assert.That(restored.Grants.ContainsSource("player", "shieldCapacityBoost"), Is.True);
        Assert.That(restored.AdvanceResources(10), Is.False);
        restored.InitializeResources();
        Assert.That(restored.Shield.Current, Is.EqualTo(source.Shield.Current));
        Assert.That(restored.Energy.Current, Is.EqualTo(source.Energy.Current));
        source.CommitConfiguration(11);
        restored.CommitConfiguration(11);
        Assert.That(restored.Stats, Is.EqualTo(source.Stats));
        source.StageComponentChange(ShipComponentKind.Effects, true);
        restored.StageComponentChange(ShipComponentKind.Effects, true);
        source.CommitConfiguration(12);
        restored.CommitConfiguration(12);
        Assert.That(restored.Stats, Is.EqualTo(source.Stats));
        var deadline = source.ActiveBehaviours[0].Deadline;
        source.Expire(deadline);
        restored.Expire(deadline);
        Assert.That(restored.Stats, Is.EqualTo(source.Stats));
        Assert.That(restored.ActiveBehaviours, Is.Empty);
        Assert.That(restored.Shield.Capacity, Is.EqualTo(inputs.ShieldCapacity));
    }

    [TestCase("resources")]
    [TestCase("behaviour")]
    [TestCase("grants")]
    [TestCase("equipment")]
    public void RejectedStateDoesNotMutatePreparedRuntime(string invalid)
    {
        var definitions = GameplayTestData.Load();
        using var source = new ShipRuntime(100, 0);
        source.InitializeResources();
        var saved = source.CaptureState();
        saved = invalid switch
        {
            "resources" => saved with { ShieldCurrent = double.NaN },
            "behaviour" => saved with { Behaviours = new[] { new BehaviourInstance("missing", 1, 20) } },
            "grants" => saved with { Grants = new[] { new AbilityGrantState(null, new(StatContributionKind.Equipment, 5), new[] { "shieldCapacityBoost" }) } },
            _ => saved with { BatteryIdentity = 1 }
        };
        using var target = new ShipRuntime(100, 12);
        var before = target.Shield;
        var revision = target.Revision;
        Assert.Throws<ArgumentException>(() => target.RestoreState(saved, definitions));
        Assert.That(target.Shield, Is.EqualTo(before));
        Assert.That(target.Revision, Is.EqualTo(revision));
        Assert.That(target.ActiveBehaviours, Is.Empty);
        Assert.That(target.Grants.Contains("shieldCapacityBoost"), Is.False);
        target.RestoreState(source.CaptureState(), definitions);
        Assert.That(target.Shield, Is.EqualTo(source.Shield));
    }

    [Test]
    public void AbilitySequenceResumesBeyondExpiredInstances()
    {
        var definitions = GameplayTestData.Load();
        using var abilities = new AbilityRuntime(definitions);
        using var source = new ShipRuntime(100, 0);
        source.InitializeResources();
        source.Grants.SetSource("player", new[] { "shieldCapacityBoost" });
        Assert.That(abilities.Activate("shieldCapacityBoost", new(source, source, 0), 0), Is.EqualTo(AbilityActivationResult.Accepted));
        source.Expire(source.ActiveBehaviours[0].Deadline);
        using var resumed = new AbilityRuntime(definitions);
        resumed.RestoreSequence(abilities.CaptureSequence());
        Assert.That(resumed.Activate("shieldCapacityBoost", new(source, source, 0), 2000), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(source.ActiveBehaviours[0].RuntimeId, Is.EqualTo(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => resumed.RestoreSequence(0));
    }
}
