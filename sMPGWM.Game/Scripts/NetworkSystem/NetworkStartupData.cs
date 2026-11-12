using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public sealed record NetworkStartupData(NetworkRole Role, NetworkType Type,
    int MaxPlayers, int ConnectionTimeoutMs, string Address = "", int Port = 0, ulong LobbyId = 0)
{
    public PlayerStartupData Player { get; init; } = PlayerStartupData.Create();
    public RestoredWorldState? RestoredWorld { get; init; }
}
