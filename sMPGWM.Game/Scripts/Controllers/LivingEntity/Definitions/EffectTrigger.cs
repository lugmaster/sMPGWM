using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public readonly struct EffectTrigger(
    EffectTriggerType type,
    int intervalTicks = 0,
    LivingEntityCondition requiredCondition = LivingEntityCondition.None,
    StatType trackedStat = StatType.Health)
{
    public EffectTriggerType Type { get; } = type;
    public int IntervalTicks { get; } = intervalTicks;
    public LivingEntityCondition RequiredCondition { get; } = requiredCondition;
    public StatType TrackedStat { get; } = trackedStat;

    public static EffectTrigger Immediate { get; } = new(EffectTriggerType.Immediate);
}
