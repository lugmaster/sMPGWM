using System;
using System.Net;
using Godot;
using sMPGWM.Scripts.Network.NetworkInternal;
using static Godot.MultiplayerPeer;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class EnetNetwork : BaseNetwork, IActiveNetwork
{
    private ENetMultiplayerPeer? _peer;
    protected override string TimeoutReason => "ENet connection failed or disconnected.";

    public void BecomeHost(int maxConnections)
    {
        if (!Begin()) return;
        if (maxConnections is < 1 or > 10)
        {
            FailTransport("ENet diagnostic hosting requires 1-10 player capacity.");
            return;
        }
        if (Startup.LanTarget?.Port is not (>= 1 and <= 65535))
        {
            FailTransport("Invalid LAN endpoint.");
            return;
        }
        _peer = new ENetMultiplayerPeer();
        var result = _peer.CreateServer(Startup.LanTarget.Port, Math.Max(1, maxConnections - 1));
        if (result != Error.Ok) { FailTransport($"CreateServer: {result}"); return; }
        // Authentication enforces capacity, including host-only sessions.
        Multiplayer.MultiplayerPeer = _peer;
        ReadyTransport("Listening");
    }

    public void BecomeClient(ulong lobbyId = 0)
    {
        if (!Begin()) return;
        var target = Startup.LanTarget;
        if (target == null || !IPAddress.TryParse(target.Address, out _) || target.Port is < 1 or > 65535)
        {
            FailTransport("Invalid LAN endpoint.");
            return;
        }
        Subscribe(() => Multiplayer.ConnectedToServer += Connected, () => Multiplayer.ConnectedToServer -= Connected);
        Subscribe(() => Multiplayer.ConnectionFailed += Failed, () => Multiplayer.ConnectionFailed -= Failed);
        Subscribe(() => Multiplayer.ServerDisconnected += Disconnected, () => Multiplayer.ServerDisconnected -= Disconnected);
        _peer = new ENetMultiplayerPeer();
        var result = _peer.CreateClient(target.Address, target.Port);
        if (result != Error.Ok) { FailTransport($"CreateClient: {result}"); return; }
        Multiplayer.MultiplayerPeer = _peer;
    }

    private void Connected() => ReadyTransport();
    private void Failed() => FailTransport(TimeoutReason);
    private void Disconnected() => FailTransport("Server disconnected.");
    public bool IsConnected() => State == TransportState.Ready;
    public ConnectionStatus GetConnectionStatus() => _peer?.GetConnectionStatus() ?? ConnectionStatus.Disconnected;
    public Node GetNode() => this;

    protected override void ReleaseResources()
    {
        if (_peer == null) return;
        if (Multiplayer.MultiplayerPeer == _peer) Multiplayer.MultiplayerPeer = null;
        _peer.Close();
        _peer.Dispose();
        _peer = null;
    }
}
