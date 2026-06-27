using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public readonly struct EffectDuration(EffectDurationType type, int ticks = 0)
{
    public EffectDurationType Type { get; } = type;
    public int Ticks { get; } = ticks;

    public static EffectDuration Once { get; } = new(EffectDurationType.Once);
    public static EffectDuration Permanent { get; } = new(EffectDurationType.Permanent);

    public static EffectDuration TemporaryTicks(int ticks)
    {
        if (ticks < 1)
            throw new ArgumentOutOfRangeException(nameof(ticks), "Temporary effects must last at least one tick.");

        return new EffectDuration(EffectDurationType.Temporary, ticks);
    }

    public static EffectDuration TemporarySeconds(float seconds, int ticksPerSecond = 30)
    {
        if (seconds < 0.1f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Authored effect durations must be at least 0.1 seconds.");

        return new EffectDuration(EffectDurationType.Temporary, Math.Max(3, (int)MathF.Ceiling(seconds * ticksPerSecond)));
    }
}
