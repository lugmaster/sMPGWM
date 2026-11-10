using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ConditionSystem;

public sealed record ConditionDefinition(string Id, string DisplayName, string Description)
    : GameplayDefinition(Id, DisplayName, Description, null);
