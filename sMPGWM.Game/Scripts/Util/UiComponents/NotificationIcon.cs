#nullable enable
using Godot;

namespace sMPGWM.Scripts.Util.UiComponents;

public partial class NotificationIcon : ClickableIcon
{
    internal void Present(Texture2D icon, string tooltip, long count)
    {
        SetDisplay(icon, new IconDisplayData(tooltip, count > 0 ? count : null));
        var color = count > 0 ? Colors.Yellow : Colors.Gray;
        foreach (var state in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_focus_color" })
            AddThemeColorOverride(state, color);
    }
}
