using Godot;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class JoinGameMenu : BaseStartMenu
{
    private Button _joinGameButton = null!;
    protected override string ScreenTitle => "Join Game";

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _joinGameButton = GetNode<Button>("%JoinGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _joinGameButton.Pressed += OnJoinButtonPressed;
    }

    private void OnJoinButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame();
    }
}
