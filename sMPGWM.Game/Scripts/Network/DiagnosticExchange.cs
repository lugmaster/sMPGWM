using System;
using System.Text;

namespace sMPGWM.Scripts.Network;

public sealed class DiagnosticExchange
{
    public const int ProtocolVersion = 1;
    public string Correlation { get; } = Guid.NewGuid().ToString("N");
    public string Payload { get; } = "ENet diagnostic hello";
    public bool Succeeded { get; private set; }
    private bool _closed;

    public static bool IsValid(int version, string correlation, string payload) =>
        version == ProtocolVersion && correlation != null && correlation.Length == 32 &&
        Guid.TryParseExact(correlation, "N", out _) && payload != null &&
        Encoding.UTF8.GetByteCount(payload) <= 256;

    public bool Accept(int sender, int version, string correlation, string payload, string session)
    {
        if (_closed || sender != 1 || !IsValid(version, correlation, payload) ||
            correlation != Correlation || payload != Payload || !Guid.TryParseExact(session, "N", out _)) return false;
        Succeeded = true;
        _closed = true;
        return true;
    }

    public void Cancel() => _closed = true;
}
