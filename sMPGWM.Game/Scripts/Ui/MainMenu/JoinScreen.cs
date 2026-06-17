using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.MainMenu.Base;

namespace sMPGWM.Scripts.Ui.MainMenu;

public partial class JoinScreen : MainMenuSubMenuScreen
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
        UiManager.Instance.StartJoinedGame();
    }
}