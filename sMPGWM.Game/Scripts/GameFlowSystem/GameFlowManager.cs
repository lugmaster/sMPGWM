using System;
using System.Threading.Tasks;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameFlowSystem;

public partial class GameFlowManager : AbstractSingleton<GameFlowManager>
{
    private GameFlowState _gameFlowState = GameFlowState.StartMenu;
    private NetworkStartupData? _pendingStartupData;
    private long _navigationGeneration;
    internal SessionCoordinator? Session { get; private set; }

    public NetworkStartupData? ConsumeNetworkStartup()
    {
        var startup = _pendingStartupData;
        _pendingStartupData = null;
        return startup;
    }

    public void StartNetworkGame(NetworkStartupData startupData)
    {
        if (_gameFlowState != GameFlowState.StartMenu) return;
        Session = new SessionCoordinator(CompleteLoading, PresentSessionFailure,
            error => Logger.Error($"Session cleanup failed: {error}"));
        _pendingStartupData = startupData;
        ChangeScene(GameFlowSceneProvider.MainGameScene, GameFlowState.Loading);
    }
    
    public event Action<GameFlowState>? StateChanged;

    private void SetState(GameFlowState state)
    {
        if (_gameFlowState == state) return;
        _gameFlowState = state;
        if (state == GameFlowState.Loading)
            ModalController.Instance.ShowLoadingScreen(() =>
                GetTree().CurrentScene?.SceneFilePath == GameFlowSceneProvider.MainGameScene.ResourcePath
                    ? (50, "Connecting session and spawning players...")
                    : (0, "Loading game scene..."));
        else
            ModalController.Instance.HideLoadingScreen();
        Logger.Info($"Game flow: {state}");
        StateChanged?.Invoke(state);
    }

    public void CompleteLoading()
    {
        if (_gameFlowState == GameFlowState.Loading) SetState(GameFlowState.InGame);
    }
    
    public GameFlowState GameFlowState => _gameFlowState;

    protected override void OnEnterTree()
    {
        GetTree().SceneChanged += OnSceneChanged;
        if (Array.Exists(OS.GetCmdlineUserArgs(), value => value is "--shield-check-host" or "--shield-check-client")) // TEST-SUPPORT: SHIELD-RUN-ENTRY-001 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
            AddChild(new Util.Mocks.ShieldSliceVerification()); // TEST-SUPPORT: SHIELD-RUN-ENTRY-002 | Tests: docs/plans/evidence/gameplay-increment-01-shield-capacity.md#enet-normal-startup
    }

    protected override void OnExitTree()
    {
        Session?.Stop();
        ++_navigationGeneration;
        _pendingStartupData = null;
        GetTree().SceneChanged -= OnSceneChanged;
    }


    public void QuitToStartMenu(string? reason = null, int delayMs = 35)
    {
        _pendingStartupData = null;
        SetState(GameFlowState.Disconnecting);
        Session?.Stop();
        
        if (!string.IsNullOrEmpty(reason))
        {
            Logger.Debug(
                $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenu)} quit triggered with {nameof(reason)}={reason}.");
        }

        Instance.CallDeferred(nameof(Instance.StartQuitAsync), delayMs, ++_navigationGeneration);
    }

    public void QuitToStartMenuBlocking(string reason, bool showAsError = true)
    {
        _pendingStartupData = null;
        SetState(GameFlowState.Disconnecting);
        Session?.Stop();
        PresentSessionResult(reason, showAsError);
    }

    private void PresentSessionFailure(string reason)
    {
        _pendingStartupData = null;
        SetState(GameFlowState.Disconnecting);
        PresentSessionResult(reason, true);
    }

    private void PresentSessionResult(string reason, bool showAsError)
    {
        Logger.Debug(
            $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenuBlocking)} graceful quit triggered with {nameof(reason)}={reason}.");

        var combinedReason = showAsError
            ? $"Error: {reason}\nClick to return to Main Menu."
            : $"Success: {reason}\nClick to return to Main Menu.";
        if (showAsError)
        {
            Logger.Error(combinedReason);
        }
        else
        {
            Logger.Debug(
                $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenuBlocking)} completed with {nameof(combinedReason)}={combinedReason}.");
        }

        var generation = ++_navigationGeneration;
        ModalController.Instance.ShowAlert(
            GD.Load<Texture2D>("res://assets/mocks/diamond.png"),
            showAsError ? "Error" : "Success",
            combinedReason,
            () => CallDeferred(nameof(QuitToStartMenuInternal), generation));
    }

    public void QuitGame(string reason)
    {
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(QuitGame)} requested with {nameof(reason)}={reason}.");
        SetState(GameFlowState.Disconnecting);
        Session?.Stop();
        if (Steam.SteamworksHelper.Instance != null) Steam.SteamworksHelper.ShutdownSteam();
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
        StartNetworkGame(new NetworkStartupData(networkRole, networkType,
            GameConfigSystem.GameConfig.MaxPlayers, GameConfigSystem.GameConfig.ConnectionTimeout,
            GameConfigSystem.GameConfig.ServerIpAddress, GameConfigSystem.GameConfig.Port));
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
        if (targetState == GameFlowState.Loading) SetState(GameFlowState.Loading);

        var error = GetTree().ChangeSceneToPacked(scene);

        if (error != Error.Ok)
        {
            _pendingStartupData = null;
            Logger.Error(
                $"{nameof(GameFlowManager)} scene change failed with {nameof(error)}={error}."
            );
            if (targetState == GameFlowState.Loading) Session?.Fail($"Cannot load game: {error}");
            return;
        }
    }

    private void OnSceneChanged()
    {
        if (_gameFlowState == GameFlowState.Disconnecting &&
            GetTree().CurrentScene?.SceneFilePath == GameFlowSceneProvider.StartMenuScene.ResourcePath)
            SetState(GameFlowState.StartMenu);
    }
}
