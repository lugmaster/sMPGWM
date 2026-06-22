#nullable enable
using System;
using Godot;
using sMPGWM.Scripts.Controllers;
using sMPGWM.Scripts.Enums.Game;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Mocks;
using sMPGWM.Scripts.Ui.Game.Hud;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game;

public partial class UiCanvas : CanvasLayer
{
    private PlayerHud _playerHud = null!;
    private GameOverlayController _gameOverlayController = null!;
    private Action<int> _hotbarSlotPressedHandler = null!;

    public event Action OverlayCloseRequested = delegate { };

    public override void _Ready()
    {
        _playerHud = GetNode<PlayerHud>("%PlayerHud");
        _gameOverlayController = GetNode<GameOverlayController>("%MenuHost");

        MainGameController.Instance.Changed += OnGameStateChanged;
        _gameOverlayController.CloseRequested += OnOverlayCloseRequested;
        MainGameController.Instance.RegisterUiCanvas(this);
        OnGameStateChanged(MainGameController.Instance.State);

        _playerHud.Bind(PlayerMock.Instance.LivingEntityState,
        [
            StatTypes.Health,
                StatTypes.Shield,
                StatTypes.Energy
        ]);
        _playerHud.SetHotbarIcons(PlayerMock.Instance.GetHotBarSkills());
        _hotbarSlotPressedHandler = PlayerMock.Instance.OnHotbarSlotPressed;
        _playerHud.HotbarSlotPressed += _hotbarSlotPressedHandler;

        Logger.Info("UiCanvas loaded");
    }

    public override void _ExitTree()
    {
        try
        {
            if (_playerHud != null && _hotbarSlotPressedHandler != null)
                _playerHud.HotbarSlotPressed -= _hotbarSlotPressedHandler;

            if (_gameOverlayController != null)
                _gameOverlayController.CloseRequested -= OnOverlayCloseRequested;

            MainGameController.Instance.UnregisterUiCanvas(this);
        }
        finally
        {
            MainGameController.Instance.Changed -= OnGameStateChanged;
        }
    }

    public bool OpenOverlayMenu(GameOverlayMenuType menuType)
    {
        return _gameOverlayController.ToggleMenu(menuType);
    }

    public void CloseOverlayMenu()
    {
        _gameOverlayController.CloseMenu();
    }

    public void ApplyGameState(GameStates gameState)
    {
        _playerHud.SetInputProcessing(gameState == GameStates.Active);
        _gameOverlayController.ApplyGameState(gameState);
    }

    private void OnGameStateChanged(GameStates gameState)
    {
        ApplyGameState(gameState);
    }

    private void OnOverlayCloseRequested()
    {
        OverlayCloseRequested();
    }
}
