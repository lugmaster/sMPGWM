using System;
using System.Threading.Tasks;
using sMPGWM.scripts.Autoloads;
using sMPGWM.scripts.Constants;
using sMPGWM.scripts.Helpers;
using Godot;
using GodotSteam;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using static Godot.MultiplayerPeer;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class SteamNetwork : sMPGWM.Scripts.Network.NetworkInternal.BaseNetwork, IActiveNetwork
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
        Logger.Log("SteamNetwork(IActiveNetwork) ready", 0);
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

        Logger.Log("SteamNetwork(IActiveNetwork): SteamMultiplayerPeer GDExtension loaded successfully.", 0);
    }

    public void BecomeHost(int maxConnections)
    {
        Logger.Log("SteamNetwork(IActiveNetwork): Become Host", 0);
        Steam.CreateLobby(Steam.LobbyType.Public, maxConnections);
    }

    public void BecomeClient(ulong lobbyId)
    {
        Logger.Log("SteamNetwork(IActiveNetwork): Become Client", 0);
        Steam.JoinLobby(lobbyId);
    }

    public Node GetNode()
    {
        return this;
    }

    private void OnLobbyCreated(long connect, ulong lobbyId)
    {
        Logger.Log("SteamNetwork(IActiveNetwork): On Steam lobby created", 0);
        _currentLobbyId = lobbyId;
        if (connect == 1)
        {
            Logger.Log($"SteamNetwork(IActiveNetwork): Created lobby: {lobbyId}", 0);
            Steam.SetLobbyJoinable(lobbyId, true);

            Steam.SetLobbyData(lobbyId, "name", GameConfig.LobbyName + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString("X"));
            Steam.SetLobbyData(lobbyId, "game_id", GameConfig.LobbyGameId);
            Steam.SetLobbyData(lobbyId, "mode", GameConfig.LobbyMode);
            Steam.SetLobbyData(lobbyId, "version", GameConfig.Version);

            Logger.Log(
                $"SteamNetwork(IActiveNetwork): Create Host with Lobby Name: {Steam.GetLobbyData(lobbyId, "name")}", 0);

            var error = _steamMultiplayerPeerWrapper.CreateHost(0);
            if (error == Error.Ok)
            {
                Multiplayer.MultiplayerPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();
                Logger.Log("SteamNetwork(IActiveNetwork): Created Host from Lobby", 0);
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
        Logger.Log("SteamNetwork(IActiveNetwork): On Steam lobby joined", 0);
        if (response == 1)
        {
            var lobbyId = Steam.GetLobbyOwner(lobby);

            // Host joins lobby itself compare with SteamId
            if (lobbyId == Steam.GetSteamID())
            {
                switch (GameFlowManager.Instance.NetworkRole)
                {
                    case NetworkRole.Host:
                        Logger.Log("SteamNetwork(IActiveNetwork): Successfully joined lobby as host", 0);
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
            Logger.Log("SteamNetwork(IActiveNetwork): Connecting to socket...", 0);
            var result = _steamMultiplayerPeerWrapper.CreateClient(steamId, 0);
            if (result != Error.Ok)
            {
                EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork) - Failed to Connect to Socket: {result}");
                return;
            }

            Logger.Log("Connecting peer to host...", 0);
            Multiplayer.MultiplayerPeer = (MultiplayerPeer)_steamMultiplayerPeerWrapper.GetPeer();
            Logger.Log($"Assigned peerId {Multiplayer.GetUniqueId()} to steamUser {Steam.GetSteamID()}", 0);
            var successful = await WaitForConnectionStatus(ConnectionStatus.Connecting);
            if (!successful)
            {
                EmitConnectedFalseAndQuit($"SteamNetwork(IActiveNetwork) - Timeout while waiting for connection. Status: {_steamMultiplayerPeerWrapper.GetConnectionStatus()}");
                return;
            }
            
            Logger.Log("SteamNetwork(IActiveNetwork): Successfully connected to socket", 0);
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
            Logger.Log($"[STEAM] User {changedId} LEFT the lobby {lobbyId}", 1);
        }
        if ((chatState & StateDisconnected) != 0)
        {
            Logger.Log($"[STEAM] User {changedId} DISCONNECTED from lobby {lobbyId}", 1);
        }
        if ((chatState & StateKicked) != 0)
        {
            Logger.Log($"[STEAM] User {changedId} was KICKED from lobby {lobbyId}", 1);
        }
        if ((chatState & StateBanned) != 0)
        {
            Logger.Log($"[STEAM] User {changedId} was BANNED from lobby {lobbyId}", 1);
        }
        if ((chatState & StateEntered) != 0)
        {
            Logger.Log($"[STEAM] User {changedId} ENTERED the lobby {lobbyId}", 1);
        }
    }

    private async Task<bool> WaitForConnectionStatus(ConnectionStatus status)
    {
        Logger.Log($"SteamNetwork(IActiveNetwork) WaitForConnectionStatus {status}...", 0);
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
        Logger.Log($"SteamNetwork(IActiveNetwork): SocketStatus = {connected}", 0);
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
        Logger.Log("SteamNetwork(IActiveNetwork): Cleanup started", 0);

        Steam.LobbyJoined -= OnLobbyJoined;
        Steam.LobbyCreated -= OnLobbyCreated;
        Steam.LobbyChatUpdate -= OnLobbyChatUpdate;

        _steamMultiplayerPeerWrapper?.Cleanup();
        _steamMultiplayerPeerWrapper = null;

        if (Multiplayer.MultiplayerPeer != null)
        {
            Multiplayer.MultiplayerPeer = null;
            Logger.Log("SteamNetwork(IActiveNetwork): Cleared MultiplayerPeer", 0);
        }

        _currentLobbyId = null;
        base.Cleanup();
        Logger.Log("SteamNetwork(IActiveNetwork): Cleanup complete", 0);
    }
    
    private void SetLobbyUnjoinable()
    {
        try
        {
            if (!_currentLobbyId.HasValue)
            {
                Logger.Log("SteamNetwork(IActiveNetwork): No lobby exists, cannot set unjoinable.", 0);
                return;
            }

            if (!Steam.IsSteamRunning())
            {
                Logger.Log("SteamNetwork(IActiveNetwork): Steam is not running, cannot set lobby unjoinable.", 0);
                return;
            }

            Steam.SetLobbyJoinable(_currentLobbyId.Value, false);
            Logger.Log($"SteamNetwork(IActiveNetwork): Lobby {_currentLobbyId.Value} set to unjoinable.", 0);
        }
        catch (Exception ex)
        {
            Logger.Log($"SteamNetwork(IActiveNetwork): Exception in SetLobbyUnjoinable: {ex.Message}", 0);
        }
        finally
        {
            _currentLobbyId = null;
        }
    }

}