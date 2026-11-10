using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.CombatSystem;

public sealed record DamageSourceDefinition(string Id, string DisplayName, string Description)
    : GameplayDefinition(Id, DisplayName, Description, null);
