using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Immutable metadata describing one living-entity condition and its hierarchy role.
/// </summary>
public readonly struct ConditionDefinition(
    LivingEntityCondition condition,
    LivingEntityCondition parentCondition,
    bool isDesignerFacing,
    bool isRuntimeStateCondition,
    bool isEffectClassificationCondition,
    string description)
{
    /// <summary>
    /// Gets the condition described by this metadata entry.
    /// </summary>
    public LivingEntityCondition Condition { get; } = condition;

    /// <summary>
    /// Gets the optional parent condition used for hierarchy matching.
    /// </summary>
    public LivingEntityCondition ParentCondition { get; } = parentCondition;

    /// <summary>
    /// Gets whether this condition should be exposed to designers.
    /// </summary>
    public bool IsDesignerFacing { get; } = isDesignerFacing;

    /// <summary>
    /// Gets whether this condition can represent runtime entity state.
    /// </summary>
    public bool IsRuntimeStateCondition { get; } = isRuntimeStateCondition;

    /// <summary>
    /// Gets whether this condition can classify active effects.
    /// </summary>
    public bool IsEffectClassificationCondition { get; } = isEffectClassificationCondition;

    /// <summary>
    /// Gets a short human-readable description of the condition.
    /// </summary>
    public string Description { get; } = description;
}
