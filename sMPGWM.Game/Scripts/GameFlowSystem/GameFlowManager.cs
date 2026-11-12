using sMPGWM.Scripts.GameConfigSystem;
using System;
using System.Threading.Tasks;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.SteamSystem;
using sMPGWM.Scripts.UtilitySystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;
using ModalController = sMPGWM.Scripts.UtilitySystem.UiComponents.ModalController;

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
        BeginSessionLoading();
    }

    private async void BeginSessionLoading()
    {
        var generation = ++_navigationGeneration;
        var session = Session!;
        SetState(GameFlowState.Loading);
        // Headless servers have no rendered frames to await.
        if (DisplayServer.GetName() == "headless")
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        else
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (!IsInsideTree() || generation != _navigationGeneration || session.State == SessionState.Stopped) return;
        session.Initialization.PrepareDefinitions(GameConfig.EnsureCompatibilityData);
        if (session.State == SessionState.Stopped) return;
        try { ChangeScene(GameFlowSceneProvider.MainGameScene, GameFlowState.Loading); }
        catch (Exception error) { session.Fail($"Cannot load game: {error.Message}"); }
    }
    
    public event Action<GameFlowState>? StateChanged;

    private void SetState(GameFlowState state)
    {
        if (_gameFlowState == state) return;
        _gameFlowState = state;
        if (state == GameFlowState.Loading)
            ModalController.Instance.ShowLoadingScreen(() =>
                Session?.Initialization.Progress ?? (0, "Loading session..."));
        else
            ModalController.Instance.HideLoadingScreen();
        Logger.Info($"Game flow: {state}");
        StateChanged?.Invoke(state);
    }

    public void CompleteLoading()
    {
        if (_gameFlowState == GameFlowState.Loading && Session?.State == SessionState.Active)
            SetState(GameFlowState.InGame);
    }
    
    public GameFlowState GameFlowState => _gameFlowState;

    protected override void OnEnterTree()
    {
        GetTree().SceneChanged += OnSceneChanged;
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
        if (SteamworksHelper.Instance != null) SteamworksHelper.ShutdownSteam();
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
            GameConfig.MaxPlayers, GameConfig.ConnectionTimeout,
            GameConfig.ServerIpAddress, GameConfig.Port));
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
