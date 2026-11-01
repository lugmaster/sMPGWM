using Godot;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.Network;

namespace sMPGWM.Tests.Network;

[Category("Godot")]
public class LocalMovementTests
{
    [Test]
    public void OnlyLocalAvatarMovesAndBlockedInputStillAllowsDeceleration()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var input = new InputController();
        var manager = new MpManager();
        var actions = new[] { "Backwards", "Forwards", "StrafeLeft", "StrafeRight",
            "Action", "AfterBurner", "AlternateAction", "Break", "EnableAutoBreaking",
            "TargetNearestSwitch", "TargetSwitch" };
        var added = actions.Where(action => !InputMap.HasAction(action)).ToArray();
        foreach (var action in added) InputMap.AddAction(action);
        var wasFocused = InputController.IsWindowFocused;
        try
        {
            root.AddChild(input);
            root.AddChild(manager);
            manager.Spawn(1, new PlayerStartupData("here i am 1"));
            manager.Spawn(2, new PlayerStartupData("here i am 2"));
            var local = manager.MpControllers[1];
            var remote = manager.MpControllers[2];
            input._Notification(1004);
            input.SetGameplayInputEnabled(true);
            Input.ActionPress("Forwards");
            input._Process(1.0 / 60);
            local._PhysicsProcess(1.0 / 60);
            remote._PhysicsProcess(1.0 / 60);
            Assert.That(local.IsLocallyControlled, Is.True);
            Assert.That(local.Velocity.X, Is.GreaterThan(0));
            Assert.That(remote.Velocity, Is.EqualTo(Vector2.Zero));
            Assert.That(remote.Position, Is.EqualTo(Vector2.Zero));

            local.Velocity = new Vector2(300, 0);
            input.SetGameplayInputEnabled(false);
            var velocity = local.Velocity;
            var rotation = local.Rotation;
            local._PhysicsProcess(1.0 / 60);
            // This synchronous test runs before the first engine physics frame;
            // verify continued simulation through velocity rather than displacement.
            Assert.That(local.Velocity.X, Is.InRange(0.01f, velocity.X - 0.01f));
            Assert.That(local.Rotation, Is.EqualTo(rotation));
        }
        finally
        {
            Input.ActionRelease("Forwards");
            manager.Free();

            input._Notification(wasFocused ? 1004 : 1005);
            input.Free();
            foreach (var action in added) InputMap.EraseAction(action);
        }
    }
}


