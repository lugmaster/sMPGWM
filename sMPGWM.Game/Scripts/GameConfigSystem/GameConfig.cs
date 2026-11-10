using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Items;
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
        _ = ItemRegistry.GetAll();
        _ = ShipSystem.ShipRegistry.GetAll();
        _ = sMPGWM.Scripts.GamePlaySystem.GameplayDefinitions.Boost;
        var inputs = new System.Collections.Generic.Dictionary<string, string>(FileLoadingSystem.FileLoader.CapturedInputs);
        GameDataChecksum = ChecksumGenerator.Compute(inputs);
        foreach (var input in inputs) Logger.Info($"Compatibility file={input.Key} hash={ChecksumGenerator.Hash(input.Value)}");
        Logger.Info($"Compatibility development={Development} config={ConfigDataChecksum} data={GameDataChecksum}");
    }

    private static Variant GetConfigValue(string section, string key)
    {
        return ConfigLoader.Instance.GetValue(section, key);
    }
}

