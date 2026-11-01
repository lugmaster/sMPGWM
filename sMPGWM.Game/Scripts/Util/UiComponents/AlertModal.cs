using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class AlertModal: GameOverlayMenu
{
    private string _title = "";
    private bool _confirmed;
    protected override string ScreenTitle => _title;

    public void Configure(Texture2D titleIcon, string titleLabel, string message)
    {
        _title = titleLabel;
        GetNode<TextureRect>("CenterContainer/MenuPanel/MarginContainer/RootContent/HeaderContainer/TitleIcon").Texture = titleIcon;
        GetNode<Label>("%TitleLabel").Text = titleLabel;
        GetNode<Label>("%ContentContainer/MessageLabel").Text = message;
    }

    protected override void OnReady()
    {
        base.OnReady();
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetNode<Button>("%CloseButton").Text = "X";
        var okButton = GetNode<Button>("%ContentContainer/OkButton");
        okButton.Pressed += RequestClose;
        okButton.FocusMode = FocusModeEnum.All;
        ReleaseExternalFocus();
        okButton.GrabFocus();
    }

    protected override void RequestClose()
    {
        if (_confirmed) return;
        _confirmed = true;
        Hide();
        QueueFree();
        base.RequestClose();
    }
}
