using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Describes runtime effect duration using integer network ticks.
/// </summary>
public readonly struct EffectDuration(EffectDurationType type, int ticks = 0)
{
    /// <summary>
    /// Gets the duration category.
    /// </summary>
    public EffectDurationType Type { get; } = type;

    /// <summary>
    /// Gets the duration length in network ticks for temporary effects.
    /// </summary>
    public int Ticks { get; } = ticks;

    /// <summary>
    /// Gets a one-shot duration that is consumed after execution.
    /// </summary>
    public static EffectDuration Once { get; } = new(EffectDurationType.Once);

    /// <summary>
    /// Gets a permanent duration that only ends through explicit removal.
    /// </summary>
    public static EffectDuration Permanent { get; } = new(EffectDurationType.Permanent);

    /// <summary>
    /// Creates a temporary duration from an already converted tick count.
    /// </summary>
    public static EffectDuration TemporaryTicks(int ticks)
    {
        if (ticks < 1)
            throw new ArgumentOutOfRangeException(nameof(ticks), "Temporary effects must last at least one tick.");

        return new EffectDuration(EffectDurationType.Temporary, ticks);
    }

    /// <summary>
    /// Creates a temporary duration from authored seconds using ceiling conversion to network ticks.
    /// </summary>
    public static EffectDuration TemporarySeconds(float seconds, int ticksPerSecond = 30)
    {
        if (seconds < 0.1f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Authored effect durations must be at least 0.1 seconds.");

        return new EffectDuration(EffectDurationType.Temporary, Math.Max(3, (int)MathF.Ceiling(seconds * ticksPerSecond)));
    }
}
