using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

public readonly record struct MovementSample(int Peer, string Entity, long Generation, long Revision,
    Vector2 Position, float Rotation, Vector2 Velocity, ShipInputState Input, long Arrival, byte AppliedFlags = 0,
    long ConfigurationRevision = 0, MovementConfiguration Configuration = default);
public readonly record struct MovementCommand(long Tick, ShipInputState Input);

/// <summary>Fixed-size, bounded movement payload; transport envelope is separate from movement fields.</summary>
public static class MovementWire
{
    public const int MaxPlayers = 10, MaxCommands = 6, MaxBytes = 1400;
    public const int SnapshotHeaderBytes = 25, SampleBytes = 111;
    public const long MaxTick = SnapshotEnvelope.MaxTick;
    public static byte[] Snapshot(Guid session, long tick, IReadOnlyList<MovementSample> samples)
    {
        if (samples.Count > MaxPlayers) throw new InvalidOperationException("Snapshot capacity exceeded.");
        using var stream = new MemoryStream(SnapshotHeaderBytes + SampleBytes * samples.Count);
        using var writer = new BinaryWriter(stream);
        SnapshotEnvelope.Write(writer, session, tick); writer.Write((byte)samples.Count);
        foreach (var s in samples)
        {
            writer.Write(s.Peer); writer.Write(Guid.ParseExact(s.Entity, "N").ToByteArray());
            writer.Write(s.Generation); writer.Write(s.Revision);
            WriteVector(writer, s.Position); writer.Write(s.Rotation); WriteVector(writer, s.Velocity);
            WriteInput(writer, s.Input); writer.Write(s.Arrival); writer.Write(s.AppliedFlags);
            writer.Write(s.ConfigurationRevision); WriteConfiguration(writer, s.Configuration);
        }
        if (samples.Count > MaxPlayers || stream.Length > MaxBytes) throw new InvalidOperationException("Snapshot capacity exceeded.");
        return stream.ToArray();
    }
    public static bool ReadSnapshot(byte[] bytes, out Guid session, out long tick, out List<MovementSample> samples)
    {
        session = default; tick = 0; samples = new();
        if (bytes.Length > MaxBytes || bytes.Length < 25) return false;
        try
        {
            using var r = new BinaryReader(new MemoryStream(bytes));
            if (!SnapshotEnvelope.Read(r, out session, out tick)) return false;
            int count = r.ReadByte();
            if (session == Guid.Empty || tick < 0 || tick > MaxTick || count > MaxPlayers) return false;
            var peers = new HashSet<int>(); var entities = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                int peer = r.ReadInt32(); string entity = new Guid(r.ReadBytes(16)).ToString("N");
                long generation = r.ReadInt64(), revision = r.ReadInt64();
                var position = ReadVector(r); float rotation = r.ReadSingle(); var velocity = ReadVector(r);
                var input = ReadInput(r); long arrival = r.ReadInt64(); byte applied = r.ReadByte();
                var configurationRevision = r.ReadInt64(); var configuration = ReadConfiguration(r);
                if (peer <= 0 || !peers.Add(peer) || !entities.Add(entity) || generation < 0 || revision < 0 ||
                    !position.IsFinite() || !velocity.IsFinite() || !float.IsFinite(rotation) || !Valid(input) ||
                    arrival < 0 || arrival > MaxTick || applied > 7 || configurationRevision < 0 || !configuration.IsValid ||
                    !configuration.Enabled && applied != 0) return false;
                samples.Add(new(peer, entity, generation, revision, position, rotation, velocity, input, arrival, applied,
                    configurationRevision, configuration));
            }
            return r.BaseStream.Position == r.BaseStream.Length;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException) { return false; }
    }
    public static byte[] Commands(Guid session, string entity, long generation, IReadOnlyList<MovementCommand> commands)
    {
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        w.Write(session.ToByteArray()); w.Write(Guid.ParseExact(entity, "N").ToByteArray());
        w.Write(generation); w.Write((byte)commands.Count);
        foreach (var c in commands) { w.Write(c.Tick); WriteInput(w, c.Input); }
        return stream.ToArray();
    }
    public static bool ReadCommands(byte[] bytes, out Guid session, out string entity, out long generation, out List<MovementCommand> commands)
    {
        session = default; entity = ""; generation = 0; commands = new();
        if (bytes.Length < 41 || bytes.Length > 256) return false;
        try
        {
            using var r = new BinaryReader(new MemoryStream(bytes));
            session = new Guid(r.ReadBytes(16)); entity = new Guid(r.ReadBytes(16)).ToString("N");
            generation = r.ReadInt64(); int count = r.ReadByte();
            if (session == Guid.Empty || generation < 0 || count < 1 || count > MaxCommands) return false;
            long previous = -1;
            for (int i = 0; i < count; i++)
            {
                long tick = r.ReadInt64(); var input = ReadInput(r);
                if (tick <= previous || tick > MaxTick || !Valid(input)) return false;
                commands.Add(new(tick, input)); previous = tick;
            }
            return r.BaseStream.Position == r.BaseStream.Length;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException) { return false; }
    }
    public static bool Valid(ShipInputState i) => i.Direction.IsFinite() && i.Direction.LengthSquared() <= 1.001f && float.IsFinite(i.AimAngle);
    private static void WriteConfiguration(BinaryWriter writer, MovementConfiguration configuration)
    {
        writer.Write(configuration.Speed); writer.Write(configuration.AfterBurnerSpeed);
        writer.Write(configuration.RotationSpeed); writer.Write(configuration.Acceleration);
        writer.Write(configuration.AfterBurnerAcceleration); writer.Write(configuration.Mass);
        writer.Write(configuration.Enabled);
    }
    private static MovementConfiguration ReadConfiguration(BinaryReader reader)
    {
        var speed = reader.ReadSingle(); var afterburnerSpeed = reader.ReadSingle();
        var rotation = reader.ReadSingle(); var acceleration = reader.ReadSingle();
        var afterburnerAcceleration = reader.ReadSingle(); var mass = reader.ReadSingle();
        var enabled = reader.ReadByte();
        if (enabled > 1) throw new InvalidDataException("Invalid powered movement flag.");
        return new(speed, afterburnerSpeed, rotation, acceleration, afterburnerAcceleration, mass, enabled != 0);
    }
    private static void WriteVector(BinaryWriter w, Vector2 v) { w.Write(v.X); w.Write(v.Y); }
    private static Vector2 ReadVector(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle());
    private static void WriteInput(BinaryWriter w, ShipInputState i)
    {
        WriteVector(w, i.Direction); w.Write(i.AimAngle);
        w.Write((byte)((i.HasAim ? 1 : 0) | (i.Afterburner ? 2 : 0) | (i.Brake ? 4 : 0) | (i.AutomaticBraking ? 8 : 0)));
    }
    private static ShipInputState ReadInput(BinaryReader r)
    {
        var direction = ReadVector(r); float angle = r.ReadSingle(); byte flags = r.ReadByte();
        if (flags > 15) throw new InvalidDataException();
        return new(direction, angle, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, (flags & 8) != 0);
    }
}
