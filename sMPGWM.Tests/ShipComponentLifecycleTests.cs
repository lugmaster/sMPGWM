using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
namespace sMPGWM.Tests;

public sealed class ShipComponentLifecycleTests
{
    [Test]
    public void PowerAndShieldDetachPreserveAmountsAndHardware()
    {
        using var ship = new ShipRuntime();
        var inputs = new ResolvedShipStats(10, 20, 1, 10, 20, 1, 0, 100, 1000, 100, 120, 60, 100, true, false, true, true, true);
        ship.ApplyEquipment(inputs, 1);
        ship.InitializeResources();
        ship.SetShieldCurrent(50);
        ship.AdvanceResources(1);
        var energy = ship.Energy.Current;
        var shield = ship.Shield.Current;
        ship.StageComponentChange(ShipComponentKind.Power | ShipComponentKind.Shield, false);
        Assert.That(ship.PoweredSystemsAvailable, Is.True);
        ship.CommitConfiguration(2);
        ship.AdvanceResources(2);
        Assert.That(ship.PoweredSystemsAvailable, Is.False);
        Assert.That(ship.MovementAvailable, Is.False);
        Assert.That(ship.ShieldProtectionAvailable, Is.False);
        Assert.That(ship.Energy.Current, Is.EqualTo(energy));
        Assert.That(ship.Shield.Current, Is.EqualTo(shield));
        Assert.That(ship.Stats, Is.EqualTo(inputs));
        ship.StageComponentChange(ShipComponentKind.Power | ShipComponentKind.Shield, true);
        ship.CommitConfiguration(3);
        Assert.That(ship.Energy.Current, Is.EqualTo(energy));
        Assert.That(ship.Shield.Current, Is.EqualTo(shield));
        Assert.That(ship.MovementAvailable, Is.True);
    }

    [Test]
    public void EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent()
    {
        using var abilities = new AbilityRuntime(GameplayTestData.Load());
        using var ship = new ShipRuntime(100, 40);
        ship.Grants.SetSource("item", new[] { "shieldCapacityBoost" });
        Assert.That(abilities.Activate("shieldCapacityBoost", new(ship, ship, 0), 1), Is.EqualTo(AbilityActivationResult.Accepted));
        var deadline = ship.ActiveBehaviours[0].Deadline;
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(150));
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(ship.Shield.Capacity));
        ship.StageComponentChange(ShipComponentKind.AbilityAdmission | ShipComponentKind.Effects, false);
        ship.CommitConfiguration(2);
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(100));
        Assert.That(ship.ActiveBehaviours.Count, Is.EqualTo(1));
        Assert.That(abilities.Activate("shieldCapacityBoost", new(ship, ship, 0), 3), Is.EqualTo(AbilityActivationResult.AbilityNotGranted));
        ship.StageComponentChange(ShipComponentKind.Effects, true);
        ship.CommitConfiguration(deadline);
        Assert.That(ship.ActiveBehaviours, Is.Empty);
        Assert.That(ship.Shield.Capacity, Is.EqualTo(100));
        Assert.That(ship.Shield.Current, Is.EqualTo(40));
        Assert.That(ship.Grants.Contains("shieldCapacityBoost"), Is.True);
    }

    [Test]
    public void RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState()
    {
        using var ship = new ShipRuntime(100, 40);
        var notifications = 0;
        ship.Changed += () => notifications++;
        ship.StageComponentChange(ShipComponentKind.Shield, false);
        ship.CommitConfiguration(1);
        var revision = ship.Revision;
        Assert.That(ship.StageComponentChange(ShipComponentKind.Shield, false), Is.False);
        ship.CommitConfiguration(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.StageComponentChange((ShipComponentKind)128, true));
        Assert.That(ship.Revision, Is.EqualTo(revision));
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(ship.Shield.Current, Is.EqualTo(40));
        ship.Dispose();
        ship.Dispose();
        var disposedRevision = ship.Revision;
        Assert.That(ship.AdvanceResources(3), Is.False);
        Assert.That(ship.Expire(3), Is.False);
        Assert.That(ship.Revision, Is.EqualTo(disposedRevision));
        Assert.That(ship.Components.Presence, Is.EqualTo(ShipComponentKind.None));
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void ReattachedShieldRetainsAbsoluteDowntime()
    {
        using var ship = new ShipRuntime();
        ship.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 1000, 1000, 0, 0, 0, false, false, true, false, false), 0);
        ship.InitializeResources();
        ship.SetShieldCurrent(0);
        var deadline = ship.ShieldDowntimeDeadline;
        ship.StageComponentChange(ShipComponentKind.Shield, false);
        ship.CommitConfiguration(1);
        ship.AdvanceResources(1);
        ship.StageComponentChange(ShipComponentKind.Shield, true);
        ship.CommitConfiguration(2);
        Assert.That(ship.ShieldDowntimeDeadline, Is.EqualTo(deadline));
        ship.AdvanceResources(2);
        Assert.That(ship.Shield.Current, Is.Zero);
        ship.AdvanceResources(deadline);
        Assert.That(ship.Shield.Current, Is.GreaterThan(0));
    }

    [Test, Category("Godot")]
    public void DetachedComponentsAndEffectiveStatsRoundTrip()
    {
        using var ship = new ShipRuntime(100, 40);
        ship.StageComponentChange(ShipComponentKind.Power | ShipComponentKind.AbilityAdmission, false);
        ship.CommitConfiguration(1);
        var definitions = GameplayTestData.Load();
        var row = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), ship);
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, definitions, out var entries), Is.True);
        var entry = entries[0];
        using var replica = new ShipRuntime();
        Assert.That(replica.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline,
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, 0, 0, entry.Components), Is.True);
        Assert.That(replica.PoweredSystemsAvailable, Is.False);
        Assert.That(replica.AbilityAdmissionAvailable, Is.False);
        Assert.That(replica.Stats.ShieldCapacity, Is.EqualTo(replica.Shield.Capacity));
        row[14] = true;
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, definitions, out _), Is.False);
    }
    [Test]
    public void InvalidReattachmentLeavesSuppressedStateIntact()
    {
        using var ship = new ShipRuntime(double.MaxValue / 2, 40);
        var definitions = GameplayTestData.Load();
        Assert.That(definitions.Abilities.TryGetPlan("shieldCapacityBoost", out var plan), Is.True);
        ship.StageComponentChange(ShipComponentKind.Effects, false);
        ship.CommitConfiguration(1);
        Assert.That(ship.ApplyBehaviour(plan.Behaviour with { Flat = double.MaxValue }, 1, 1, out _), Is.True);
        var revision = ship.Revision;
        var capacity = ship.Stats.ShieldCapacity;
        Assert.Throws<InvalidOperationException>(() => ship.StageComponentChange(ShipComponentKind.Effects, true));
        Assert.That(ship.Components.Effects, Is.Null);
        Assert.That(ship.Revision, Is.EqualTo(revision));
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(capacity));
        Assert.That(ship.Shield.Current, Is.EqualTo(40));
        ship.CommitConfiguration(2);
        Assert.That(ship.Components.Effects, Is.Null);
        Assert.That(ship.Revision, Is.EqualTo(revision));
    }
}
