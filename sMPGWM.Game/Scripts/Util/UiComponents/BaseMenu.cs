using Godot;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Util.UiComponents;

public abstract partial class BaseMenu : Control
{
    private Label _titleLabel = null!;

    protected abstract string ScreenTitle { get; }

    public sealed override void _EnterTree()
    {
        Logger.Trace($"{nameof(BaseMenu)}.{nameof(_EnterTree)} called for {GetType().Name}.");
        _titleLabel = GetNode<Label>("%TitleLabel");
        OnEnterTree();
    }

    public sealed override void _Ready()
    {
        Logger.Trace($"{nameof(BaseMenu)}.{nameof(_Ready)} called for {GetType().Name}.");
        _titleLabel.Text = ScreenTitle;

        OnReady();

        Logger.Debug($"{nameof(BaseMenu)}.{nameof(_Ready)} loaded {GetType().Name}.");
    }

    protected virtual void OnEnterTree()
    {
    }

    protected virtual void OnReady()
    {
        
    }
}
