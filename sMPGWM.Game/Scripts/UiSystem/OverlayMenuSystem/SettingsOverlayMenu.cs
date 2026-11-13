using sMPGWM.Scripts.UtilitySystem.UiComponents;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.KeyBindingSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

public partial class SettingsOverlayMenu : GameOverlayMenu
{
    protected override string ScreenTitle => "Settings";
    internal KeyBindingMenuController Bindings => GetNode<KeyBindingMenuController>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/SettingsTabMenu/KeybindingMenuContainer");
    protected override void OnReady()
    {
        base.OnReady();
        Bindings.CaptureExcludedControl = GetNode<Godot.Control>("%CloseButton");
    }
}
