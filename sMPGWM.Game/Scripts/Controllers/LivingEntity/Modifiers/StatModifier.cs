using System;
using Godot;
using sMPGWM.Scripts.Enums.Game;
using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;

public class StatModifier
{
    public StatModifier(
        string instanceId,
        string sourceId,
        float value,
        ModifierType type,
        ModifierSourceType sourceType = ModifierSourceType.Unknown,
        string displayName = "",
        ModifierStackPolicy stackPolicy = ModifierStackPolicy.Stack)
    {
        InstanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString() : instanceId;
        SourceId = sourceId ?? string.Empty;
        SourceType = sourceType;
        DisplayName = displayName ?? string.Empty;
        Value = value;
        Type = type;
        StackPolicy = stackPolicy;
    }

    public StatModifier(GCol.Array values)
    {
        if (values.Count < 2)
            throw new ArgumentException("Invalid stat modifier variant format");

        Value = (float)values[0];
        Type = (ModifierType)(int)values[1];
        InstanceId = values.Count >= 7 ? values[2].AsString() : Guid.NewGuid().ToString();
        SourceId = values.Count >= 7 ? values[3].AsString() : string.Empty;
        SourceType = values.Count >= 7 ? (ModifierSourceType)(int)values[4] : ModifierSourceType.Unknown;
        DisplayName = values.Count >= 7 ? values[5].AsString() : string.Empty;
        StackPolicy = values.Count >= 7 ? (ModifierStackPolicy)(int)values[6] : ModifierStackPolicy.Stack;
    }

    public string InstanceId { get; }
    public string SourceId { get; }
    public ModifierSourceType SourceType { get; }
    public string DisplayName { get; }
    public float Value { get; }
    public ModifierType Type { get; }
    public ModifierStackPolicy StackPolicy { get; }

    public virtual GCol.Array ToVariant()
    {
        return new GCol.Array
        {
            Value,
            (int)Type,
            InstanceId,
            SourceId,
            (int)SourceType,
            DisplayName,
            (int)StackPolicy
        };
    }

    public override bool Equals(object obj)
    {
        return obj is StatModifier other &&
               GetType() == other.GetType() &&
               InstanceId == other.InstanceId &&
               SourceId == other.SourceId &&
               SourceType == other.SourceType &&
               DisplayName == other.DisplayName &&
               Mathf.IsEqualApprox(Value, other.Value) &&
               Type == other.Type &&
               StackPolicy == other.StackPolicy;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), InstanceId, SourceId, SourceType, Type, StackPolicy);
    }
}
