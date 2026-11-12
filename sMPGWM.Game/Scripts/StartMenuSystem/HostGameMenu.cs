using Godot;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class HostGameMenu : BaseStartMenu
{
    protected override string ScreenTitle => "Host Game";

    private Button _hostLanGameButton = null!;
    private Button _hostSteamGameButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _hostLanGameButton = GetNode<Button>("%HostLanGameButton");
        _hostSteamGameButton = GetNode<Button>("%HostSteamGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _hostLanGameButton.Pressed += StartMenuManager.Instance.ShowHostLanGameMenu;
        _hostSteamGameButton.Pressed += StartMenuManager.Instance.ShowHostSteamGameMenu;
    }
}