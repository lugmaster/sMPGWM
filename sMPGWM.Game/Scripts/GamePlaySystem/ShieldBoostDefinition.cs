namespace sMPGWM.Scripts.GamePlaySystem;

public sealed record ShieldBoostDefinition(string AbilityId, string DisplayName, string Icon, string BehaviourId,
    long DurationTicks, double Flat, double Fraction, float Range);
