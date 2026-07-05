#nullable enable
using System;
using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Controllers;
using sMPGWM.Scripts.Enums.Game;
using sMPGWM.Scripts.Enums.GameState;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Mocks;
using sMPGWM.Scripts.Ui.Game.Hud;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game;

public partial class UiCanvas : CanvasLayer
{
    private PlayerHud _playerHud = null!;
    private GameOverlayController _gameOverlayController = null!;
    public event Action<MenuActionType>? OnRequestMenuAction;
    public event Action<int>? OnRequestHotbarAction;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(UiCanvas)}.{nameof(_EnterTree)} called.");
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _gameOverlayController = GetNode<GameOverlayController>("%MenuHost");
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
        _gameOverlayController.OnMenuAction +=  RequestMenuAction;
            
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(_Ready)} loaded.");
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(UiCanvas)}.{nameof(_ExitTree)} called.");
        _playerHud.HotbarSlotPressed -= RequestHotbarAction;
        _gameOverlayController.OnMenuAction -=  RequestMenuAction;
        GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
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
        _gameOverlayController.ApplyMenuAction(menuActionType);
    }
    
    public void ApplyGameState(MainGameState mainGameState)
    {
        Logger.Debug($"{nameof(UiCanvas)}.{nameof(ApplyGameState)} called with {nameof(mainGameState)}={mainGameState}.");
        _playerHud.ApplyGameState(mainGameState);
        _gameOverlayController.ApplyGameState(mainGameState);
    }

    private void OnGameStateChanged(MainGameState mainGameState)
    {
        ApplyGameState(mainGameState);
    }
    
}
