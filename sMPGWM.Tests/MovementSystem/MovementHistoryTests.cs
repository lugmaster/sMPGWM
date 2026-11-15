using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Tests.MovementSystem;

[Category("Godot")]
public sealed class MovementHistoryTests
{
    private static MovementCommand[] Recent(LocalMovementPrediction prediction)
    {
        var buffer = new MovementCommand[MovementWire.MaxCommands];
        return buffer[..prediction.CopyRecentCommands(buffer)];
    }

    [Test]
    public void FixedHistoryWrapsWithoutReplayingOverwrittenCommands()
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var prediction = new LocalMovementPrediction();
            prediction.ApplyBaseline(0, MovementStateAdapter.Capture(1, owner, 0), owner.ControlledShip!);
            var samples = new MovementSample[16];
            for (var tick = 1; tick <= 320; tick++)
            {
                prediction.Step(1, owner);
                samples[tick % 16] = MovementStateAdapter.Capture(1, owner, tick);
                if (tick % 16 != 0) continue;
                Assert.That(prediction.Reconcile(1, owner, tick - 4, samples[(tick - 4) % 16]), Is.True);
                var recent = Recent(prediction);
                Assert.That(recent.Length, Is.EqualTo(4));
                for (var i = 0; i < recent.Length; i++) Assert.That(recent[i].Tick, Is.EqualTo(tick - 3 + i));
            }
            prediction.Reset(320);
            for (var i = 0; i < 128; i++) prediction.Step(1, owner);
            Assert.That(prediction.AtCapacity, Is.True);
            Assert.Throws<InvalidOperationException>(() => prediction.Step(1, owner));
            prediction.Reset(500);
            Assert.That(prediction.AtCapacity, Is.False);
        }
        finally { world.Free(); }
    }

    [Test]
    public void RecentCommandsStayOrderedAcrossAcknowledgementReplayAndReset()
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            var prediction = new LocalMovementPrediction();
            prediction.ApplyBaseline(0, MovementStateAdapter.Capture(1, owner, 0), ship);
            Assert.That(Recent(prediction), Is.Empty);
            var samples = new MovementSample[10];
            for (var tick = 1; tick <= 9; tick++)
            {
                prediction.Step(1, owner);
                samples[tick] = MovementStateAdapter.Capture(1, owner, tick);
            }
            var recent = Recent(prediction);
            Assert.That(recent.Length, Is.EqualTo(6));
            for (var i = 0; i < recent.Length; i++) Assert.That(recent[i].Tick, Is.EqualTo(i + 4));
            Assert.That(prediction.Reconcile(1, owner, 6, samples[6]), Is.True);
            recent = Recent(prediction);
            Assert.That(recent.Length, Is.EqualTo(3));
            for (var i = 0; i < recent.Length; i++) Assert.That(recent[i].Tick, Is.EqualTo(i + 7));
            Assert.That(prediction.Reconcile(1, owner, 6, samples[6]), Is.False);
            Assert.That(prediction.Reconcile(1, owner, 7, samples[7] with { Position = samples[7].Position + Vector2.Right }), Is.True);
            Assert.That(Recent(prediction).Length, Is.EqualTo(2));
            Assert.That(prediction.Reconcile(1, owner, 9, MovementStateAdapter.Capture(1, owner, 9)), Is.True);
            Assert.That(Recent(prediction), Is.Empty);
            prediction.Step(1, owner);
            prediction.Reset(50);
            Assert.That(Recent(prediction), Is.Empty);
            Assert.That(prediction.Tick, Is.EqualTo(50));
        }
        finally { world.Free(); }
    }
}

