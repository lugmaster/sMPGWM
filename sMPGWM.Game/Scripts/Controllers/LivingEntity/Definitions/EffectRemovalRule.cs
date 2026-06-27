using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public sealed class EffectRemovalRule
{
    public int? MatchEffectDefinitionId { get; init; }
    public int? MatchStackGroupId { get; init; }
    public SourceMatchPolicy SourceMatchPolicy { get; init; } = SourceMatchPolicy.AnySource;
    public long? MatchSourceId { get; init; }
    public long? MatchOriginId { get; init; }
    public LivingEntityCondition RequiredConditions { get; init; } = LivingEntityCondition.None;
    public LivingEntityCondition ExcludedConditions { get; init; } = LivingEntityCondition.None;
    public bool IncludeChildConditions { get; init; } = true;
    public int MaxRemoveCount { get; init; }
    public bool RemoveOldestFirst { get; init; } = true;
}
