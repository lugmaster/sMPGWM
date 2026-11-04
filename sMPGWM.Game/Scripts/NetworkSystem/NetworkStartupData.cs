using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public sealed record NetworkStartupData(NetworkRole Role, NetworkType Type,
    int MaxPlayers, int ConnectionTimeoutMs, string Address = "", int Port = 0, ulong LobbyId = 0)
{
    public PlayerStartupData Player { get; init; } = PlayerStartupData.Create();
}
