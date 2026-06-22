using Godot;
using sMPGWM.Scripts.Autoload;

namespace sMPGWM.Scripts.Ui.Base;

public abstract partial class BaseStartMenu : BaseMenu
{

    private Button _backButton = null!;

    protected override void OnReady()
    {
        _backButton = GetNode<Button>("%BackButton");
        _backButton.Text = "Back";
        _backButton.Pressed += StartMenuManager.Instance.GoBack;
    }
    
    
}