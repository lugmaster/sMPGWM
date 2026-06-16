using Godot;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class UiManager : AbstractAutoload<UiManager>
{
    public void ShowJoinScreen()
    {
        GameLogger.Info("ShowJoinScreen requested.");
        ScreenManager.Instance.NavigateTo(SceneProvider.CreateJoinScreen());
    }
    
    public void ShowHostScreen()
    {
        GameLogger.Info("ShowHostScreen requested.");
        ScreenManager.Instance.NavigateTo(SceneProvider.CreateHostScreen());
    }

    public void ShowSettingsScreen()
    {
        GameLogger.Info("ShowSettingsScreen requested.");
        ScreenManager.Instance.NavigateTo(SceneProvider.CreateSettingsScreen());
    }

    public void QuitGame()
    {
        GameLogger.Info("Quit game requested.");
        GetTree().Quit();
    }
}