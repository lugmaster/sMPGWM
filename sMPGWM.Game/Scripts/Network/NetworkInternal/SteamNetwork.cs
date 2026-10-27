using System;
using System.Threading.Tasks;
using Godot;
using GodotSteam;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Network.NetworkInternal;
using static Godot.MultiplayerPeer;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class SteamNetwork : BaseNetwork, IActiveNetwork
{
    private SteamMultiplayerPeerWrapper _steamMultiplayerPeerWrapper;
    private ulong? _currentLobbyId;
    
    private const int StateEntered      = 0x01;
    private const int StateLeft         = 0x02;
    private const int StateDisconnected = 0x04;
    private const int StateKicked       = 0x08;
    private const int StateBanned       = 0x10;

    public override void _Ready()
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(_Ready)} Steam network ready.");
        _steamMultiplayerPeerWrapper = new SteamMultiplayerPeerWrapper();

        Steam.LobbyJoined += OnLobbyJoined;
        Steam.LobbyCreated += OnLobbyCreated;
        Steam.LobbyChatUpdate += OnLobbyChatUpdate;
        GameFlowManager.Instance.OnSuddenExit += SetLobbyUnjoinable;

        if (_steamMultiplayerPeerWrapper == null)
        {
            EmitConnectedFalseAndQuit("SteamNetwork(IActiveNetwork): Failed to load SteamMultiplayerPeer GDExtension!");
            return;
        }

        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(_Ready)} SteamMultiplayerPeer GDExtension loaded successfully.");
    }

    public void BecomeHost(int maxConnections)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(BecomeHost)} becoming host.");
        Steam.CreateLobby(Steam.LobbyType.Public, maxConnections);
    }

    public void BecomeClient(ulong lobbyId)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(BecomeClient)} becoming client.");
        Steam.JoinLobby(lobbyId);
    }

    public Node GetNode()
    {
        return this;
    }

    private void OnLobbyCreated(long connect, ulong lobbyId)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} received lobby creation result.");
        _currentLobbyId = lobbyId;
        if (connect == 1)
        {
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} created lobby with {nameof(lobbyId)}={lobbyId}.");
            Steam.SetLobbyJoinable(lobbyId, true);

            Steam.SetLobbyData(lobbyId, "name", GameConfig.LobbyName + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString("X"));
            Steam.SetLobbyData(lobbyId, "game_id", GameConfig.LobbyGameId);
            Steam.SetLobbyData(lobbyId, "mode", GameConfig.LobbyMode);
            Steam.SetLobbyData(lobbyId, "version", GameConfig.Version);

            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} creating host with lobby name={Steam.GetLobbyData(lobbyId, "name")}.");

            var error = _steamMultiplayerPeerWrapper.CreateHost(0);
            if (error == Error.Ok)
            {
                Multiplayer.MultiplayerPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();
                Logger.Debug(
                    $"{nameof(SteamNetwork)}.{nameof(OnLobbyCreated)} created host from lobby.");
            }
            else
            {
                EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork): Error creating host: {error}");
            }
        }
        else
        {
            EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork): Failed to create lobby. Error: {connect}");
        }
    }

    private void OnLobbyJoined(ulong lobby, long permissions, bool locked, long response)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(OnLobbyJoined)} received lobby join result.");
        if (response == 1)
        {
            var lobbyId = Steam.GetLobbyOwner(lobby);

            // Host joins lobby itself compare with SteamId
            if (lobbyId == Steam.GetSteamID())
            {
                switch (GameFlowManager.Instance.NetworkRole)
                {
                    case NetworkRole.Host:
                        Logger.Debug(
                            $"{nameof(SteamNetwork)}.{nameof(OnLobbyJoined)} successfully joined lobby as host.");
                        EmitConnected(true);
                        return;
                    case NetworkRole.Client:
                        EmitConnectedFalseAndQuit(
                            "SteamNetwork(IActiveNetwork): Trying to join self-hosted lobby as client");
                        return;
                    default:
                        EmitConnectedFalseAndQuit(
                            "SteamNetwork(IActiveNetwork): Unknown MultiplayerRole tried to join lobby");
                        return;
                }
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

            EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork) failed: {failReason}");
        }
    }

    private async void ConnectSocket(ulong steamId)
    {
        try
        {
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(ConnectSocket)} connecting to socket.");
            var result = _steamMultiplayerPeerWrapper.CreateClient(steamId, 0);
            if (result != Error.Ok)
            {
                EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork) - Failed to Connect to Socket: {result}");
                return;
            }

            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(ConnectSocket)} connecting peer to host.");
            Multiplayer.MultiplayerPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(ConnectSocket)} assigned peerId={Multiplayer.GetUniqueId()} to steamId={Steam.GetSteamID()}.");
            var successful = await WaitForConnectionStatus(ConnectionStatus.Connecting);
            if (!successful)
            {
                EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork) - Timeout while waiting for connection. Status: {_steamMultiplayerPeerWrapper.GetConnectionStatus()}");
                return;
            }
            
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(ConnectSocket)} successfully connected to socket.");
            EmitConnected(true);
        }
        catch (Exception e)
        {
            EmitConnectedFalseAndQuit($"Error creating client: {e.Message}");
        }
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

    private async Task<bool> WaitForConnectionStatus(ConnectionStatus status)
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(WaitForConnectionStatus)} waiting for connection to leave {nameof(status)}={status}.");
        var elapsed = 0;

        while (_steamMultiplayerPeerWrapper.GetConnectionStatus() == (int)status)
        {
            if (elapsed >= ConnectionTimeout)
            {
                return false;
            }

            await Task.Delay(100);
            elapsed += 100;
        }

        var connected = _steamMultiplayerPeerWrapper.GetConnectionStatus() == (int)ConnectionStatus.Connected;
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(WaitForConnectionStatus)} completed with {nameof(connected)}={connected}.");
        return connected;
    }

    public bool IsConnected()
    {
        return _steamMultiplayerPeerWrapper != null &&
               _steamMultiplayerPeerWrapper.GetConnectionStatus() == (int)ConnectionStatus.Connected;
    }

    public ConnectionStatus GetConnectionStatus()
    {
        return _steamMultiplayerPeerWrapper != null
            ? (ConnectionStatus)_steamMultiplayerPeerWrapper.GetConnectionStatus()
            : ConnectionStatus.Disconnected;
    }


    public override void Cleanup()
    {
        Logger.Debug(
            $"{nameof(SteamNetwork)}.{nameof(Cleanup)} cleanup started.");

        Steam.LobbyJoined -= OnLobbyJoined;
        Steam.LobbyCreated -= OnLobbyCreated;
        Steam.LobbyChatUpdate -= OnLobbyChatUpdate;

        _steamMultiplayerPeerWrapper?.Cleanup();
        _steamMultiplayerPeerWrapper = null;

        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer = null;
            Logger.Debug(
                $"{nameof(SteamNetwork)}.{nameof(Cleanup)} cleared {nameof(Multiplayer.MultiplayerPeer)}.");
        }

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
        finally
        {
            _currentLobbyId = null;
        }
    }

}