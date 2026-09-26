using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class StartMenu : BaseStartMenu
{
    protected override string ScreenTitle => "SettingsMenu";
    private Button _joinButton = null!;
    private Button _hostButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();

        _joinButton = GetNode<Button>("%JoinButton");
        _hostButton = GetNode<Button>("%HostButton");
        _settingsButton = GetNode<Button>("%SettingsButton");
        _quitButton = GetNode<Button>("%QuitButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        StartMenuManager.Instance.RegisterInitialScreen(this);

        _joinButton.Pressed += StartMenuManager.Instance.ShowJoinGameMenu;
        _hostButton.Pressed += StartMenuManager.Instance.ShowHostGameMenu;
        _settingsButton.Pressed += StartMenuManager.Instance.ShowSettingsMenu;
        _quitButton.Pressed += () => GameFlowManager.Instance.QuitGame("Pressed QuitGameButton");

        Logger.Debug($"{nameof(StartMenu)}.{nameof(OnReady)} loaded.");
    }
}
