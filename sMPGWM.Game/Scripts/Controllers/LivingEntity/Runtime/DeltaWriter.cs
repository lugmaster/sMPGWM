using System;
using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

public sealed class DeltaWriter
{
    public const byte CurrentVersion = 1;
    private byte[] _buffer;
    private int _position;

    public DeltaWriter(int initialCapacity = 4096)
    {
        _buffer = new byte[initialCapacity];
    }

    public PackedByteArray Write(
        LivingEntityState state,
        SimulationLane lane,
        ulong dirtyStats,
        bool includeConditions,
        bool includeEffects,
        CausalRecord[] causalRecords,
        int causalRecordCount)
    {
        _position = 0;

        var flags = DeltaPayloadFlags.None;
        if (dirtyStats != 0)
            flags |= DeltaPayloadFlags.HasFinalStats;
        if (includeConditions)
            flags |= DeltaPayloadFlags.HasFinalConditions;
        if (includeEffects)
            flags |= DeltaPayloadFlags.HasFinalEffects;
        if (causalRecordCount > 0)
            flags |= DeltaPayloadFlags.HasCausalRecords;

        WriteByte(CurrentVersion);
        WriteInt64(state.EntityId);
        WriteInt64(state.NetworkTick);
        WriteByte((byte)lane);
        WriteByte((byte)flags);
        WriteUInt64(dirtyStats);
        WriteUInt64(includeConditions ? state.ConditionFlags : 0UL);
        WriteInt32(causalRecordCount);

        var changedStatCountPosition = _position;
        WriteInt32(0);
        var changedStatCount = 0;
        if ((flags & DeltaPayloadFlags.HasFinalStats) != 0)
        {
            var mask = dirtyStats;
            while (mask != 0)
            {
                var bit = mask & (~mask + 1);
                var statIndex = BitIndex(bit);
                WriteByte((byte)statIndex);
                WriteSingle(state.CurrentValues[statIndex]);
                WriteSingle(state.MaxValues[statIndex]);
                WriteSingle(state.BaseValues[statIndex]);
                changedStatCount++;
                mask &= ~bit;
            }
        }

        WriteInt32At(changedStatCountPosition, changedStatCount);

        if ((flags & DeltaPayloadFlags.HasFinalEffects) != 0)
        {
            WriteInt32(state.ActiveEffectCount);
            for (var i = 0; i < state.ActiveEffectCount; i++)
                WriteActiveEffect(state.ActiveEffects[i]);
        }
        else
        {
            WriteInt32(0);
        }

        for (var i = 0; i < causalRecordCount; i++)
            WriteCausalRecord(causalRecords[i]);

        var result = new byte[_position];
        Array.Copy(_buffer, result, _position);
        return new PackedByteArray(result);
    }

    private void WriteActiveEffect(ActiveEffect effect)
    {
        WriteInt64(effect.RuntimeEffectId);
        WriteInt32(effect.EffectDefinitionId);
        WriteInt64(effect.SourceId);
        WriteInt64(effect.OriginId);
        WriteInt64(effect.TargetEntityId);
        WriteInt64(effect.AppliedNetworkTick);
        WriteInt64(effect.EffectNetworkTick);
        WriteInt64(effect.LastExecutedNetworkTick);
        WriteInt64(effect.ExpireNetworkTick);
        WriteInt32(effect.StackCount);
        WriteInt64(effect.ParentRuntimeEffectId);
        WriteInt32(effect.ChildDepth);
        WriteInt32((int)effect.RuntimeFlags);
        WriteInt64(effect.InsertionSequence);
        WriteInt32(effect.ChildSequence);
    }

    private void WriteCausalRecord(CausalRecord record)
    {
        WriteByte((byte)record.Type);
        WriteInt64(record.RuntimeEffectId);
        WriteInt32(record.EffectDefinitionId);
        WriteByte((byte)record.StatType);
        WriteSingle(record.Value);
        WriteInt64(record.NetworkTick);
    }

    private void WriteByte(byte value)
    {
        Ensure(1);
        _buffer[_position++] = value;
    }

    private void WriteInt32(int value)
    {
        Ensure(4);
        WriteInt32At(_position, value);
        _position += 4;
    }

    private void WriteInt64(long value)
    {
        Ensure(8);
        WriteUInt64Unchecked((ulong)value);
    }

    private void WriteUInt64(ulong value)
    {
        Ensure(8);
        WriteUInt64Unchecked(value);
    }

    private void WriteSingle(float value)
    {
        WriteInt32(BitConverter.SingleToInt32Bits(value));
    }

    private void WriteInt32At(int position, int value)
    {
        _buffer[position] = (byte)value;
        _buffer[position + 1] = (byte)(value >> 8);
        _buffer[position + 2] = (byte)(value >> 16);
        _buffer[position + 3] = (byte)(value >> 24);
    }

    private void WriteUInt64Unchecked(ulong value)
    {
        _buffer[_position++] = (byte)value;
        _buffer[_position++] = (byte)(value >> 8);
        _buffer[_position++] = (byte)(value >> 16);
        _buffer[_position++] = (byte)(value >> 24);
        _buffer[_position++] = (byte)(value >> 32);
        _buffer[_position++] = (byte)(value >> 40);
        _buffer[_position++] = (byte)(value >> 48);
        _buffer[_position++] = (byte)(value >> 56);
    }

    private void Ensure(int required)
    {
        if (_position + required <= _buffer.Length)
            return;

        Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _position + required));
    }

    private static int BitIndex(ulong singleBit)
    {
        var index = 0;
        while ((singleBit >>= 1) != 0)
            index++;
        return index;
    }
}
