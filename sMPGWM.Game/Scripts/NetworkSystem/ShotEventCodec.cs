using System;
using System.Buffers.Binary;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.VfxSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public static class ShotEventCodec
{
    public const int MaxBytes = 1200, HeaderBytes = 17, EventBytes = 91;
    public const int MaxEvents = (MaxBytes - HeaderBytes) / EventBytes;
    public static byte[] Encode(Guid session, ReadOnlySpan<AcceptedShot> shots)
    {
        if (shots.Length > MaxEvents) throw new ArgumentOutOfRangeException(nameof(shots));
        var bytes = new byte[HeaderBytes + shots.Length * EventBytes];
        session.TryWriteBytes(bytes);
        bytes[16] = (byte)shots.Length;
        var remaining = bytes.AsSpan(HeaderBytes);
        foreach (var shot in shots)
        {
            var entity = Guid.ParseExact(shot.EntityId, "N");
            var target = shot.TargetEntityId == "" ? Guid.Empty : Guid.ParseExact(shot.TargetEntityId, "N");
            if (!ValidContact(entity, target, shot.Flags)) throw new ArgumentException("Invalid shot contact.");
            BinaryPrimitives.WriteInt64LittleEndian(remaining, shot.Tick);
            BinaryPrimitives.WriteUInt64LittleEndian(remaining[8..], shot.ItemId.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(remaining[16..], shot.Sequence);
            entity.TryWriteBytes(remaining[24..]);
            BinaryPrimitives.WriteInt64LittleEndian(remaining[40..], shot.ControlGeneration);
            remaining[48] = WeaponPresentationProfile.TryGetIndex(shot.ProfileId, out var profile) ? checked((byte)profile) : throw new ArgumentException("Unknown shot profile.");
            remaining[49] = shot.MuzzleIndex;
            Write(remaining[50..], shot.Origin); Write(remaining[58..], shot.End); Write(remaining[66..], shot.Normal);
            remaining[74] = (byte)shot.Flags;
            target.TryWriteBytes(remaining[75..]);
            remaining = remaining[EventBytes..];
        }
        return bytes;
    }
    public static bool Decode(byte[] bytes, out Guid session, out AcceptedShot[] shots)
    {
        session = default; shots = Array.Empty<AcceptedShot>();
        if (bytes.Length < HeaderBytes || bytes.Length > MaxBytes) return false;
        session = new Guid(bytes.AsSpan(0, 16));
        var count = bytes[16];
        if (session == Guid.Empty || count > MaxEvents || bytes.Length != HeaderBytes + count * EventBytes) return false;
        var decoded = new AcceptedShot[count];
        if (!Decode(bytes, decoded, out session, out _)) return false;
        shots = decoded;
        return true;
    }
    public static bool Decode(ReadOnlySpan<byte> bytes, Span<AcceptedShot> shots, out Guid session, out int count)
    {
        session = default; count = 0;
        if (bytes.Length < HeaderBytes || bytes.Length > MaxBytes) return false;
        session = new Guid(bytes[..16]);
        var length = bytes[16];
        if (session == Guid.Empty || length > MaxEvents || length > shots.Length || bytes.Length != HeaderBytes + length * EventBytes) return false;
        var remaining = bytes[HeaderBytes..];
        for (var i = 0; i < length; i++)
        {
            var tick = BinaryPrimitives.ReadInt64LittleEndian(remaining);
            var item = BinaryPrimitives.ReadInt64LittleEndian(remaining[8..]);
            var sequence = BinaryPrimitives.ReadUInt64LittleEndian(remaining[16..]);
            var entity = new Guid(remaining.Slice(24, 16));
            var generation = BinaryPrimitives.ReadInt64LittleEndian(remaining[40..]);
            var profile = remaining[48]; var muzzle = remaining[49];
            var origin = Read(remaining[50..]); var end = Read(remaining[58..]); var normal = Read(remaining[66..]);
            var flags = (ShotResultFlags)remaining[74];
            var target = new Guid(remaining.Slice(75, 16));
            if (tick < 0 || tick > SnapshotEnvelope.MaxTick || item <= 0 || sequence == 0 || generation < 0 || profile >= WeaponPresentationProfile.Count || muzzle > 1 ||
                !origin.IsFinite() || !end.IsFinite() || !normal.IsFinite() || !ValidContact(entity, target, flags)) return false;
            shots[i] = new(tick, ItemEntityId.FromNetwork(item), sequence, entity.ToString("N"), generation,
                WeaponPresentationProfile.GetId(profile), muzzle, origin, end, normal, flags,
                target == Guid.Empty ? "" : target.ToString("N"));
            remaining = remaining[EventBytes..];
        }
        count = length;
        return true;
    }
    private static bool ValidContact(Guid entity, Guid target, ShotResultFlags flags)
    {
        if (entity == Guid.Empty || entity == target || (byte)flags > 31) return false;
        if ((flags & ShotResultFlags.World) != 0) return flags == ShotResultFlags.World && target == Guid.Empty;
        if (target == Guid.Empty) return flags == ShotResultFlags.Miss;
        if ((flags & ShotResultFlags.Wreck) != 0 && (flags & (ShotResultFlags.Shield | ShotResultFlags.Hull)) != 0) return false;
        return (flags & ShotResultFlags.Shield) == 0 || (flags & ShotResultFlags.ShieldContact) != 0;
    }
    private static void Write(Span<byte> bytes, Vector2 value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes, value.X);
        BinaryPrimitives.WriteSingleLittleEndian(bytes[4..], value.Y);
    }
    private static Vector2 Read(ReadOnlySpan<byte> bytes) => new(BinaryPrimitives.ReadSingleLittleEndian(bytes), BinaryPrimitives.ReadSingleLittleEndian(bytes[4..]));
}
