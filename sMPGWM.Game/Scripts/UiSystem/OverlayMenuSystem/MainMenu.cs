using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using GameOverlayMenu = sMPGWM.Scripts.Util.UiComponents.GameOverlayMenu;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UiSystem.GameOverlaySystem;

public partial class MainMenu : Util.UiComponents.GameOverlayMenu
{
    protected override string ScreenTitle => "Main Menu";
    
    private Button _quitGameButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _quitGameButton = GetNode<Button>("%QuitGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _quitGameButton.Pressed += OnQuitGameButtonOnPressed;
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(MainMenu)}.{nameof(_ExitTree)} called.");
        if (_quitGameButton != null)
            _quitGameButton.Pressed -= OnQuitGameButtonOnPressed;

        base._ExitTree();
    }

    private void OnQuitGameButtonOnPressed()
    {
        GameFlowManager.Instance.QuitToStartMenu("Quitting game from main menu");
    }
}
