using System;
using Godot;

namespace sMPGWM.Scripts.Autoload;

public partial class SteamworksHelper : Node
{
    private const uint SteamAppId = 480;
    private GodotObject ? _steam;
    private bool _runSteamEx = true;


    public override void _EnterTree()
    {
        Logger.Debug("Setting Steam app id");
        OS.SetEnvironment("SteamAppId", SteamAppId.ToString());
        OS.SetEnvironment("SteamGameId", SteamAppId.ToString());
    }

    public override void _Ready()
    {
        _steam = Engine.GetSingleton("Steam");

        if (_steam == null)
            QuitWithOsError("Steam singleton not found.");
        try
        {
            var isRunning = _steam!.Call("isSteamRunning").AsBool();
            Logger.Debug(isRunning ? "Steam is running" : "Steam is not running");

            if (_runSteamEx)
            {
                Logger.Debug("Using Steam.SteamInitEx().");
                InitializeSteamEx();
            }
            else
            {
                Logger.Debug("Using Steam.SteamInit().");
                InitializeSteam();
            }
        }
        catch (Exception e)
        {
            QuitWithOsError("Steamworks error: " + e);
        }
    }

    public override void _Process(double delta)
    {
        _steam?.Call("run_callbacks");
    }

    public override void _ExitTree()
    {
        Logger.Debug("Exiting Steam");

        try
        {
            _steam?.Call("steamShutdown");
        }
        catch (Exception e)
        {
            QuitWithOsError("Steamworks error: " + e);
        }
    }

    private void InitializeSteamEx()
    {
        var response = _steam!.Call("steamInitEx", SteamAppId, true).AsGodotDictionary();
        var status = response["status"];
        var verbal = response["verbal"];

        Logger.Debug($"SteamInitEx response: {status} - {verbal}");

        if ((int)status != 0)
            QuitWithOsError($"Failed to initialize SteamEX: {status} - {verbal}");

        Logger.Debug("Steam initialized successfully with SteamInitEx.");
    }

    private void InitializeSteam()
    {
        var response = _steam!.Call("steamInit", SteamAppId, true).AsGodotDictionary();
        var status = response["status"];
        var verbal = response["verbal"];

        Logger.Debug($"SteamInit response: {status} - {verbal}");

        if ((int)status != 0)
            QuitWithOsError($"Failed to initialize Steam: {status} - {verbal}");

        Logger.Debug("Steam initialized successfully with SteamInit.");
    }

    private void QuitWithOsError(string reason)
    {
        Logger.Error($"Error: {reason}");
        OS.Alert($"Error: {reason}");
        GetTree().Quit(1);
    }
}