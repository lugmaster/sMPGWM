using System.Reflection;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.World;

namespace sMPGWM.Tests.Network;

[Category("Godot")]
public class LocalCameraTests
{
    [Test]
    public void CameraFollowsByParentingAndSurvivesReplacementAndRemoval()
    {
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var world = new Node2D();
        host.AddChild(world);
        // Supply the UI's scene references without mounting or exercising the full HUD scene.
        var canvas = new UiCanvas();
        var hud = new PlayerHud();
        var windows = new UiWindowManager();
        var hotbar = new HotbarControl();
        var buttons = new HBoxContainer();
        SetField(hotbar, "_buttonContainer", buttons);
        SetField(hud, "_hotbarControl", hotbar);
        SetField(canvas, "_playerHud", hud);
        SetField(canvas, "_windowManager", windows);
        var items = new ItemStore();
        using var inventory = new InventoryController(items);
        using var equipment = new EquipmentController(items);
        using var transfer = new InventoryEquipmentTransferService(inventory, equipment);
        var player = new PlayerController();
        player.Initialize(canvas, transfer, _ => { }, world);
        host.AddChild(player);
        var participant = new PlayerSession { ControllingPeerId = 1 };
        var ship = ShipFactory.Create(ShipRegistry.GetById("fighter"), "first", new Vector2(100, 200), 0, Vector2.Zero);
        var replacement = ShipFactory.Create(ShipRegistry.GetById("fighter"), "second", new Vector2(800, 400), 0, Vector2.Zero);
        try
        {
            world.AddChild(ship);
            world.AddChild(replacement);
            participant.AssignShip(ship);
            player.BindParticipant(participant);
            var camera = ship.GetNode<Camera2D>("LocalCamera");
            Assert.That(camera.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(camera.IgnoreRotation, Is.True);
            ship.Position += new Vector2(30, 50);
            ship.Rotation = 1;
            Assert.That(camera.GlobalPosition, Is.EqualTo(ship.GlobalPosition));
            Assert.That(camera.GetScreenRotation(), Is.Zero);
            ship.Teleport(new Vector2(900, 500), 2, Vector2.Zero);
            Assert.That(camera.GetScreenCenterPosition().DistanceTo(ship.GlobalPosition), Is.LessThan(0.01f));

            player.UsePredictionCamera(true);
            ship.Presentation.Present(new Vector2(890, 495), 2);
            Assert.That(camera.GlobalPosition, Is.EqualTo(ship.DisplayedPosition));
            ship.Presentation.Snap();
            Assert.That(camera.GlobalPosition, Is.EqualTo(ship.GlobalPosition));

            participant.AssignShip(replacement);
            Assert.That(replacement.GetNode<Camera2D>("LocalCamera"), Is.SameAs(camera));
            var currentCameraPosition = camera.GlobalPosition;
            ship.Presentation.Present(Vector2.Zero, 0);
            Assert.That(camera.GlobalPosition, Is.EqualTo(currentCameraPosition), "Old presentation must no longer drive the camera.");
            ship.Free();
            Assert.That(GodotObject.IsInstanceValid(camera), Is.True);
            var lastView = camera.GetScreenCenterPosition();
            participant.AssignShip(null);
            replacement.QueueFree();
            Assert.That(camera.GetParent(), Is.SameAs(world));
            Assert.That(camera.GlobalPosition.DistanceTo(lastView), Is.LessThan(0.01f));
            Assert.That(player.IsProcessing(), Is.False, "Following must not need a player frame callback.");
        }
        finally
        {
            player.Shutdown();
            player.Free();
            world.Free();
            canvas.Free(); hud.Free(); windows.Free(); hotbar.Free(); buttons.Free();
        }
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
