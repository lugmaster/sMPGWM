#nullable enable
using System;
using Godot;

namespace sMPGWM.Scripts.Ui.MainMenu.Base;

public partial class InGameMenuScreen : BaseMenuScreen
{
    protected override string ScreenTitle => "InGameMenuScreen Placeholder";
    
    public event Action? MenuClosed;
    
    private Button _closeButton = null!;

    public virtual bool PauseGame => false;
    public virtual bool BlocksGameInput => true;

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