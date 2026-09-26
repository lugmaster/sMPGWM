using System;
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class GameOverlayMenu : BaseMenu
{
    protected override string ScreenTitle => "InGameMenuScreen Placeholder";

    public event Action? OnMenuClosed;

    private Button _closeButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _closeButton = GetNode<Button>("%CloseButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _closeButton.Text = "Close";
        _closeButton.Pressed += RequestClose;
    }

    protected void RequestClose()
    {
        OnMenuClosed?.Invoke();
    }
}
