using sMPGWM.scripts.Network;
namespace sMPGWM.Scripts.Network;

public sealed record NetworkStartupData(NetworkType Type, NetworkRole Role, string Address,
    int Port, int MaxPlayers, int ConnectionTimeoutMs, bool Diagnostic = false, ulong LobbyId = 0);
