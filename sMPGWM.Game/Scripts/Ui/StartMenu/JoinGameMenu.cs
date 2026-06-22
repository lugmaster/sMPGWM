using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.Base;


namespace sMPGWM.Scripts.Ui.StartMenu;

public partial class JoinGameMenu : BaseStartMenu
{
    private Button _startGameButton = null!;
    protected override string ScreenTitle => "Join Game";

    protected override void OnReady()
    {
        base.OnReady();

        _startGameButton = GetNode<Button>("%StartGameButton");
        _startGameButton.Pressed += OnJoinButtonPressed;
    }

    private void OnJoinButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame();
    }
}