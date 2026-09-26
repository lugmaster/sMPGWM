using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.Hud;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class PlayerHud : Control
{
    public event Action<int>? HotbarSlotPressed;

    private Control _statsAnchor = null!;
    private Control _radarAnchor = null!;
    private Control _hotbarAnchor = null!;

    private PlayerStatsPanel _statsPanel = null!;
    private RadarControl _radarControl = null!;
    private HudSystem.HotbarControl _hotbarControl = null!;
    private bool _isProcessingInput = true;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(PlayerHud)}.{nameof(_EnterTree)} called.");
        _statsAnchor = GetNode<Control>("%StatsAnchor");
        _radarAnchor = GetNode<Control>("%RadarAnchor");
        _hotbarAnchor = GetNode<Control>("%HotbarAnchor");
        _statsPanel = GetNode<PlayerStatsPanel>("%PlayerStatsPanel");
        _radarControl = GetNode<RadarControl>("%RadarControl");
        _hotbarControl = GetNode<HudSystem.HotbarControl>("%HotbarControl");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(PlayerHud)}.{nameof(_Ready)} called.");
        _hotbarControl.SlotPressed += OnHotbarSlotPressed;

        GetViewport().SizeChanged += ApplyLayout;
        ApplyLayout();
        Logger.Debug($"{nameof(PlayerHud)}.{nameof(_Ready)} loaded.");
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(PlayerHud)}.{nameof(_ExitTree)} called.");
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

    public void SetHotbarIcons(IReadOnlyList<IconDefinition> iconDefinitions)
    {
        Logger.Debug(
            $"{nameof(PlayerHud)}.{nameof(SetHotbarIcons)} called with {nameof(iconDefinitions)} count={iconDefinitions.Count}.");
        _hotbarControl.SetIcons(iconDefinitions);
    }

    public void ApplyGameState(MainGameState mainGameState)
    {
        Logger.Debug(
            $"{nameof(PlayerHud)}.{nameof(ApplyGameState)} called with {nameof(mainGameState)}={mainGameState}.");
        _hotbarControl?.ApplyGameState(mainGameState);
        switch (mainGameState)
        {
            case MainGameState.Active:
                ApplyActiveState();
                break;

            case MainGameState.InMenu:
            case MainGameState.Inactive:
                ApplyInactiveState();
                break;

            default:
                throw new InvalidOperationException($"Unexpected game state: {mainGameState}");
        }
    }

    private void ApplyActiveState()
    {
        ProcessMode = ProcessModeEnum.Inherit;
        MouseFilter = MouseFilterEnum.Pass;
        _isProcessingInput = true;
    }

    private void ApplyInactiveState()
    {
        ProcessMode = ProcessModeEnum.Disabled;
        MouseFilter = MouseFilterEnum.Ignore;
        _isProcessingInput = false;
    }
}