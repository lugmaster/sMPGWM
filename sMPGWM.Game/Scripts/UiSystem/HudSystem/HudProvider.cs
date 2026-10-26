using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.HudSystem;

public static class HudProvider
{
    private const string StatBarPath = "res://scenes/ui/base/stat_bar.tscn";
    private const string ClickableIconPath = "res://scenes/ui/base/clickable_icon.tscn";
    private const string IconWithLabelPath = "res://scenes/ui/base/icon_with_label.tscn";

    internal static IconWithLabel CreateIconWithLabel() =>
        SceneLoader.LoadAndInstantiate<IconWithLabel>(IconWithLabelPath);
    
    public static StatBar CreateStatBar()
    {
        return SceneLoader.LoadAndInstantiate<StatBar>(StatBarPath);
    }

    public static ClickableIcon CreateClickableIcon()
    {
        return SceneLoader.LoadAndInstantiate<ClickableIcon>(ClickableIconPath);
    }
}
