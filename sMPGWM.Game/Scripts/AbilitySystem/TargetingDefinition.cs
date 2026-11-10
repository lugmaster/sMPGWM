using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record TargetingDefinition(
    string Id, string DisplayName, string Description, TargetRelation Relation, TargetLifeState LifeState,
    bool MechanicalOnly, double? MaxRange, double? Radius)
    : GameplayDefinition(Id, DisplayName, Description, null);
