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
    private readonly PendingLobbySearch _search = new();
    private bool _lobbySearchSubscribed;
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
        if (_lobbySearchSubscribed)
        {
            Api.LobbyMatchListSignal -= LobbySearchCompleted;
            _lobbySearchSubscribed = false;
        }
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
        return _create.Submit(0, completion,
            () => Api.CreateLobby(GDExtension.Wrappers.Steam.LobbyType.Public, capacity));
    }

    internal bool SearchLobbies(LobbyDiscovery discovery, Action<long[]> completion)
    {
        if (!_lobbySearchSubscribed)
        {
            Api.LobbyMatchListSignal += LobbySearchCompleted;
            _lobbySearchSubscribed = true;
        }
        return _search.Submit(completion, () =>
        {
            Api.AddRequestLobbyListDistanceFilter(GDExtension.Wrappers.Steam.LobbyDistanceFilter.Far);
            foreach (var filter in discovery.Filters)
                Api.AddRequestLobbyListStringFilter(filter.Key, filter.Value,
                    GDExtension.Wrappers.Steam.LobbyComparison.Equal);
            Api.RequestLobbyList();
        });
    }

    internal void CancelLobbySearch(Action<long[]> completion) => _search.Cancel(completion);

    private void LobbySearchCompleted(Godot.Collections.Array lobbies)
    {
        var ids = new long[lobbies.Count];
        for (var i = 0; i < ids.Length; i++) ids[i] = lobbies[i].AsInt64();
        _search.Complete(ids);
    }

    public bool JoinSessionLobby(ulong lobby, Action<long, ulong> completion)
    {
        SubscribeLobbyEvents();
        return _join.Submit(lobby, completion, () => Api.JoinLobby(checked((long)lobby)));
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
