using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
namespace sMPGWM.Tests.UiSystem; // TEST-SUPPORT: SETTINGS-OVERLAY-002 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
[Category("Godot")] // TEST-SUPPORT: SETTINGS-OVERLAY-003 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
public class SettingsOverlayTests // TEST-SUPPORT: SETTINGS-OVERLAY-004 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
{
    [Test] // TEST-SUPPORT: SETTINGS-OVERLAY-006 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
    public void NavigationPreservesBlockingAndTeardown() // TEST-SUPPORT: SETTINGS-OVERLAY-007 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
    {
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate(); // TEST-SUPPORT: SETTINGS-OVERLAY-009 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(canvas); // TEST-SUPPORT: SETTINGS-OVERLAY-010 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
        try // TEST-SUPPORT: SETTINGS-OVERLAY-011 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
        {
            var manager = canvas.GetNode<UiWindowManager>("Root/MenuLayer/UiContainer/MenuHost"); // TEST-SUPPORT: SETTINGS-OVERLAY-013 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            var row = manager.GetNode<Control>("WindowScroll/WindowRow"); // TEST-SUPPORT: SETTINGS-OVERLAY-014 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            manager.SetNavigationEnabled(true); // TEST-SUPPORT: SETTINGS-OVERLAY-015 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            manager.Open(UiWindowId.MainMenu); // TEST-SUPPORT: SETTINGS-OVERLAY-016 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            var unblocks = 0; // TEST-SUPPORT: SETTINGS-OVERLAY-017 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            manager.BlockingChanged += blocked => { if (!blocked) unblocks++; }; // TEST-SUPPORT: SETTINGS-OVERLAY-018 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            for (var i = 0; i < 3; i++) // TEST-SUPPORT: SETTINGS-OVERLAY-019 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            {
                row.GetChild(0).GetNode<Button>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/Settings").EmitSignal(Button.SignalName.Pressed); // TEST-SUPPORT: SETTINGS-OVERLAY-021 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                Assert.That(row.GetChild(0), Is.TypeOf<SettingsOverlayMenu>()); // TEST-SUPPORT: SETTINGS-OVERLAY-022 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                Assert.That(row.GetChildCount(), Is.EqualTo(1)); // TEST-SUPPORT: SETTINGS-OVERLAY-023 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                manager.Activate(UiWindowId.InventoryMenu); // TEST-SUPPORT: SETTINGS-OVERLAY-024 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                Assert.That(row.GetChild(0), Is.TypeOf<SettingsOverlayMenu>()); // TEST-SUPPORT: SETTINGS-OVERLAY-025 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                row.GetChild(0).GetNode<Button>("%CloseButton").EmitSignal(Button.SignalName.Pressed); // TEST-SUPPORT: SETTINGS-OVERLAY-026 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                Assert.That(row.GetChild(0), Is.TypeOf<MainMenu>()); // TEST-SUPPORT: SETTINGS-OVERLAY-027 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
                Assert.That(unblocks, Is.Zero); // TEST-SUPPORT: SETTINGS-OVERLAY-028 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            }
            row.GetChild(0).GetNode<Button>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/Settings").EmitSignal(Button.SignalName.Pressed); // TEST-SUPPORT: SETTINGS-OVERLAY-030 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            manager.CloseAll(); // TEST-SUPPORT: SETTINGS-OVERLAY-031 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            Assert.That(row.GetChildCount(), Is.Zero); // TEST-SUPPORT: SETTINGS-OVERLAY-032 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
            Assert.That(manager.HasBlockingWindow, Is.False); // TEST-SUPPORT: SETTINGS-OVERLAY-033 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
        }
        finally // TEST-SUPPORT: SETTINGS-OVERLAY-035 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
        {
            canvas.Free(); // TEST-SUPPORT: SETTINGS-OVERLAY-037 | Tests: sMPGWM.Tests.UiSystem.SettingsOverlayTests.NavigationPreservesBlockingAndTeardown
        }
    }
}
