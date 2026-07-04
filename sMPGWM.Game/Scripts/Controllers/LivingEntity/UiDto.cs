using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Pull-based read-only data transfer object for living-entity UI inspection.
/// </summary>
public sealed class UiDto
{
    /// <summary>
    /// Gets the entity id represented by this UI update.
    /// </summary>
    public long EntityId { get; init; }

    /// <summary>
    /// Gets the network tick represented by this UI update.
    /// </summary>
    public long NetworkTick { get; init; }

    /// <summary>
    /// Gets current and max values for every stat.
    /// </summary>
    public UiStatDto[] Stats { get; init; } = [];

    /// <summary>
    /// Gets the current effective condition flags.
    /// </summary>
    public LivingEntityCondition Conditions { get; init; }

    /// <summary>
    /// Gets visible active effects for HUD and menu display.
    /// </summary>
    public UiEffectDto[] VisibleEffects { get; init; } = [];
}

/// <summary>
/// UI-facing current and max values for one stat.
/// </summary>
public readonly struct UiStatDto(StatType statType, float currentValue, float maxValue)
{
    /// <summary>
    /// Gets the stat represented by this UI stat row.
    /// </summary>
    public StatType StatType { get; } = statType;

    /// <summary>
    /// Gets the current value to display.
    /// </summary>
    public float CurrentValue { get; } = currentValue;

    /// <summary>
    /// Gets the maximum value to display.
    /// </summary>
    public float MaxValue { get; } = maxValue;
}

/// <summary>
/// UI-facing description of one HUD- or menu-visible active effect.
/// </summary>
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
    /// <summary>
    /// Gets the runtime effect id.
    /// </summary>
    public long RuntimeEffectId { get; } = runtimeEffectId;

    /// <summary>
    /// Gets the effect definition id.
    /// </summary>
    public int EffectDefinitionId { get; } = effectDefinitionId;

    /// <summary>
    /// Gets the concrete source id.
    /// </summary>
    public long SourceId { get; } = sourceId;

    /// <summary>
    /// Gets the originator id behind the source.
    /// </summary>
    public long OriginId { get; } = originId;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the display description.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the current stack count.
    /// </summary>
    public int StackCount { get; } = stackCount;

    /// <summary>
    /// Gets remaining duration in ticks, or -1 for permanent effects.
    /// </summary>
    public long RemainingTicks { get; } = remainingTicks;

    /// <summary>
    /// Gets the effect trigger type.
    /// </summary>
    public EffectTriggerType TriggerType { get; } = triggerType;

    /// <summary>
    /// Gets the target calculation type when the effect modifies a stat.
    /// </summary>
    public StatCalculation? Calculation { get; } = calculation;

    /// <summary>
    /// Gets the affected stat when the effect modifies a stat.
    /// </summary>
    public StatType? AffectedStat { get; } = affectedStat;

    /// <summary>
    /// Gets effect classification conditions.
    /// </summary>
    public LivingEntityCondition ClassificationConditions { get; } = classificationConditions;

    /// <summary>
    /// Gets conditions granted or requested by the effect.
    /// </summary>
    public LivingEntityCondition GrantedConditions { get; } = grantedConditions;

    /// <summary>
    /// Gets whether the effect is currently blocked by target conditions.
    /// </summary>
    public bool IsBlocked { get; } = isBlocked;

    /// <summary>
    /// Gets the configured effect duration.
    /// </summary>
    public EffectDuration Duration { get; } = duration;

    /// <summary>
    /// Gets the periodic execution interval in ticks.
    /// </summary>
    public int TickInterval { get; } = tickInterval;

    /// <summary>
    /// Gets whether the effect raises or reduces its target stat.
    /// </summary>
    public StatManipulation? Manipulation { get; } = manipulation;

    /// <summary>
    /// Gets the configured stat impact value.
    /// </summary>
    public float ValuePerTick { get; } = valuePerTick;
}
