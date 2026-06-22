#nullable enable
using System;
using Godot;
using sMPGWM.Scripts.Enums.GameState;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Provider;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game;

public partial class GameOverlayController : CenterContainer
{
    private Control _menuLayer = null!;
    private GameOverlayMenu? _currentMenu;
    private MenuActionType? _currentOpenMenuAction;
    private bool _isDisabled;

    public event Action<MenuActionType>? OnMenuAction;

    public bool IsMenuOpen => _currentMenu != null;

    public override void _Ready()
    {
        _menuLayer = GetParent<Control>();
        EnableVisibility(false);
    }

    public override void _ExitTree()
    {
        DestroyCurrentMenu();
    }

    public void ApplyMenuAction(MenuActionType actionType)
    {
        if (_isDisabled)
            return;
        
        if (actionType == MenuActionType.CloseOverlayMenu)
        {
            DestroyCurrentMenu();
            return;
        }

        if (actionType == _currentOpenMenuAction)
        {
            CloseMenu();
            return;
        }

        OpenMenu(actionType);
    }

    private void CloseMenu()
    {
        Logger.Info("Request Menu Close");
        OnMenuAction?.Invoke(MenuActionType.CloseOverlayMenu);
    }

    public void ApplyGameState(MainGameState mainGameState)
    {
        switch (mainGameState)
        {
            case MainGameState.Active:
                SetEnabled();
                break;

            case MainGameState.InMenu:
                SetEnabled();
                break;

            case MainGameState.Inactive:
                SetDisabled();
                DestroyCurrentMenu();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mainGameState), mainGameState, null);
        }
    }

    private void OpenMenu(MenuActionType actionType)
    {
        DestroyCurrentMenu();

        var menu = CreateMenu(actionType);

        menu.OnMenuClosed += CloseMenu;

        AddChild(menu);

        _currentMenu = menu;
        _currentOpenMenuAction = actionType;

        EnableVisibility(true);

        Logger.Info($"Opened in-game menu: {_currentMenu.Name}");
    }

    private void DestroyCurrentMenu()
    {
        if (_currentMenu == null)
        {
            EnableVisibility(false);
            _currentOpenMenuAction = null;
            return;
        }


        _currentMenu.OnMenuClosed -= CloseMenu;
        _currentMenu.QueueFree();

        _currentMenu = null;
        _currentOpenMenuAction = null;
        EnableVisibility(false);
    }

    private void SetEnabled()
    {
        _isDisabled = false;
    }

    private void SetDisabled()
    {
        _isDisabled = true;
    }

    private static GameOverlayMenu CreateMenu(MenuActionType actionType)
    {
        return actionType switch
        {
            MenuActionType.OpenMainMenu => GameOverlayProvider.CreateMainMenu(),
            MenuActionType.OpenInventoryMenu => GameOverlayProvider.CreateInventoryMenu(),
            MenuActionType.OpenMapMenu => GameOverlayProvider.CreateMapMenu(),

            MenuActionType.CloseOverlayMenu =>
                throw new InvalidOperationException(
                    $"{nameof(MenuActionType.CloseOverlayMenu)} does not create a menu."),

            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
        };
    }

    private void EnableVisibility(bool visible)
    {
        _menuLayer.SetVisible(visible);
        SetVisible(visible);
    }


    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("MainMenu"))
        {
            Logger.Info("Main Menu toggled");
            OnMenuAction?.Invoke(MenuActionType.OpenMainMenu);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("InventoryMenu"))
        {
            OnMenuAction?.Invoke(MenuActionType.OpenInventoryMenu);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("MapMenu"))
        {
            OnMenuAction?.Invoke(MenuActionType.OpenMapMenu);
            GetViewport().SetInputAsHandled();
        }
    }
}