using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.MainMenu.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui;

public partial class StartingScreen : StartMenuScreen
{
    protected override string ScreenTitle => "Settings";
    private Button _joinButton = null!;
    private Button _hostButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    protected override void OnReady()
    {
        StartingScreenManager.Instance.RegisterInitialScreen(this);

        _joinButton = GetNode<Button>("%JoinButton");
        _hostButton = GetNode<Button>("%HostButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _joinButton.Pressed += StartingScreenManager.Instance.ShowJoinScreen;
        _hostButton.Pressed += StartingScreenManager.Instance.ShowHostScreen;
        _settingsButton.Pressed += StartingScreenManager.Instance.ShowSettingsScreen;
        _quitButton.Pressed += () => GameHandler.Instance.QuitGame("Pressed QuitGameButton");

        Logger.Info("StartingScreen loaded.");
    }
}
