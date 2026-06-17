using Godot;
using sMPGWM.Scripts.Autoload;

namespace sMPGWM.Scripts.Ui.MainMenu.Base;

public abstract partial class StartMenuScreen : BaseMenuScreen
{
    private Button _backButton = null!;

    protected override void OnReady()
    {
        _backButton = GetNode<Button>("%BackButton");
        _backButton.Text = "Back";
        _backButton.Pressed += StartingScreenManager.Instance.GoBack;
    }
}