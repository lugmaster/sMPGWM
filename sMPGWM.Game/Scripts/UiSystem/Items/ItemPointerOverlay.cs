#nullable enable

using Godot;

namespace sMPGWM.Scripts.UiSystem.Items;

public partial class ItemPointerOverlay : Control
{
    private TextureRect _preview = null!;
    private Label _feedback = null!;

    public override void _Ready()
    {
        _preview = GetNode<TextureRect>("%ItemPointerPreview");
        _feedback = GetNode<Label>("%ItemPointerFeedback");
        Clear();
    }

    internal void UpdateDisplay(Texture2D? texture, string feedback, bool showPreview)
    {
        _preview.Texture = texture;
        _preview.Visible = showPreview;
        _feedback.Text = feedback;
        _feedback.Show();
        Show();
        SetProcess(true);
        UpdatePosition();
    }

    internal void Clear()
    {
        Hide();
        _preview.Hide();
        _preview.Texture = null;
        _feedback.Hide();
        _feedback.Text = "";
        SetProcess(false);
    }

    internal void SetDragPreviewFor(Control source, Texture2D? texture)
    {
        source.SetDragPreview(new TextureRect
        {
            Texture = texture,
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = _preview.Size,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });
    }

    public override void _Process(double delta) => UpdatePosition();

    private void UpdatePosition()
    {
        _preview.GlobalPosition = GetGlobalMousePosition() + new Vector2(16, 16);
        _feedback.GlobalPosition = _preview.GlobalPosition + new Vector2(0, _preview.Size.Y + 4);
    }
}
