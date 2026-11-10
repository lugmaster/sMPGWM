namespace sMPGWM.Scripts.AbilitySystem;

public sealed record RemoveBehavioursEffect(string Id, string DisplayName, string Description, BehaviourSelector Selector, int? MaxCount, BehaviourRemovalOrder Order)
    : EffectDefinition(Id, DisplayName, Description);
