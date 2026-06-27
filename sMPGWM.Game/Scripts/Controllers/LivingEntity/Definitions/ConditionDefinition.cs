using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public readonly struct ConditionDefinition(
    LivingEntityCondition condition,
    LivingEntityCondition parentCondition,
    bool isDesignerFacing,
    bool isRuntimeStateCondition,
    bool isEffectClassificationCondition,
    string description)
{
    public LivingEntityCondition Condition { get; } = condition;
    public LivingEntityCondition ParentCondition { get; } = parentCondition;
    public bool IsDesignerFacing { get; } = isDesignerFacing;
    public bool IsRuntimeStateCondition { get; } = isRuntimeStateCondition;
    public bool IsEffectClassificationCondition { get; } = isEffectClassificationCondition;
    public string Description { get; } = description;
}
