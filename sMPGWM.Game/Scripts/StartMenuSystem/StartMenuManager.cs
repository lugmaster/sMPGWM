using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Steam;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class StartMenuManager : AbstractSingleton<StartMenuManager>
{
    public void ShowJoinGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowJoinGameMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateJoinGameMenu());
    }
    
    public void ShowJoinLanGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowJoinLanGameMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateJoinLanGameMenu());
    }
    
    public void ShowJoinSteamGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowJoinSteamGameMenu)} requested.");
        SteamworksHelper.Instance.SubscribeLobbyEvents();
        NavigateTo(StartMenuProvider.CreateJoinSteamGameMenu());
    }
    
    public void ShowHostGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowHostGameMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateHostGameMenu());
    }
    
    public void ShowHostLanGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowHostLanGameMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateHostLanGameMenu());
    }
    
    public void ShowHostSteamGameMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowHostSteamGameMenu)} requested.");
        SteamworksHelper.Instance.SubscribeLobbyEvents();
        NavigateTo(StartMenuProvider.CreateHostSteamGameMenu());
    }

    public void ShowSettingsMenu()
    {
        Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ShowSettingsMenu)} requested.");
        NavigateTo(StartMenuProvider.CreateSettingsMenu());
    }

    public void InitializeGame(NetworkRole networkRole, NetworkType networkType)
    {
        GameFlowManager.Instance.StartGame(networkRole, networkType);
    }
    
    public void StartSteamGame(NetworkRole role, ulong lobbyId = 0)
    {
        GameFlowManager.Instance.StartNetworkGame(new NetworkStartupData(role, NetworkType.Steam,
            GameConfig.MaxPlayers,
            GameConfig.ConnectionTimeout, LobbyId: lobbyId));
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
            if (IsInstanceValid(screen) && !screen.IsQueuedForDeletion())
                screen.QueueFree();
        }
        catch (ObjectDisposedException e)
        {
            // Scene transitions can dispose nodes before the manager clears stale wrappers.
            Logger.Debug($"{nameof(StartMenuManager)}.{nameof(ObjectDisposedException)}: {e}");
        }
    }
}
