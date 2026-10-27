using Godot;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class MessageListItem : Button
{
    internal void Display(sMPGWM.Scripts.MessageSystem.PlayerMessage message, bool selected)
    {
        ToggleMode = true;
        ButtonPressed = selected;
        if (selected) AddThemeStyleboxOverride("disabled", GetThemeStylebox("pressed"));
        GetNode<Label>("HBoxContainer/UnreadIndicator").Text = message.IsRead ? "" : "●";
        var title = GetNode<Label>("HBoxContainer/TitleLabel");
        title.Text = message.Subject;
        title.Modulate = message.IsRead ? new Color(0.75f, 0.75f, 0.75f) : Colors.White;
        title.AddThemeFontSizeOverride("font_size", message.IsRead ? 13 : 15);
        TooltipText = message.Subject;
    }
}
