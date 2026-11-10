using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record BehaviourClassificationDefinition(string Id, string DisplayName, string Description)
    : GameplayDefinition(Id, DisplayName, Description, null);
