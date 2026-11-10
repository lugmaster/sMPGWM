namespace sMPGWM.Scripts.AbilitySystem;

public sealed record ApplyBehaviourEffect(string Id, string DisplayName, string Description, string BehaviourId)
    : EffectDefinition(Id, DisplayName, Description);
