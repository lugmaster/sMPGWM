using Godot;
using sMPGWM.Scripts.Autoload;

namespace sMPGWM.Scripts.Ui.MainMenu.Base;

public abstract partial class MainMenuSubMenuScreen : Control
{
    private Button _backButton = null!;
    private Label _titleLabel = null!;

    protected abstract string ScreenTitle { get; }

    public sealed override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _backButton = GetNode<Button>("%BackButton");

        _titleLabel.Text = ScreenTitle;
        _backButton.Pressed += ScreenManager.Instance.GoBack;

        OnReady();

        GameLogger.Info($"{GetType().Name} loaded.");
    }

    protected virtual void OnReady()
    {
    }
}