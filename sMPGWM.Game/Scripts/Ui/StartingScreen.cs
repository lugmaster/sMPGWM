using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.MainMenu.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui;

public partial class StartingScreen : MainMenuSubMenuScreen
{
    protected override string ScreenTitle => "Settings";
    private Button _joinButton = null!;
    private Button _hostButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    protected override void OnReady()
    {
        ScreenManager.Instance.RegisterInitialScreen(this);

        _joinButton = GetNode<Button>("%JoinButton");
        _hostButton = GetNode<Button>("%HostButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _joinButton.Pressed += StartMenuManager.Instance.ShowJoinScreen;
        _hostButton.Pressed += StartMenuManager.Instance.ShowHostScreen;
        _settingsButton.Pressed += StartMenuManager.Instance.ShowSettingsScreen;
        _quitButton.Pressed += () => GameHandler.Instance.QuitGame("Pressed QuitGameButton");

        Logger.Info("StartingScreen loaded.");
    }
}
