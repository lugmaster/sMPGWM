using Godot;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class JoinGameMenu : BaseStartMenu
{
    private Button _joinLanGameButton = null!;
    private Button _joinSteamGameButton = null!;
    protected override string ScreenTitle => "Join Game";

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _joinLanGameButton = GetNode<Button>("%JoinLanGameButton");
        _joinSteamGameButton = GetNode<Button>("%JoinSteamGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _joinLanGameButton.Pressed += StartMenuManager.Instance.ShowJoinLanGameMenu;
        _joinSteamGameButton.Pressed += StartMenuManager.Instance.ShowJoinSteamGameMenu;
    }
}
