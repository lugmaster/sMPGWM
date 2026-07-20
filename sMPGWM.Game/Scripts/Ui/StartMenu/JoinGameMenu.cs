using Godot;
using sMPGWM.Scripts.Singletons.Autoload;
using sMPGWM.Scripts.Ui.Base;


namespace sMPGWM.Scripts.Ui.StartMenu;

public partial class JoinGameMenu : BaseStartMenu
{
    private Button _startGameButton = null!;
    protected override string ScreenTitle => "Join Game";

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _startGameButton = GetNode<Button>("%StartGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _startGameButton.Pressed += OnJoinButtonPressed;
    }

    private void OnJoinButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame();
    }
}
