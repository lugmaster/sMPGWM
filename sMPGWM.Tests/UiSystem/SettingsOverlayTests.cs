using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
namespace sMPGWM.Tests.UiSystem;
[Category("Godot")]
public class SettingsOverlayTests
{
    [Test]
    public void NavigationPreservesBlockingAndTeardown()
    {
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(canvas);
        try
        {
            var manager = canvas.GetNode<UiWindowManager>("Root/MenuLayer/UiContainer/MenuHost");
            var row = manager.GetNode<Control>("WindowScroll/WindowRow");
            manager.SetNavigationEnabled(true);
            manager.Open(UiWindowId.MainMenu);
            var unblocks = 0;
            manager.BlockingChanged += blocked => { if (!blocked) unblocks++; };
            for (var i = 0; i < 3; i++)
            {
                row.GetChild(0).GetNode<Button>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/Settings").EmitSignal(Button.SignalName.Pressed);
                Assert.That(row.GetChild(0), Is.TypeOf<SettingsOverlayMenu>());
                Assert.That(row.GetChildCount(), Is.EqualTo(1));
                manager.Activate(UiWindowId.InventoryMenu);
                Assert.That(row.GetChild(0), Is.TypeOf<SettingsOverlayMenu>());
                row.GetChild(0).GetNode<Button>("%CloseButton").EmitSignal(Button.SignalName.Pressed);
                Assert.That(row.GetChild(0), Is.TypeOf<MainMenu>());
                Assert.That(unblocks, Is.Zero);
            }
            row.GetChild(0).GetNode<Button>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/Settings").EmitSignal(Button.SignalName.Pressed);
            manager.CloseAll();
            Assert.That(row.GetChildCount(), Is.Zero);
            Assert.That(manager.HasBlockingWindow, Is.False);
        }
        finally
        {
            canvas.Free();
        }
    }
}
