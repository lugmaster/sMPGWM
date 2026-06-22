#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Provider;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.Game;

public partial class GameOverlayController : CenterContainer
{
    private readonly Dictionary<GameOverlayMenuType, GameOverlayMenu> _cachedMenus = new();

    private Control _menuLayer = null!;
    private GameOverlayMenu? _currentMenu;
    private bool _isDisabled;

    public event Action CloseRequested = delegate { };

    public GameOverlayMenuType? CurrentMenuType { get; private set; }
    public bool IsMenuOpen => _currentMenu != null;

    public override void _Ready()
    {
        _menuLayer = GetParent<Control>();
        SetOverlayVisible(false);
    }

    public override void _ExitTree()
    {
        DestroyCachedMenus();
    }

    public bool ToggleMenu(GameOverlayMenuType menuType)
    {
        if (_isDisabled)
            return false;

        if (CurrentMenuType == menuType)
        {
            CloseMenu();
            return false;
        }

        OpenMenu(menuType);
        return true;
    }

    public void OpenMenu(GameOverlayMenuType menuType)
    {
        if (_isDisabled)
            return;

        HideCurrentMenu();

        var nextMenu = GetOrCreateMenu(menuType);

        nextMenu.Visible = true;
        _currentMenu = nextMenu;
        CurrentMenuType = menuType;

        SetOverlayVisible(true);

        Logger.Info($"Opened in-game menu: {_currentMenu.Name}");
    }

    public void CloseMenu()
    {
        if (_currentMenu != null)
        {
            HideCurrentMenu();
            Logger.Info("Closed in-game menu.");
        }
        else
        {
            CurrentMenuType = null;
        }

        SetOverlayVisible(false);
    }

    public void ApplyGameState(GameStates gameState)
    {
        switch (gameState)
        {
            case GameStates.Active:
                SetEnabled();
                MouseFilter = MouseFilterEnum.Ignore;
                CloseMenu();
                break;
            case GameStates.InMenu:
                SetEnabled();
                MouseFilter = MouseFilterEnum.Stop;
                break;
            case GameStates.Inactive:
                SetDisabled();
                CloseMenu();
                break;
        }
    }

    private void SetEnabled()
    {
        _isDisabled = false;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    private void SetDisabled()
    {
        _isDisabled = true;
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Disabled;
    }

    private GameOverlayMenu GetOrCreateMenu(GameOverlayMenuType menuType)
    {
        if (_cachedMenus.TryGetValue(menuType, out var cachedMenu))
            return cachedMenu;

        var menu = CreateMenu(menuType);

        menu.Visible = false;
        menu.MenuClosed += RequestClose;

        AddChild(menu);
        _cachedMenus.Add(menuType, menu);

        Logger.Info($"Created cached in-game menu: {menuType}");

        return menu;
    }

    private static GameOverlayMenu CreateMenu(GameOverlayMenuType menuType)
    {
        return menuType switch
        {
            GameOverlayMenuType.Main => GameOverlayProvider.CreateMainMenu(),
            GameOverlayMenuType.Inventory => GameOverlayProvider.CreateInventoryMenu(),
            GameOverlayMenuType.Map => GameOverlayProvider.CreateMapMenu(),
            _ => throw new ArgumentOutOfRangeException(nameof(menuType), menuType, null)
        };
    }

    private void RequestClose()
    {
        if (_isDisabled)
            return;

        CloseRequested();
    }

    private void HideCurrentMenu()
    {
        if (_currentMenu == null)
            return;

        _currentMenu.Visible = false;
        _currentMenu = null;
        CurrentMenuType = null;
    }

    private void SetOverlayVisible(bool visible)
    {
        _menuLayer.Visible = visible;
        Visible = visible;
    }

    private void DestroyCachedMenus()
    {
        CloseMenu();

        foreach (var menu in _cachedMenus.Values)
        {
            menu.MenuClosed -= RequestClose;
            menu.QueueFree();
        }

        _cachedMenus.Clear();
    }
}
