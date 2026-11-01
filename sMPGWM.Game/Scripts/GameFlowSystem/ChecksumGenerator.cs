using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sMPGWM.Scripts.GameFlowSystem;

public static class ChecksumGenerator
{
    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(text.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace("\r", "\n")))).ToLowerInvariant();

    public static string Compute(IReadOnlyDictionary<string, string> inputs) =>
        Hash(string.Concat(inputs.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key.Length}:{x.Key}:{Hash(x.Value)}\n")));
}
