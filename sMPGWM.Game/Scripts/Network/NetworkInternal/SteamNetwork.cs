using System;
using Godot;
using GodotSteam;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Steam;
using sMPGWM.Scripts.Network.NetworkInternal;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class SteamNetwork : BaseNetwork, IActiveNetwork
{
    private SteamMultiplayerPeerWrapper _steamMultiplayerPeerWrapper;
    private ulong? _currentLobbyId;
    private bool _cleaned;
    private bool _started;
    private bool _hosting;
    private bool _completed;
    private ulong _targetLobby;
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
            _steamMultiplayerPeerWrapper = new SteamMultiplayerPeerWrapper();

            Multiplayer.ConnectedToServer += Connected;
            Multiplayer.ConnectionFailed += Failed;
            Multiplayer.ServerDisconnected += Disconnected;
            Steam.LobbyChatUpdate += OnLobbyChatUpdate;
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
            Steam.SetLobbyJoinable(lobbyId, false);

            Steam.SetLobbyData(lobbyId, "name", GameConfig.LobbyName + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString("X"));
            Steam.SetLobbyData(lobbyId, "game_id", GameConfig.LobbyGameId);
            Steam.SetLobbyData(lobbyId, "mode", GameConfig.LobbyMode);
            Steam.SetLobbyData(lobbyId, "version", GameConfig.Version);

            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} creating host with lobby name={Steam.GetLobbyData(lobbyId, "name")}.");

            var error = _steamMultiplayerPeerWrapper.CreateHost(0);
            if (error == Error.Ok)
            {
                _ownedPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();
                Multiplayer.MultiplayerPeer = _ownedPeer;
                Logger.Debug(
                    $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} created host from lobby.");
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
            var lobbyId = Steam.GetLobbyOwner(lobby);

            // Host joins lobby itself compare with SteamId
            if (lobbyId == Steam.GetSteamID())
            {
                Report(false, "Cannot join a self-hosted lobby.");
                return;
            }
            else
            {
                // Client joins lobby - connect socket to host
                ConnectSocket(lobbyId);
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

    private void ConnectSocket(ulong steamId)
    {
        try
        {
            var result = _steamMultiplayerPeerWrapper.CreateClient(steamId, 0);
            if (result != Error.Ok) { Report(false, $"CreateClient: {result}"); return; }
            _ownedPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();
            Multiplayer.MultiplayerPeer = _ownedPeer;
        }
        catch (Exception e) { Report(false, $"Error creating client: {e.Message}"); }
    }

    private void OnLobbyChatUpdate(ulong lobbyId, long changedId, long makingChangeId, long chatState)
    {
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
        return _steamMultiplayerPeerWrapper != null &&
               _steamMultiplayerPeerWrapper.GetConnectionStatus() == (int)MultiplayerPeer.ConnectionStatus.Connected;
    }

    public MultiplayerPeer.ConnectionStatus GetConnectionStatus()
    {
        return _steamMultiplayerPeerWrapper != null
            ? (MultiplayerPeer.ConnectionStatus)_steamMultiplayerPeerWrapper.GetConnectionStatus()
            : MultiplayerPeer.ConnectionStatus.Disconnected;
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
        if (_currentLobbyId.HasValue) Steam.LeaveLobby(_currentLobbyId.Value);
        SteamworksHelper.Instance?.CancelLobbyOperations(LobbyResult);
        Steam.LobbyChatUpdate -= OnLobbyChatUpdate;

        _steamMultiplayerPeerWrapper?.Cleanup();
        _steamMultiplayerPeerWrapper = null;

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

            if (!Steam.IsSteamRunning())
            {
                Logger.Debug(
                    $"{nameof(SteamNetwork)}.{nameof(SetLobbyUnjoinable)} Steam is not running, cannot set lobby unjoinable.");
                return;
            }

            Steam.SetLobbyJoinable(_currentLobbyId.Value, false);
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
