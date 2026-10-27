#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerHud : Control
{
    internal event Action<int>? HotbarSlotPressed;
    internal event Action<UiWindowId>? MenuRequested;
    private MenuToolbarControl _toolbar = null!;
    internal void SetNavigationEnabled(bool enabled) => _toolbar.SetNavigationEnabled(enabled);
    internal void SetUnreadMessages(int count) => _toolbar.PresentNotification(NotificationKind.UnreadMessages, count);
    private void RequestMenu(UiWindowId id) => MenuRequested?.Invoke(id);

    private Control _statsAnchor = null!;
    private Control _radarAnchor = null!;
    private Control _hotbarAnchor = null!;

    private PlayerStatsPanel _statsPanel = null!;
    private RadarControl _radarControl = null!;
    private HotbarControl _hotbarControl = null!;
    private bool _isProcessingInput;
    internal HotbarControl Hotbar => _hotbarControl;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(PlayerHud)}.{nameof(_EnterTree)} called.");
        _toolbar = GetNode<MenuToolbarControl>("%MenuToolbar");
        _statsAnchor = GetNode<Control>("%StatsAnchor");
        _radarAnchor = GetNode<Control>("%RadarAnchor");
        _hotbarAnchor = GetNode<Control>("%HotbarAnchor");
        _statsPanel = GetNode<PlayerStatsPanel>("%PlayerStatsPanel");
        _radarControl = GetNode<RadarControl>("%RadarControl");
        _hotbarControl = GetNode<HotbarControl>("%HotbarControl");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(PlayerHud)}.{nameof(_Ready)} called.");
        _toolbar.MenuRequested += RequestMenu;
        _hotbarControl.SlotPressed += OnHotbarSlotPressed;

        GetViewport().SizeChanged += ApplyLayout;
        ApplyLayout();
        Logger.Debug($"{nameof(PlayerHud)}.{nameof(_Ready)} loaded.");
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(PlayerHud)}.{nameof(_ExitTree)} called.");
        _toolbar.MenuRequested -= RequestMenu;
        if (_hotbarControl != null)
            _hotbarControl.SlotPressed -= OnHotbarSlotPressed;

        GetViewport().SizeChanged -= ApplyLayout;
    }

    private void ApplyLayout()
    {
        // TODO implement later
    }

    private void OnHotbarSlotPressed(int slotIndex)
    {
        if (!_isProcessingInput)
            return;

        HotbarSlotPressed?.Invoke(slotIndex);
    }

    // public void Bind(Object state // LivingintEntitityState
    //     , IReadOnlyCollection<StatType> visibleStats)
    // {
    //     Logger.Debug($"{nameof(PlayerHud)}.{nameof(Bind)} called.");
    //     // _statsPanel.Bind(state, visibleStats);
    // }

    internal void SetHotbarIcons(IReadOnlyList<IconDefinition> iconDefinitions)
    {
        Logger.Debug(
            $"{nameof(PlayerHud)}.{nameof(SetHotbarIcons)} called with {nameof(iconDefinitions)} count={iconDefinitions.Count}.");
        _hotbarControl.SetIcons(iconDefinitions);
    }

    internal void SetGameplayInputEnabled(bool enabled)
    {
        _isProcessingInput = enabled;
        _hotbarControl.SetGameplayInputEnabled(enabled);
    }
}
