using System.Threading.Tasks;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class GameHandler : AbstractSingleton<GameHandler>
{
    
    public static void QuitToMainMenu(string reason = null, int delayMs = 35)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            Logger.Info($"Quit triggered: {reason}");
        }

        Instance.CallDeferred(nameof(Instance.StartQuitAsync), delayMs);
    }

    public static void QuitToMainMenuBlocking(string reason, bool showAsError = true)
    {
        Logger.Info($"Graceful quit triggered: {reason}");

        var combinedReason = showAsError
            ? $"Error: {reason}\nClick to return to Main Menu."
            : $"Success: {reason}\nClick to return to Main Menu.";
        // StartingScreenManager.Instance.ShowNotificationBlocking(
        //     combinedReason,
        //     () => Instance.CallDeferred(nameof(QuitToMainMenuInternal))
        // );
        if (showAsError)
        {
            Logger.Error(combinedReason);
        }
        else
        {
            Logger.Info(combinedReason);
        }

        Instance.CallDeferred(nameof(QuitToMainMenuInternal));
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
        CallDeferred(nameof(QuitToMainMenuInternal));
    }

    private static void QuitToMainMenuInternal()
    {
        Logger.Info("Quitting game, returning to main menu.");
        StartingScreenManager.Instance.LoadStartingScreen();
    }

    public static void StartGame()
    {
        Logger.Info("Starting up game.");
        StartingScreenManager.Instance.InitializeGame();
    }
}