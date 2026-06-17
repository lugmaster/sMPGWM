using System;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;
using sMPGWM.Scripts.Ui.MainMenu.Base;

namespace sMPGWM.Scripts.Autoload;

public partial class InGameScreenManager : AbstractSingleton<InGameScreenManager>
{
    private Control _menuLayer = null!;
    private Control _menuHost = null!;
    private InGameMenuScreen? _currentMenu;
    private bool _pausedByCurrentMenu;

    public bool IsMenuOpen => _currentMenu != null;
    public bool IsGameInputBlocked => _currentMenu?.BlocksGameInput ?? false;

    public void Register(Control menuLayer, Control menuHost)
    {
        ArgumentNullException.ThrowIfNull(menuLayer);
        ArgumentNullException.ThrowIfNull(menuHost);

        if (_menuLayer != null || _menuHost != null)
            throw new InvalidOperationException("In-game screen manager is already registered.");

        _menuLayer = menuLayer;
        _menuHost = menuHost;

        _menuLayer.Visible = false;

        Logger.Info("Registered in-game screen manager nodes.");
    }

    public void Unregister(Control menuLayer, Control menuHost)
    {
        ArgumentNullException.ThrowIfNull(menuLayer);
        ArgumentNullException.ThrowIfNull(menuHost);

        if (_menuLayer != menuLayer || _menuHost != menuHost)
            throw new InvalidOperationException("Tried to unregister invalid in-game screen manager nodes.");

        CloseCurrentMenu();

        _menuLayer = null!;
        _menuHost = null!;

        Logger.Info("Unregistered in-game screen manager nodes.");
    }

    public void TogglePauseMenu()
    {
        ToggleMenu(InGameScreenSceneProvider.CreateMainMenu);
    }

    public void ToggleInventory()
    {
        ToggleMenu(InGameScreenSceneProvider.CreateInventoryMenu);
    }

    public void ToggleMap()
    {
        ToggleMenu(InGameScreenSceneProvider.CreateMapMenu);
    }

    public void CloseCurrentMenu()
    {
        if (_currentMenu == null)
            return;

        var oldMenu = _currentMenu;
        _currentMenu = null;

        oldMenu.QueueFree();
        _menuHost.Visible = false;

        if (GetTree().Paused)
            GetTree().Paused = false;

        Logger.Info("Closed in-game menu.");
    }

    private void ToggleMenu(Func<InGameMenuScreen> createMenu)
    {
        if (_currentMenu != null)
        {
            CloseCurrentMenu();
            return;
        }

        OpenMenu(createMenu());
    }

    private void OpenMenu(InGameMenuScreen nextMenu)
    {
        ArgumentNullException.ThrowIfNull(nextMenu);

        if (_menuHost == null)
            throw new InvalidOperationException("No in-game menu host registered.");

        CloseCurrentMenu();

        _menuHost.AddChild(nextMenu);
        _menuHost.Visible = true;

        _currentMenu = nextMenu;
        _currentMenu.MenuClosed += CloseCurrentMenu;

        _pausedByCurrentMenu = _currentMenu.PauseGame;

        if (_pausedByCurrentMenu)
            GetTree().Paused = true;

        Logger.Info($"Opened in-game menu: {_currentMenu.Name}");
    }
}