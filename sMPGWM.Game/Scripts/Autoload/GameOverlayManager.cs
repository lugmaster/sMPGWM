using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Enums.Ui;
using sMPGWM.Scripts.Provider;
using sMPGWM.Scripts.Ui.Base;

namespace sMPGWM.Scripts.Autoload;

public partial class GameOverlayManager : AbstractSingleton<GameOverlayManager>
{
    private readonly Dictionary<string, GameOverlayMenu> _cachedMenus = new();

    private Control _menuLayer = null!;
    private Control _menuHost = null!;
    private GameOverlayMenu? _currentMenu;
    private GameStates _gameState = GameStates.Active;

    public event Action<GameStates> GameStateChanged = delegate { };

    public GameStates GameState => _gameState;
    public bool IsMenuOpen => _currentMenu != null;

    public void Register(Control menuLayer, Control menuHost)
    {
        ArgumentNullException.ThrowIfNull(menuLayer);
        ArgumentNullException.ThrowIfNull(menuHost);

        if (_menuLayer != null || _menuHost != null)
            throw new InvalidOperationException("In-game screen manager is already registered.");

        _menuLayer = menuLayer;
        _menuHost = menuHost;
        _menuLayer.Visible = false;
        _menuHost.Visible = false;
        SetGameState(GameStates.Active);

        Logger.Info("Registered in-game screen manager nodes.");
    }

    public void Unregister(Control menuLayer, Control menuHost)
    {
        ArgumentNullException.ThrowIfNull(menuLayer);
        ArgumentNullException.ThrowIfNull(menuHost);

        if (_menuLayer != menuLayer || _menuHost != menuHost)
            throw new InvalidOperationException("Tried to unregister invalid in-game screen manager nodes.");

        DestroyCachedMenus();

        _menuLayer = null!;
        _menuHost = null!;
        _currentMenu = null;
        SetGameState(GameStates.Active);

        Logger.Info("Unregistered in-game screen manager nodes.");
    }

    public void ToggleMainMenu()
    {
        ToggleMenu("MainMenu", GameOverlayProvider.CreateMainMenu);
    }

    public void ToggleInventoryMenu()
    {
        ToggleMenu("InventoryMenu", GameOverlayProvider.CreateInventoryMenu);
    }

    public void ToggleMapMenu()
    {
        ToggleMenu("MapMenu", GameOverlayProvider.CreateMapMenu);
    }

    public void CloseCurrentMenu()
    {
        if (_currentMenu == null)
            return;

        _currentMenu.Visible = false;
        _currentMenu = null;

        _menuLayer.Visible = false;
        _menuHost.Visible = false;
        SetGameState(GameStates.Active);

        Logger.Info("Closed in-game menu.");
    }

    private void ToggleMenu(string key, Func<GameOverlayMenu> createMenu)
    {
        if (_currentMenu != null && _cachedMenus.TryGetValue(key, out var menu) && _currentMenu == menu)
        {
            CloseCurrentMenu();
            return;
        }

        OpenMenu(key, createMenu);
    }

    private void OpenMenu(string key, Func<GameOverlayMenu> createMenu)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(createMenu);

        if (_menuLayer == null || _menuHost == null)
            throw new InvalidOperationException("In-game screen manager is not registered.");

        HideCurrentMenuOnly(false);

        var nextMenu = GetOrCreateMenu(key, createMenu);

        nextMenu.Visible = true;
        _currentMenu = nextMenu;

        _menuLayer.Visible = true;
        _menuHost.Visible = true;
        SetGameState(GameStates.InMenu);

        Logger.Info($"Opened in-game menu: {_currentMenu.Name}");
    }

    private GameOverlayMenu GetOrCreateMenu(string key, Func<GameOverlayMenu> createMenu)
    {
        
        if (_cachedMenus.TryGetValue(key, out var cachedMenu))
            return cachedMenu;

        var menu = createMenu();

        menu.Visible = false;
        menu.MenuClosed += CloseCurrentMenu;

        _menuHost.AddChild(menu);
        _cachedMenus.Add(key, menu);

        Logger.Info($"Created cached in-game menu: {key}");

        return menu;
    }

    private void HideCurrentMenuOnly(bool updateGameState = true)
    {
        if (_currentMenu == null)
            return;

        _currentMenu.Visible = false;
        _currentMenu = null;

        if (updateGameState)
            SetGameState(GameStates.Active);
    }

    private void DestroyCachedMenus()
    {
        CloseCurrentMenu();

        foreach (var menu in _cachedMenus.Values)
        {
            menu.MenuClosed -= CloseCurrentMenu;
            menu.QueueFree();
        }

        _cachedMenus.Clear();
    }

    private void SetGameState(GameStates gameState)
    {
        if (_gameState == gameState)
            return;

        _gameState = gameState;
        GameStateChanged(_gameState);
        Logger.Info($"Game UI state changed: {_gameState}");
    }
}
