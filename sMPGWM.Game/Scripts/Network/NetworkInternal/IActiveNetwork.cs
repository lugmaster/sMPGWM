using Godot;
using static Godot.MultiplayerPeer;

namespace sMPGWM.scripts.Network.NetworkInternal;

public interface IActiveNetwork
{
    void BecomeHost(int maxConnections);
    void BecomeClient(ulong lobbyId);
    bool IsConnected();
    ConnectionStatus GetConnectionStatus();
    Node GetNode();
    void Cleanup();
}