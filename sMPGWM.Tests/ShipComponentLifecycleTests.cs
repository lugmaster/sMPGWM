using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: COMPONENT-LIFECYCLE-000 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: COMPONENT-LIFECYCLE-001 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: COMPONENT-LIFECYCLE-002 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: COMPONENT-LIFECYCLE-003 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
namespace sMPGWM.Tests; // TEST-SUPPORT: COMPONENT-LIFECYCLE-004 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware

public sealed class ShipComponentLifecycleTests // TEST-SUPPORT: COMPONENT-LIFECYCLE-006 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
{ // TEST-SUPPORT: COMPONENT-LIFECYCLE-007 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
    [Test] // TEST-SUPPORT: COMPONENT-LIFECYCLE-008 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
    public void PowerAndShieldDetachPreserveAmountsAndHardware() // TEST-SUPPORT: COMPONENT-LIFECYCLE-009 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
    { // TEST-SUPPORT: COMPONENT-LIFECYCLE-010 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        using var ship = new ShipRuntime(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-011 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        var inputs = new ResolvedShipStats(10, 20, 1, 10, 20, 1, 0, 100, 1000, 100, 120, 60, 100, true, false, true, true, true); // TEST-SUPPORT: COMPONENT-LIFECYCLE-012 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.ApplyEquipment(inputs, 1); // TEST-SUPPORT: COMPONENT-LIFECYCLE-013 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.InitializeResources(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-014 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.SetShieldCurrent(50); // TEST-SUPPORT: COMPONENT-LIFECYCLE-015 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.AdvanceResources(1); // TEST-SUPPORT: COMPONENT-LIFECYCLE-016 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        var energy = ship.Energy.Current; // TEST-SUPPORT: COMPONENT-LIFECYCLE-017 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        var shield = ship.Shield.Current; // TEST-SUPPORT: COMPONENT-LIFECYCLE-018 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.StageComponentChange(ShipComponentKind.Power | ShipComponentKind.Shield, false); // TEST-SUPPORT: COMPONENT-LIFECYCLE-019 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.PoweredSystemsAvailable, Is.True); // TEST-SUPPORT: COMPONENT-LIFECYCLE-020 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.CommitConfiguration(2); // TEST-SUPPORT: COMPONENT-LIFECYCLE-021 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.AdvanceResources(2); // TEST-SUPPORT: COMPONENT-LIFECYCLE-022 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.PoweredSystemsAvailable, Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-023 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.MovementAvailable, Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-024 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.ShieldProtectionAvailable, Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-025 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.Energy.Current, Is.EqualTo(energy)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-026 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.Shield.Current, Is.EqualTo(shield)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-027 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.Stats, Is.EqualTo(inputs)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-028 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.StageComponentChange(ShipComponentKind.Power | ShipComponentKind.Shield, true); // TEST-SUPPORT: COMPONENT-LIFECYCLE-029 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        ship.CommitConfiguration(3); // TEST-SUPPORT: COMPONENT-LIFECYCLE-030 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.Energy.Current, Is.EqualTo(energy)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-031 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.Shield.Current, Is.EqualTo(shield)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-032 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
        Assert.That(ship.MovementAvailable, Is.True); // TEST-SUPPORT: COMPONENT-LIFECYCLE-033 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
    } // TEST-SUPPORT: COMPONENT-LIFECYCLE-034 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware

    [Test] // TEST-SUPPORT: COMPONENT-LIFECYCLE-036 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.PowerAndShieldDetachPreserveAmountsAndHardware
    public void EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent() // TEST-SUPPORT: COMPONENT-LIFECYCLE-037 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
    { // TEST-SUPPORT: COMPONENT-LIFECYCLE-038 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        using var abilities = new AbilityRuntime(GameplayTestData.Load()); // TEST-SUPPORT: COMPONENT-LIFECYCLE-039 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        using var ship = new ShipRuntime(100, 40); // TEST-SUPPORT: COMPONENT-LIFECYCLE-040 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        ship.Grants.SetSource("item", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: COMPONENT-LIFECYCLE-041 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(abilities.Activate("shieldCapacityBoost", new(ship, ship, 0), 1), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-042 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        var deadline = ship.ActiveBehaviours[0].Deadline; // TEST-SUPPORT: COMPONENT-LIFECYCLE-043 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(150)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-044 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(ship.Shield.Capacity)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-045 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        ship.StageComponentChange(ShipComponentKind.AbilityAdmission | ShipComponentKind.Effects, false); // TEST-SUPPORT: COMPONENT-LIFECYCLE-046 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        ship.CommitConfiguration(2); // TEST-SUPPORT: COMPONENT-LIFECYCLE-047 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(100)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-048 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.ActiveBehaviours.Count, Is.EqualTo(1)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-049 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(abilities.Activate("shieldCapacityBoost", new(ship, ship, 0), 3), Is.EqualTo(AbilityActivationResult.AbilityNotGranted)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-050 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        ship.StageComponentChange(ShipComponentKind.Effects, true); // TEST-SUPPORT: COMPONENT-LIFECYCLE-051 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        ship.CommitConfiguration(deadline); // TEST-SUPPORT: COMPONENT-LIFECYCLE-052 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.ActiveBehaviours, Is.Empty); // TEST-SUPPORT: COMPONENT-LIFECYCLE-053 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.Shield.Capacity, Is.EqualTo(100)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-054 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-055 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
        Assert.That(ship.Grants.Contains("shieldCapacityBoost"), Is.True); // TEST-SUPPORT: COMPONENT-LIFECYCLE-056 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
    } // TEST-SUPPORT: COMPONENT-LIFECYCLE-057 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent

    [Test] // TEST-SUPPORT: COMPONENT-LIFECYCLE-059 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.EffectDeadlineContinuesWhileAdmissionAndProcessingAreAbsent
    public void RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState() // TEST-SUPPORT: COMPONENT-LIFECYCLE-060 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
    { // TEST-SUPPORT: COMPONENT-LIFECYCLE-061 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        using var ship = new ShipRuntime(100, 40); // TEST-SUPPORT: COMPONENT-LIFECYCLE-062 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        var notifications = 0; // TEST-SUPPORT: COMPONENT-LIFECYCLE-063 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        ship.Changed += () => notifications++; // TEST-SUPPORT: COMPONENT-LIFECYCLE-064 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        ship.StageComponentChange(ShipComponentKind.Shield, false); // TEST-SUPPORT: COMPONENT-LIFECYCLE-065 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        ship.CommitConfiguration(1); // TEST-SUPPORT: COMPONENT-LIFECYCLE-066 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        var revision = ship.Revision; // TEST-SUPPORT: COMPONENT-LIFECYCLE-067 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.StageComponentChange(ShipComponentKind.Shield, false), Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-068 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        ship.CommitConfiguration(2); // TEST-SUPPORT: COMPONENT-LIFECYCLE-069 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.StageComponentChange((ShipComponentKind)128, true)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-070 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-071 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(notifications, Is.EqualTo(1)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-072 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-073 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        ship.Dispose(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-074 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        ship.Dispose(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-075 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        var disposedRevision = ship.Revision; // TEST-SUPPORT: COMPONENT-DISPOSE-001 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.AdvanceResources(3), Is.False); // TEST-SUPPORT: COMPONENT-DISPOSE-002 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.Expire(3), Is.False); // TEST-SUPPORT: COMPONENT-DISPOSE-003 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.Revision, Is.EqualTo(disposedRevision)); // TEST-SUPPORT: COMPONENT-DISPOSE-004 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(ship.Components.Presence, Is.EqualTo(ShipComponentKind.None)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-076 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
        Assert.That(notifications, Is.EqualTo(1)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-077 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
    } // TEST-SUPPORT: COMPONENT-LIFECYCLE-078 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState

    [Test] // TEST-SUPPORT: COMPONENT-LIFECYCLE-080 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.RepeatedDetachAndInvalidAttachmentDoNotPublishOrChangeState
    public void ReattachedShieldRetainsAbsoluteDowntime() // TEST-SUPPORT: COMPONENT-LIFECYCLE-081 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
    { // TEST-SUPPORT: COMPONENT-LIFECYCLE-082 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        using var ship = new ShipRuntime(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-083 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 1000, 1000, 0, 0, 0, false, false, true, false, false), 0); // TEST-SUPPORT: COMPONENT-LIFECYCLE-084 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.InitializeResources(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-085 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.SetShieldCurrent(0); // TEST-SUPPORT: COMPONENT-LIFECYCLE-086 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        var deadline = ship.ShieldDowntimeDeadline; // TEST-SUPPORT: COMPONENT-LIFECYCLE-087 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.StageComponentChange(ShipComponentKind.Shield, false); // TEST-SUPPORT: COMPONENT-LIFECYCLE-088 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.CommitConfiguration(1); // TEST-SUPPORT: COMPONENT-LIFECYCLE-089 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.AdvanceResources(1); // TEST-SUPPORT: COMPONENT-LIFECYCLE-090 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.StageComponentChange(ShipComponentKind.Shield, true); // TEST-SUPPORT: COMPONENT-LIFECYCLE-091 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.CommitConfiguration(2); // TEST-SUPPORT: COMPONENT-LIFECYCLE-092 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        Assert.That(ship.ShieldDowntimeDeadline, Is.EqualTo(deadline)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-093 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.AdvanceResources(2); // TEST-SUPPORT: COMPONENT-LIFECYCLE-094 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        Assert.That(ship.Shield.Current, Is.Zero); // TEST-SUPPORT: COMPONENT-LIFECYCLE-095 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        ship.AdvanceResources(deadline); // TEST-SUPPORT: COMPONENT-LIFECYCLE-096 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
        Assert.That(ship.Shield.Current, Is.GreaterThan(0)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-097 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
    } // TEST-SUPPORT: COMPONENT-LIFECYCLE-098 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime

    [Test, Category("Godot")] // TEST-SUPPORT: COMPONENT-LIFECYCLE-100 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.ReattachedShieldRetainsAbsoluteDowntime
    public void DetachedComponentsAndEffectiveStatsRoundTrip() // TEST-SUPPORT: COMPONENT-LIFECYCLE-101 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
    { // TEST-SUPPORT: COMPONENT-LIFECYCLE-102 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        using var ship = new ShipRuntime(100, 40); // TEST-SUPPORT: COMPONENT-LIFECYCLE-103 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        ship.StageComponentChange(ShipComponentKind.Power | ShipComponentKind.AbilityAdmission, false); // TEST-SUPPORT: COMPONENT-LIFECYCLE-104 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        ship.CommitConfiguration(1); // TEST-SUPPORT: COMPONENT-LIFECYCLE-105 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-106 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        var row = GameplaySnapshotCodec.Encode(Guid.NewGuid().ToString("N"), ship); // TEST-SUPPORT: COMPONENT-LIFECYCLE-107 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, definitions, out var entries), Is.True); // TEST-SUPPORT: COMPONENT-LIFECYCLE-108 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        var entry = entries[0]; // TEST-SUPPORT: COMPONENT-LIFECYCLE-109 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        using var replica = new ShipRuntime(); // TEST-SUPPORT: COMPONENT-LIFECYCLE-110 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        Assert.That(replica.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions, // TEST-SUPPORT: COMPONENT-LIFECYCLE-111 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
            entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable, entry.ShieldDowntimeDeadline, // TEST-SUPPORT: COMPONENT-LIFECYCLE-112 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
            entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive, entry.Components), Is.True); // TEST-SUPPORT: COMPONENT-LIFECYCLE-113 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        Assert.That(replica.PoweredSystemsAvailable, Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-114 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        Assert.That(replica.AbilityAdmissionAvailable, Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-115 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        Assert.That(replica.Stats.ShieldCapacity, Is.EqualTo(replica.Shield.Capacity)); // TEST-SUPPORT: COMPONENT-LIFECYCLE-116 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        row[14] = true; // TEST-SUPPORT: COMPONENT-LIFECYCLE-117 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
        Assert.That(GameplaySnapshotCodec.TryDecode(new() { row }, definitions, out _), Is.False); // TEST-SUPPORT: COMPONENT-LIFECYCLE-118 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
    } // TEST-SUPPORT: COMPONENT-LIFECYCLE-119 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
    [Test] // TEST-SUPPORT: COMPONENT-ROLLBACK-000 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
    public void InvalidReattachmentLeavesSuppressedStateIntact() // TEST-SUPPORT: COMPONENT-ROLLBACK-001 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
    { // TEST-SUPPORT: COMPONENT-ROLLBACK-002 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        using var ship = new ShipRuntime(double.MaxValue / 2, 40); // TEST-SUPPORT: COMPONENT-ROLLBACK-003 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: COMPONENT-ROLLBACK-004 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(definitions.Abilities.TryGetPlan("shieldCapacityBoost", out var plan), Is.True); // TEST-SUPPORT: COMPONENT-ROLLBACK-005 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        ship.StageComponentChange(ShipComponentKind.Effects, false); // TEST-SUPPORT: COMPONENT-ROLLBACK-006 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        ship.CommitConfiguration(1); // TEST-SUPPORT: COMPONENT-ROLLBACK-007 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.ApplyBehaviour(plan.Behaviour with { Flat = double.MaxValue }, 1, 1, out _), Is.True); // TEST-SUPPORT: COMPONENT-ROLLBACK-008 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        var revision = ship.Revision; // TEST-SUPPORT: COMPONENT-ROLLBACK-009 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        var capacity = ship.Stats.ShieldCapacity; // TEST-SUPPORT: COMPONENT-ROLLBACK-010 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.Throws<InvalidOperationException>(() => ship.StageComponentChange(ShipComponentKind.Effects, true)); // TEST-SUPPORT: COMPONENT-ROLLBACK-011 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.Components.Effects, Is.Null); // TEST-SUPPORT: COMPONENT-ROLLBACK-012 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: COMPONENT-ROLLBACK-013 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.Stats.ShieldCapacity, Is.EqualTo(capacity)); // TEST-SUPPORT: COMPONENT-ROLLBACK-014 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.Shield.Current, Is.EqualTo(40)); // TEST-SUPPORT: COMPONENT-ROLLBACK-015 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        ship.CommitConfiguration(2); // TEST-SUPPORT: COMPONENT-ROLLBACK-016 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.Components.Effects, Is.Null); // TEST-SUPPORT: COMPONENT-ROLLBACK-017 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
        Assert.That(ship.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: COMPONENT-ROLLBACK-018 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
    } // TEST-SUPPORT: COMPONENT-ROLLBACK-019 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.InvalidReattachmentLeavesSuppressedStateIntact
} // TEST-SUPPORT: COMPONENT-LIFECYCLE-120 | Tests: sMPGWM.Tests.ShipComponentLifecycleTests.DetachedComponentsAndEffectiveStatsRoundTrip
