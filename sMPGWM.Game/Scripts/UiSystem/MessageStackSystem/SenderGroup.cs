#nullable enable

using System;
using Godot;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class SenderGroup : VBoxContainer
{
    private Button _header = null!;
    private VBoxContainer _list = null!;
    private string _name = "";
    private int _unread;
    private bool _hasPresentation;
    private Action<bool>? _changed;

    public override void _Ready()
    {
        _header = GetNode<Button>("SenderButton");
        _list = GetNode<VBoxContainer>("MessageList");
        _header.Pressed += ToggleExpanded;
    }

    internal void Bind(string name, int unread, bool expanded, bool disabled, Action<bool> changed)
    {
        _changed = changed;
        SetPresentation(name, unread, expanded, disabled);
    }

    internal void SetPresentation(string name, int unread, bool expanded, bool disabled)
    {
        if (_hasPresentation && _name == name && _unread == unread && _list.Visible == expanded &&
            _header.Disabled == disabled) return;
        _hasPresentation = true;
        _name = name;
        _unread = unread;
        _list.Visible = expanded;
        _header.TooltipText = name;
        _header.Disabled = disabled;
        UpdateHeader();
    }

    private void ToggleExpanded()
    {
        _list.Visible = !_list.Visible;
        UpdateHeader();
        _changed?.Invoke(_list.Visible);
    }

    private void UpdateHeader() =>
        _header.Text = $"{(_list.Visible ? "▾" : "▸")} {_name}{(_unread > 0 ? $" ({_unread})" : "")}";

    public override void _ExitTree()
    {
        _header.Pressed -= ToggleExpanded;
        _changed = null;
    }
}
