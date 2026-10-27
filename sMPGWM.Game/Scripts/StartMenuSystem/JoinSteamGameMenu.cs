using Godot;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.Network;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class JoinSteamGameMenu: BaseStartMenu
{
    private Button _joinSteamGameButton = null!;
    protected override string ScreenTitle => "Start Steam Game";

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _joinSteamGameButton = GetNode<Button>("%JoinSteamGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _joinSteamGameButton.Pressed += OnJoinSteamGameButtonPressed;
    }

    private void OnJoinSteamGameButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame(NetworkRole.Client, NetworkType.Steam);
    }
    
    // TODO add Lobby - Query, Selection and Join Selected
}