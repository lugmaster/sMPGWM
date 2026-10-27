using Godot;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.Network;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class JoinLanGameMenu: BaseStartMenu
{
    private Button _joinLanGameButton = null!;
    protected override string ScreenTitle => "Start Lan Game";

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _joinLanGameButton = GetNode<Button>("%JoinLanGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _joinLanGameButton.Pressed += OnJoinLanGameButtonPressed;
    }

    private void OnJoinLanGameButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame(NetworkRole.Client, NetworkType.Lan);
    }
}