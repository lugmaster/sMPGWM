using sMPGWM.Scripts.UtilitySystem;

namespace sMPGWM.Scripts.FileLoadingSystem;

public partial class FilePaths : AbstractSingleton<FilePaths>
{
    // Game configs
    public const string SystemConfigPath = "res://config/config.cfg";
    public const string UserConfigDirectory = "user://config/";
    public const string UserConfigPath = UserConfigDirectory + "config.cfg";

    // Scenes
    public const string MessageEntryScene = "res://scenes/ui/menu/game_overlay_menu/message_menu/message_entry.tscn";

}
