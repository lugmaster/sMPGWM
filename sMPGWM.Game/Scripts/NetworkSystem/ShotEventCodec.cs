using System;
using System.Buffers.Binary;
using Godot;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public static class ShotEventCodec
{
    public const int MaxBytes = 1200;
    public const int HeaderBytes = 17;
    public const int EventBytes = 66;
    public const int MaxEvents = (MaxBytes - HeaderBytes) / EventBytes;

    public static byte[] Encode(ReadOnlySpan<AcceptedShot> shots)
    {
        if (shots.Length is < 1 or > MaxEvents) throw new ArgumentException("Invalid shot batch size.");
        var bytes = new byte[HeaderBytes + shots.Length * EventBytes];
        EncodeInto(shots, bytes);
        return bytes;
    }

    public static void EncodeInto(ReadOnlySpan<AcceptedShot> shots, Span<byte> bytes)
    {
        if (shots.Length is < 1 or > MaxEvents || bytes.Length != HeaderBytes + shots.Length * EventBytes)
            throw new ArgumentException("Invalid shot batch size.");
        var session = shots[0].Session;
        session.TryWriteBytes(bytes);
        bytes[16] = (byte)shots.Length;
        var offset = HeaderBytes;
        foreach (var shot in shots)
        {
            if (shot.Session != session || !IsValid(shot)) throw new ArgumentException("Invalid shot event.");
            WriteLong(bytes, ref offset, shot.Tick);
            WriteLong(bytes, ref offset, checked((long)shot.Item));
            BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..], shot.Entity); offset += 4;
            WriteLong(bytes, ref offset, shot.Generation);
            WriteLong(bytes, ref offset, shot.Sequence);
            bytes[offset++] = (byte)shot.Profile;
            BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..], shot.Muzzle); offset += 4;
            WriteVector(bytes, ref offset, shot.Origin);
            WriteVector(bytes, ref offset, shot.Endpoint);
            WriteVector(bytes, ref offset, shot.Normal);
            bytes[offset++] = (byte)((shot.Hit ? 1 : 0) | (shot.ShieldContact ? 2 : 0));
        }
    }

    public static bool TryDecode(ReadOnlySpan<byte> bytes, Span<AcceptedShot> destination, out int count)
    {
        count = 0;
        if (bytes.Length < HeaderBytes || bytes.Length > MaxBytes) return false;
        var session = new Guid(bytes[..16]);
        var length = bytes[16];
        if (session == Guid.Empty || length is < 1 or > MaxEvents || length > destination.Length ||
            bytes.Length != HeaderBytes + length * EventBytes) return false;
        var offset = HeaderBytes;
        for (var i = 0; i < length; i++)
        {
            var tick = ReadLong(bytes, ref offset);
            var item = ReadLong(bytes, ref offset);
            var entity = BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]); offset += 4;
            var generation = ReadLong(bytes, ref offset);
            var sequence = ReadLong(bytes, ref offset);
            var profile = bytes[offset++];
            var muzzle = BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]); offset += 4;
            var origin = ReadVector(bytes, ref offset);
            var endpoint = ReadVector(bytes, ref offset);
            var normal = ReadVector(bytes, ref offset);
            var flags = bytes[offset++];
            var shot = new AcceptedShot(session, tick, unchecked((ulong)item), entity, generation, sequence,
                profile, muzzle, origin, endpoint, normal, (flags & 1) != 0, (flags & 2) != 0);
            if (flags > 3 || !IsValid(shot)) return false;
            destination[i] = shot;
        }
        count = length;
        return true;
    }

    public static bool IsValid(in AcceptedShot shot) => shot.Session != Guid.Empty && shot.Tick >= 0 && shot.Tick <= SnapshotEnvelope.MaxTick &&
        shot.Item is > 0 and <= long.MaxValue && shot.Entity > 0 && shot.Generation >= 0 && shot.Generation <= SnapshotEnvelope.MaxTick &&
        shot.Sequence > 0 && shot.Sequence < long.MaxValue && shot.Profile is >= 0 and < 2 && shot.Muzzle >= 0 &&
        shot.Origin.IsFinite() && shot.Endpoint.IsFinite() && shot.Normal.IsFinite() && (!shot.ShieldContact || shot.Hit) &&
        (!shot.Hit || shot.Normal.LengthSquared() is >= .99f and <= 1.01f);

    private static void WriteLong(Span<byte> bytes, ref int offset, long value)
    { BinaryPrimitives.WriteInt64LittleEndian(bytes[offset..], value); offset += 8; }
    private static long ReadLong(ReadOnlySpan<byte> bytes, ref int offset)
    { var value = BinaryPrimitives.ReadInt64LittleEndian(bytes[offset..]); offset += 8; return value; }
    private static void WriteVector(Span<byte> bytes, ref int offset, Vector2 value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value.X);
        BinaryPrimitives.WriteSingleLittleEndian(bytes[(offset + 4)..], value.Y); offset += 8;
    }
    private static Vector2 ReadVector(ReadOnlySpan<byte> bytes, ref int offset)
    {
        var value = new Vector2(BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset + 4)..]));
        offset += 8; return value;
    }
}
