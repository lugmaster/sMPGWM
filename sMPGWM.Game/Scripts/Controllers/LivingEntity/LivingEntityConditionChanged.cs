using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Describes a condition mask change emitted by a living-entity runtime wrapper.
/// </summary>
public readonly struct LivingEntityConditionChanged(
    int livingEntityId,
    LivingEntityCondition added,
    LivingEntityCondition removed,
    LivingEntityCondition current,
    string sourceId)
{
    /// <summary>
    /// Gets the living entity whose effective condition mask changed.
    /// </summary>
    public int LivingEntityId { get; } = livingEntityId;

    /// <summary>
    /// Gets the condition flags added by the change.
    /// </summary>
    public LivingEntityCondition Added { get; } = added;

    /// <summary>
    /// Gets the condition flags removed by the change.
    /// </summary>
    public LivingEntityCondition Removed { get; } = removed;

    /// <summary>
    /// Gets the final effective condition mask after the change.
    /// </summary>
    public LivingEntityCondition Current { get; } = current;

    /// <summary>
    /// Gets the source identifier associated with the condition change.
    /// </summary>
    public string SourceId { get; } = sourceId;
}
