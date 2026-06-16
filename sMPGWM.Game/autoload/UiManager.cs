using System;
using System.Collections.Generic;
using Godot;

namespace sMPGWM.autoload;

public partial class UiManager : AbstractAutoload<UiManager>
{
    private readonly Stack<Control> _screenHistory = new();

    private Control _currentScreen = null!;

    public void RegisterCurrentScreen(Control screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (_currentScreen != null)
            throw new InvalidOperationException("Current screen is already registered.");

        _currentScreen = screen;

        GameLogger.Info($"Registered current UI screen: {screen.Name}");
    }

    public void NavigateTo(PackedScene screenScene)
    {
        ArgumentNullException.ThrowIfNull(screenScene);

        if (_currentScreen == null)
            throw new InvalidOperationException("No current screen registered.");

        var parent = _currentScreen.GetParent();

        if (parent == null)
            throw new InvalidOperationException($"Current screen '{_currentScreen.Name}' has no parent.");

        var nextScreen = screenScene.Instantiate<Control>();

        _currentScreen.Visible = false;
        _screenHistory.Push(_currentScreen);

        parent.AddChild(nextScreen);
        _currentScreen = nextScreen;

        GameLogger.Info($"Navigated to screen: {nextScreen.Name}");
    }

    public void GoBack()
    {
        if (_currentScreen == null)
            throw new InvalidOperationException("No current screen registered.");

        if (_screenHistory.Count == 0)
        {
            GameLogger.Warning("Cannot go back. Screen history is empty.");
            return;
        }

        var oldScreen = _currentScreen;
        var previousScreen = _screenHistory.Pop();

        _currentScreen = previousScreen;
        _currentScreen.Visible = true;

        oldScreen.QueueFree();

        GameLogger.Info($"Returned to screen: {_currentScreen.Name}");
    }

    public bool CanGoBack()
    {
        return _screenHistory.Count > 0;
    }

    public void ShowJoinScreen()
    {
        GameLogger.Info("ShowJoinScreen requested.");
    }

    public void ShowHostScreen()
    {
        GameLogger.Info("ShowHostScreen requested.");
    }

    public void ShowSettingsScreen()
    {
        GameLogger.Info("ShowSettingsScreen requested.");
    }

    public void QuitGame()
    {
        GameLogger.Info("Quit game requested.");
        GetTree().Quit();
    }
}