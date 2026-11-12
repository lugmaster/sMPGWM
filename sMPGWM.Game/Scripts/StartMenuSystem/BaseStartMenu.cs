using Godot;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using BaseMenu = sMPGWM.Scripts.UtilitySystem.UiComponents.BaseMenu;

namespace sMPGWM.Scripts.StartMenuSystem;

public abstract partial class BaseStartMenu : BaseMenu
{

    private Button _backButton = null!;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _backButton = GetNode<Button>("%BackButton");
    }

    protected override void OnReady()
    {
        base.OnReady();
        _backButton.Text = "Back";
        _backButton.Pressed += StartMenuManager.Instance.GoBack;
    }
    
    
}
