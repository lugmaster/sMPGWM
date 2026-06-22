#nullable enable
using System;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Enums.Game;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Ui.Game;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Controllers;

public partial class MainGameController : AbstractSingleton<MainGameController>
{
    private GameStates _state = GameStates.Active;
    private UiCanvas? _uiCanvas;

    public event Action<GameStates> Changed = delegate { };

    public GameStates State => _state;
    public bool IsActive => _state == GameStates.Active;
    public bool IsInMenu => _state == GameStates.InMenu;
    public bool IsInactive => _state == GameStates.Inactive;

    public void RegisterUiCanvas(UiCanvas uiCanvas)
    {
        ArgumentNullException.ThrowIfNull(uiCanvas);

        if (_uiCanvas != null && _uiCanvas != uiCanvas)
            throw new InvalidOperationException("Main game UI canvas is already registered.");

        _uiCanvas = uiCanvas;
        _uiCanvas.OverlayCloseRequested += OnOverlayCloseRequested;
        SetState(GameStates.Active);
    }

    public void UnregisterUiCanvas(UiCanvas uiCanvas)
    {
        ArgumentNullException.ThrowIfNull(uiCanvas);

        if (_uiCanvas != uiCanvas)
            return;

        _uiCanvas.OverlayCloseRequested -= OnOverlayCloseRequested;
        _uiCanvas = null;
        SetState(GameStates.Inactive);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_uiCanvas == null || IsInactive)
            return;

        if (@event.IsActionPressed("open_menu"))
        {
            Logger.Info("Main Menu toggled");
            Dispatch(MainGameAction.OpenMainMenu);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("open_inventory"))
        {
            Dispatch(MainGameAction.OpenInventory);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("open_map"))
        {
            Dispatch(MainGameAction.OpenMap);
            GetViewport().SetInputAsHandled();
        }
    }

    public void Dispatch(MainGameAction action)
    {
        if (_uiCanvas == null)
            return;

        switch (_state)
        {
            case GameStates.Active:
                ReduceActive(action);
                break;
            case GameStates.InMenu:
                ReduceInMenu(action);
                break;
            case GameStates.Inactive:
                break;
        }
    }

    private void ReduceActive(MainGameAction action)
    {
        switch (action)
        {
            case MainGameAction.OpenMainMenu:
                OpenOverlayMenuRequest(GameOverlayMenuType.Main);
                break;
            case MainGameAction.OpenInventory:
                OpenOverlayMenuRequest(GameOverlayMenuType.Inventory);
                break;
            case MainGameAction.OpenMap:
                OpenOverlayMenuRequest(GameOverlayMenuType.Map);
                break;
            case MainGameAction.CloseOverlayMenu:
                CloseOverlayMenuRequest();
                break;
        }
    }

    private void ReduceInMenu(MainGameAction action)
    {
        switch (action)
        {
            case MainGameAction.OpenMainMenu:
                OpenOverlayMenuRequest(GameOverlayMenuType.Main);
                break;
            case MainGameAction.OpenInventory:
                OpenOverlayMenuRequest(GameOverlayMenuType.Inventory);
                break;
            case MainGameAction.OpenMap:
                OpenOverlayMenuRequest(GameOverlayMenuType.Map);
                break;
            case MainGameAction.CloseOverlayMenu:
                CloseOverlayMenuRequest();
                break;
        }
    }

    public void SetState(GameStates state)
    {
        if (_state == state)
            return;

        _state = state;
        Changed(_state);
        Logger.Info($"Game state changed: {_state}");
    }

    private void OpenOverlayMenuRequest(GameOverlayMenuType menuType)
    {
        if (_uiCanvas == null)
            return;

        SetState(_uiCanvas.OpenOverlayMenu(menuType) ? GameStates.InMenu : GameStates.Active);
    }

    private void CloseOverlayMenuRequest()
    {
        if (_uiCanvas == null)
            return;

        _uiCanvas.CloseOverlayMenu();
        SetState(GameStates.Active);
    }

    private void OnOverlayCloseRequested()
    {
        Dispatch(MainGameAction.CloseOverlayMenu);
    }
}
