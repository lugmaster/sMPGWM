using System;
using System.IO;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public static class ShotEventCodec
{
    public const int MaxBytes = 1200, HeaderBytes = 17, EventBytes = 75;
    public const int MaxEvents = (MaxBytes - HeaderBytes) / EventBytes;
    public static byte[] Encode(Guid session, ReadOnlySpan<AcceptedShot> shots)
    {
        if (shots.Length > MaxEvents) throw new ArgumentOutOfRangeException(nameof(shots));
        using var stream = new MemoryStream(HeaderBytes + shots.Length * EventBytes);
        using var w = new BinaryWriter(stream);
        w.Write(session.ToByteArray()); w.Write((byte)shots.Length);
        foreach (var s in shots)
        {
            w.Write(s.Tick); w.Write(s.ItemId.Value); w.Write(s.Sequence);
            w.Write(Guid.ParseExact(s.EntityId, "N").ToByteArray()); w.Write(s.ControlGeneration);
            w.Write(s.ProfileId == "smg_standard" ? (byte)0 : s.ProfileId == "laser_standard" ? (byte)1 : throw new ArgumentException("Unknown shot profile."));
            w.Write(s.MuzzleIndex); Write(w, s.Origin); Write(w, s.End); Write(w, s.Normal); w.Write((byte)s.Flags);
        }
        return stream.ToArray();
    }
    public static bool Decode(byte[] bytes, out Guid session, out AcceptedShot[] shots)
    {
        session = default; shots = System.Array.Empty<AcceptedShot>();
        if (bytes.Length < HeaderBytes || bytes.Length > MaxBytes) return false;
        try
        {
            using var r = new BinaryReader(new MemoryStream(bytes));
            session = new Guid(r.ReadBytes(16)); var count = r.ReadByte();
            if (session == Guid.Empty || count > MaxEvents || bytes.Length != HeaderBytes + count * EventBytes) return false;
            var decoded = new AcceptedShot[count];
            for (var i = 0; i < count; i++)
            {
                var tick = r.ReadInt64(); var item = r.ReadInt64(); var sequence = r.ReadUInt64();
                var entity = new Guid(r.ReadBytes(16)).ToString("N"); var generation = r.ReadInt64();
                var profile = r.ReadByte(); var muzzle = r.ReadByte();
                var origin = Read(r); var end = Read(r); var normal = Read(r); var flags = r.ReadByte();
                if (tick < 0 || tick > SnapshotEnvelope.MaxTick || item <= 0 || sequence == 0 || generation < 0 || profile > 1 || muzzle > 1 ||
                    !origin.IsFinite() || !end.IsFinite() || !normal.IsFinite() || flags > 15) return false;
                decoded[i] = new(tick, ItemEntityId.FromNetwork(item), sequence, entity, generation,
                    profile == 0 ? "smg_standard" : "laser_standard", muzzle, origin, end, normal, (ShotResultFlags)flags);
            }
            shots = decoded; return true;
        }
        catch (Exception e) when (e is IOException or ArgumentException) { return false; }
    }
    private static void Write(BinaryWriter w, Vector2 value) { w.Write(value.X); w.Write(value.Y); }
    private static Vector2 Read(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle());
}
