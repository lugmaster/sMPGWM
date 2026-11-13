using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.FileLoadingSystem;
using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.InputSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameConfigSystem;

public static class GameConfig
{
    public static string LogLevelValue { get; private set; } = string.Empty;
    public static bool RunSteamEx { get; private set; }
    public static string LobbyName { get; private set; } = string.Empty;
    public static string LobbyMode { get; private set; } = string.Empty;
    public static string LobbyGameId { get; private set; } = string.Empty;
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static string ServerIpAddress { get; set; } = string.Empty;
    public static string Version { get; private set; } = string.Empty;
    public static int ConnectionTimeout { get; private set; }
    public static bool ReloadConfigOnStart { get; private set; }
    public static bool Development { get; private set; }
    public static string GameDataChecksum { get; private set; } = string.Empty;
    public static string ConfigDataChecksum { get; private set; } = string.Empty;
    
    public static void Load()
    {
        Logger.Info("Loading game config values.");
        try
        {
            LobbyName = GetConfigValue("general", "lobby_name").AsString();
            LobbyMode = GetConfigValue("general", "lobby_mode").AsString();
            Port = GetConfigValue("general", "port").AsInt32();
            LogLevelValue = GetConfigValue("general", "log_level_value").AsString();
            Version = GetConfigValue("general", "version").AsString();
            ConnectionTimeout = GetConfigValue("general", "connection_timeout").AsInt32();
            RunSteamEx = GetConfigValue("general", "run_steam_ex").AsBool();
            ServerIpAddress = GetConfigValue("general", "server_ip_address").AsString();
            ReloadConfigOnStart = GetConfigValue("config", "reload_config_on_start").AsBool();
            Development = GetConfigValue("general", "development").AsBool();
            LobbyGameId = GetConfigValue("general", "lobby_game_id").AsString();
            MaxPlayers = GetConfigValue("general", "max_players").AsInt32();
            KeyBindingSettings.LoadConfiguration(
                ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey),
                GetConfigValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey),
                ConfigLoader.Instance.GetValueSource(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey));
            ConfigDataChecksum = ChecksumGenerator.Compute(ConfigLoader.Instance.CaptureValues());
            Logger.Info("Successfully loaded config values.");
        }
        catch (Exception ex)
        {
            GameFlowManager.Instance.QuitWithOsError($"Error while loading configuration values: {ex.Message}");
        }
    }
    
    public static void EnsureCompatibilityData()
    {
        if (!string.IsNullOrEmpty(GameDataChecksum)) return;
        if (string.IsNullOrEmpty(ConfigDataChecksum))
            throw new InvalidOperationException("Configuration is not loaded.");
        try
        {
            _ = ItemRegistry.GetAll();
            _ = ShipRegistry.GetAll();
            _ = GameplayDefinitions.Current;
            sMPGWM.Scripts.GamePlaySystem.StarterLoadout.ValidateDefinitions();
            var inputs = new System.Collections.Generic.Dictionary<string, string>(FileLoader.CapturedInputs);
            sMPGWM.Scripts.VfxSystem.WeaponPresentationCompatibility.Capture(inputs);
            inputs.Add("GameplayProtocol", "7:hitscan-weapons-and-combat-state");
            var checksum = ChecksumGenerator.Compute(inputs);
            foreach (var input in inputs) Logger.Info($"Compatibility file={input.Key} hash={ChecksumGenerator.Hash(input.Value)}");
            Logger.Info($"Compatibility development={Development} config={ConfigDataChecksum} data={checksum}");
            GameDataChecksum = checksum;
        }
        catch
        {
            GameplayDefinitions.ResetPreparation();
            ShipRegistry.ResetPreparation();
            ItemRegistry.ResetPreparation();
            FileLoader.CapturedInputs.Clear();
            throw;
        }
    }

    public static Error SaveKeyBindings(InputEvent?[,] bindings)
    {
        if (!KeyBindingSettings.IsValid(bindings)) return Error.InvalidData;
        var error = ConfigLoader.Instance.SaveValue(KeyBindingSettings.ConfigSection,
            KeyBindingSettings.ConfigKey, KeyBindingSettings.Serialize(bindings));
        if (error == Error.Ok) KeyBindingSettings.Apply(bindings);
        return error;
    }

    private static Variant GetConfigValue(string section, string key)
    {
        return ConfigLoader.Instance.GetValue(section, key);
    }
}

