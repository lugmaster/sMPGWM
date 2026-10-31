using sMPGWM.scripts.Network;
namespace sMPGWM.Scripts.Network;

public sealed record NetworkStartupData(NetworkRole Role, NetworkTarget Target,
    int MaxPlayers, int ConnectionTimeoutMs, bool Diagnostic = false)
{
    public NetworkType Type => Target switch
    {
        LanEndpoint => NetworkType.Lan,
        SteamLobbyTarget => NetworkType.Steam,
        _ => throw new System.ArgumentException("Unknown network target.")
    };
    public LanEndpoint? LanTarget => Target as LanEndpoint;
    public SteamLobbyTarget? SteamTarget => Target as SteamLobbyTarget;
}
