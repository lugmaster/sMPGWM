using sMPGWM.Scripts.UtilitySystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using ClickableIcon = sMPGWM.Scripts.UtilitySystem.UiComponents.ClickableIcon;
using IconWithLabel = sMPGWM.Scripts.UtilitySystem.UiComponents.IconWithLabel;
using StatBar = sMPGWM.Scripts.UtilitySystem.UiComponents.StatBar;

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
