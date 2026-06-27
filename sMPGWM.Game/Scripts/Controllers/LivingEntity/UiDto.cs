using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class UiDto
{
    public long EntityId { get; init; }
    public long NetworkTick { get; init; }
    public UiStatDto[] Stats { get; init; } = [];
    public LivingEntityCondition Conditions { get; init; }
    public UiEffectDto[] VisibleEffects { get; init; } = [];
}

public readonly struct UiStatDto(StatType statType, float currentValue, float maxValue)
{
    public StatType StatType { get; } = statType;
    public float CurrentValue { get; } = currentValue;
    public float MaxValue { get; } = maxValue;
}

public readonly struct UiEffectDto(
    long runtimeEffectId,
    int effectDefinitionId,
    long sourceId,
    long originId,
    string name,
    string description,
    int stackCount,
    long remainingTicks,
    EffectTriggerType triggerType,
    StatCalculation? calculation,
    StatType? affectedStat,
    LivingEntityCondition classificationConditions,
    LivingEntityCondition grantedConditions,
    bool isBlocked,
    EffectDuration duration,
    int tickInterval,
    StatManipulation? manipulation,
    float valuePerTick)
{
    public long RuntimeEffectId { get; } = runtimeEffectId;
    public int EffectDefinitionId { get; } = effectDefinitionId;
    public long SourceId { get; } = sourceId;
    public long OriginId { get; } = originId;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public int StackCount { get; } = stackCount;
    public long RemainingTicks { get; } = remainingTicks;
    public EffectTriggerType TriggerType { get; } = triggerType;
    public StatCalculation? Calculation { get; } = calculation;
    public StatType? AffectedStat { get; } = affectedStat;
    public LivingEntityCondition ClassificationConditions { get; } = classificationConditions;
    public LivingEntityCondition GrantedConditions { get; } = grantedConditions;
    public bool IsBlocked { get; } = isBlocked;
    public EffectDuration Duration { get; } = duration;
    public int TickInterval { get; } = tickInterval;
    public StatManipulation? Manipulation { get; } = manipulation;
    public float ValuePerTick { get; } = valuePerTick;
}
