using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Describes deterministic matching rules for active-effect removal requests.
/// </summary>
public sealed class EffectRemovalRule
{
    /// <summary>
    /// Gets the optional exact effect definition id that candidates must match.
    /// </summary>
    public int? MatchEffectDefinitionId { get; init; }

    /// <summary>
    /// Gets the optional stack group id that candidates must match.
    /// </summary>
    public int? MatchStackGroupId { get; init; }

    /// <summary>
    /// Gets the source/origin policy used when matching candidates.
    /// </summary>
    public SourceMatchPolicy SourceMatchPolicy { get; init; } = SourceMatchPolicy.AnySource;

    /// <summary>
    /// Gets the explicit source id required by explicit source policies.
    /// </summary>
    public long? MatchSourceId { get; init; }

    /// <summary>
    /// Gets the explicit origin id required by explicit origin policies.
    /// </summary>
    public long? MatchOriginId { get; init; }

    /// <summary>
    /// Gets classification conditions required on candidate effects.
    /// </summary>
    public LivingEntityCondition RequiredConditions { get; init; } = LivingEntityCondition.None;

    /// <summary>
    /// Gets classification conditions that reject candidate effects.
    /// </summary>
    public LivingEntityCondition ExcludedConditions { get; init; } = LivingEntityCondition.None;

    /// <summary>
    /// Gets whether condition matching includes descendant conditions.
    /// </summary>
    public bool IncludeChildConditions { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of matching active effects to remove; zero means no explicit cap.
    /// </summary>
    public int MaxRemoveCount { get; init; }

    /// <summary>
    /// Gets whether removal selects oldest matching effects before newest matching effects.
    /// </summary>
    public bool RemoveOldestFirst { get; init; } = true;
}
