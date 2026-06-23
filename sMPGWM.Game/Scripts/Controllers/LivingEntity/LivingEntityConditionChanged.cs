using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public readonly struct LivingEntityConditionChanged(
    int livingEntityId,
    LivingEntityCondition added,
    LivingEntityCondition removed,
    LivingEntityCondition current,
    string sourceId)
{
    public int LivingEntityId { get; } = livingEntityId;
    public LivingEntityCondition Added { get; } = added;
    public LivingEntityCondition Removed { get; } = removed;
    public LivingEntityCondition Current { get; } = current;
    public string SourceId { get; } = sourceId;
}
