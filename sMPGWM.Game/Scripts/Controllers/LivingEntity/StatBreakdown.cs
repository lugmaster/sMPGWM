using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public readonly struct StatBreakdown(
    StatType statType,
    float baseValue,
    float currentValue,
    float maxValue,
    StatModifierContribution[] permanentContributions,
    StatModifierContribution[] temporaryContributions,
    StatModifierContribution[] recurringContributions)
{
    public StatType StatType { get; } = statType;
    public float BaseValue { get; } = baseValue;
    public float CurrentValue { get; } = currentValue;
    public float MaxValue { get; } = maxValue;
    public StatModifierContribution[] PermanentContributions { get; } = permanentContributions;
    public StatModifierContribution[] TemporaryContributions { get; } = temporaryContributions;
    public StatModifierContribution[] RecurringContributions { get; } = recurringContributions;
}
