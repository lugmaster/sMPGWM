using System;
using System.Collections.Generic;
using System.Text.Json;
using sMPGWM.Scripts.GameConfigSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class Compatibility
{
    internal static Dictionary<string, string> Capture()
    {
        GameConfig.EnsureCompatibilityData();
        return new()
        {
            ["handshake"] = "2",
            ["protocol"] = "5",
            ["game_id"] = GameConfig.LobbyGameId,
            ["mode"] = GameConfig.LobbyMode,
            ["version"] = GameConfig.Version,
            ["development"] = GameConfig.Development ? "true" : "false",
            ["config"] = GameConfig.ConfigDataChecksum,
            ["data"] = GameConfig.GameDataChecksum,
            ["build"] = typeof(Compatibility).Module.ModuleVersionId.ToString("N")
        };
    }

    internal static byte[] Payload() => JsonSerializer.SerializeToUtf8Bytes(Capture());

    internal static bool Matches(Func<string, string> remote)
        => Matches(Capture(), remote, message => Logger.Info(message));

    internal static bool Matches(Dictionary<string, string> local, Func<string, string> remote, Action<string>? log = null)
    {
        foreach (var field in local)
        {
            var actual = remote(field.Key);
            if (string.IsNullOrEmpty(actual) || actual.Length > 1024) return false;
            if (field.Key == "build" && local["development"] == "true")
            {
                if (!Guid.TryParseExact(actual, "N", out _)) return false;
                log?.Invoke($"Compatibility assembly equality skipped development=true local={field.Value} remote={actual}");
                continue;
            }
            if (actual != field.Value)
            {
                log?.Invoke($"Compatibility {field.Key} mismatch.");
                return false;
            }
        }
        return true;
    }

    internal static bool Accept(byte[] data)
    {
        if (data.Length == 0 || data.Length > 8192) return false;
        try
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
            return fields != null && fields.Count == 9 && Matches(key => fields.GetValueOrDefault(key, ""));
        }
        catch (JsonException) { return false; }
    }
}
