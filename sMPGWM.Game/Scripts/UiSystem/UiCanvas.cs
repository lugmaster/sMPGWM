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

    internal bool HasBlockingWindow => _windowManager.HasBlockingWindow;
    internal event Action<bool>? BlockingChanged;
    internal event Action<int>? OnRequestHotbarAction;

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

    public void OpenMenu(UiWindowId id) => _windowManager.Open(id);

    public void CloseMenu(UiWindowId id) => _windowManager.Close(id);

    public void ToggleMenu(UiWindowId id) => _windowManager.Toggle(id);

    public void CloseAllMenus() => _windowManager.CloseAll();

    internal void SetHotbarIcons(IReadOnlyList<IconDefinition> icons) => _playerHud.SetHotbarIcons(icons);

    internal void SetNavigationEnabled(bool enabled) => _windowManager.SetNavigationEnabled(enabled);

    internal void SetGameplayInputEnabled(bool enabled) => _playerHud.SetGameplayInputEnabled(enabled);

    private void RequestHotbarAction(int slotIndex) => OnRequestHotbarAction?.Invoke(slotIndex);

    internal bool IsPointerOverInteractiveUi()
    {
        var hovered = GetViewport().GuiGetHoveredControl();
        return hovered != null && IsAncestorOf(hovered);
    }

    private void OnBlockingChanged(bool blocking) => BlockingChanged?.Invoke(blocking);
}
