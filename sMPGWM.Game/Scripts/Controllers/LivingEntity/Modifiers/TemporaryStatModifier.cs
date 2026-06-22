using System;
using Godot;
using Array = Godot.Collections.Array;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;

public class TemporaryStatModifier(float value, ModifierType type, float timeRemaining)
    : StatModifier(value, type)
{
    public TemporaryStatModifier(Array values) : this((float)values[0], (ModifierType)(int)values[1], (float)values[2])
    {
    }
    
    public bool IsExpired => TimeRemaining <= 0;
    public float TimeRemaining { get; private set; } = timeRemaining;

    public virtual void Tick(float delta)
    {
        TimeRemaining -= delta;
    }

    public new Array ToVariant()
    {
        return new Array
        {
            Value,
            (int)Type,
            TimeRemaining
        };
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
