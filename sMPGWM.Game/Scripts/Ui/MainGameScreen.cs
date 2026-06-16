using Godot;
using sMPGWM.Autoload;

namespace sMPGWM.Scripts.Ui;

public partial class MainGameScreen : Control
{
    private Button _joinButton = null!;
    private Button _hostButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    public override void _Ready()
    {
        UiManager.Instance.RegisterCurrentScreen(this);

        _joinButton = GetNode<Button>("%JoinButton");
        _hostButton = GetNode<Button>("%HostButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _joinButton.Pressed += UiManager.Instance.ShowJoinScreen;
        _hostButton.Pressed += UiManager.Instance.ShowHostScreen;
        _settingsButton.Pressed += UiManager.Instance.ShowSettingsScreen;
        _quitButton.Pressed += UiManager.Instance.QuitGame;

        GameLogger.Info("MainGameScreen loaded.");
    }
}
