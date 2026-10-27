using Godot;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.Network;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class HostLanGameMenu : BaseStartMenu
{
    protected override string ScreenTitle => "Host LAN Game";

    private Button _hostLanGameButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _hostLanGameButton = GetNode<Button>("%HostLanGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _hostLanGameButton.Pressed += OnHostLanGameButtonPressed;
    }

    private void OnHostLanGameButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame(NetworkRole.Host, NetworkType.Lan);
    }
}