using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class StartMenuManager : AbstractSingleton<StartMenuManager>
{
    public void ShowJoinGameMenu()
    {
        Logger.Info("ShowJoinGameMenu requested.");
        NavigateTo(StartMenuProvider.CreateJoinGameMenu());
    }
    
    public void ShowHostGameMenu()
    {
        Logger.Info("ShowHostGameMenu requested.");
        NavigateTo(StartMenuProvider.CreateHostGameMenu());
    }

    public void ShowSettingsMenu()
    {
        Logger.Info("ShowSettingsMenu requested.");
        NavigateTo(StartMenuProvider.CreateSettingsMenu());
    }

    public void InitializeGame()
    {
        TransitToNewScene(BaseSceneProvider.GetMainGameScene());
    }

    public void LoadStartMenu()
    {
        TransitToNewScene(BaseSceneProvider.GetStartMenuScene());
    }
    
    private readonly Stack<Control> _screenHistory = new();

    private Control _currentScreen = null!;

    public void RegisterInitialScreen(Control screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (_currentScreen != null)
            throw new InvalidOperationException("Initial screen is already registered.");

        _currentScreen = screen;
        _currentScreen.Visible = true;

        Logger.Info($"Registered initial screen: {screen.Name}");
    }

    public void NavigateTo(Control nextScreen)
    {
        ArgumentNullException.ThrowIfNull(nextScreen);

        if (_currentScreen == null)
            throw new InvalidOperationException("No current screen registered.");

        var parent = _currentScreen.GetParent();

        if (parent == null)
            throw new InvalidOperationException(
                $"Current screen '{_currentScreen.Name}' has no parent.");

        _currentScreen.Visible = false;
        _screenHistory.Push(_currentScreen);

        parent.AddChild(nextScreen);

        nextScreen.Visible = true;
        _currentScreen = nextScreen;

        Logger.Info($"Navigated to screen: {nextScreen.Name}");
    }

    //Loads entirely new Scene and cleans up old history. Used for starting game and coming back to main menu
    private void TransitToNewScene(PackedScene newScreen)
    {
        if (newScreen == null)
        {
            throw new InvalidOperationException($"Error when transiting to {nameof(newScreen)} - cannot be null.");
        }
        Logger.Info("Transiting to new screen.");

        _currentScreen = null!;
        _screenHistory.Clear();

        GetTree().ChangeSceneToPacked(newScreen);
    }

    public void GoBack()
    {
        if (_currentScreen == null)
            throw new InvalidOperationException("No current screen registered.");

        if (_screenHistory.Count == 0)
        {
            Logger.Warning("Cannot go back. Screen history is empty.");
            return;
        }

        var oldScreen = _currentScreen;
        var previousScreen = _screenHistory.Pop();

        _currentScreen = previousScreen;
        _currentScreen.Visible = true;

        oldScreen.QueueFree();

        Logger.Info($"Returned to screen: {_currentScreen.Name}");
    }

    private bool CanGoBack()
    {
        return _screenHistory.Count > 0;
    }
}
