using sMPGWM.Scripts.Util;

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

    // Effects
    public const string UserEffectDataJson = UserConfigDirectory + "effects/effects.json";
    public const string EffectDataJson = "res://config/effects/effects.json";
    public const string EffectDataJsonSchema = "res://config/effects/effects.json";

    // Ships
    public const string UserShipDataJson = UserConfigDirectory + "ships/ships.json";
    public const string ShipDataJson = "res://config/ships/ships.json";
    public const string ShipDataJsonSchema = "res://config/ships/ships_schema.json";

    // Weapons
    public const string UserWeaponDataJson = UserConfigDirectory + "weapons/weapons.json";
    public const string WeaponDataJson = "res://config/weapons/weapons.json";
    public const string WeaponDataJsonSchema = "res://config/weapons/weapons_schema.json";


    // Scenes
    public const string GameCanvasScenePath = "res://scenes/game_overlay/game_canvas.tscn";
    public const string MessageEntryScene = "res://scenes/ui/message_entry.tscn";
    public const string MainGameScenePath = "res://scenes/main_game.tscn";
    public const string MainMenuScenePath = "res://scenes/ui/main_menu.tscn";
    
    // Scenes - MultiplayerController
    private const string MpControllerBasePath = "res://scenes/game_objects/multiplayer_controller/";
    public const string MultiplayerControllerScenePath = MpControllerBasePath + "multiplayer_controller.tscn";
    public const string ClientControllerScenePath = MpControllerBasePath + "client_controller.tscn";
    public const string HostControllerScenePath = MpControllerBasePath + "host_controller.tscn";
    public const string DummyControllerScenePath = MpControllerBasePath + "dummy_controller.tscn";
    
    // Scenes - AgentController
    private const string AgentControllerBasePath = "res://scenes/game_objects/agent_controller/";
    public const string AgentControllerScenePath = AgentControllerBasePath + "agent_controller.tscn";

    // Textures
    public const string TargetIndicatorTexturePath = "res://assets/textures/icons/pointer.png";
    public const string TargetingCornersTexturePath = "res://assets/textures/targeting/ul_corner.png";
    public const string TargetingCornersInactiveTexturePath = "res://assets/textures/targeting/ul_corner_inactive.png";

    public const string TargetingReticlesNearestTexturePath =
        "res://assets/textures/targeting/targeting_reticle_nearest.png";

    public const string TargetingReticlesBestTexturePath = "res://assets/textures/targeting/targeting_reticle_best.png";

    // Music
    public const string MainMenuIntroMusicPath = "res://assets/sounds/music/galactic_clash_intro.ogg";
    public const string PeacefulMusicPathOne = "res://assets/sounds/music/Explore_the_universe.ogg";
    public const string PeacefulMusicPathTwo = "res://assets/sounds/music/Into_the_void.ogg";
    public const string PeacefulMusicPathThree = "res://assets/sounds/music/Peaceful_stars.ogg";
    public const string BattleMusicPathOne = "res://assets/sounds/music/Dangerzone.ogg";
    public const string BattleMusicPathTwo = "res://assets/sounds/music/Galactic_inferno.ogg";

    // Sounds
    public const string SpaceShipAmbientSoundPath = "res://assets/sounds/sound_effects/spaceship_ambient.wav";
    public const string SpaceShipAccelerationSoundPath = "res://assets/sounds/sound_effects/spaceship_acceleration.wav";
    public const string SpaceShipAfterBurnerSoundPath = "res://assets/sounds/sound_effects/spaceship_afterburner.wav";


    // RootNode inside SubViewport
    public const string GameSceneNodePath = "/root/MainGame/SubViewportContainer/SubViewport/FlatEarthUniverse";
}
