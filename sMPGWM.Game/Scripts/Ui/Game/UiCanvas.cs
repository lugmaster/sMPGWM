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

    public override void _Ready()
    {
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _gameOverlayController = GetNode<GameOverlayController>("%MenuHost");

        _playerHud.Bind(PlayerMock.Instance.LivingEntityState,
        [
            StatType.Health,
                StatType.Shield,
                StatType.Energy
        ]);
        _playerHud.SetHotbarIcons(PlayerMock.Instance.GetHotBarSkills());
        _playerHud.HotbarSlotPressed += RequestHotbarAction;
        _gameOverlayController.OnMenuAction +=  RequestMenuAction;
            
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        Logger.Info("UiCanvas loaded");
    }

    public void RequestMenuAction(MenuActionType actionType)
    {
        Logger.Info($"Requesting menu action {actionType}");
        OnRequestMenuAction?.Invoke(actionType);
    }

    public void RequestHotbarAction(int slot)
    {
        Logger.Info($"Requesting hotbar slot {slot}");
        OnRequestHotbarAction?.Invoke(slot);
    }

    public void ApplyMenuAction(MenuActionType menuActionType)
    {
        Logger.Info($"Applying menu action {menuActionType}");
        _gameOverlayController.ApplyMenuAction(menuActionType);
    }
    
    public void ApplyGameState(MainGameState mainGameState)
    {
        _playerHud.ApplyGameState(mainGameState);
        _gameOverlayController.ApplyGameState(mainGameState);
    }

    private void OnGameStateChanged(MainGameState mainGameState)
    {
        ApplyGameState(mainGameState);
    }
    
}
