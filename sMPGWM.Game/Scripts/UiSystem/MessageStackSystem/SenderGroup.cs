using Godot;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class SenderGroup : VBoxContainer
{
    internal void Bind(string name, int unread, bool expanded, bool disabled, System.Action<bool> changed)
    {
        var header = GetNode<Button>("SenderButton");
        var list = GetNode<VBoxContainer>("MessageList");
        list.Visible = expanded;
        header.Text = $"{(expanded ? "▾" : "▸")} {name}{(unread > 0 ? $" ({unread})" : "")}";
        header.TooltipText = name;
        header.Disabled = disabled;
        header.Pressed += () =>
        {
            list.Visible = !list.Visible;
            header.Text = $"{(list.Visible ? "▾" : "▸")} {name}{(unread > 0 ? $" ({unread})" : "")}";
            changed(list.Visible);
        };
    }
}
