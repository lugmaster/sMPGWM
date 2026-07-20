using System;
using Godot;
using sMPGWM.Scripts.Enums;
using sMPGWM.Scripts.Singletons.Base;

namespace sMPGWM.Scripts.Singletons.FileLoaders.Config;

public partial class ConfigLoader : AbstractSingleton<ConfigLoader>
{
    private readonly ConfigFile _userConfig = new();
    private readonly ConfigFile _defaultConfig = new();

    public (Error error, ConfigSource source) Load()
    {
        // Logger.Log("Loading config files...", 0);
        //
        // // Load default config
        // if (!FileAccess.FileExists(FilePaths.SystemConfigPath))
        // {
        //     Logger.Error($"Default config missing at {FilePaths.SystemConfigPath}");
        //     return (Error.CantOpen, ConfigSource.None);
        // }
        //
        // var defaultError = _defaultConfig.Load(FilePaths.SystemConfigPath);
        // if (defaultError != Error.Ok)
        // {
        //     Logger.Error($"Failed to load default config: {defaultError}");
        //     return (defaultError, ConfigSource.None);
        // }
        //
        // Logger.Log("Default config loaded.", 0);
        //
        // // Check for reload_config_on_start
        // var reloadOnStart = false;
        // if (_defaultConfig.HasSectionKey("config", "reload_config_on_start"))
        // {
        //     reloadOnStart = _defaultConfig.GetValue("config", "reload_config_on_start").AsBool();
        // }
        //
        // if (reloadOnStart)
        // {
        //     Logger.Log("reload_config_on_start = true — Resetting user config to default.", 0);
        //     return CopyDefaultToUserConfig();
        // }
        //
        // // If user config doesn't exist, copy from default
        // if (!FileAccess.FileExists(FilePaths.UserConfigPath))
        // {
        //     Logger.Log("User config not found. Copying default to user://", 0);
        //     return CopyDefaultToUserConfig();
        // }
        //
        // // Load user config
        // var userError = _userConfig.Load(FilePaths.UserConfigPath);
        // if (userError != Error.Ok)
        // {
        //     Logger.Error($"Failed to load user config: {userError}");
        //     return (userError, ConfigSource.None);
        // }
        //
        // ValidateUserKeys();
        //
        // Logger.Log("User config loaded successfully.", 0);
        return (Error.Ok, ConfigSource.User);
    }

    private (Error, ConfigSource) CopyDefaultToUserConfig()
    {
        try
        {
            // DirAccess.MakeDirRecursiveAbsolute(FilePaths.UserConfigDirectory);
            //
            // using var defaultFile = FileAccess.Open(FilePaths.SystemConfigPath, FileAccess.ModeFlags.Read);
            // using var userFile = FileAccess.Open(FilePaths.UserConfigPath, FileAccess.ModeFlags.Write);
            // userFile.StoreString(defaultFile.GetAsText());
            //
            // var loaded = _userConfig.Load(FilePaths.UserConfigPath);
            // if (loaded != Error.Ok)
            // {
            //     Logger.Error($"Failed to load copied user config: {loaded}");
            //     return (loaded, ConfigSource.CopiedDefaultToUser);
            // }
            //
            // Logger.Log("Copied and loaded default config into user://", 0);
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
        // if (_userConfig.HasSectionKey(section, key))
        // {
        //     return _userConfig.GetValue(section, key);
        // }
        //
        // if (_defaultConfig.HasSectionKey(section, key))
        // {
        //     Logger.Log($"[WARNING] Missing key in user config: [{section}] {key} — using default value.", 1);
        //     return _defaultConfig.GetValue(section, key);
        // }
        //
        // GlobalStateManager.QuitWithOsError($"Missing required configuration key: [{section}] {key} (not in user:// or res://)");
        throw new Exception($"Missing required configuration key: [{section}] {key}");
    }

    public void SetValue(string section, string key, Variant value)
    {
        _userConfig.SetValue(section, key, value);
    }

    public void Save()
    {
        // var error = _userConfig.Save(FilePaths.UserConfigPath);
        // if (error != Error.Ok)
        // {
        //     Logger.Error($"Failed to save config to {FilePaths.UserConfigPath}: {error}");
        // }
        // else
        // {
        //     Logger.Log($"Config saved to {FilePaths.UserConfigPath}", 0);
        // }
    }

    private void ValidateUserKeys()
    {
        // foreach (var section in _userConfig.GetSections())
        // {
        //     foreach (var key in _userConfig.GetSectionKeys(section))
        //     {
        //         if (!_defaultConfig.HasSectionKey(section, key))
        //         {
        //             Logger.Log(
        //                 $"[WARNING] Unknown key in user config: [{section}] {key} — not present in default config.", 1);
        //         }
        //     }
        // }
    }
}