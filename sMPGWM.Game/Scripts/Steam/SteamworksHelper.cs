using System;
using Godot;
using GDExtension.Wrappers;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Steam;

public partial class SteamworksHelper : AbstractSingleton<SteamworksHelper>
{
    public static GDExtension.Wrappers.Steam Api { get; private set; } = null!;
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
                Api.LobbyCreatedSignal += LobbyCreated;
                _lobbyCreatedSubscribed = true;
            }

            if (!_lobbyJoinedSubscribed)
            {
                Api.LobbyJoinedSignal += LobbyJoined;
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
            Api.LobbyCreatedSignal -= LobbyCreated;
            _lobbyCreatedSubscribed = false;
        }

        if (_lobbyJoinedSubscribed)
        {
            Api.LobbyJoinedSignal -= LobbyJoined;
            _lobbyJoinedSubscribed = false;
        }
    }

    public bool CreateSessionLobby(int capacity, Action<long, ulong> completion)
    {
        SubscribeLobbyEvents();
        if (!_create.Begin(0, completion)) return false;
        Api.CreateLobby(GDExtension.Wrappers.Steam.LobbyType.Private, capacity);
        return true;
    }

    public bool JoinSessionLobby(ulong lobby, Action<long, ulong> completion)
    {
        SubscribeLobbyEvents();
        if (!_join.Begin(lobby, completion)) return false;
        Api.JoinLobby(checked((long)lobby));
        return true;
    }

    public void CancelLobbyOperations(Action<long, ulong> completion)
    {
        _create.Cancel(completion);
        _join.Cancel(completion);
    }

    private void LobbyCreated(long result, long lobby) =>
        _create.Complete(result, checked((ulong)lobby), LeaveLateLobby);

    private void LobbyJoined(long lobby, long permissions, bool locked, long result) =>
        _join.Complete(result, checked((ulong)lobby), LeaveLateLobby);

    private static void LeaveLateLobby(ulong lobby)
    {
        Api.LeaveLobby(checked((long)lobby));
        Logger.Info("Canceled Steam lobby membership drained.");
    }

    protected override void OnExitTree()
    {
        UnsubscribeLobbyEvents();
    }

    protected override void OnEnterTree()
    {
        Api = GDExtension.Wrappers.Steam.GetSingleton();
        ProcessMode = ProcessModeEnum.Always;
        Logger.Debug("Setting Steam app id");
        OS.SetEnvironment("SteamAppId", SteamAppId.ToString());
        OS.SetEnvironment("SteamGameId", SteamAppId.ToString());
    }

    protected override void OnReady()
    {
        try
        {
            Logger.Debug(Api.IsSteamRunning() ? "Steam is running" : "Steam is not running");

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
        Api.RunCallbacks();
    }

    public static void ShutdownSteam()
    {
        Logger.Debug("Exiting Steam");

        try
        {
            Api.SteamShutdown();
        }
        catch (Exception e)
        {
            QuitWithOsError("Steamworks error: " + e);
        }
    }

    private static void InitializeSteamEx()
    {
        var initializeResponse = Api.SteamInitEx(SteamAppId, false);

        Logger.Debug($"SteamAPI.InitEx() response: {initializeResponse}");
        if (!initializeResponse.ContainsKey("status") ||
            initializeResponse["status"].As<long>() != (long)GDExtension.Wrappers.Steam.SteamApiInitResult.Ok)
            QuitWithOsError($"Failed to initialize SteamEX: {initializeResponse}");

        Logger.Debug("Steam initialized successfully with SteamInitEx.");
    }

    private static void InitializeSteam()
    {
        var initializeResponse = Api.SteamInit(SteamAppId, false);

        if (!initializeResponse)
            QuitWithOsError("Failed to initialize Steam.");

        Logger.Debug("Steam initialized successfully with SteamInit.");
    }

    private static void QuitWithOsError(string reason)
    {
        GameFlowManager.Instance.QuitWithOsError(reason);
    }
}
