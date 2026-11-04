using System;
using System.IO;

namespace sMPGWM.Scripts.NetworkSystem;

/// <summary>System-independent identity and completed host tick framing. Payload codecs own their records.</summary>
public static class SnapshotEnvelope
{
    public const long MaxTick = long.MaxValue - 1024;
    public static void Write(BinaryWriter writer, Guid session, long tick)
    {
        if (session == Guid.Empty || tick < 0 || tick > MaxTick) throw new ArgumentException("Invalid snapshot identity.");
        writer.Write(session.ToByteArray()); writer.Write(tick);
    }
    public static bool Read(BinaryReader reader, out Guid session, out long tick)
    {
        session = new Guid(reader.ReadBytes(16)); tick = reader.ReadInt64();
        return session != Guid.Empty && tick >= 0 && tick <= MaxTick;
    }
}
