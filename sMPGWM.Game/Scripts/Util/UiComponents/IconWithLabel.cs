using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class IconWithLabel: VBoxContainer
{
    private string _labelText = "";

    internal ClickableIcon Cell => GetNode<ClickableIcon>("%ClickableIcon");

    internal string LabelText
    {
        get => _labelText;
        set
        {
            _labelText = value;
            if (IsNodeReady()) GetNode<Label>("%Label").Text = value;
        }
    }

    public override void _Ready() => GetNode<Label>("%Label").Text = _labelText;
}
