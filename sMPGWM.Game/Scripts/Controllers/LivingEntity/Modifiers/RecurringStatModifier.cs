using System;
using Godot;
using sMPGWM.Scripts.Enums.Game;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;

public class RecurringStatModifier(float value, ModifierType type, float timeRemaining, float tickInterval, Stat target)
    : TemporaryStatModifier(value, type, timeRemaining)
{
    public Stat Target { get;} = target;
    public float TickInterval { get; } = tickInterval;
    private float _tickTimer;
    private static int RoundingPrecision => 1;
    
    public RecurringStatModifier(Array values, Stat target) : 
        this((float)values[0], (ModifierType)(int)values[1], (float)values[2], (float)values[3], target)
    {
    }

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
            ModifierType.Flat => Value,
            ModifierType.Percent => Round(Target.CurrentValue * Value),
            _ => 0f
        };
        
        if (amount < 0f)
            Target.Reduce(-amount);
        else
            Target.Raise(amount);
    }
    
    public new Array ToVariant()
    {
        return new Array
        {
            Value,
            (int)Type,
            TimeRemaining,
            TickInterval,
            _tickTimer
        };
    }

    public override bool Equals(object obj)
    {
        return obj is RecurringStatModifier other &&
               base.Equals(other) &&
               Mathf.IsEqualApprox(TickInterval, other.TickInterval);
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
