using sMPGWM.Scripts.Util;
using ClickableIcon = sMPGWM.Scripts.Util.UiComponents.ClickableIcon;
using StatBar = sMPGWM.Scripts.Util.UiComponents.StatBar;

namespace sMPGWM.Scripts.UiSystem.Hud;

public static class HudProvider
{
    private const string StatBarPath = "res://scenes/ui/base/stat_bar.tscn";
    private const string ClickableIconPath = "res://scenes/ui/base/clickable_icon.tscn";
    
    public static StatBar CreateStatBar()
    {
        return SceneLoader.LoadAndInstantiate<StatBar>(StatBarPath);
    }

    public static ClickableIcon CreateClickableIcon()
    {
        return SceneLoader.LoadAndInstantiate<ClickableIcon>(ClickableIconPath);
    }
}