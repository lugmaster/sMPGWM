using System;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Reads compact state-engine binary deltas into structured delta data.
/// </summary>
public static class DeltaReader
{
    /// <summary>
    /// Attempts to parse a packed state delta payload.
    /// </summary>
    public static bool TryRead(PackedByteArray packedBytes, out StateDelta delta)
    {
        var bytes = packedBytes.ToArray();
        if (bytes.Length == 0)
        {
            delta = new StateDelta();
            return false;
        }

        var reader = new Reader(bytes);
        var version = reader.ReadByte();
        if (version != DeltaWriter.CurrentVersion)
            throw new InvalidOperationException($"Unsupported state delta version {version}.");

        var entityId = reader.ReadInt64();
        var networkTick = reader.ReadInt64();
        var lane = (SimulationLane)reader.ReadByte();
        var flags = (DeltaPayloadFlags)reader.ReadByte();
        var dirtyStats = reader.ReadUInt64();
        var conditionFlags = reader.ReadUInt64();
        var causalCount = reader.ReadInt32();
        var changedStatCount = reader.ReadInt32();

        var changedStats = changedStatCount == 0 ? [] : new ChangedStatPayload[changedStatCount];
        for (var i = 0; i < changedStatCount; i++)
        {
            changedStats[i] = new ChangedStatPayload
            {
                StatIndex = reader.ReadByte(),
                CurrentValue = reader.ReadSingle(),
                MaxValue = reader.ReadSingle(),
                BaseValue = reader.ReadSingle()
            };
        }

        var effectCount = reader.ReadInt32();
        var effects = effectCount == 0 ? [] : new ActiveEffect[effectCount];
        for (var i = 0; i < effectCount; i++)
            effects[i] = reader.ReadActiveEffect();

        var causalRecords = causalCount == 0 ? [] : new CausalRecord[causalCount];
        for (var i = 0; i < causalCount; i++)
            causalRecords[i] = reader.ReadCausalRecord();

        delta = new StateDelta
        {
            DeltaVersion = version,
            EntityId = entityId,
            NetworkTick = networkTick,
            SimulationLane = lane,
            PayloadFlags = flags,
            DirtyStats = dirtyStats,
            ConditionFlags = conditionFlags,
            ChangedStats = changedStats,
            ActiveEffects = effects,
            CausalRecords = causalRecords
        };
        return true;
    }

    private ref struct Reader(byte[] bytes)
    {
        private readonly byte[] _bytes = bytes;
        private int _position;

        public byte ReadByte()
        {
            return _bytes[_position++];
        }

        public int ReadInt32()
        {
            var value = _bytes[_position] |
                        (_bytes[_position + 1] << 8) |
                        (_bytes[_position + 2] << 16) |
                        (_bytes[_position + 3] << 24);
            _position += 4;
            return value;
        }

        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        public ulong ReadUInt64()
        {
            var value =
                (ulong)_bytes[_position] |
                ((ulong)_bytes[_position + 1] << 8) |
                ((ulong)_bytes[_position + 2] << 16) |
                ((ulong)_bytes[_position + 3] << 24) |
                ((ulong)_bytes[_position + 4] << 32) |
                ((ulong)_bytes[_position + 5] << 40) |
                ((ulong)_bytes[_position + 6] << 48) |
                ((ulong)_bytes[_position + 7] << 56);
            _position += 8;
            return value;
        }

        public float ReadSingle()
        {
            return BitConverter.Int32BitsToSingle(ReadInt32());
        }

        public ActiveEffect ReadActiveEffect()
        {
            return new ActiveEffect
            {
                RuntimeEffectId = ReadInt64(),
                EffectDefinitionId = ReadInt32(),
                SourceId = ReadInt64(),
                OriginId = ReadInt64(),
                TargetEntityId = ReadInt64(),
                AppliedNetworkTick = ReadInt64(),
                EffectNetworkTick = ReadInt64(),
                LastExecutedNetworkTick = ReadInt64(),
                ExpireNetworkTick = ReadInt64(),
                StackCount = ReadInt32(),
                ParentRuntimeEffectId = ReadInt64(),
                ChildDepth = ReadInt32(),
                RuntimeFlags = (EffectRuntimeFlags)ReadInt32(),
                InsertionSequence = ReadInt64(),
                ChildSequence = ReadInt32()
            };
        }

        public CausalRecord ReadCausalRecord()
        {
            return new CausalRecord
            {
                Type = (CausalRecordType)ReadByte(),
                RuntimeEffectId = ReadInt64(),
                EffectDefinitionId = ReadInt32(),
                StatType = (StatType)ReadByte(),
                Value = ReadSingle(),
                NetworkTick = ReadInt64()
            };
        }
    }
}
