using System;
using System.Threading.Tasks;
using Godot;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameFlowSystem;

public partial class GameFlowManager : AbstractSingleton<GameFlowManager>
{
    private GameFlowState _gameFlowState = GameFlowState.StartMenu;
    private GameFlowState _pendingGameFlowState = GameFlowState.StartMenu;
    private NetworkStartupData? _pendingNetworkStartup;
    private long _navigationGeneration;

    public NetworkStartupData? ConsumeNetworkStartup()
    {
        var startup = _pendingNetworkStartup;
        _pendingNetworkStartup = null;
        return startup;
    }

    protected override void OnReady()
    {
        CallDeferred(nameof(StartDiagnosticFromArguments));
    }

    private void StartDiagnosticFromArguments()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument != "--enet-host" && argument != "--enet-client") continue;
            var config = new NetworkStartupData(NetworkType.Lan,
                argument == "--enet-host" ? NetworkRole.Host : NetworkRole.Client,
                GameConfigSystem.GameConfig.ServerIpAddress, GameConfigSystem.GameConfig.Port,
                GameConfigSystem.GameConfig.MaxPlayers, GameConfigSystem.GameConfig.ConnectionTimeout, true);
            StartNetworkGame(config);
            return;
        }
    }

    public void StartNetworkGame(NetworkStartupData startup)
    {
        if (_gameFlowState == GameFlowState.Loading || NetworkManager.Instance != null) return;
        _pendingNetworkStartup = startup;
        ChangeScene(GameFlowSceneProvider.MainGameScene, GameFlowState.InGame);
    }
    
    // TODO use this to handle SuddenExits and inform other systems
    public event Action OnSuddenExit;
    
    public GameFlowState GameFlowState => _gameFlowState;

    protected override void OnEnterTree()
    {
        GetTree().SceneChanged += OnSceneChanged;
    }

    protected override void OnExitTree()
    {
        ++_navigationGeneration;
        _pendingNetworkStartup = null;
        GetTree().SceneChanged -= OnSceneChanged;
    }


    public void QuitToStartMenu(string reason = null, int delayMs = 35)
    {
        _pendingNetworkStartup = null;
        NetworkManager.Instance?.StopSession();
        _gameFlowState = GameFlowState.Disconnecting;
        
        if (!string.IsNullOrEmpty(reason))
        {
            Logger.Debug(
                $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenu)} quit triggered with {nameof(reason)}={reason}.");
        }

        Instance.CallDeferred(nameof(Instance.StartQuitAsync), delayMs, ++_navigationGeneration);
    }

    public void QuitToStartMenuBlocking(string reason, bool showAsError = true)
    {
        _pendingNetworkStartup = null;
        NetworkManager.Instance?.StopSession();
        _gameFlowState = GameFlowState.Disconnecting;
        Logger.Debug(
            $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenuBlocking)} graceful quit triggered with {nameof(reason)}={reason}.");

        var combinedReason = showAsError
            ? $"Error: {reason}\nClick to return to Main Menu."
            : $"Success: {reason}\nClick to return to Main Menu.";
        // StartMenuManager.Instance.ShowNotificationBlocking(
        //     combinedReason,
        //     () => Instance.CallDeferred(nameof(QuitToStartMenuInternal))
        // );
        if (showAsError)
        {
            Logger.Error(combinedReason);
        }
        else
        {
            Logger.Debug(
                $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenuBlocking)} completed with {nameof(combinedReason)}={combinedReason}.");
        }

        Instance.CallDeferred(nameof(QuitToStartMenuInternal), ++_navigationGeneration);
    }

    public void QuitGame(string reason)
    {
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(QuitGame)} requested with {nameof(reason)}={reason}.");
        NetworkManager.Instance?.StopSession();
        Steam.SteamworksHelper.ShutdownSteam();
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(QuitGame)} completed Steam shutdown.");
        Instance.CallDeferred(nameof(QuitGameInternal));
    }

    private void QuitGameInternal()
    {
        GetTree().Quit();
    }

    private async void StartQuitAsync(int delayMs, long generation)
    {
        await Task.Delay(Math.Max(0, delayMs));
        if (!IsInsideTree() || generation != _navigationGeneration) return;
        CallDeferred(nameof(QuitToStartMenuInternal), generation);
    }

    public void StartGame(NetworkRole networkRole, NetworkType networkType)
    {
        StartNetworkGame(new NetworkStartupData(networkType, networkRole,
            GameConfigSystem.GameConfig.ServerIpAddress, GameConfigSystem.GameConfig.Port,
            GameConfigSystem.GameConfig.MaxPlayers, GameConfigSystem.GameConfig.ConnectionTimeout));
    }

    private void QuitToStartMenuInternal(long generation)
    {
        if (generation != _navigationGeneration) return;
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(QuitToStartMenuInternal)} returning to start menu.");
        ChangeScene(GameFlowSceneProvider.StartMenuScene, GameFlowState.StartMenu);
    }

    public void QuitWithOsError(string reason)
    {
        Logger.Error($"Error: {reason}");
        OS.Alert($"Error: {reason}");
        if (!Engine.IsEditorHint())
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            tree?.Quit(1);
        }
    }


    private void ChangeScene(PackedScene scene, GameFlowState targetState)
    {
        ++_navigationGeneration;
        _gameFlowState = GameFlowState.Loading;
        _pendingGameFlowState = targetState;

        var error = GetTree().ChangeSceneToPacked(scene);

        if (error != Error.Ok)
        {
            _pendingNetworkStartup = null;
            Logger.Error(
                $"{nameof(GameFlowManager)} scene change failed with {nameof(error)}={error}."
            );

            return;
        }
    }

    private void OnSceneChanged()
    {
        _gameFlowState = _pendingGameFlowState;
    }
}
