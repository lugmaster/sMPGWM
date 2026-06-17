using sMPGWM.Scripts.Ui.MainMenu.Base;

namespace sMPGWM.Scripts.Ui.InGame;

public partial class MainMenuScreen: InGameMenuScreen
{
    protected override string ScreenTitle => "Main Menu";
    
    public override bool PauseGame => true;
}