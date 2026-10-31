using sMPGWM.scripts.Network;
namespace sMPGWM.Scripts.Network;

public sealed record NetworkStartupData(NetworkType Type, NetworkRole Role, string Address,
    int Port, int MaxPlayers, int ConnectionTimeoutMs, bool Diagnostic = false, ulong LobbyId = 0)
{
    public LanEndpoint? LanTarget { get; } = Type == NetworkType.Lan ? new(Address, Port) : null;
    public SteamLobbyTarget? SteamTarget { get; } = Type == NetworkType.Steam ? new(LobbyId) : null;
}
