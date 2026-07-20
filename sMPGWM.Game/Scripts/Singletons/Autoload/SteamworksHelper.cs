using System;
using Godot;
using GodotSteam;
using sMPGWM.Scripts.Singletons.Autoload.Base;

namespace sMPGWM.Scripts.Singletons.Autoload;

public partial class SteamworksHelper : AbstractSingleton<SteamworksHelper>
{
    private const uint SteamAppId = 480;
    private bool _runSteamEx = true;


    protected override void OnEnterTree()
    {
        Logger.Debug("Setting Steam app id");
        OS.SetEnvironment("SteamAppId", SteamAppId.ToString());
        OS.SetEnvironment("SteamGameId", SteamAppId.ToString());
    }

    protected override void OnReady()
    {
        try
        {
            Logger.Debug(Steam.IsSteamRunning() ? "Steam is running" : "Steam is not running");

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
        Steam.RunCallbacks();
    }

    public static void ShutdownSteam()
    {
        Logger.Debug("Exiting Steam");

        try
        {
            Steam.SteamShutdown();
        }
        catch (Exception e)
        {
            QuitWithOsError("Steamworks error: " + e);
        }
    }

    private static void InitializeSteamEx()
    {
        var initializeResponse = Steam.SteamInitEx(SteamAppId, true);

        Logger.Debug($"SteamAPI.InitEx() response: {initializeResponse.Status} - {initializeResponse.Verbal}");

        if (initializeResponse.Status != SteamInitExStatus.SteamworksActive)
            QuitWithOsError($"Failed to initialize SteamEX: {initializeResponse.Status} - {initializeResponse.Verbal}");

        Logger.Debug("Steam initialized successfully with SteamInitEx.");
    }

    private static void InitializeSteam()
    {
        var initializeResponse = Steam.SteamInit(SteamAppId, true);

        Logger.Debug($"SteamAPI.InitEx() response: {initializeResponse.Status} - {initializeResponse.Verbal}");

        if (initializeResponse.Status != SteamInitStatus.SteamworksActive)
            QuitWithOsError($"Failed to initialize Steam: {initializeResponse.Status} - {initializeResponse.Verbal}");

        Logger.Debug("Steam initialized successfully with SteamInit.");
    }

    private static void QuitWithOsError(string reason)
    {
        GameHandler.Instance.QuitWithOsError(reason);
    }
}