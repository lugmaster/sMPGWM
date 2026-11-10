using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public abstract record EffectDefinition(string Id, string DisplayName, string Description)
    : GameplayDefinition(Id, DisplayName, Description, null);
