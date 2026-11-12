using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.SteamSystem;

// Discovery needs no prior knowledge of a host, session or content fingerprint.
internal sealed class LobbyDiscovery(string gameId, string mode, string version)
{
    internal IReadOnlyDictionary<string, string> Filters { get; } = new Dictionary<string, string>
    {
        ["game_id"] = gameId,
        ["mode"] = mode,
        ["version"] = version
    };

    internal bool Matches(Func<string, string> metadata)
    {
        foreach (var filter in Filters)
            if (metadata(filter.Key) != filter.Value) return false;
        return true;
    }
}
