using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Autoload;

public partial class StartMenuManager : AbstractAutoload<StartMenuManager>
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
}