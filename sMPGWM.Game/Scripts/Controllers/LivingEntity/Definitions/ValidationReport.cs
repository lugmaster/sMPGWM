using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// One deterministic validation result produced for an effect definition.
/// </summary>
public readonly struct ValidationReport(
    int effectDefinitionId,
    ValidationSeverity severity,
    ValidationCode code,
    string message,
    int? referencedChildEffectId = null,
    StatType? referencedStat = null,
    LivingEntityCondition referencedCondition = LivingEntityCondition.None)
{
    /// <summary>
    /// Gets the effect definition id that produced the report.
    /// </summary>
    public int EffectDefinitionId { get; } = effectDefinitionId;

    /// <summary>
    /// Gets the report severity.
    /// </summary>
    public ValidationSeverity Severity { get; } = severity;

    /// <summary>
    /// Gets the stable validation code.
    /// </summary>
    public ValidationCode Code { get; } = code;

    /// <summary>
    /// Gets the human-readable validation message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the optional child effect id referenced by the report.
    /// </summary>
    public int? ReferencedChildEffectId { get; } = referencedChildEffectId;

    /// <summary>
    /// Gets the optional stat referenced by the report.
    /// </summary>
    public StatType? ReferencedStat { get; } = referencedStat;

    /// <summary>
    /// Gets the optional condition referenced by the report.
    /// </summary>
    public LivingEntityCondition ReferencedCondition { get; } = referencedCondition;
}
