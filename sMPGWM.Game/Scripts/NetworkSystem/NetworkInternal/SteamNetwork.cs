using System;
using GDExtension.Wrappers;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.Steam;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.NetworkSystem.NetworkInternal;

public partial class SteamNetwork : BaseNetwork
{
    private SteamMultiplayerPeer _steamPeer = null!;
    private ulong _lobbyId;
    private long _hostId;
    private bool Hosting => Startup.Role == NetworkRole.Host;

    protected override void StartTransport()
    {
        _steamPeer = SteamMultiplayerPeer.Instantiate();
        Peer = _steamPeer;
        _steamPeer.ServerRelay = true;
        var api = (SceneMultiplayer)Multiplayer;
        Subscribe(() => api.PeerAuthenticating += VerifyIdentity, () => api.PeerAuthenticating -= VerifyIdentity);
        Subscribe(() => Multiplayer.ConnectedToServer += ReadyTransport, () => Multiplayer.ConnectedToServer -= ReadyTransport);
        Subscribe(() => Multiplayer.ConnectionFailed += Disconnected, () => Multiplayer.ConnectionFailed -= Disconnected);
        Subscribe(() => Multiplayer.ServerDisconnected += Disconnected, () => Multiplayer.ServerDisconnected -= Disconnected);
        Subscribe(() => SteamworksHelper.Api.LobbyChatUpdateSignal += OnLobbyChatUpdate, () => SteamworksHelper.Api.LobbyChatUpdateSignal -= OnLobbyChatUpdate);
        Subscribe(() => SteamworksHelper.Api.LobbyDataUpdateSignal += OnLobbyDataUpdate, () => SteamworksHelper.Api.LobbyDataUpdateSignal -= OnLobbyDataUpdate);
        if (!Hosting && Startup.LobbyId == 0) { FailTransport("Invalid Steam lobby target."); return; }
        var submitted = Hosting
            ? SteamworksHelper.Instance.CreateSessionLobby(Startup.MaxPlayers, LobbyResult)
            : SteamworksHelper.Instance.JoinSessionLobby(Startup.LobbyId, LobbyResult);
        if (!submitted) FailTransport("Previous Steam lobby operation is still draining. Try again shortly.");
    }

    private void Disconnected() => FailTransport("Steam server disconnected or connection failed.");

    private void LobbyResult(long result, ulong lobby)
    {
        if (!Starting || (!Hosting && lobby != Startup.LobbyId)) return;
        if (result != 1) { FailTransport($"Steam lobby operation failed: {result}"); return; }
        _lobbyId = lobby;
        try
        {
            Error error;
            if (Hosting)
            {
                SetJoinable(false);
                SetData("name", GameConfig.LobbyName);
                foreach (var field in Compatibility.Capture()) SetData(field.Key, field.Value);
                SetData("state", "preparing");
                error = _steamPeer.HostWithLobby(checked((long)lobby));
            }
            else
            {
                _hostId = SteamworksHelper.Api.GetLobbyOwner(checked((long)lobby));
                if (_hostId == checked((long)SteamworksHelper.Api.GetSteamId()) || !ValidLobby())
                {
                    FailTransport("Steam lobby is self-hosted, unavailable, or incompatible.");
                    return;
                }
                error = _steamPeer.ConnectToLobby(checked((long)lobby));
            }
            if (error != Error.Ok) { FailTransport($"Steam peer startup failed: {error}"); return; }
            Multiplayer.MultiplayerPeer = Peer;
            if (Hosting)
            {
                SetData("state", "ready");
                SetJoinable(true);
                Logger.Info($"Steam lobby ready: {lobby}");
                ReadyTransport();
            }
        }
        catch (Exception e) { FailTransport($"Steam lobby startup failed: {e.Message}"); }
    }

    private void SetData(string key, string value)
    {
        if (!SteamworksHelper.Api.SetLobbyData(checked((long)_lobbyId), key, value))
            throw new InvalidOperationException($"Cannot set Steam lobby {key}.");
    }

    private void SetJoinable(bool joinable)
    {
        if (!SteamworksHelper.Api.SetLobbyJoinable(checked((long)_lobbyId), joinable))
            throw new InvalidOperationException("Cannot change Steam lobby joinability.");
    }

    private bool ValidLobby() => _hostId != 0 &&
        SteamworksHelper.Api.GetLobbyOwner(checked((long)_lobbyId)) == _hostId &&
        SteamworksHelper.Api.GetLobbyData(checked((long)_lobbyId), "state") == "ready" &&
        Compatibility.Matches(key => SteamworksHelper.Api.GetLobbyData(checked((long)_lobbyId), key));

    private void VerifyIdentity(long peer)
    {
        if (Stopped) return;
        try
        {
            var steamId = _steamPeer.GetSteamIdForPeerId(peer);
            var member = false;
            for (var i = 0; i < SteamworksHelper.Api.GetNumLobbyMembers(checked((long)_lobbyId)); i++)
                if (SteamworksHelper.Api.GetLobbyMemberByIndex(checked((long)_lobbyId), i) == steamId) member = true;
            if (steamId > 0 && member && _steamPeer.GetPeerIdForSteamId(steamId) == peer &&
                (Hosting || (peer == 1 && steamId == _hostId))) return;
        }
        catch (Exception e) { Logger.Warning($"Steam peer identity failed: {e.Message}"); }
        ((SceneMultiplayer)Multiplayer).DisconnectPeer(checked((int)peer));
    }

    private void OnLobbyDataUpdate(long success, long lobby, long member)
    {
        if (Stopped || Hosting || _lobbyId != (ulong)lobby || member != lobby) return;
        if (success != 1 || !ValidLobby()) FailTransport("Steam lobby changed or became unavailable.");
    }

    private void OnLobbyChatUpdate(long lobby, long changedId, long makingChangeId, long state)
    {
        // Left, disconnected, kicked, or banned. Host migration is not supported.
        if (!Stopped && !Hosting && _lobbyId == (ulong)lobby && changedId == _hostId && (state & 0x1e) != 0)
            FailTransport("Steam host left the lobby.");
    }

    protected override void ReleaseResources()
    {
        SteamworksHelper.Instance?.CancelLobbyOperations(LobbyResult);
        if (_lobbyId == 0) return;
        try
        {
            if (Hosting) SetJoinable(false);
        }
        catch (Exception e) { Logger.Warning($"Cannot close Steam lobby: {e.Message}"); }
        finally
        {
            SteamworksHelper.Api.LeaveLobby(checked((long)_lobbyId));
            _lobbyId = 0;
        }
    }
}
