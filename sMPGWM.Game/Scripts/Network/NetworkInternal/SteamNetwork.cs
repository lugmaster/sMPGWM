using System;
using Godot;
using GDExtension.Wrappers;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Steam;
using sMPGWM.Scripts.Network.NetworkInternal;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class SteamNetwork : BaseNetwork, IActiveNetwork
{
    private SteamMultiplayerPeer? _steamMultiplayerPeer;
    private ulong? _currentLobbyId;
    private bool _cleaned;
    private bool _started;
    private bool _hosting;
    private bool _completed;
    private ulong _targetLobby;
    private ulong _pinnedHost;
    private string _pinnedSession = "";
    public NetworkStartupData? Startup { get; init; }
    private ulong _deadline;
    private MultiplayerPeer? _ownedPeer;

    private void Report(bool success, string reason = "Connected")
    {
        if (_cleaned || _completed) return;
        _completed = true;
        EmitConnected(success, reason);
    }

    private void Connected() { if (!_hosting && _ownedPeer != null) Report(true); }
    private void Failed() => Report(false, "Steam connection failed.");
    private void Disconnected()
    {
        if (!_cleaned) EmitConnected(false, "Steam server disconnected.");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_started && !_completed && !_cleaned && Time.GetTicksMsec() >= _deadline)
            Report(false, "Steam startup timed out.");
    }

    private bool Begin(bool hosting)
    {
        if (_started || _cleaned) return false;
        _started = true;
        _hosting = hosting;
        _deadline = Time.GetTicksMsec() + (ulong)Math.Max(1, Startup?.ConnectionTimeoutMs ?? ConnectionTimeout);
        return true;
    }
    
    private const int StateEntered      = 0x01;
    private const int StateLeft         = 0x02;
    private const int StateDisconnected = 0x04;
    private const int StateKicked       = 0x08;
    private const int StateBanned       = 0x10;

    public override void _Ready()
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(_Ready)} Steam network ready.");
        try
        {
            _steamMultiplayerPeer = SteamMultiplayerPeer.Instantiate();

            _steamMultiplayerPeer.ServerRelay = true;

            Multiplayer.ConnectedToServer += Connected;
            Multiplayer.ConnectionFailed += Failed;
            Multiplayer.ServerDisconnected += Disconnected;
            SteamworksHelper.Api.LobbyChatUpdateSignal += OnLobbyChatUpdate;
            SteamworksHelper.Api.LobbyDataUpdateSignal += OnLobbyDataUpdate;
            GameFlowManager.Instance.OnSuddenExit += SetLobbyUnjoinable;

            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(_Ready)} SteamMultiplayerPeer GDExtension loaded successfully.");
        }
        catch (Exception e)
        {
            // Godot invokes _Ready through its bridge; report here rather than throwing across it.
            Report(false, e.Message);
        }
    }

    private void TraceCapabilities(string phase, Error result = Error.Ok)
    {
        if (Startup?.Diagnostic != true && OS.GetEnvironment("NET00_CAPABILITIES") != "1")
            return;

        var peer = (MultiplayerPeer)_steamMultiplayerPeer;
        using var api = new SceneMultiplayer();
        Logger.Info($"NET00 phase={phase} engine={Engine.GetVersionInfo()["string"]} native={_steamMultiplayerPeer.GetClass()} result={result} status={peer.GetConnectionStatus()}");
        foreach (var method in new[] { "host_with_lobby", "connect_to_lobby", "close", "get_steam_id_for_peer_id", "get_peer_id_for_steam_id" })
            Logger.Info($"NET00 native_method={method} available={_steamMultiplayerPeer.HasMethod(method)}");
        foreach (var method in new[] { "set_auth_callback", "send_auth", "complete_auth", "set_server_relay_enabled" })
            Logger.Info($"NET00 api_method={method} available={api.HasMethod(method)}");
        if (phase == "host" && result == Error.Ok)
            Logger.Info($"NET00 server_id={peer.GetUniqueId()} relay_supported={peer.IsServerRelaySupported()}");
    }

    public void BecomeHost(int maxConnections)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(BecomeHost)} becoming host.");
        if (!Begin(true)) return;
        if (maxConnections is < 1 or > 10) { Report(false, "Invalid player capacity."); return; }
        try
        {
            if (!SteamworksHelper.Instance.CreateSessionLobby(maxConnections, LobbyResult))
                Report(false, "Steam create unavailable: previous operation is still draining; restart Steam runtime if it never returns.");
        }
        catch (Exception e) { Report(false, e.Message); }
    }

    public void BecomeClient(ulong lobbyId)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(BecomeClient)} becoming client.");
        if (!Begin(false)) return;
        _targetLobby = lobbyId;
        if (lobbyId == 0) { Report(false, "Invalid Steam lobby target."); return; }
        try
        {
            if (!SteamworksHelper.Instance.JoinSessionLobby(lobbyId, LobbyResult))
                Report(false, "Steam join unavailable: previous operation is still draining; restart Steam runtime if it never returns.");
        }
        catch (Exception e) { Report(false, e.Message); }
    }

    public Node GetNode()
    {
        return this;
    }

    private void LobbyResult(long result, ulong lobby)
    {
        try
        {
            if (_hosting) OnLobbyCreated(result, lobby);
            else OnLobbyJoined(lobby, 0, false, result);
        }
        catch (Exception e) { Report(false, $"Steam lobby callback failed: {e.Message}"); }
    }

    private void OnLobbyCreated(long connect, ulong lobbyId)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} received lobby creation result.");
        if (_cleaned || !_started || !_hosting || _completed) return;
        if (connect == 1) _currentLobbyId = lobbyId;
        if (connect == 1)
        {
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} created lobby with {nameof(lobbyId)}={lobbyId}.");
            if (!SteamworksHelper.Api.SetLobbyJoinable(checked((long)lobbyId), false)) throw new InvalidOperationException("Cannot lock lobby.");

            SetData(lobbyId, "name", GameConfig.LobbyName + " " + Guid.NewGuid().ToString("N")[..8]);
            SetData(lobbyId, "game_id", GameConfig.LobbyGameId);
            SetData(lobbyId, "mode", GameConfig.LobbyMode);
            SetData(lobbyId, "version", GameConfig.Version);
            SetData(lobbyId, "protocol", DiagnosticExchange.ProtocolVersion.ToString());
            SetData(lobbyId, "host_steam_id", SteamworksHelper.Api.GetSteamId().ToString());
            SetData(lobbyId, "session_id", Guid.NewGuid().ToString("N"));
            SetData(lobbyId, "state", "preparing");

            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} creating host with lobby name={SteamworksHelper.Api.GetLobbyData(checked((long)lobbyId), "name")}.");

            TraceCapabilities("constructed");
            var error = _steamMultiplayerPeer.HostWithLobby(checked((long)lobbyId));
            TraceCapabilities("host", error);
            if (error == Error.Ok)
            {
                _ownedPeer = _steamMultiplayerPeer;
                Multiplayer.MultiplayerPeer = _ownedPeer;
                Logger.Debug(
                    $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} created host from lobby.");
                if (Startup?.Diagnostic == true)
                {
                    SetData(lobbyId, "state", "diagnostic");
                    if (!SteamworksHelper.Api.SetLobbyJoinable(checked((long)lobbyId), true)) throw new InvalidOperationException("Cannot open diagnostic lobby.");
                    var layer = new CanvasLayer { Layer = 101 };
                    var label = new Label { Text = $"Private lobby ID: {lobbyId}", Position = new Vector2(20, 150) };
                    layer.AddChild(label);
                    AddChild(layer);
                    Logger.Info($"Steam diagnostic lobby={lobbyId} ready; gameplay inactive.");
                }
                Report(true, "Listening");
            }
            else
            {
                Report(false, $"SteamNetwork(IActiveNetwork): Error creating host: {error}");
            }
        }
        else
        {
            Report(false, $"SteamNetwork(IActiveNetwork): Failed to create lobby. Error: {connect}");
        }
    }

    private void OnLobbyJoined(ulong lobby, long permissions, bool locked, long response)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(OnLobbyJoined)} received lobby join result.");
        if (_cleaned || !_started || _hosting || _completed || lobby != _targetLobby || _ownedPeer != null) return;
        if (response == 1)
        {
            _currentLobbyId = lobby;
            var lobbyId = SteamworksHelper.Api.GetLobbyOwner(checked((long)lobby));
            _pinnedHost = checked((ulong)lobbyId);
            _pinnedSession = SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "session_id");
            if (Startup?.Diagnostic == true && !ValidDiagnosticLobby(lobby))
            {
                Report(false, "Steam lobby is stale or incompatible.");
                return;
            }

            // Host joins lobby itself compare with SteamId
            if (lobbyId == checked((long)SteamworksHelper.Api.GetSteamId()))
            {
                Report(false, "Cannot join a self-hosted lobby.");
                return;
            }
            else
            {
                // Client joins lobby - connect socket to host
                ConnectSocket(lobby);
            }
        }
        else
        {
            var failReason = response switch
            {
                2 => "This lobby no longer exists.",
                3 => "You don't have permission to join this lobby.",
                4 => "The lobby is now full.",
                5 => "Uh... something unexpected happened!",
                6 => "You are banned from this lobby.",
                7 => "You cannot join due to having a limited account.",
                8 => "This lobby is locked or disabled.",
                9 => "This lobby is community locked.",
                10 => "A user in the lobby has blocked you from joining.",
                11 => "A user you have blocked is in the lobby.",
                _ => $"Unknown Reason: {response}"
            };

            Report(false, $"SteamNetwork(IActiveNetwork) failed: {failReason}");
        }
    }

    private static void SetData(ulong lobby, string key, string value)
    {
        if (!SteamworksHelper.Api.SetLobbyData(checked((long)lobby), key, value))
            throw new InvalidOperationException($"Steam metadata write failed: {key}");
    }

    private bool ValidDiagnosticLobby(ulong lobby)
    {
        if (_pinnedHost == 0 || SteamworksHelper.Api.GetLobbyOwner(checked((long)lobby)) != checked((long)_pinnedHost) ||
            SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "host_steam_id") != checked((long)_pinnedHost).ToString() ||
            !Guid.TryParseExact(_pinnedSession, "N", out _) ||
            SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "session_id") != _pinnedSession ||
            SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "game_id") != GameConfig.LobbyGameId ||
            SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "version") != GameConfig.Version ||
            SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "protocol") != DiagnosticExchange.ProtocolVersion.ToString() ||
            SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "state") != "diagnostic") return false;
        for (var i = 0; i < SteamworksHelper.Api.GetNumLobbyMembers(checked((long)lobby)); i++)
            if (SteamworksHelper.Api.GetLobbyMemberByIndex(checked((long)lobby), i) == checked((long)_pinnedHost)) return true;
        return false;
    }

    private void OnLobbyDataUpdate(long success, long lobby, long member)
    {
        if (_cleaned || _hosting || _currentLobbyId != (ulong)lobby || member != lobby || Startup?.Diagnostic != true) return;
        if (success != 1 || !ValidDiagnosticLobby(checked((ulong)lobby)))
            EmitConnected(false, "Steam diagnostic lobby changed or original host was lost.");
    }

    private void ConnectSocket(ulong steamId)
    {
        try
        {
            var result = _steamMultiplayerPeer.ConnectToLobby(checked((long)steamId));
            TraceCapabilities("client", result);
            if (result != Error.Ok) { Report(false, $"CreateClient: {result}"); return; }
            _ownedPeer = _steamMultiplayerPeer;
            Multiplayer.MultiplayerPeer = _ownedPeer;
        }
        catch (Exception e) { Report(false, $"Error creating client: {e.Message}"); }
    }

    private void OnLobbyChatUpdate(long lobbyId, long changedId, long makingChangeId, long chatState)
    {
        if (_cleaned || _currentLobbyId != (ulong)lobbyId) return;
        if (!_hosting && changedId == checked((long)_pinnedHost) && (chatState & (StateLeft | StateDisconnected | StateKicked | StateBanned)) != 0)
        {
            EmitConnected(false, "Original Steam host left the lobby.");
            return;
        }
        if ((chatState & StateLeft) != 0)
        {
            Logger.Info(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyChatUpdate)} user left lobby with {nameof(changedId)}={changedId}, {nameof(lobbyId)}={lobbyId}.");
        }
        if ((chatState & StateDisconnected) != 0)
        {
            Logger.Info(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyChatUpdate)} user disconnected from lobby with {nameof(changedId)}={changedId}, {nameof(lobbyId)}={lobbyId}.");
        }
        if ((chatState & StateKicked) != 0)
        {
            Logger.Info(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyChatUpdate)} user was kicked from lobby with {nameof(changedId)}={changedId}, {nameof(lobbyId)}={lobbyId}.");
        }
        if ((chatState & StateBanned) != 0)
        {
            Logger.Info(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyChatUpdate)} user was banned from lobby with {nameof(changedId)}={changedId}, {nameof(lobbyId)}={lobbyId}.");
        }
        if ((chatState & StateEntered) != 0)
        {
            Logger.Info(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyChatUpdate)} user entered lobby with {nameof(changedId)}={changedId}, {nameof(lobbyId)}={lobbyId}.");
        }
    }

    public bool IsConnected()
    {
        return _steamMultiplayerPeer != null &&
               _steamMultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
    }

    public MultiplayerPeer.ConnectionStatus GetConnectionStatus()
    {
        return _steamMultiplayerPeer?.GetConnectionStatus() ?? MultiplayerPeer.ConnectionStatus.Disconnected;
    }


    public override void Cleanup()
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(Cleanup)} cleanup started.");

        if (_cleaned) return;
        _cleaned = true;
        Multiplayer.ConnectedToServer -= Connected;
        Multiplayer.ConnectionFailed -= Failed;
        Multiplayer.ServerDisconnected -= Disconnected;
        if (_hosting) SetLobbyUnjoinable();
        if (_ownedPeer != null && Multiplayer.MultiplayerPeer == _ownedPeer)
            Multiplayer.MultiplayerPeer = null;
        _ownedPeer = null;
        GameFlowManager.Instance.OnSuddenExit -= SetLobbyUnjoinable;
        if (_currentLobbyId.HasValue) SteamworksHelper.Api.LeaveLobby(checked((long)_currentLobbyId.Value));
        SteamworksHelper.Instance?.CancelLobbyOperations(LobbyResult);
        SteamworksHelper.Api.LobbyChatUpdateSignal -= OnLobbyChatUpdate;
        SteamworksHelper.Api.LobbyDataUpdateSignal -= OnLobbyDataUpdate;

        _steamMultiplayerPeer?.Close();
        _steamMultiplayerPeer?.Dispose();
        _steamMultiplayerPeer = null;

        _currentLobbyId = null;
        base.Cleanup();
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(Cleanup)} cleanup complete.");
    }
    
    private void SetLobbyUnjoinable()
    {
        try
        {
            if (!_currentLobbyId.HasValue)
            {
                Logger.Debug(
                    $"{nameof(SteamNetwork)}.{nameof(SetLobbyUnjoinable)} no lobby exists, cannot set unjoinable.");
                return;
            }

            if (!SteamworksHelper.Api.IsSteamRunning())
            {
                Logger.Debug(
                    $"{nameof(SteamNetwork)}.{nameof(SetLobbyUnjoinable)} Steam is not running, cannot set lobby unjoinable.");
                return;
            }

            SteamworksHelper.Api.SetLobbyJoinable(checked((long)_currentLobbyId.Value), false);
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(SetLobbyUnjoinable)} lobby set to unjoinable with {nameof(_currentLobbyId)}={_currentLobbyId.Value}.");
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"{nameof(SteamNetwork)}.{nameof(SetLobbyUnjoinable)} failed to set lobby unjoinable with {nameof(ex.Message)}={ex.Message}.");
        }
    }

}
