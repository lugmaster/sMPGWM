using sMPGWM.Scripts.UtilitySystem;

namespace sMPGWM.Scripts.FileLoadingSystem;

public partial class FilePaths : AbstractSingleton<FilePaths>
{
    // Game configs
    public const string SystemConfigPath = "res://config/config.cfg";
    public const string UserConfigDirectory = "user://config/";
    public const string UserConfigPath = UserConfigDirectory + "config.cfg";

    public const string UserItemCategoryDataYaml = UserConfigDirectory + "items/ItemCategoryDefinitions.yml";
    public const string ItemCategoryDataYaml = "res://assets/game_data/ItemCategoryDefinitions.yml";
    public const string ItemCategoryDataYamlSchema = "res://assets/game_data/ItemCategorySchema.yml";

    public const string UserItemDataYaml = UserConfigDirectory + "items/ItemDefinitions.yml";
    public const string ItemDataYaml = "res://assets/game_data/ItemDefinitions.yml";
    public const string ItemDataYamlSchema = "res://assets/game_data/ItemSchema.yml";

    // Ships
    public const string UserShipDataYaml = UserConfigDirectory + "ships/Ships.yml";
    public const string ShipDataYaml = "res://assets/game_data/Ships.yml";
    public const string ShipDataYamlSchema = "res://assets/game_data/ShipsSchema.yml";

    // Scenes
    public const string MessageEntryScene = "res://scenes/ui/menu/game_overlay_menu/message_menu/message_entry.tscn";

}
