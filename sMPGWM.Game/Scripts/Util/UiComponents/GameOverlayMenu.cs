using System;
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class GameOverlayMenu : BaseMenu
{
    protected override string ScreenTitle => "InGameMenuScreen Placeholder";

    public event Action? OnMenuClosed;

    private Button _closeButton = null!;
    private Control _menuPanel = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _closeButton = GetNode<Button>("%CloseButton");
        _menuPanel = GetNode<Control>("CenterContainer/MenuPanel");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _closeButton.Text = "Close";
        _closeButton.Pressed += RequestClose;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _menuPanel.MinimumSizeChanged += UpdateWindowMinimumSize;
        UpdateWindowMinimumSize();
    }

    public override void _ExitTree()
    {
        if (_closeButton != null)
            _closeButton.Pressed -= RequestClose;
        if (_menuPanel != null)
            _menuPanel.MinimumSizeChanged -= UpdateWindowMinimumSize;
    }

    private void UpdateWindowMinimumSize() => CustomMinimumSize = _menuPanel.GetCombinedMinimumSize();

    public void GrabWindowFocus()
    {
        _closeButton.GrabFocus();
    }

    protected void RequestClose()
    {
        OnMenuClosed?.Invoke();
    }
}
