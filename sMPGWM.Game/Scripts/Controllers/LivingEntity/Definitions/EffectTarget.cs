using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public readonly struct EffectTarget(
    StatType targetStat,
    StatValueKind targetValueKind,
    StatManipulation manipulation,
    StatCalculation calculation,
    float value)
{
    public StatType TargetStat { get; } = targetStat;
    public StatValueKind TargetValueKind { get; } = targetValueKind;
    public StatManipulation Manipulation { get; } = manipulation;
    public StatCalculation Calculation { get; } = calculation;
    public float Value { get; } = value;
}
