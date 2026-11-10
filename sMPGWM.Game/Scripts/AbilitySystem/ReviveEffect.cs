namespace sMPGWM.Scripts.AbilitySystem;

public sealed record ReviveEffect(string Id, string DisplayName, string Description, double Health)
    : EffectDefinition(Id, DisplayName, Description);
