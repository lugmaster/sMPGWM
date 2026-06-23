using System.Threading.Tasks;
using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class GameHandler : AbstractSingleton<GameHandler>
{
    public void QuitToStartMenu(string reason = null, int delayMs = 35)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            Logger.Debug($"{nameof(GameHandler)}.{nameof(QuitToStartMenu)} quit triggered with {nameof(reason)}={reason}.");
        }

        Instance.CallDeferred(nameof(Instance.StartQuitAsync), delayMs);
    }

    public void QuitToStartMenuBlocking(string reason, bool showAsError = true)
    {
        Logger.Debug($"{nameof(GameHandler)}.{nameof(QuitToStartMenuBlocking)} graceful quit triggered with {nameof(reason)}={reason}.");

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
            Logger.Debug($"{nameof(GameHandler)}.{nameof(QuitToStartMenuBlocking)} completed with {nameof(combinedReason)}={combinedReason}.");
        }

        Instance.CallDeferred(nameof(QuitToStartMenuInternal));
    }

    public void QuitGame(string reason)
    {
        Logger.Debug($"{nameof(GameHandler)}.{nameof(QuitGame)} requested with {nameof(reason)}={reason}.");
        SteamworksHelper.ShutdownSteam();
        Logger.Debug($"{nameof(GameHandler)}.{nameof(QuitGame)} completed Steam shutdown.");
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

    private void QuitToStartMenuInternal()
    {
        Logger.Debug($"{nameof(GameHandler)}.{nameof(QuitToStartMenuInternal)} returning to main menu.");
        GetTree().ChangeSceneToPacked(BaseSceneProvider.GetStartMenuScene());
    }

    public void StartGame()
    {
        Logger.Debug($"{nameof(GameHandler)}.{nameof(StartGame)} starting up game.");
        GetTree().ChangeSceneToPacked(BaseSceneProvider.GetMainGameScene());
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
}
