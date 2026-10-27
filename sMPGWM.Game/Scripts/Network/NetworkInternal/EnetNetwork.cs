using System;
using System.Threading.Tasks;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.Network.NetworkInternal;
using static Godot.MultiplayerPeer;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class EnetNetwork : BaseNetwork, IActiveNetwork
{
    private ENetMultiplayerPeer _peer;

    public override void _Ready()
    {
        Logger.Log("Setting up EnetNetwork(IActiveNetwork)", 0);
        _peer = new ENetMultiplayerPeer();
    }

    public async void BecomeHost(int maxConnections)
    {
        try
        {
            Logger.Log("EnetNetwork(IActiveNetwork): Become Host", 0);
            var result = _peer.CreateServer(GameConfig.Port, maxConnections);

            if (result != Error.Ok)
            {
                EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork) - Failed to start the server: {result}");
                return;
            }

            Logger.Log(
                $"EnetNetwork(IActiveNetwork): Server started on port {GameConfig.Port} with a max of {maxConnections} connections.",
                0);
            Multiplayer.MultiplayerPeer = _peer;

            var success = await WaitForConnectionStatus(ConnectionStatus.Connecting);
            if (!success)
            {
                EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork): Timeout while waiting to create Server. Status: {_peer.GetConnectionStatus()}");
                return;
            }

            Logger.Log("EnetNetwork(IActiveNetwork): Server successfully started.", 0);
            EmitConnected(true);
        }
        catch (Exception e)
        {
            EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork): Failed becoming host: {e.Message}");
        }
    }

    public async void BecomeClient(ulong lobbyId = 0)
    {
        try
        {
            Logger.Log("EnetNetwork(IActiveNetwork): Attempting to become client", 0);

            _peer.CreateClient(GameConfig.ServerIpAddress, GameConfig.Port);
            Multiplayer.MultiplayerPeer = _peer;

            var successful = await WaitForConnectionStatus(ConnectionStatus.Connecting);
            if (!successful)
            {
                EmitConnectedFalseAndQuit(
                    $"EnetNetwork(IActiveNetwork) - Timeout while waiting for connection. Status: {_peer.GetConnectionStatus()}");
                return;
            }

            Logger.Log(
                $"EnetNetwork(IActiveNetwork): Client successfully connected with peerId: {Multiplayer.GetUniqueId()}",
                0);
            EmitConnected(true);
        }
        catch (Exception e)
        {
            EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork): Failed becoming client: {e.Message}");
        }
    }

    public Node GetNode()
    {
        Logger.Log("EnetNetwork(IActiveNetwork) GetNode", 0);
        return this;
    }

    private async Task<bool> WaitForConnectionStatus(ConnectionStatus status)
    {
        Logger.Log($"EnetNetwork(IActiveNetwork) WaitForConnectionStatus {status}...", 0);
        var elapsed = 0;

        while (_peer.GetConnectionStatus() == status)
        {
            if (elapsed >= ConnectionTimeout)
            {
                return false;
            }

            await Task.Delay(100);
            elapsed += 100;
        }

        var connected = _peer.GetConnectionStatus() == ConnectionStatus.Connected;
        Logger.Log($"EnetNetwork(IActiveNetwork): SocketStatus = {connected}", 0);
        return connected;
    }

    public bool IsConnected()
    {
        return _peer != null && _peer.GetConnectionStatus() == ConnectionStatus.Connected;
    }

    public ConnectionStatus GetConnectionStatus()
    {
        return _peer?.GetConnectionStatus() ?? ConnectionStatus.Disconnected;
    }

    public override void Cleanup()
    {
        Logger.Log("EnetNetwork(IActiveNetwork): Cleaning up ENet peer", 0);

        if (_peer != null && GetConnectionStatus() != ConnectionStatus.Disconnected)
        {
            Logger.Log("EnetNetwork(IActiveNetwork): Closing ENet peer", 0);
            _peer.Close();
        }

        if (Multiplayer.MultiplayerPeer == _peer)
        {
            Multiplayer.MultiplayerPeer = null;
        }

        _peer = null;

        base.Cleanup();
    }
}