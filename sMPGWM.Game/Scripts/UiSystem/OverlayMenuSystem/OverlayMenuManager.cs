using System;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using GameOverlayMenu = sMPGWM.Scripts.Util.UiComponents.GameOverlayMenu;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UiSystem.GameOverlaySystem;

public partial class OverlayMenuManager : CenterContainer
{
    private Control _menuLayer = null!;
    private GameOverlayMenu? _currentMenu;
    private MenuActionType? _currentOpenMenuAction;
    private bool _isDisabled;

    public event Action<MenuActionType>? OnMenuAction;

    public bool IsMenuOpen => _currentMenu != null;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(OverlayMenuManager)}.{nameof(_EnterTree)} called.");
        _menuLayer = GetParent<Control>();
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(OverlayMenuManager)}.{nameof(_Ready)} called.");
        EnableVisibility(false);
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(OverlayMenuManager)}.{nameof(_ExitTree)} called.");
        DestroyCurrentMenu();
    }

    public void ApplyMenuAction(MenuActionType actionType)
    {
        Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(ApplyMenuAction)} called with {nameof(actionType)}={actionType}.");
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
        Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(CloseMenu)} requested menu close.");
        OnMenuAction?.Invoke(MenuActionType.CloseOverlayMenu);
    }

    public void ApplyGameState(MainGameState mainGameState)
    {
        Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(ApplyGameState)} called with {nameof(mainGameState)}={mainGameState}.");
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
                Logger.Error($"{nameof(OverlayMenuManager)}.{nameof(ApplyGameState)} reached unsupported {nameof(mainGameState)}={mainGameState}.");
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

        Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(OpenMenu)} opened in-game menu {_currentMenu.Name}.");
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
                throw CreateCloseOverlayMenuException(),

            _ => throw CreateUnsupportedMenuActionException(actionType)
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
            Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(_UnhandledInput)} toggled main menu.");
            OnMenuAction?.Invoke(MenuActionType.OpenMainMenu);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("InventoryMenu"))
        {
            Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(_UnhandledInput)} toggled inventory menu.");
            OnMenuAction?.Invoke(MenuActionType.OpenInventoryMenu);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("MapMenu"))
        {
            Logger.Debug($"{nameof(OverlayMenuManager)}.{nameof(_UnhandledInput)} toggled map menu.");
            OnMenuAction?.Invoke(MenuActionType.OpenMapMenu);
            GetViewport().SetInputAsHandled();
        }
    }

    private static InvalidOperationException CreateCloseOverlayMenuException()
    {
        Logger.Error($"{nameof(OverlayMenuManager)}.{nameof(CreateMenu)} reached {nameof(MenuActionType.CloseOverlayMenu)} which does not create a menu.");
        return new InvalidOperationException($"{nameof(MenuActionType.CloseOverlayMenu)} does not create a menu.");
    }

    private static ArgumentOutOfRangeException CreateUnsupportedMenuActionException(MenuActionType actionType)
    {
        Logger.Error($"{nameof(OverlayMenuManager)}.{nameof(CreateMenu)} reached unsupported {nameof(actionType)}={actionType}.");
        return new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
    }
}
