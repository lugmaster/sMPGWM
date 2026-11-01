using sMPGWM.scripts.Network;

namespace sMPGWM.Scripts.Network;

public sealed record NetworkStartupData(NetworkRole Role, NetworkType Type,
    int MaxPlayers, int ConnectionTimeoutMs, string Address = "", int Port = 0, ulong LobbyId = 0);
