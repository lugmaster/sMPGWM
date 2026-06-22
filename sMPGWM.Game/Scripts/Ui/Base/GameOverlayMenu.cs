#nullable enable
using System;
using Godot;

namespace sMPGWM.Scripts.Ui.Base;

public partial class GameOverlayMenu : BaseMenu
{
    protected override string ScreenTitle => "InGameMenuScreen Placeholder";
    
    public event Action? MenuClosed;
    public bool IsInputProcessingEnabled { get; private set; } = true;
    
    private Button _closeButton = null!;

    protected override void OnReady()
    {
        _closeButton = GetNode<Button>("%CloseButton");
        _closeButton.Text = "Close";
        _closeButton.Pressed += RequestClose;
    }

    public override void _ExitTree()
    {
        if (_closeButton != null)
            _closeButton.Pressed -= RequestClose;

        MenuClosed = null;
    }

    public virtual void SetInputProcessing(bool isProcessing)
    {
        IsInputProcessingEnabled = isProcessing;
        ProcessMode = isProcessing ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        MouseFilter = isProcessing ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        if (_closeButton != null)
            _closeButton.Disabled = !isProcessing;
    }

    protected void RequestClose()
    {
        MenuClosed?.Invoke();
    }
}
