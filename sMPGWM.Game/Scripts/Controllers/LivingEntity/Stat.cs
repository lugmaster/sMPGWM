using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public readonly struct Stat
{
    public Stat(float baseValue)
        : this(baseValue, baseValue, baseValue)
    {
    }

    public Stat(float baseValue, float currentValue)
        : this(baseValue, baseValue, Math.Clamp(currentValue, 0f, MathF.Max(baseValue, 0f)))
    {
    }

    public Stat(float baseValue, float maxValue, float currentValue)
    {
        BaseValue = MathF.Max(0f, baseValue);
        MaxValue = MathF.Max(0f, maxValue);
        CurrentValue = Math.Clamp(currentValue, 0f, MaxValue);
    }

    public float BaseValue { get; }
    public float CurrentValue { get; }
    public float MaxValue { get; }
}
