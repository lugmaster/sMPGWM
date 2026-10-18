using System.Threading.Tasks;
using Godot;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameFlowSystem;

public partial class GameFlowManager : AbstractSingleton<GameFlowManager>
{
    private GameFlowState _gameFlowState = GameFlowState.StartMenu;
    private GameFlowState _pendingGameFlowState = GameFlowState.StartMenu;
    
    public GameFlowState GameFlowState => _gameFlowState;

    protected override void OnEnterTree()
    {
        GetTree().SceneChanged += OnSceneChanged;
    }

    protected override void OnExitTree()
    {
        GetTree().SceneChanged -= OnSceneChanged;
    }


    public void QuitToStartMenu(string reason = null, int delayMs = 35)
    {
        _gameFlowState = GameFlowState.Disconnecting;
        
        if (!string.IsNullOrEmpty(reason))
        {
            Logger.Debug(
                $"{nameof(GameFlowManager)}.{nameof(QuitToStartMenu)} quit triggered with {nameof(reason)}={reason}.");
        }

        Instance.CallDeferred(nameof(Instance.StartQuitAsync), delayMs);
    }

    public void QuitToStartMenuBlocking(string reason, bool showAsError = true)
    {
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

        Instance.CallDeferred(nameof(QuitToStartMenuInternal));
    }

    public void QuitGame(string reason)
    {
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(QuitGame)} requested with {nameof(reason)}={reason}.");
        Steam.SteamworksHelper.ShutdownSteam();
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(QuitGame)} completed Steam shutdown.");
        Instance.CallDeferred(nameof(QuitGameInternal));
    }

    private void QuitGameInternal()
    {
        GetTree().Quit();
    }

    private async void StartQuitAsync(int delayMs)
    {
        await Task.Delay(delayMs);
        CallDeferred(nameof(QuitToStartMenuInternal));
    }

    public void StartGame()
    {
        Logger.Debug($"{nameof(GameFlowManager)}.{nameof(StartGame)} starting game.");
        ChangeScene(GameFlowSceneProvider.MainGameScene, GameFlowState.InGame);
    }

    private void QuitToStartMenuInternal()
    {
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
        _gameFlowState = GameFlowState.Loading;
        _pendingGameFlowState = targetState;

        var error = GetTree().ChangeSceneToPacked(scene);

        if (error != Error.Ok)
        {
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