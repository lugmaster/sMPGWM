using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public readonly struct ValidationReport(
    int effectDefinitionId,
    ValidationSeverity severity,
    ValidationCode code,
    string message,
    int? referencedChildEffectId = null,
    StatType? referencedStat = null,
    LivingEntityCondition referencedCondition = LivingEntityCondition.None)
{
    public int EffectDefinitionId { get; } = effectDefinitionId;
    public ValidationSeverity Severity { get; } = severity;
    public ValidationCode Code { get; } = code;
    public string Message { get; } = message;
    public int? ReferencedChildEffectId { get; } = referencedChildEffectId;
    public StatType? ReferencedStat { get; } = referencedStat;
    public LivingEntityCondition ReferencedCondition { get; } = referencedCondition;
}
