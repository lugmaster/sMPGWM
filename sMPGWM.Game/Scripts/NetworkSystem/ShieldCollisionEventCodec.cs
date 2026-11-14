using System;
using System.Buffers.Binary;

namespace sMPGWM.Scripts.NetworkSystem;

public static class ShieldCollisionEventCodec
{
    public const int MaxBytes = 1200, HeaderBytes = 17, EventBytes = 24, MaxEvents = 10;

    public static byte[] Encode(Guid session, ReadOnlySpan<ShieldCollisionEvent> events)
    {
        if (session == Guid.Empty) throw new ArgumentException("Shield collision session cannot be empty.", nameof(session));
        if (events.Length > MaxEvents) throw new ArgumentOutOfRangeException(nameof(events));
        var bytes = new byte[HeaderBytes + events.Length * EventBytes];
        session.TryWriteBytes(bytes.AsSpan(0, 16));
        bytes[16] = (byte)events.Length;
        var offset = HeaderBytes;
        foreach (var contact in events)
        {
            if (contact.Tick < 0 || contact.Tick > SnapshotEnvelope.MaxTick ||
                !Guid.TryParseExact(contact.EntityId, "N", out var entity) || entity == Guid.Empty)
                throw new ArgumentException("Invalid shield collision event.", nameof(events));
            BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(offset, 8), contact.Tick);
            entity.TryWriteBytes(bytes.AsSpan(offset + 8, 16));
            offset += EventBytes;
        }
        return bytes;
    }

    public static bool Decode(byte[] bytes, out Guid session, out ShieldCollisionEvent[] events)
    {
        session = default;
        events = Array.Empty<ShieldCollisionEvent>();
        if (bytes.Length < HeaderBytes || bytes.Length > MaxBytes) return false;
        var count = bytes[16];
        if (count > MaxEvents || bytes.Length != HeaderBytes + count * EventBytes) return false;
        session = new Guid(bytes.AsSpan(0, 16));
        if (session == Guid.Empty) return false;
        var decoded = new ShieldCollisionEvent[count];
        var offset = HeaderBytes;
        for (var i = 0; i < count; i++)
        {
            var tick = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(offset, 8));
            var entity = new Guid(bytes.AsSpan(offset + 8, 16));
            if (tick < 0 || tick > SnapshotEnvelope.MaxTick || entity == Guid.Empty) return false;
            decoded[i] = new(tick, entity.ToString("N"));
            offset += EventBytes;
        }
        events = decoded;
        return true;
    }
}
