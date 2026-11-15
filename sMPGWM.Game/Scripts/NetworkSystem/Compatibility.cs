using System;
using System.Collections.Generic;
using System.Text.Json;
using sMPGWM.Scripts.GameConfigSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class Compatibility
{
    private static readonly string[] Fields = ["handshake", "protocol", "game_id", "mode", "version", "development", "config", "data", "build"];
    internal static Dictionary<string, string> Capture()
    {
        if (string.IsNullOrEmpty(GameConfig.GameDataChecksum))
            throw new InvalidOperationException("Session definitions and compatibility data have not been prepared.");
        return new()
        {
            ["handshake"] = "3",
            ["protocol"] = "15",
            ["game_id"] = GameConfig.LobbyGameId,
            ["mode"] = GameConfig.LobbyMode,
            ["version"] = GameConfig.Version,
            ["development"] = GameConfig.Development ? "true" : "false",
            ["config"] = GameConfig.ConfigDataChecksum,
            ["data"] = GameConfig.GameDataChecksum,
            ["build"] = typeof(Compatibility).Module.ModuleVersionId.ToString("N")
        };
    }

    internal static byte[] Payload()
    {
        var captured = Capture();
        var values = new string[Fields.Length];
        for (var i = 0; i < Fields.Length; i++) values[i] = captured[Fields[i]];
        return JsonSerializer.SerializeToUtf8Bytes(values);
    }

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
            var values = JsonSerializer.Deserialize<string[]>(data);
            if (values == null || values.Length != Fields.Length) return false;
            for (var i = 0; i < values.Length; i++)
                if (string.IsNullOrEmpty(values[i]) || values[i].Length > 1024) return false;
            return Matches(key => values[Array.IndexOf(Fields, key)]);
        }
        catch (JsonException) { return false; }
    }
}
