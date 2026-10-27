#nullable enable

using Godot;
using sMPGWM.Scripts.MessageSystem;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class MessageListItem : Button
{
    private PlayerMessage? _message;
    private bool _selected;
    private Label _unread = null!;
    private Label _title = null!;

    public override void _Ready()
    {
        ToggleMode = true;
        _unread = GetNode<Label>("HBoxContainer/UnreadIndicator");
        _title = GetNode<Label>("HBoxContainer/TitleLabel");
    }

    internal void Display(sMPGWM.Scripts.MessageSystem.PlayerMessage message, bool selected)
    {
        SetPressedNoSignal(selected);
        if (ReferenceEquals(_message, message) && _selected == selected) return;
        _message = message;
        _selected = selected;
        if (selected) AddThemeStyleboxOverride("disabled", GetThemeStylebox("pressed"));
        else RemoveThemeStyleboxOverride("disabled");
        _unread.Text = message.IsRead ? "" : "●";
        _title.Text = message.Subject;
        _title.Modulate = message.IsRead ? new Color(0.75f, 0.75f, 0.75f) : Colors.White;
        _title.AddThemeFontSizeOverride("font_size", message.IsRead ? 13 : 15);
        TooltipText = message.Subject;
    }
}
