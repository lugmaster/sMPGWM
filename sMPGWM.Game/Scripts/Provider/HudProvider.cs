using sMPGWM.Scripts.Ui.Base;

namespace sMPGWM.Scripts.Provider;

public class HudProvider
{
    private const string StatBarPath = "res://scenes/ui/base/stat_bar.tscn";
    
    public static StatBar CreateStatBar()
    {
        return BaseSceneProvider.LoadAndInstantiate<StatBar>(StatBarPath);
    }
}