#nullable enable
using System;
using Godot;

namespace sMPGWM.Scripts.Ui.Base;

public partial class GameOverlayMenu : BaseMenu
{
    protected override string ScreenTitle => "InGameMenuScreen Placeholder";

    public event Action? MenuClosed;

    private Button _closeButton = null!;

    protected override void OnReady()
    {
        _closeButton = GetNode<Button>("%CloseButton");
        _closeButton.Text = "Close";
        _closeButton.Pressed += RequestClose;
    }

    protected void RequestClose()
    {
        MenuClosed?.Invoke();
    }
}