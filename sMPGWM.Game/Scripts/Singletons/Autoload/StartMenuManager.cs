using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Provider;
using sMPGWM.Scripts.Singletons.Autoload.Base;

namespace sMPGWM.Scripts.Singletons.Autoload;

public partial class StartMenuManager : AbstractSingleton<StartMenuManager>
{
    public void ShowJoinGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowJoinGameMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateJoinGameMenu());
    }
    
    public void ShowHostGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowHostGameMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateHostGameMenu());
    }

    public void ShowSettingsMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowSettingsMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateSettingsMenu());
    }

    public void InitializeGame()
    {
        GameHandler.Instance.StartGame();
    }
    
    private readonly Stack<Control> _screenHistory = new();

    private Control _currentScreen = null!;

    public void RegisterInitialScreen(Control screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (_currentScreen != null)
        {
            Logger.Error($"{nameof(StartMenuManager)}.{nameof(RegisterInitialScreen)} reached duplicate initial screen.");
            throw new InvalidOperationException("Initial screen is already registered.");
        }

        _currentScreen = screen;
        _currentScreen.Visible = true;

        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(RegisterInitialScreen)} registered initial screen {screen.Name}.");
    }

    public void NavigateTo(Control nextScreen)
    {
        ArgumentNullException.ThrowIfNull(nextScreen);

        if (_currentScreen == null)
        {
            Logger.Error($"{nameof(StartMenuManager)}.{nameof(NavigateTo)} reached missing current screen.");
            throw new InvalidOperationException("No current screen registered.");
        }

        var parent = _currentScreen.GetParent();

        if (parent == null)
        {
            Logger.Error($"{nameof(StartMenuManager)}.{nameof(NavigateTo)} found no parent for current screen {_currentScreen.Name}.");
            throw new InvalidOperationException(
                $"Current screen '{_currentScreen.Name}' has no parent.");
        }

        _currentScreen.Visible = false;
        _screenHistory.Push(_currentScreen);

        parent.AddChild(nextScreen);

        nextScreen.Visible = true;
        _currentScreen = nextScreen;

        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(NavigateTo)} navigated to screen {nextScreen.Name}.");
    }

    public void GoBack()
    {
        if (_currentScreen == null)
        {
            Logger.Error($"{nameof(StartMenuManager)}.{nameof(GoBack)} reached missing current screen.");
            throw new InvalidOperationException("No current screen registered.");
        }

        if (_screenHistory.Count == 0)
        {
            Logger.Warning($"{nameof(StartMenuManager)}.{nameof(GoBack)} cannot go back because screen history is empty.");
            return;
        }

        var oldScreen = _currentScreen;
        var previousScreen = _screenHistory.Pop();

        _currentScreen = previousScreen;
        _currentScreen.Visible = true;

        oldScreen.QueueFree();

        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(GoBack)} returned to screen {_currentScreen.Name}.");
    }

    private bool CanGoBack()
    {
        return _screenHistory.Count > 0;
    }

    protected override void OnExitTree()
    {
        ClearManagedScreens();
    }

    private void ClearManagedScreens()
    {
        SafeQueueFree(_currentScreen);

        foreach (var screen in _screenHistory)
            SafeQueueFree(screen);

        _currentScreen = null!;
        _screenHistory.Clear();
    }

    private static void SafeQueueFree(Control screen)
    {
        if (screen == null)
            return;

        try
        {
            if (GodotObject.IsInstanceValid(screen) && !screen.IsQueuedForDeletion())
                screen.QueueFree();
        }
        catch (ObjectDisposedException)
        {
            // Scene transitions can dispose nodes before the manager clears stale wrappers.
        }
    }
}
