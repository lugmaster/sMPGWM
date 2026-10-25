using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.UiSystem.HudSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem;

public partial class UiCanvas : CanvasLayer
{
    private PlayerHud _playerHud = null!;
    private UiWindowManager _windowManager = null!;

    public bool HasBlockingWindow => _windowManager.HasBlockingWindow;
    public event Action<bool>? BlockingChanged;
    public event Action<int>? OnRequestHotbarAction;

    public override void _EnterTree()
    {
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _windowManager = GetNode<UiWindowManager>("%MenuHost");
    }

    public override void _Ready()
    {
        _playerHud.HotbarSlotPressed += RequestHotbarAction;
        _windowManager.BlockingChanged += OnBlockingChanged;
    }

    public override void _ExitTree()
    {
        _playerHud.HotbarSlotPressed -= RequestHotbarAction;
        _windowManager.BlockingChanged -= OnBlockingChanged;
    }

    public void SetHotbarIcons(IReadOnlyList<IconDefinition> icons) => _playerHud.SetHotbarIcons(icons);

    public void SetNavigationEnabled(bool enabled) => _windowManager.SetNavigationEnabled(enabled);

    public void SetGameplayInputEnabled(bool enabled) => _playerHud.SetGameplayInputEnabled(enabled);

    public void RequestHotbarAction(int slotIndex) => OnRequestHotbarAction?.Invoke(slotIndex);

    public bool IsPointerOverInteractiveUi()
    {
        var hovered = GetViewport().GuiGetHoveredControl();
        return hovered != null && IsAncestorOf(hovered);
    }

    private void OnBlockingChanged(bool blocking) => BlockingChanged?.Invoke(blocking);
}
