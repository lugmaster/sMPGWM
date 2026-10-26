using System;
using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.GameConfigSystem;

public partial class ConfigLoader : AbstractSingleton<ConfigLoader>
{
    private const string ConfigSection = "config";
    private const string ReloadConfigOnStartKey = "reload_config_on_start";

    private readonly ConfigFile _userConfig = new();
    private readonly ConfigFile _defaultConfig = new();

    protected override void OnReady()
    {
        var (error, source) = Load();
        if (error != Error.Ok)
        {
            var message = $"Failed to load config on startup: {error}";
            Logger.Error(message);
            GameFlowManager.Instance.QuitWithOsError(message);
            return;
        }

        Logger.Info($"Config startup load completed from {source}.");
        GameConfig.Load();
    }

    public (Error error, ConfigSource source) Load()
    {
        Logger.Info("Loading config files...");
        
        // Load default config
        if (!FileAccess.FileExists(FilePaths.SystemConfigPath))
        {
            Logger.Error($"Default config missing at {FilePaths.SystemConfigPath}");
            return (Error.CantOpen, ConfigSource.None);
        }
        
        var defaultError = _defaultConfig.Load(FilePaths.SystemConfigPath);
        if (defaultError != Error.Ok)
        {
            Logger.Error($"Failed to load default config: {defaultError}");
            return (defaultError, ConfigSource.None);
        }
        
        Logger.Info("Default config loaded.");
        
        // Check for reload_config_on_start
        var reloadOnStart = false;
        if (_defaultConfig.HasSectionKey(ConfigSection, ReloadConfigOnStartKey))
        {
            reloadOnStart = _defaultConfig.GetValue(ConfigSection, ReloadConfigOnStartKey).AsBool();
        }
        
        if (reloadOnStart)
        {
            Logger.Info("reload_config_on_start = true - resetting user config to default.");
            return CopyDefaultToUserConfig();
        }
        
        // If user config doesn't exist, copy from default
        if (!FileAccess.FileExists(FilePaths.UserConfigPath))
        {
            Logger.Info("User config not found. Copying default to user://");
            return CopyDefaultToUserConfig();
        }
        
        // Load user config
        var userError = _userConfig.Load(FilePaths.UserConfigPath);
        if (userError != Error.Ok)
        {
            Logger.Error($"Failed to load user config: {userError}");
            return (userError, ConfigSource.None);
        }
        
        ValidateUserKeys();
        
        Logger.Info("User config loaded successfully.");
        return (Error.Ok, ConfigSource.User);
    }

    private (Error, ConfigSource) CopyDefaultToUserConfig()
    {
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(FilePaths.UserConfigDirectory);
            
            using var defaultFile = FileAccess.Open(FilePaths.SystemConfigPath, FileAccess.ModeFlags.Read);
            using var userFile = FileAccess.Open(FilePaths.UserConfigPath, FileAccess.ModeFlags.Write);
            userFile.StoreString(defaultFile.GetAsText());
            userFile.Flush();
            
            var loaded = _userConfig.Load(FilePaths.UserConfigPath);
            if (loaded != Error.Ok)
            {
                Logger.Error($"Failed to load copied user config: {loaded}");
                return (loaded, ConfigSource.CopiedDefaultToUser);
            }
            
            Logger.Info("Copied and loaded default config into user://");
            return (Error.Ok, ConfigSource.CopiedDefaultToUser);
        }
        catch (Exception e)
        {
            Logger.Error($"Error copying default config: {e.Message}");
            return (Error.CantOpen, ConfigSource.None);
        }
    }

    public Variant GetValue(string section, string key)
    {
        if (_userConfig.HasSectionKey(section, key))
        {
            return _userConfig.GetValue(section, key);
        }
        
        if (_defaultConfig.HasSectionKey(section, key))
        {
            Logger.Warning($"[WARNING] Missing key in user config: [{section}] {key} - using default value.");
            return _defaultConfig.GetValue(section, key);
        }
        
        GameFlowManager.Instance.QuitWithOsError($"Missing required configuration key: [{section}] {key} (not in user:// or res://)");
        throw new Exception($"Missing required configuration key: [{section}] {key}");
    }

    public void SetValue(string section, string key, Variant value)
    {
        _userConfig.SetValue(section, key, value);
    }

    public void Save()
    {
        var error = _userConfig.Save(FilePaths.UserConfigPath);
        if (error != Error.Ok)
        {
            Logger.Error($"Failed to save config to {FilePaths.UserConfigPath}: {error}");
        }
        else
        {
            Logger.Info($"Config saved to {FilePaths.UserConfigPath}");
        }
    }

    private void ValidateUserKeys()
    {
        foreach (var section in _userConfig.GetSections())
        {
            foreach (var key in _userConfig.GetSectionKeys(section))
            {
                if (!_defaultConfig.HasSectionKey(section, key))
                {
                    Logger.Warning(
                        $"[WARNING] Unknown key in user config: [{section}] {key} - not present in default config.");
                }
            }
        }
    }
}
