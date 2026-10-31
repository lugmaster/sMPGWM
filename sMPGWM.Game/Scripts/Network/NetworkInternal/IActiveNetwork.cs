using System;
using Godot;
using static Godot.MultiplayerPeer;

namespace sMPGWM.scripts.Network.NetworkInternal;

public interface IActiveNetwork
{
    event Action<string>? TransportReady;
    event Action<string>? StartupFailed;
    event Action<string>? ConnectionLost;
    void BecomeHost(int maxConnections);
    void BecomeClient(ulong lobbyId);
    bool IsConnected();
    ConnectionStatus GetConnectionStatus();
    Node GetNode();
    void Cleanup();
}