using Godot;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Ui.Base;

namespace sMPGWM.Scripts.Ui.Game.GameOverlayMenus;

public partial class MainMenu : GameOverlayMenu
{
    protected override string ScreenTitle => "Main Menu";
    
    private Button _quitGameButton = null!;

    protected override void OnReady()
    {
        base.OnReady();

        _quitGameButton = GetNode<Button>("%QuitGameButton");
        _quitGameButton.Pressed += OnQuitGameButtonOnPressed;
    }

    public override void _ExitTree()
    {
        if (_quitGameButton != null)
            _quitGameButton.Pressed -= OnQuitGameButtonOnPressed;

        base._ExitTree();
    }

    private void OnQuitGameButtonOnPressed()
    {
        GameHandler.QuitToStartMenu("Quitting game from main menu");
    }
}
