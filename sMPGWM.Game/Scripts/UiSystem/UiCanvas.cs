using System;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;
using PlayerHud = sMPGWM.Scripts.UiSystem.HudSystem.PlayerHud;
using PlayerMock = sMPGWM.Scripts.Util.Mocks.PlayerMock;

namespace sMPGWM.Scripts.UiSystem;

public partial class UiCanvas : CanvasLayer
{
    private PlayerHud _playerHud = null!;
    private OverlayMenuManager _overlayMenuManager = null!;
    public event Action<MenuActionType>? OnRequestMenuAction;
    public event Action<int>? OnRequestHotbarAction;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(UiCanvas)}.{nameof(_EnterTree)} called.");
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _overlayMenuManager = GetNode<OverlayMenuManager>("%MenuHost");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(UiCanvas)}.{nameof(_Ready)} called.");
        // _playerHud.Bind(PlayerMock.Instance.LivingEntityState,
        // [
        //     StatType.Health,
        //         StatType.Shield,
        //         StatType.EnergyStorage
        // ]);
        _playerHud.SetHotbarIcons(PlayerMock.Instance.GetHotBarSkills());
        _playerHud.HotbarSlotPressed += RequestHotbarAction;
        _overlayMenuManager.OnMenuAction +=  RequestMenuAction;
            
        GamePlayManager.Instance.OnGameStateChanged += OnGameStateChanged;
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(_Ready)} loaded.");
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(UiCanvas)}.{nameof(_ExitTree)} called.");
        _playerHud.HotbarSlotPressed -= RequestHotbarAction;
        _overlayMenuManager.OnMenuAction -=  RequestMenuAction;
        GamePlayManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    public void RequestMenuAction(MenuActionType actionType)
    {
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(RequestMenuAction)} called with {nameof(actionType)}={actionType}.");
        OnRequestMenuAction?.Invoke(actionType);
    }

    public void RequestHotbarAction(int slot)
    {
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(RequestHotbarAction)} called with {nameof(slot)}={slot}.");
        OnRequestHotbarAction?.Invoke(slot);
    }

    public void ApplyMenuAction(MenuActionType menuActionType)
    {
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(ApplyMenuAction)} called with {nameof(menuActionType)}={menuActionType}.");
        _overlayMenuManager.ApplyMenuAction(menuActionType);
    }
    
    public void ApplyGameState(MainGameState mainGameState)
    {
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(ApplyGameState)} called with {nameof(mainGameState)}={mainGameState}.");
        _playerHud.ApplyGameState(mainGameState);
        _overlayMenuManager.ApplyGameState(mainGameState);
    }

    private void OnGameStateChanged(MainGameState mainGameState)
    {
        ApplyGameState(mainGameState);
    }
    
}
