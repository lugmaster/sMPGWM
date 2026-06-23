using System;
using Godot;
using sMPGWM.Scripts.Enums.Game;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;

public class RecurringStatModifier : TemporaryStatModifier
{
    private static int RoundingPrecision => 3;
    private float _tickTimer;

    public RecurringStatModifier(
        string instanceId,
        string sourceId,
        float value,
        ModifierType type,
        float timeRemaining,
        float tickInterval,
        Stat target,
        ModifierSourceType sourceType = ModifierSourceType.Unknown,
        string displayName = "",
        ModifierStackPolicy stackPolicy = ModifierStackPolicy.Stack,
        float tickTimer = 0f)
        : base(instanceId, sourceId, value, type, timeRemaining, sourceType, displayName, stackPolicy)
    {
        TickInterval = tickInterval;
        Target = target;
        _tickTimer = tickTimer;
    }

    public RecurringStatModifier(Array values, Stat target)
        : base(values)
    {
        if (values.Count < 4)
            throw new ArgumentException("Invalid recurring stat modifier variant format");

        TickInterval = values.Count >= 10 ? (float)values[8] : (float)values[3];
        Target = target;
        _tickTimer = values.Count >= 10 ? (float)values[9] : values.Count > 4 ? (float)values[4] : 0f;
    }

    public Stat Target { get; }
    public float TickInterval { get; }
    public float TickTimer => _tickTimer;

    public override void Tick(float delta)
    {
        base.Tick(delta);
        _tickTimer += delta;

        while (_tickTimer >= TickInterval)
        {
            ApplyEffect();
            _tickTimer -= TickInterval;
        }
    }

    private void ApplyEffect()
    {
        var amount = Type switch
        {
            ModifierType.Flat => Value * TickInterval,
            ModifierType.Percent => Round(Target.CurrentValue * Value * TickInterval),
            _ => 0f
        };

        if (amount < 0f)
            Target.Reduce(-amount);
        else
            Target.Raise(amount);
    }

    public override Array ToVariant()
    {
        var result = base.ToVariant();
        result.Add(TickInterval);
        result.Add(_tickTimer);
        return result;
    }

    public override bool Equals(object obj)
    {
        return obj is RecurringStatModifier other &&
               base.Equals(other) &&
               Mathf.IsEqualApprox(TickInterval, other.TickInterval) &&
               Mathf.IsEqualApprox(_tickTimer, other._tickTimer);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    private static float Round(float value)
    {
        return MathF.Round(value, RoundingPrecision);
    }
}
