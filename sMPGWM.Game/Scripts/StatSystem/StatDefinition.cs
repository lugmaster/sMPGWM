using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.StatSystem;

public sealed record StatDefinition(string Id, string DisplayName, string Description, bool IsResource)
    : GameplayDefinition(Id, DisplayName, Description, null);
