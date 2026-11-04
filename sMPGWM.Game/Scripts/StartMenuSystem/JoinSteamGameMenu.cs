using Godot;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class JoinSteamGameMenu: BaseStartMenu
{
    private LobbyList.LobbyList _lobbies = null!;
    private Button _joinSteamGameButton = null!;
    protected override string ScreenTitle => "Join Steam Game";

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _joinSteamGameButton = GetNode<Button>("%JoinSteamGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _lobbies = GetNode<LobbyList.LobbyList>("%LobbyListPanel");
        _joinSteamGameButton.Disabled = true;
        _lobbies.SelectionChanged += id => _joinSteamGameButton.Disabled = id == 0;
        _joinSteamGameButton.Pressed += OnJoinSteamGameButtonPressed;
    }

    private void OnJoinSteamGameButtonPressed()
    {
        if (_lobbies.SelectedLobbyId != 0)
            StartMenuManager.Instance.StartSteamGame(NetworkRole.Client, _lobbies.SelectedLobbyId);
    }
    

}
