using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.MainMenu.Base;
using Logger = sMPGWM.Scripts.Autoload.Logger;
using UiManager = sMPGWM.Scripts.Autoload.UiManager;

namespace sMPGWM.Scripts.Ui.MainMenu;

public partial class MainGameScreen : MainMenuSubMenuScreen
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

        _joinButton.Pressed += UiManager.Instance.ShowJoinScreen;
        _hostButton.Pressed += UiManager.Instance.ShowHostScreen;
        _settingsButton.Pressed += UiManager.Instance.ShowSettingsScreen;
        _quitButton.Pressed += UiManager.Instance.QuitGame;

        Logger.Info("MainGameScreen loaded.");
    }
}
