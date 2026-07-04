using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Describes when an active effect should execute during state-engine ticks.
/// </summary>
public readonly struct EffectTrigger(
    EffectTriggerType type,
    int intervalTicks = 0,
    LivingEntityCondition requiredCondition = LivingEntityCondition.None,
    StatType trackedStat = StatType.Health)
{
    /// <summary>
    /// Gets the trigger type.
    /// </summary>
    public EffectTriggerType Type { get; } = type;

    /// <summary>
    /// Gets the execution interval in network ticks for periodic triggers.
    /// </summary>
    public int IntervalTicks { get; } = intervalTicks;

    /// <summary>
    /// Gets the condition required by condition-present triggers.
    /// </summary>
    public LivingEntityCondition RequiredCondition { get; } = requiredCondition;

    /// <summary>
    /// Gets the stat observed by stat-raised and stat-reduced triggers.
    /// </summary>
    public StatType TrackedStat { get; } = trackedStat;

    /// <summary>
    /// Gets the default immediate trigger.
    /// </summary>
    public static EffectTrigger Immediate { get; } = new(EffectTriggerType.Immediate);
}
