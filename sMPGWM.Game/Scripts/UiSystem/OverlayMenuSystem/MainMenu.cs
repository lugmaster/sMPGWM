using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using GameOverlayMenu = sMPGWM.Scripts.UtilitySystem.UiComponents.GameOverlayMenu;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;


namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

public partial class MainMenu : GameOverlayMenu
{
    internal event System.Action? SettingsRequested;
    private void RequestSettings() => SettingsRequested?.Invoke();
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
        GetNode<Button>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/Settings").Pressed += RequestSettings;
        _quitGameButton.Pressed += OnQuitGameButtonOnPressed;
    }

    public override void _ExitTree()
    {
        Logger.Trace($"{nameof(MainMenu)}.{nameof(_ExitTree)} called.");
        if (_quitGameButton != null)
            _quitGameButton.Pressed -= OnQuitGameButtonOnPressed;

        GetNode<Button>("CenterContainer/MenuPanel/MarginContainer/RootContent/ContentContainer/Settings").Pressed -= RequestSettings;
        base._ExitTree();
    }

    private void OnQuitGameButtonOnPressed()
    {
        GameFlowManager.Instance.QuitToStartMenu("Quitting game from main menu");
    }
}