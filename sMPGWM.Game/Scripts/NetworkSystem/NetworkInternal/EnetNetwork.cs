using System;
using System.Net;
using Godot;

namespace sMPGWM.Scripts.NetworkSystem.NetworkInternal;

public partial class EnetNetwork : BaseNetwork
{
    protected override void StartTransport()
    {
        var hosting = Startup.Role == NetworkRole.Host;
        if (Startup.Port is < 1 or > 65535 || (!hosting && !IPAddress.TryParse(Startup.Address, out _)))
        {
            FailTransport("Invalid LAN endpoint.");
            return;
        }
        Subscribe(() => Multiplayer.ConnectedToServer += ReadyTransport, () => Multiplayer.ConnectedToServer -= ReadyTransport);
        Subscribe(() => Multiplayer.ConnectionFailed += Disconnected, () => Multiplayer.ConnectionFailed -= Disconnected);
        Subscribe(() => Multiplayer.ServerDisconnected += Disconnected, () => Multiplayer.ServerDisconnected -= Disconnected);
        var peer = new ENetMultiplayerPeer();
        Peer = peer;
        var result = hosting
            ? peer.CreateServer(Startup.Port, Math.Max(1, Startup.MaxPlayers - 1))
            : peer.CreateClient(Startup.Address, Startup.Port);
        if (result != Error.Ok) { FailTransport($"ENet startup failed: {result}"); return; }
        Multiplayer.MultiplayerPeer = peer;
        if (hosting) ReadyTransport();
    }

    private void Disconnected() => FailTransport("Server disconnected or connection failed.");
}
