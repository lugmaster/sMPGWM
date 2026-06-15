using Godot;
using sMPGWM.autoload;

namespace sMPGWM.scripts.ui;

public partial class MainGameScreen : Control
{
    private Button _joinButton = null!;
    private Button _hostButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    public override void _Ready()
    {
        _joinButton = GetNode<Button>("%JoinButton");
        _hostButton = GetNode<Button>("%HostButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _joinButton.Pressed += OnJoinPressed;
        _hostButton.Pressed += OnHostPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _quitButton.Pressed += OnQuitPressed;

        GameLogger.Info("MainGameScreen loaded.");
    }

    private void OnJoinPressed()
    {
        GameLogger.Info("Join button clicked.");
    }

    private void OnHostPressed()
    {
        GameLogger.Info("Host button clicked.");
    }

    private void OnSettingsPressed()
    {
        GameLogger.Info("Settings button clicked.");
    }
    
    private void OnQuitPressed()
    {
        GameLogger.Info("Settings button clicked.");
    }
}