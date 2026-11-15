using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

/// <summary>Bounded positional records using session-scoped entity indices and caller-owned buffers.</summary>
public static class MovementWire
{
    public const int MaxPlayers = 10, MaxCommands = 6, MaxBytes = 1400;
    public const int SnapshotHeaderBytes = 25, SampleBytes = 108;
    public const int CommandHeaderBytes = 29, CommandBytes = 30, MaxCommandBytes = CommandHeaderBytes + MaxCommands * CommandBytes;
    public const long MaxTick = SnapshotEnvelope.MaxTick;

    public static void SnapshotInto(Guid session, long tick, IReadOnlyList<MovementSample> samples,
        Func<string, int> entityIndex, Span<byte> destination)
    {
        if (session == Guid.Empty || tick < 0 || tick > MaxTick || samples.Count > MaxPlayers ||
            destination.Length != SnapshotHeaderBytes + samples.Count * SampleBytes)
            throw new ArgumentException("Invalid movement snapshot or buffer size.");
        var writer = new Writer(destination);
        writer.Guid(session); writer.Long(tick); writer.Byte((byte)samples.Count);
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            var index = entityIndex(sample.Entity);
            if (index <= 0) throw new ArgumentException("Movement entity has no session index.");
            writer.Int(sample.Peer); writer.Int(index);
            writer.Long(sample.Generation); writer.Long(sample.Revision);
            writer.Vector(sample.Position); writer.Float(sample.Rotation); writer.Vector(sample.Velocity);
            writer.Input(sample.Input); writer.Long(sample.Arrival); writer.Byte(sample.AppliedFlags);
            writer.Long(sample.ConfigurationRevision); writer.Configuration(sample.Configuration);
        }
    }

    public static bool ReadSnapshot(ReadOnlySpan<byte> bytes, Func<int, string?> resolveEntity,
        List<MovementSample> samples, out Guid session, out long tick)
    {
        samples.Clear(); session = default; tick = 0;
        if (bytes.Length < SnapshotHeaderBytes || bytes.Length > MaxBytes) return false;
        var reader = new Reader(bytes);
        session = reader.Guid(); tick = reader.Long(); var count = reader.Byte();
        if (session == Guid.Empty || tick < 0 || tick > MaxTick || count > MaxPlayers ||
            bytes.Length != SnapshotHeaderBytes + count * SampleBytes) return false;
        for (var i = 0; i < count; i++)
        {
            var peer = reader.Int(); var index = reader.Int(); var entity = resolveEntity(index);
            var generation = reader.Long(); var revision = reader.Long();
            var position = reader.Vector(); var rotation = reader.Float(); var velocity = reader.Vector();
            var input = reader.Input(out var validInput); var arrival = reader.Long(); var applied = reader.Byte();
            var configurationRevision = reader.Long(); var configuration = reader.Configuration(out var validConfiguration);
            if (peer <= 0 || index <= 0 || entity == null || generation < 0 || revision < 0 ||
                !position.IsFinite() || !velocity.IsFinite() || !float.IsFinite(rotation) || !validInput ||
                arrival < 0 || arrival > MaxTick || applied > 7 || configurationRevision < 0 || !validConfiguration ||
                !configuration.Enabled && applied != 0) { samples.Clear(); return false; }
            for (var j = 0; j < samples.Count; j++)
                if (samples[j].Peer == peer || samples[j].Entity == entity) { samples.Clear(); return false; }
            samples.Add(new(peer, entity, generation, revision, position, rotation, velocity, input, arrival, applied,
                configurationRevision, configuration));
        }
        return true;
    }

    public static void CommandsInto(Guid session, int entity, long generation, ReadOnlySpan<MovementCommand> commands,
        Span<byte> destination)
    {
        if (session == Guid.Empty || entity <= 0 || generation < 0 || commands.Length is < 1 or > MaxCommands ||
            destination.Length != CommandHeaderBytes + commands.Length * CommandBytes)
            throw new ArgumentException("Invalid movement commands or buffer size.");
        var writer = new Writer(destination);
        writer.Guid(session); writer.Int(entity); writer.Long(generation); writer.Byte((byte)commands.Length);
        for (var i = 0; i < commands.Length; i++) { writer.Long(commands[i].Tick); writer.Input(commands[i].Input); }
    }

    public static bool ReadCommands(ReadOnlySpan<byte> bytes, Span<MovementCommand> commands, out Guid session,
        out int entity, out long generation, out int count)
    {
        session = default; entity = 0; generation = 0; count = 0;
        if (bytes.Length < CommandHeaderBytes || bytes.Length > MaxCommandBytes) return false;
        var reader = new Reader(bytes);
        session = reader.Guid(); entity = reader.Int(); generation = reader.Long(); var length = reader.Byte();
        if (session == Guid.Empty || entity <= 0 || generation < 0 || length is < 1 or > MaxCommands ||
            commands.Length < length || bytes.Length != CommandHeaderBytes + length * CommandBytes) return false;
        var previous = -1L;
        for (var i = 0; i < length; i++)
        {
            var tick = reader.Long(); var input = reader.Input(out var valid);
            if (tick <= previous || tick > MaxTick || !valid) return false;
            commands[i] = new(tick, input); previous = tick;
        }
        count = length;
        return true;
    }

    public static bool Valid(ShipInputState input) => input.Direction.IsFinite() && input.Direction.LengthSquared() <= 1.001f &&
        float.IsFinite(input.AimAngle) && input.FireMask <= 3 && input.WeaponRevision >= 0 && input.WeaponRevision < long.MaxValue;

    internal ref struct Writer(Span<byte> bytes)
    {
        private Span<byte> _bytes = bytes;
        public void Byte(byte value) { _bytes[0] = value; _bytes = _bytes[1..]; }
        public void Int(int value) { BinaryPrimitives.WriteInt32LittleEndian(_bytes, value); _bytes = _bytes[4..]; }
        public void Long(long value) { BinaryPrimitives.WriteInt64LittleEndian(_bytes, value); _bytes = _bytes[8..]; }
        public void Float(float value) { BinaryPrimitives.WriteSingleLittleEndian(_bytes, value); _bytes = _bytes[4..]; }
        public void Guid(Guid value) { value.TryWriteBytes(_bytes); _bytes = _bytes[16..]; }
        public void Vector(Vector2 value) { Float(value.X); Float(value.Y); }
        public void Input(ShipInputState value)
        {
            Vector(value.Direction); Float(value.AimAngle);
            Byte((byte)((value.HasAim ? 1 : 0) | (value.Afterburner ? 2 : 0) | (value.Brake ? 4 : 0) | (value.AutomaticBraking ? 8 : 0)));
            Byte(value.FireMask); Long(value.WeaponRevision);
        }
        public void Configuration(MovementConfiguration value)
        {
            Float(value.Speed); Float(value.AfterBurnerSpeed); Float(value.RotationSpeed);
            Float(value.Acceleration); Float(value.AfterBurnerAcceleration); Float(value.Mass); Byte(value.Enabled ? (byte)1 : (byte)0);
        }
    }

    internal ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private ReadOnlySpan<byte> _bytes = bytes;
        public byte Byte() { var value = _bytes[0]; _bytes = _bytes[1..]; return value; }
        public int Int() { var value = BinaryPrimitives.ReadInt32LittleEndian(_bytes); _bytes = _bytes[4..]; return value; }
        public long Long() { var value = BinaryPrimitives.ReadInt64LittleEndian(_bytes); _bytes = _bytes[8..]; return value; }
        public float Float() { var value = BinaryPrimitives.ReadSingleLittleEndian(_bytes); _bytes = _bytes[4..]; return value; }
        public Guid Guid() { var value = new Guid(_bytes[..16]); _bytes = _bytes[16..]; return value; }
        public Vector2 Vector() => new(Float(), Float());
        public ShipInputState Input(out bool valid)
        {
            var direction = Vector(); var angle = Float(); var flags = Byte();
            var value = new ShipInputState(direction, angle, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0,
                (flags & 8) != 0, Byte(), Long());
            valid = flags <= 15 && Valid(value);
            return value;
        }
        public MovementConfiguration Configuration(out bool valid)
        {
            var speed = Float(); var afterburner = Float(); var rotation = Float();
            var acceleration = Float(); var afterburnerAcceleration = Float(); var mass = Float(); var enabled = Byte();
            var value = new MovementConfiguration(speed, afterburner, rotation, acceleration, afterburnerAcceleration, mass, enabled != 0);
            valid = enabled <= 1 && value.IsValid;
            return value;
        }
    }
}
