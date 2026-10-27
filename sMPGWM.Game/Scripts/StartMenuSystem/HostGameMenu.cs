using Godot;

namespace sMPGWM.Scripts.StartMenuSystem;

public partial class HostGameMenu : BaseStartMenu
{
    protected override string ScreenTitle => "HostGameMenu Game";

    private Button _hostGameButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _hostGameButton = GetNode<Button>("%HostGameButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _hostGameButton.Pressed += OnHostButtonPressed;
    }

    private void OnHostButtonPressed()
    {
        StartMenuManager.Instance.InitializeGame();
    }
}