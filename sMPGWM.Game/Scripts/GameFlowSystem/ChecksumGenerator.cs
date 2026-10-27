using System.Security.Cryptography;
using System.Text;
using Godot;

namespace sMPGWM.Scripts.GameFlowSystem;

public static class ChecksumGenerator
{
    public static string ComputeCombinedHash(params string[] filePaths)
    {
        var sb = new StringBuilder();

        foreach (var path in filePaths)
        {
            if (!FileAccess.FileExists(path))
            {
                GameFlowManager.Instance.QuitWithOsError($"Cannot compute hash. Missing file: {path}");
                continue;
            }

            var content = FileAccess.GetFileAsString(path);
            var hash = ComputeSha256(content);
            sb.Append(hash);
        }

        return ComputeSha256(sb.ToString());
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return ConvertToHex(hash);
    }

    private static string ConvertToHex(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}