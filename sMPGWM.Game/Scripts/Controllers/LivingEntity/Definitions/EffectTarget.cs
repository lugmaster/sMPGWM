using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Describes the single stat value modified by an effect definition.
/// </summary>
public readonly struct EffectTarget(
    StatType targetStat,
    StatValueKind targetValueKind,
    StatManipulation manipulation,
    StatCalculation calculation,
    float value)
{
    /// <summary>
    /// Gets the stat affected by the target.
    /// </summary>
    public StatType TargetStat { get; } = targetStat;

    /// <summary>
    /// Gets whether current or max value is affected.
    /// </summary>
    public StatValueKind TargetValueKind { get; } = targetValueKind;

    /// <summary>
    /// Gets whether the value raises or reduces the target.
    /// </summary>
    public StatManipulation Manipulation { get; } = manipulation;

    /// <summary>
    /// Gets whether the value is flat or additive percentage.
    /// </summary>
    public StatCalculation Calculation { get; } = calculation;

    /// <summary>
    /// Gets the authored magnitude applied by the target.
    /// </summary>
    public float Value { get; } = value;
}
