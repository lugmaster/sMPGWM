using Godot; // TEST-SUPPORT: MOVEMENT-HISTORY-001 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
using sMPGWM.Scripts.MovementSystem; // TEST-SUPPORT: MOVEMENT-HISTORY-002 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
using sMPGWM.Scripts.World; // TEST-SUPPORT: MOVEMENT-HISTORY-003 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset

namespace sMPGWM.Tests.MovementSystem; // TEST-SUPPORT: MOVEMENT-HISTORY-004 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset

[Category("Godot")] // TEST-SUPPORT: MOVEMENT-HISTORY-005 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
public sealed class MovementHistoryTests // TEST-SUPPORT: MOVEMENT-HISTORY-006 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
{
    [Test] // TEST-SUPPORT: MOVEMENT-HISTORY-007 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
    public void RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset() // TEST-SUPPORT: MOVEMENT-HISTORY-008 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
    {
        var world = new GameWorld(); // TEST-SUPPORT: MOVEMENT-HISTORY-009 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: MOVEMENT-HISTORY-010 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
        try // TEST-SUPPORT: MOVEMENT-HISTORY-011 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
        {
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: MOVEMENT-HISTORY-012 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            var owner = world.Players[1]; // TEST-SUPPORT: MOVEMENT-HISTORY-013 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            var ship = owner.ControlledShip!; // TEST-SUPPORT: MOVEMENT-HISTORY-014 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            var prediction = new LocalMovementPrediction(); // TEST-SUPPORT: MOVEMENT-HISTORY-015 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            prediction.ApplyBaseline(0, MovementStateAdapter.Capture(1, owner, 0), ship); // TEST-SUPPORT: MOVEMENT-HISTORY-016 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.RecentCommands(), Is.Empty); // TEST-SUPPORT: MOVEMENT-HISTORY-017 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            var samples = new MovementSample[10]; // TEST-SUPPORT: MOVEMENT-HISTORY-018 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            for (var tick = 1; tick <= 9; tick++) // TEST-SUPPORT: MOVEMENT-HISTORY-019 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            {
                prediction.Step(1, owner); // TEST-SUPPORT: MOVEMENT-HISTORY-020 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
                samples[tick] = MovementStateAdapter.Capture(1, owner, tick); // TEST-SUPPORT: MOVEMENT-HISTORY-021 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            }
            var recent = prediction.RecentCommands(); // TEST-SUPPORT: MOVEMENT-HISTORY-022 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(recent.Length, Is.EqualTo(6)); // TEST-SUPPORT: MOVEMENT-HISTORY-023 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            for (var i = 0; i < recent.Length; i++) Assert.That(recent[i].Tick, Is.EqualTo(i + 4)); // TEST-SUPPORT: MOVEMENT-HISTORY-024 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.Reconcile(1, owner, 6, samples[6]), Is.True); // TEST-SUPPORT: MOVEMENT-HISTORY-025 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            recent = prediction.RecentCommands(); // TEST-SUPPORT: MOVEMENT-HISTORY-026 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(recent.Length, Is.EqualTo(3)); // TEST-SUPPORT: MOVEMENT-HISTORY-027 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            for (var i = 0; i < recent.Length; i++) Assert.That(recent[i].Tick, Is.EqualTo(i + 7)); // TEST-SUPPORT: MOVEMENT-HISTORY-028 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.Reconcile(1, owner, 6, samples[6]), Is.False); // TEST-SUPPORT: MOVEMENT-HISTORY-029 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.Reconcile(1, owner, 7, samples[7] with { Position = samples[7].Position + Vector2.Right }), Is.True); // TEST-SUPPORT: MOVEMENT-HISTORY-030 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.RecentCommands().Length, Is.EqualTo(2)); // TEST-SUPPORT: MOVEMENT-HISTORY-031 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.Reconcile(1, owner, 9, MovementStateAdapter.Capture(1, owner, 9)), Is.True); // TEST-SUPPORT: MOVEMENT-HISTORY-032 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.RecentCommands(), Is.Empty); // TEST-SUPPORT: MOVEMENT-HISTORY-033 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            prediction.Step(1, owner); // TEST-SUPPORT: MOVEMENT-HISTORY-034 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            prediction.Reset(50); // TEST-SUPPORT: MOVEMENT-HISTORY-035 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.RecentCommands(), Is.Empty); // TEST-SUPPORT: MOVEMENT-HISTORY-036 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
            Assert.That(prediction.Tick, Is.EqualTo(50)); // TEST-SUPPORT: MOVEMENT-HISTORY-037 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
        }
        finally { world.Free(); } // TEST-SUPPORT: MOVEMENT-HISTORY-038 | Tests: sMPGWM.Tests.MovementSystem.MovementHistoryTests.RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset
    }
}

