using Godot;
using sMPGWM.Scripts.MovementSystem;

namespace sMPGWM.Tests.MovementSystem; // TEST-SUPPORT: EQMOVE-1 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
 // TEST-SUPPORT: EQMOVE-2 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
public class EquipmentMovementTests // TEST-SUPPORT: EQMOVE-3 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
{ // TEST-SUPPORT: EQMOVE-4 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip, sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
    [Test] // TEST-SUPPORT: EQMOVE-5 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
    public void UnpoweredMovementPreservesVelocityDespiteEveryControl() // TEST-SUPPORT: EQMOVE-6 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
    { // TEST-SUPPORT: EQMOVE-7 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
        var velocity = new Vector2(300, -180); // TEST-SUPPORT: EQMOVE-8 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
        var result = MovementPhysics.MovementStep(velocity, Vector2.One, Vector2.Right, Vector2.Down, // TEST-SUPPORT: EQMOVE-9 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
            900, 50, .15f, 1f / 60, true, false); // TEST-SUPPORT: EQMOVE-10 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
        Assert.That(result, Is.EqualTo(velocity)); // TEST-SUPPORT: EQMOVE-11 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
        var slow = new Vector2(1, 2); // TEST-SUPPORT: EQMOVE-12 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
        Assert.That(MovementPhysics.MovementStep(slow, Vector2.Zero, Vector2.Right, Vector2.Down, // TEST-SUPPORT: EQMOVE-13 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
            900, 0, .15f, 1f / 60, true, false), Is.EqualTo(slow)); // TEST-SUPPORT: EQMOVE-14 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
    } // TEST-SUPPORT: EQMOVE-15 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.UnpoweredMovementPreservesVelocityDespiteEveryControl
    [Test] // TEST-SUPPORT: EQMOVE-16 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
    public void TenConfiguredShipsFitAndRoundTrip() // TEST-SUPPORT: EQMOVE-17 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
    { // TEST-SUPPORT: EQMOVE-18 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        var samples = new MovementSample[10]; // TEST-SUPPORT: EQMOVE-19 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        for (var i = 0; i < samples.Length; i++) // TEST-SUPPORT: EQMOVE-20 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
            samples[i] = new(i + 1, Guid.NewGuid().ToString("N"), 1, 0, new Vector2(i, i), .5f, // TEST-SUPPORT: EQMOVE-21 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
                Vector2.One, default, 20, 0, 7, new(350, 600, 3, 2, 4, 9, i % 2 == 0)); // TEST-SUPPORT: EQMOVE-22 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        var bytes = MovementWire.Snapshot(Guid.NewGuid(), 20, samples); // TEST-SUPPORT: EQMOVE-23 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        Assert.That(bytes.Length, Is.EqualTo(1135)); // TEST-SUPPORT: EQMOVE-24 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        Assert.That(bytes.Length, Is.LessThanOrEqualTo(MovementWire.MaxBytes)); // TEST-SUPPORT: EQMOVE-25 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        Assert.That(MovementWire.ReadSnapshot(bytes, out _, out _, out var decoded), Is.True); // TEST-SUPPORT: EQMOVE-26 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
        Assert.That(decoded, Is.EqualTo(samples)); // TEST-SUPPORT: EQMOVE-27 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
    } // TEST-SUPPORT: EQMOVE-28 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.TenConfiguredShipsFitAndRoundTrip
    [Test] // TEST-SUPPORT: EQMOVE-29 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
    public void MalformedMovementConfigurationIsRejected() // TEST-SUPPORT: EQMOVE-30 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
    { // TEST-SUPPORT: EQMOVE-31 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        var sample = new MovementSample(1, Guid.NewGuid().ToString("N"), 1, 0, Vector2.Zero, 0, // TEST-SUPPORT: EQMOVE-32 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
            Vector2.Zero, default, 20, 0, 7, new(350, 600, 3, 2, 4, 9, true)); // TEST-SUPPORT: EQMOVE-33 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        var bytes = MovementWire.Snapshot(Guid.NewGuid(), 20, new[] { sample }); // TEST-SUPPORT: EQMOVE-34 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        bytes[^1] = 2; // TEST-SUPPORT: EQMOVE-35 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        Assert.That(MovementWire.ReadSnapshot(bytes, out _, out _, out _), Is.False); // TEST-SUPPORT: EQMOVE-36 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        var invalid = sample with { Configuration = sample.Configuration with { Speed = float.NaN } }; // TEST-SUPPORT: EQMOVE-37 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        Assert.That(MovementWire.ReadSnapshot(MovementWire.Snapshot(Guid.NewGuid(), 20, new[] { invalid }), // TEST-SUPPORT: EQMOVE-38 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
            out _, out _, out _), Is.False); // TEST-SUPPORT: EQMOVE-39 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        invalid = sample with { ConfigurationRevision = -1 }; // TEST-SUPPORT: EQMOVE-40 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
        Assert.That(MovementWire.ReadSnapshot(MovementWire.Snapshot(Guid.NewGuid(), 20, new[] { invalid }), // TEST-SUPPORT: EQMOVE-41 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
            out _, out _, out _), Is.False); // TEST-SUPPORT: EQMOVE-42 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
    } // TEST-SUPPORT: EQMOVE-43 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.MalformedMovementConfigurationIsRejected
    [Test, Category("Godot")] // TEST-SUPPORT: EQMOVE-101 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
    public void ConfigurationChangesDiscardIncompatiblePredictionHistory() // TEST-SUPPORT: EQMOVE-102 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
    { // TEST-SUPPORT: EQMOVE-103 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
        var world = new sMPGWM.Scripts.World.GameWorld(); // TEST-SUPPORT: EQMOVE-104 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: EQMOVE-105 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
        try // TEST-SUPPORT: EQMOVE-106 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
        { // TEST-SUPPORT: EQMOVE-107 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: EQMOVE-108 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            var owner = world.Players[1]; // TEST-SUPPORT: EQMOVE-109 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            var ship = owner.ControlledShip!; // TEST-SUPPORT: EQMOVE-110 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            ship.InstallMovementConfiguration(1, new(350, 600, 3, 2, 4, 9, true)); // TEST-SUPPORT: EQMOVE-111 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            var prediction = new LocalMovementPrediction(); // TEST-SUPPORT: EQMOVE-112 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            prediction.ApplyBaseline(0, MovementStateAdapter.Capture(1, owner, 0), ship); // TEST-SUPPORT: EQMOVE-113 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            ship.SetInput(new(Vector2.Right, 0, false, false, false, false)); // TEST-SUPPORT: EQMOVE-114 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            for (var tick = 1; tick <= 6; tick++) prediction.Step(1, owner); // TEST-SUPPORT: EQMOVE-115 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            var authoritative = MovementStateAdapter.Capture(1, owner, 4) with // TEST-SUPPORT: EQMOVE-116 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            { // TEST-SUPPORT: EQMOVE-117 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
                Position = new Vector2(12, 20), ConfigurationRevision = 2, // TEST-SUPPORT: EQMOVE-118 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
                Configuration = ship.MovementConfiguration with { Enabled = false } // TEST-SUPPORT: EQMOVE-119 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            }; // TEST-SUPPORT: EQMOVE-120 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(prediction.Reconcile(1, owner, 4, authoritative), Is.True); // TEST-SUPPORT: EQMOVE-121 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(prediction.Tick, Is.EqualTo(4)); // TEST-SUPPORT: EQMOVE-122 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(prediction.RecentCommands(), Is.Empty); // TEST-SUPPORT: EQMOVE-123 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(ship.GlobalPosition, Is.EqualTo(authoritative.Position)); // TEST-SUPPORT: EQMOVE-124 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(ship.MovementConfiguration.Enabled, Is.False); // TEST-SUPPORT: EQMOVE-125 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            var energy = ship.Runtime.Energy.Current; // TEST-SUPPORT: EQMOVE-126 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            prediction.Step(1, owner); // TEST-SUPPORT: EQMOVE-127 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(ship.Runtime.Energy.Current, Is.EqualTo(energy)); // TEST-SUPPORT: EQMOVE-128 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(prediction.Reconcile(1, owner, 3, authoritative with { ConfigurationRevision = 1 }), Is.True); // TEST-SUPPORT: EQMOVE-129 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
            Assert.That(ship.MovementConfigurationRevision, Is.EqualTo(2)); // TEST-SUPPORT: EQMOVE-130 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
        } // TEST-SUPPORT: EQMOVE-131 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
        finally { world.Free(); } // TEST-SUPPORT: EQMOVE-132 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
    } // TEST-SUPPORT: EQMOVE-133 | Tests: sMPGWM.Tests.MovementSystem.EquipmentMovementTests.ConfigurationChangesDiscardIncompatiblePredictionHistory
}

