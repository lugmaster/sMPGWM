using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Immutable snapshot of one living-entity stat's base, current, and max values.
/// </summary>
public readonly struct Stat
{
    /// <summary>
    /// Creates a full stat snapshot with current and max equal to the base value.
    /// </summary>
    public Stat(float baseValue)
        : this(baseValue, baseValue, baseValue)
    {
    }

    /// <summary>
    /// Creates a stat snapshot with current clamped between zero and the base-derived max.
    /// </summary>
    public Stat(float baseValue, float currentValue)
        : this(baseValue, baseValue, Math.Clamp(currentValue, 0f, MathF.Max(baseValue, 0f)))
    {
    }

    /// <summary>
    /// Creates a stat snapshot with explicit base, max, and current values.
    /// </summary>
    public Stat(float baseValue, float maxValue, float currentValue)
    {
        BaseValue = MathF.Max(0f, baseValue);
        MaxValue = MathF.Max(0f, maxValue);
        CurrentValue = Math.Clamp(currentValue, 0f, MaxValue);
    }

    /// <summary>
    /// Gets the unmodified stat value provided as simulation input.
    /// </summary>
    public float BaseValue { get; }

    /// <summary>
    /// Gets the current clamped stat value.
    /// </summary>
    public float CurrentValue { get; }

    /// <summary>
    /// Gets the current maximum stat value after active modifiers.
    /// </summary>
    public float MaxValue { get; }
}
