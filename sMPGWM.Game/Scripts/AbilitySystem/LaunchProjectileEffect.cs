namespace sMPGWM.Scripts.AbilitySystem;

public sealed record LaunchProjectileEffect(string Id, string DisplayName, string Description, string ProjectileId, string ImpactEffectId)
    : EffectDefinition(Id, DisplayName, Description);
