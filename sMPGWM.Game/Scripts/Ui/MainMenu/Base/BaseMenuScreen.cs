using Godot;
using Logger = sMPGWM.Scripts.Autoload.Logger;

namespace sMPGWM.Scripts.Ui.MainMenu.Base;

public abstract partial class BaseMenuScreen : Control
{
    private Label _titleLabel = null!;

    protected abstract string ScreenTitle { get; }

    public sealed override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");

        _titleLabel.Text = ScreenTitle;

        OnReady();

        Logger.Info($"{GetType().Name} loaded.");
    }

    protected virtual void OnReady()
    {
        
    }
}