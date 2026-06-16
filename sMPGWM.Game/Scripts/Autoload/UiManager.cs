using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class UiManager : AbstractAutoload<UiManager>
{
    public void ShowJoinScreen()
    {
        Logger.Info("ShowJoinScreen requested.");
        ScreenManager.Instance.NavigateTo(SceneProvider.CreateJoinScreen());
    }
    
    public void ShowHostScreen()
    {
        Logger.Info("ShowHostScreen requested.");
        ScreenManager.Instance.NavigateTo(SceneProvider.CreateHostScreen());
    }

    public void ShowSettingsScreen()
    {
        Logger.Info("ShowSettingsScreen requested.");
        ScreenManager.Instance.NavigateTo(SceneProvider.CreateSettingsScreen());
    }

    public void QuitGame()
    {
        Logger.Info("Quit game requested.");
        SteamworksHelper.Instance.ShutdownSteam();
        Logger.Info("After Steam Shutdown");
        CallDeferred(nameof(QuitGameInternal));
    }
    
    private void QuitGameInternal()
    {
        GetTree().Quit();
    }
}