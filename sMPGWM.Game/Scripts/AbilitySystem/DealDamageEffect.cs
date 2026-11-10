namespace sMPGWM.Scripts.AbilitySystem;

public sealed record DealDamageEffect(string Id, string DisplayName, string Description, string DamageSourceId, double Amount)
    : EffectDefinition(Id, DisplayName, Description);
