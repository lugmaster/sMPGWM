using System;
using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Enums.Game;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Mocks;
using sMPGWM.Scripts.Ui.Game.Hud;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game;

public partial class UiCanvas : CanvasLayer
{

    private Control _menuLayer = null!;
    private Control _menuHost = null!;
    private PlayerHud _playerHud = null!;
    private Action<int> _hotbarSlotPressedHandler = null!;
    
    public override void _Ready()
    {
        _menuLayer = GetNode<Control>("%MenuLayer");
        _menuHost = GetNode<Control>("%MenuHost");
        _playerHud = GetNode<PlayerHud>("%PlayerHud");

        GameOverlayManager.Instance.GameStateChanged += OnGameStateChanged;
        GameOverlayManager.Instance.Register(_menuLayer, _menuHost);
        OnGameStateChanged(GameOverlayManager.Instance.GameState);

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

            if (_menuLayer != null && _menuHost != null)
                GameOverlayManager.Instance.Unregister(_menuLayer, _menuHost);
        }
        finally
        {
            GameOverlayManager.Instance.GameStateChanged -= OnGameStateChanged;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("open_menu"))
        {
            Logger.Info("Main Menu toggled");
            GameOverlayManager.Instance.ToggleMainMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("open_inventory"))
        {
            GameOverlayManager.Instance.ToggleInventoryMenu();
            GetViewport().SetInputAsHandled();
            return;
        }
        
        if (@event.IsActionPressed("open_map"))
        {
            GameOverlayManager.Instance.ToggleMapMenu();
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    private void OnGameStateChanged(GameStates gameState)
    {
        _playerHud.SetInputProcessing(gameState == GameStates.Active);
    }
}
