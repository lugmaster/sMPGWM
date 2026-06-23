using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public readonly struct StatModifierContribution(
    string instanceId,
    string sourceId,
    ModifierSourceType sourceType,
    string displayName,
    ModifierType modifierType,
    float value,
    float? timeRemaining)
{
    public string InstanceId { get; } = instanceId;
    public string SourceId { get; } = sourceId;
    public ModifierSourceType SourceType { get; } = sourceType;
    public string DisplayName { get; } = displayName;
    public ModifierType ModifierType { get; } = modifierType;
    public float Value { get; } = value;
    public float? TimeRemaining { get; } = timeRemaining;
}
