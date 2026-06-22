using System.Threading.Tasks;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class GameHandler : AbstractSingleton<GameHandler>
{
    
    public static void QuitToStartMenu(string reason = null, int delayMs = 35)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            Logger.Info($"Quit triggered: {reason}");
        }

        Instance.CallDeferred(nameof(Instance.StartQuitAsync), delayMs);
    }

    public static void QuitToStartMenuBlocking(string reason, bool showAsError = true)
    {
        Logger.Info($"Graceful quit triggered: {reason}");

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
            Logger.Info(combinedReason);
        }

        Instance.CallDeferred(nameof(QuitToStartMenuInternal));
    }

    public void QuitGame(string reason)
    {
        Logger.Info($"Quit game requested. Reason: {reason}");
        SteamworksHelper.Instance.ShutdownSteam();
        Logger.Info("After Steam Shutdown");
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
        Logger.Info("Quitting game, returning to main menu.");
        GetTree().ChangeSceneToPacked(BaseSceneProvider.GetStartMenuScene());
    }

    public  void StartGame()
    {
        Logger.Info("Starting up game.");
        GetTree().ChangeSceneToPacked(BaseSceneProvider.GetMainGameScene());
    }
}