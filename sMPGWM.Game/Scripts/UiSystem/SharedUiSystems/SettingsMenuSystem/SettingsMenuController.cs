using Godot;

namespace sMPGWM.Scripts.UiSystem.SharedUiSystems.SettingsMenuSystem;

public partial class SettingsMenuController : TabContainer
{
    public override void _Ready()
    {
        var keybindings = GetChild<Control>(0);
        keybindings.Name = "Keybindings";
        SetTabTitle(0, "Keybindings");
        AddPlaceholderTab("Audio");
        AddPlaceholderTab("Graphics");
        AddPlaceholderTab("Game");
    }

    // PLACEHOLDER SETTINGS TABS: replace these controls with the corresponding settings menus.
    private void AddPlaceholderTab(string title)
    {
        var placeholder = new CenterContainer { Name = title };
        placeholder.SetMeta("settings_placeholder", true);
        placeholder.AddChild(new Label { Text = $"{title} settings — not implemented yet." });
        AddChild(placeholder);
        SetTabTitle(GetTabCount() - 1, title);
    }
}
