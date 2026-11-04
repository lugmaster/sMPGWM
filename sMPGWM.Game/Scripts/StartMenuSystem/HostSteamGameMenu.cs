using Godot;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class HostSteamGameMenu : BaseStartMenu
{
    protected override string ScreenTitle => "Host Steam Game";

    private Button _hostSteamGameButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _hostSteamGameButton = GetNode<Button>("%HostSteamGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _hostSteamGameButton.Pressed += OnHostSteamGameButtonPressed;
    }

    private void OnHostSteamGameButtonPressed()
    {
        StartMenuManager.Instance.StartSteamGame(NetworkRole.Host);
    }
}