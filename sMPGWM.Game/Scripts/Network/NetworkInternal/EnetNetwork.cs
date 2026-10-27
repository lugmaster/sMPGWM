using System;
using System.Threading.Tasks;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.Network.NetworkInternal;
using static Godot.MultiplayerPeer;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class EnetNetwork : BaseNetwork, IActiveNetwork
{
    private ENetMultiplayerPeer _peer;

    public override void _Ready()
    {
        Logger.Debug(
            $"{nameof(EnetNetwork)}.{nameof(_Ready)} setting up ENet network.");
        _peer = new ENetMultiplayerPeer();
    }

    public async void BecomeHost(int maxConnections)
    {
        try
        {
            Logger.Debug(
                $"{nameof(EnetNetwork)}.{nameof(BecomeHost)} becoming host.");
            var result = _peer.CreateServer(GameConfig.Port, maxConnections);

            if (result != Error.Ok)
            {
                EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork) - Failed to start the server: {result}");
                return;
            }

            Logger.Debug(
                $"{nameof(EnetNetwork)}.{nameof(BecomeHost)} server started with {nameof(GameConfig.Port)}={GameConfig.Port}, {nameof(maxConnections)}={maxConnections}.");
            Multiplayer.MultiplayerPeer = _peer;

            var success = await WaitForConnectionStatus(ConnectionStatus.Connecting);
            if (!success)
            {
                EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork): Timeout while waiting to create Server. Status: {_peer.GetConnectionStatus()}");
                return;
            }

            Logger.Debug(
                $"{nameof(EnetNetwork)}.{nameof(BecomeHost)} server successfully started.");
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
            Logger.Debug(
                $"{nameof(EnetNetwork)}.{nameof(BecomeClient)} attempting to become client.");

            _peer.CreateClient(GameConfig.ServerIpAddress, GameConfig.Port);
            Multiplayer.MultiplayerPeer = _peer;

            var successful = await WaitForConnectionStatus(ConnectionStatus.Connecting);
            if (!successful)
            {
                EmitConnectedFalseAndQuit(
                    $"EnetNetwork(IActiveNetwork) - Timeout while waiting for connection. Status: {_peer.GetConnectionStatus()}");
                return;
            }

            Logger.Debug(
                $"{nameof(EnetNetwork)}.{nameof(BecomeClient)} client successfully connected with peerId={Multiplayer.GetUniqueId()}.");
            EmitConnected(true);
        }
        catch (Exception e)
        {
            EmitConnectedFalseAndQuit($"EnetNetwork(IActiveNetwork): Failed becoming client: {e.Message}");
        }
    }

    public Node GetNode()
    {
        Logger.Debug(
            $"{nameof(EnetNetwork)}.{nameof(GetNode)} returning network node.");
        return this;
    }

    private async Task<bool> WaitForConnectionStatus(ConnectionStatus status)
    {
        Logger.Debug(
            $"{nameof(EnetNetwork)}.{nameof(WaitForConnectionStatus)} waiting for connection to leave {nameof(status)}={status}.");
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
        Logger.Debug(
            $"{nameof(EnetNetwork)}.{nameof(WaitForConnectionStatus)} completed with {nameof(connected)}={connected}.");
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
        Logger.Debug(
            $"{nameof(EnetNetwork)}.{nameof(Cleanup)} cleaning up ENet peer.");

        if (_peer != null && GetConnectionStatus() != ConnectionStatus.Disconnected)
        {
            Logger.Debug(
                $"{nameof(EnetNetwork)}.{nameof(Cleanup)} closing ENet peer.");
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