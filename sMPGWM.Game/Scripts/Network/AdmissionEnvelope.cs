using System;

namespace sMPGWM.Scripts.Network;

public sealed record AdmissionEnvelope(int Protocol, string Session, long WorldEpoch, long ContentRevision, string Token)
{
    public const int MaximumPayloadBytes = 4096;

    public bool Matches(AdmissionEnvelope expected, int payloadBytes) =>
        payloadBytes >= 0 && payloadBytes <= MaximumPayloadBytes &&
        Protocol == DiagnosticExchange.ProtocolVersion && Protocol == expected.Protocol &&
        Guid.TryParseExact(Session, "N", out _) && Session == expected.Session &&
        WorldEpoch > 0 && WorldEpoch == expected.WorldEpoch &&
        ContentRevision > 0 && ContentRevision == expected.ContentRevision &&
        Guid.TryParseExact(Token, "N", out _) && Token == expected.Token;
}
