using System;
using Godot;
using System.Reflection;
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
    private bool Hosting => Startup.Role == NetworkRole.Host;
    private ulong _targetLobby;
    private ulong _pinnedHost;
    private string _pinnedSession = "";
    private string _lobbyValidationFailure = "";
    private MultiplayerPeer? _ownedPeer;
    public ulong? LobbyId => _currentLobbyId;
    protected override string TimeoutReason => "Steam startup timed out.";

    private void Connected() { if (!Hosting && _ownedPeer != null) ReadyTransport(); }
    private void Failed() => FailTransport("Steam connection failed.");
    private void Disconnected() => FailTransport("Steam server disconnected.");

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

            if (Multiplayer is SceneMultiplayer api)
            {
                Subscribe(() => api.PeerAuthenticating += VerifyIdentity, () => api.PeerAuthenticating -= VerifyIdentity);
            }
            Subscribe(() => Multiplayer.ConnectedToServer += Connected, () => Multiplayer.ConnectedToServer -= Connected);
            Subscribe(() => Multiplayer.ConnectionFailed += Failed, () => Multiplayer.ConnectionFailed -= Failed);
            Subscribe(() => Multiplayer.ServerDisconnected += Disconnected, () => Multiplayer.ServerDisconnected -= Disconnected);
            Subscribe(() => SteamworksHelper.Api.LobbyChatUpdateSignal += OnLobbyChatUpdate, () => SteamworksHelper.Api.LobbyChatUpdateSignal -= OnLobbyChatUpdate);
            Subscribe(() => SteamworksHelper.Api.LobbyDataUpdateSignal += OnLobbyDataUpdate, () => SteamworksHelper.Api.LobbyDataUpdateSignal -= OnLobbyDataUpdate);
            Subscribe(() => GameFlowManager.Instance.OnSuddenExit += SetLobbyUnjoinable, () => GameFlowManager.Instance.OnSuddenExit -= SetLobbyUnjoinable);

            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(_Ready)} SteamMultiplayerPeer GDExtension loaded successfully.");
        }
        catch (Exception e)
        {
            // Godot invokes _Ready through its bridge; report here rather than throwing across it.
            FailTransport(e.Message);
        }
    }

    private void VerifyIdentity(long peer)
    {
        if (State == TransportState.Stopped || _steamMultiplayerPeer == null) return;
        try
        {
            var steamId = _steamMultiplayerPeer.GetSteamIdForPeerId(peer);
            var reverse = steamId > 0 ? _steamMultiplayerPeer.GetPeerIdForSteamId(steamId) : 0;
            var member = false;
            if (_currentLobbyId.HasValue)
            {
                var lobby = checked((long)_currentLobbyId.Value);
                for (var i = 0; i < SteamworksHelper.Api.GetNumLobbyMembers(lobby); i++)
                    if (SteamworksHelper.Api.GetLobbyMemberByIndex(lobby, i) == steamId) member = true;
            }
            var valid = steamId > 0 && reverse == peer && member &&
                        (Hosting || (peer == 1 && steamId == checked((long)_pinnedHost)));
            Logger.Info($"NET00 identity peer={peer} steam={steamId} reverse={reverse} member={member} valid={valid}");
            if (!valid) ((SceneMultiplayer)Multiplayer).DisconnectPeer(checked((int)peer));
        }
        catch (Exception e)
        {
            Logger.Warning($"NET00 identity failed peer={peer}: {e.Message}");
            ((SceneMultiplayer)Multiplayer).DisconnectPeer(checked((int)peer));
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
        if (!Begin()) return;
        if (maxConnections is < 1 or > 10) { FailTransport("Invalid player capacity."); return; }
        try
        {
            if (!SteamworksHelper.Instance.CreateSessionLobby(maxConnections, LobbyResult))
                FailTransport("Steam create unavailable: previous operation is still draining; restart Steam runtime if it never returns.");
        }
        catch (Exception e) { FailTransport(e.Message); }
    }

    public void BecomeClient(ulong lobbyId)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(BecomeClient)} becoming client.");
        if (!Begin()) return;
        _targetLobby = lobbyId;
        if (lobbyId == 0) { FailTransport("Invalid Steam lobby target."); return; }
        try
        {
            if (!SteamworksHelper.Instance.JoinSessionLobby(lobbyId, LobbyResult))
                FailTransport("Steam join unavailable: previous operation is still draining; restart Steam runtime if it never returns.");
        }
        catch (Exception e) { FailTransport(e.Message); }
    }

    public Node GetNode()
    {
        return this;
    }

    private void LobbyResult(long result, ulong lobby)
    {
        try
        {
            if (Hosting) OnLobbyCreated(result, lobby);
            else OnLobbyJoined(lobby, 0, false, result);
        }
        catch (Exception e) { FailTransport($"Steam lobby callback failed: {e.Message}"); }
    }

    private void OnLobbyCreated(long connect, ulong lobbyId)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} received lobby creation result.");
        if (State != TransportState.Starting || !Hosting) return;
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
            SetData(lobbyId, "session_id", NetworkManager.Instance.HostSession);
            LogBuildIdentity(lobbyId);
            foreach (var field in Compatibility.Capture()) SetData(lobbyId, field.Key, field.Value);
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
                SetData(lobbyId, "state", Startup.Diagnostic ? "diagnostic" : "ready");
                if (!SteamworksHelper.Api.SetLobbyJoinable(checked((long)lobbyId), true))
                    throw new InvalidOperationException("Cannot open lobby.");
                if (Startup.Diagnostic)
                    Logger.Info($"Steam diagnostic lobby={lobbyId} ready; gameplay inactive.");
                ReadyTransport("Listening");
            }
            else
            {
                FailTransport($"SteamNetwork(IActiveNetwork): Error creating host: {error}");
            }
        }
        else
        {
            FailTransport($"SteamNetwork(IActiveNetwork): Failed to create lobby. Error: {connect}");
        }
    }

    private void OnLobbyJoined(ulong lobby, long permissions, bool locked, long response)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(OnLobbyJoined)} received lobby join result.");
        if (State != TransportState.Starting || Hosting || lobby != _targetLobby || _ownedPeer != null) return;
        if (response == 1)
        {
            _currentLobbyId = lobby;
            var lobbyId = SteamworksHelper.Api.GetLobbyOwner(checked((long)lobby));
            _pinnedHost = checked((ulong)lobbyId);
            _pinnedSession = SteamworksHelper.Api.GetLobbyData(checked((long)lobby), "session_id");
            // Steam supplies the owner only after lobby entry. It is never a discovery filter.
            if (!ValidDiagnosticLobby(lobby))
            {
                FailTransport($"Steam lobby is stale or incompatible: {_lobbyValidationFailure}");
                return;
            }

            // Host joins lobby itself compare with SteamId
            if (lobbyId == checked((long)SteamworksHelper.Api.GetSteamId()))
            {
                FailTransport("Cannot join a self-hosted lobby.");
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

            FailTransport($"SteamNetwork(IActiveNetwork) failed: {failReason}");
        }
    }

    private static void SetData(ulong lobby, string key, string value)
    {
        if (!SteamworksHelper.Api.SetLobbyData(checked((long)lobby), key, value))
            throw new InvalidOperationException($"Steam metadata write failed: {key}");
        Logger.Info($"Steam lobby metadata published lobby={lobby} key={key} value='{value}' length={value.Length}");
    }

    private void LogBuildIdentity(ulong lobby)
    {
        var module = typeof(NetworkManager).Module;
        var assembly = module.Assembly;
        Logger.Info($"Steam lobby local build lobby={lobby} role={Startup.Role} " +
                    $"source=NetworkManager.Module.ModuleVersionId build={module.ModuleVersionId:N} " +
                    $"assembly='{assembly.FullName}' module='{module.ScopeName}' location='{assembly.Location}' " +
                    $"configuration='{assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration}' " +
                    $"informationalVersion='{assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}'");
    }

    private bool ValidDiagnosticLobby(ulong lobby)
    {
        _lobbyValidationFailure = "";
        var owner = SteamworksHelper.Api.GetLobbyOwner(checked((long)lobby));
        Logger.Info($"Steam lobby validation lobby={lobby} targetLobby={_targetLobby} " +
                    $"pinnedHost={_pinnedHost} actualOwner={owner} pinnedSession='{_pinnedSession}'");
        if (_pinnedHost == 0 || owner != checked((long)_pinnedHost))
            return RejectLobby($"owner changed (expected={_pinnedHost}, actual={owner}).");
        if (!Guid.TryParseExact(_pinnedSession, "N", out _))
            return RejectLobby("session_id is missing or invalid.");

        LogBuildIdentity(lobby);
        if (!Compatibility.Matches(key => SteamworksHelper.Api.GetLobbyData(checked((long)lobby), key)))
            return RejectLobby("Compatibility mismatch; see compatibility log.");
        string? firstMismatch = null;
        foreach (var (key, expected) in new[]
        {
            ("host_steam_id", checked((long)_pinnedHost).ToString()),
            ("session_id", _pinnedSession),
            ("game_id", GameConfig.LobbyGameId),
            ("version", GameConfig.Version),
            ("protocol", DiagnosticExchange.ProtocolVersion.ToString()),
            ("state", Startup.Diagnostic ? "diagnostic" : "ready")
        })
        {
            var actual = SteamworksHelper.Api.GetLobbyData(checked((long)lobby), key);
            Logger.Info($"Steam lobby metadata comparison lobby={lobby} key={key} " +
                        $"expected='{expected}' expectedLength={expected.Length} " +
                        $"actual='{actual}' actualLength={actual.Length} match={actual == expected} " +
                        "actualSource=Steam.GetLobbyData");
            if (actual != expected)
                firstMismatch ??= $"{key} mismatch (expected={expected}, actual={actual})." +
                    (key == "build" ? " Use the same built game on both machines." : "");
        }
        // Log every field even when an earlier comparison failed, then reject as before.
        if (firstMismatch != null) return RejectLobby(firstMismatch);
        for (var i = 0; i < SteamworksHelper.Api.GetNumLobbyMembers(checked((long)lobby)); i++)
            if (SteamworksHelper.Api.GetLobbyMemberByIndex(checked((long)lobby), i) == checked((long)_pinnedHost)) return true;
        return RejectLobby("original host is no longer a lobby member.");
    }

    private bool RejectLobby(string reason)
    {
        _lobbyValidationFailure = reason;
        Logger.Warning($"Steam lobby validation failed lobby={_currentLobbyId}: {reason}");
        return false;
    }

    private void OnLobbyDataUpdate(long success, long lobby, long member)
    {
        if (State == TransportState.Stopped || Hosting || _currentLobbyId != (ulong)lobby || member != lobby) return;
        if (success != 1)
            FailTransport("Steam lobby metadata update failed.");
        else if (!ValidDiagnosticLobby(checked((ulong)lobby)))
            FailTransport($"Steam lobby changed: {_lobbyValidationFailure}");
    }

    private void ConnectSocket(ulong steamId)
    {
        try
        {
            var result = _steamMultiplayerPeer.ConnectToLobby(checked((long)steamId));
            TraceCapabilities("client", result);
            if (result != Error.Ok) { FailTransport($"CreateClient: {result}"); return; }
            _ownedPeer = _steamMultiplayerPeer;
            Multiplayer.MultiplayerPeer = _ownedPeer;
        }
        catch (Exception e) { FailTransport($"Error creating client: {e.Message}"); }
    }

    private void OnLobbyChatUpdate(long lobbyId, long changedId, long makingChangeId, long chatState)
    {
        if (State == TransportState.Stopped || _currentLobbyId != (ulong)lobbyId) return;
        if (!Hosting && changedId == checked((long)_pinnedHost) && (chatState & (StateLeft | StateDisconnected | StateKicked | StateBanned)) != 0)
        {
            FailTransport("Original Steam host left the lobby.");
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
        return State == TransportState.Ready;
    }

    public MultiplayerPeer.ConnectionStatus GetConnectionStatus()
    {
        return _steamMultiplayerPeer?.GetConnectionStatus() ?? MultiplayerPeer.ConnectionStatus.Disconnected;
    }


    protected override void ReleaseResources()
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(ReleaseResources)} cleanup started.");

        if (Hosting) SetLobbyUnjoinable();
        if (_ownedPeer != null && Multiplayer.MultiplayerPeer == _ownedPeer)
            Multiplayer.MultiplayerPeer = null;
        _ownedPeer = null;
        if (_currentLobbyId.HasValue) SteamworksHelper.Api.LeaveLobby(checked((long)_currentLobbyId.Value));
        SteamworksHelper.Instance?.CancelLobbyOperations(LobbyResult);

        _steamMultiplayerPeer?.Close();
        _steamMultiplayerPeer?.Dispose();
        _steamMultiplayerPeer = null;

        _currentLobbyId = null;
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(ReleaseResources)} cleanup complete.");
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
