using Godot;

namespace sMPGWM.Scripts.UiSystem;

public partial class LoadingScreen : TextureRect
{
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        _progressBar = GetNode<ProgressBar>("%LoadingBar");
        _statusLabel = GetNode<Label>("%LoadingStatusLabel");
        UpdateProgress(0, "Loading game...");
    }

    public void UpdateProgress(double percent, string message)
    {
        _progressBar.Value = double.IsFinite(percent) ? System.Math.Clamp(percent, 0, 100) : 0;
        _statusLabel.Text = message;
    }
}
