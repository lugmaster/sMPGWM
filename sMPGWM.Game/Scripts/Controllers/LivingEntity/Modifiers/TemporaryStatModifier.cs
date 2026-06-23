using System;
using Godot;
using sMPGWM.Scripts.Enums.Game;
using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;

public class TemporaryStatModifier : StatModifier
{
    public TemporaryStatModifier(
        string instanceId,
        string sourceId,
        float value,
        ModifierType type,
        float timeRemaining,
        ModifierSourceType sourceType = ModifierSourceType.Unknown,
        string displayName = "",
        ModifierStackPolicy stackPolicy = ModifierStackPolicy.Stack)
        : base(instanceId, sourceId, value, type, sourceType, displayName, stackPolicy)
    {
        TimeRemaining = timeRemaining;
    }

    public TemporaryStatModifier(GCol.Array values)
        : base(values)
    {
        if (values.Count < 3)
            throw new ArgumentException("Invalid temporary stat modifier variant format");

        TimeRemaining = values.Count >= 8 ? (float)values[7] : (float)values[2];
    }

    public bool IsExpired => TimeRemaining <= 0;
    public float TimeRemaining { get; private set; }

    public virtual void Tick(float delta)
    {
        TimeRemaining -= delta;
    }

    public override GCol.Array ToVariant()
    {
        var result = base.ToVariant();
        result.Add(TimeRemaining);
        return result;
    }

    public override bool Equals(object obj)
    {
        return obj is TemporaryStatModifier other &&
               base.Equals(other) &&
               Mathf.IsEqualApprox(TimeRemaining, other.TimeRemaining);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
