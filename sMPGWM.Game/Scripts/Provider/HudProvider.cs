using sMPGWM.Scripts.Ui.Base;

namespace sMPGWM.Scripts.Provider;

public static class HudProvider
{
    private const string StatBarPath = "res://scenes/ui/base/stat_bar.tscn";
    private const string ClickableIconPath = "res://scenes/ui/base/clickable_icon.tscn";
    
    public static StatBar CreateStatBar()
    {
        return BaseSceneProvider.LoadAndInstantiate<StatBar>(StatBarPath);
    }

    public static ClickableIcon CreateClickableIcon()
    {
        return BaseSceneProvider.LoadAndInstantiate<ClickableIcon>(ClickableIconPath);
    }
}