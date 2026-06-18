using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.StartMenu;

public partial class StartMenu : BaseStartMenu
{
    protected override string ScreenTitle => "SettingsMenu";
    private Button _joinButton = null!;
    private Button _hostButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    protected override void OnReady()
    {
        StartMenuManager.Instance.RegisterInitialScreen(this);

        _joinButton = GetNode<Button>("%JoinButton");
        _hostButton = GetNode<Button>("%HostButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _joinButton.Pressed += StartMenuManager.Instance.ShowJoinGameMenu;
        _hostButton.Pressed += StartMenuManager.Instance.ShowHostGameMenu;
        _settingsButton.Pressed += StartMenuManager.Instance.ShowSettingsMenu;
        _quitButton.Pressed += () => GameHandler.Instance.QuitGame("Pressed QuitGameButton");

        Logger.Info("StartingScreen loaded.");
    }
}
