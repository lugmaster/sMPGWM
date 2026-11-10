namespace sMPGWM.Scripts.AbilitySystem;

public sealed record SearchAreaEffect(string Id, string DisplayName, string Description, string TargetingId, string EffectId)
    : EffectDefinition(Id, DisplayName, Description);
