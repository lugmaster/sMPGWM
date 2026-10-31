using System;
using Godot;
using GodotSteam;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Steam;

public partial class SteamworksHelper : AbstractSingleton<SteamworksHelper>
{
    private const uint SteamAppId = 480;
    private readonly PendingLobbyOperation _create = new();
    private readonly PendingLobbyOperation _join = new();
    private bool _lobbyCreatedSubscribed;
    private bool _lobbyJoinedSubscribed;

    public void SubscribeLobbyEvents()
    {
        try
        {
            if (!_lobbyCreatedSubscribed)
            {
                GodotSteam.Steam.LobbyCreated += LobbyCreated;
                _lobbyCreatedSubscribed = true;
            }

            if (!_lobbyJoinedSubscribed)
            {
                GodotSteam.Steam.LobbyJoined += LobbyJoined;
                _lobbyJoinedSubscribed = true;
            }
        }
        catch
        {
            UnsubscribeLobbyEvents();
            throw;
        }
    }

    public void UnsubscribeLobbyEvents()
    {
        if (_lobbyCreatedSubscribed)
        {
            GodotSteam.Steam.LobbyCreated -= LobbyCreated;
            _lobbyCreatedSubscribed = false;
        }

        if (_lobbyJoinedSubscribed)
        {
            GodotSteam.Steam.LobbyJoined -= LobbyJoined;
            _lobbyJoinedSubscribed = false;
        }
    }

    public bool CreateSessionLobby(int capacity, Action<long, ulong> completion)
    {
        if (!_create.Begin(0, completion)) return false;
        GodotSteam.Steam.CreateLobby(GodotSteam.Steam.LobbyType.Private, capacity);
        return true;
    }

    public bool JoinSessionLobby(ulong lobby, Action<long, ulong> completion)
    {
        if (!_join.Begin(lobby, completion)) return false;
        GodotSteam.Steam.JoinLobby(lobby);
        return true;
    }

    public void CancelLobbyOperations(Action<long, ulong> completion)
    {
        _create.Cancel(completion);
        _join.Cancel(completion);
    }

    private void LobbyCreated(long result, ulong lobby) =>
        _create.Complete(result, lobby, LeaveLateLobby);

    private void LobbyJoined(ulong lobby, long permissions, bool locked, long result) =>
        _join.Complete(result, lobby, LeaveLateLobby);

    private static void LeaveLateLobby(ulong lobby)
    {
        GodotSteam.Steam.LeaveLobby(lobby);
        Logger.Info("Canceled Steam lobby membership drained.");
    }

    protected override void OnExitTree()
    {
        UnsubscribeLobbyEvents();
    }

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
            Logger.Debug(GodotSteam.Steam.IsSteamRunning() ? "Steam is running" : "Steam is not running");

            if (GameConfig.RunSteamEx)
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
        GodotSteam.Steam.RunCallbacks();
    }

    public static void ShutdownSteam()
    {
        Logger.Debug("Exiting Steam");

        try
        {
            GodotSteam.Steam.SteamShutdown();
        }
        catch (Exception e)
        {
            QuitWithOsError("Steamworks error: " + e);
        }
    }

    private static void InitializeSteamEx()
    {
        var initializeResponse = GodotSteam.Steam.SteamInitEx(SteamAppId, true);

        Logger.Debug($"SteamAPI.InitEx() response: {initializeResponse.Status} - {initializeResponse.Verbal}");

        if (initializeResponse.Status != SteamInitExStatus.SteamworksActive)
            QuitWithOsError($"Failed to initialize SteamEX: {initializeResponse.Status} - {initializeResponse.Verbal}");

        Logger.Debug("Steam initialized successfully with SteamInitEx.");
    }

    private static void InitializeSteam()
    {
        var initializeResponse = GodotSteam.Steam.SteamInit(SteamAppId, true);

        Logger.Debug($"SteamAPI.InitEx() response: {initializeResponse.Status} - {initializeResponse.Verbal}");

        if (initializeResponse.Status != SteamInitStatus.SteamworksActive)
            QuitWithOsError($"Failed to initialize Steam: {initializeResponse.Status} - {initializeResponse.Verbal}");

        Logger.Debug("Steam initialized successfully with SteamInit.");
    }

    private static void QuitWithOsError(string reason)
    {
        GameFlowManager.Instance.QuitWithOsError(reason);
    }
}
