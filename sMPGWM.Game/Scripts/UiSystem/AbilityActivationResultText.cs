using sMPGWM.Scripts.AbilitySystem;

namespace sMPGWM.Scripts.UiSystem;

public static class AbilityActivationResultText
{
    public static string Format(AbilityActivationResult result) => result switch
    {
        AbilityActivationResult.SessionEnded => "Rejected: session ended",
        AbilityActivationResult.UnsupportedAbility => "Rejected: unsupported ability",
        AbilityActivationResult.AbilityNotGranted => "Rejected: ability not granted",
        AbilityActivationResult.BehaviourLimit => "Rejected: behaviour limit",
        AbilityActivationResult.InvalidGameplayValues => "Rejected: invalid gameplay values",
        AbilityActivationResult.Accepted => "Accepted",
        AbilityActivationResult.CasterIsDead => "Rejected: caster is dead",
        AbilityActivationResult.BlockedByCondition => "Rejected: blocked by condition",
        AbilityActivationResult.TargetIsNotALivingAlly => "Rejected: target is not a living ally",
        AbilityActivationResult.OutOfRange => "Rejected: out of range",
        AbilityActivationResult.CasterChanged => "Rejected: caster changed",
        AbilityActivationResult.UnknownAbility => "Rejected: unknown ability",
        AbilityActivationResult.AbilitySourceChanged => "Rejected: ability source changed",
        AbilityActivationResult.Pending => "Pending",
        AbilityActivationResult.OldRequest => "Rejected: old request",
        AbilityActivationResult.RateLimit => "Rejected: rate limit",
        AbilityActivationResult.Busy => "Rejected: busy",
        AbilityActivationResult.SendFailed => "Rejected: ability request send failed",
        _ => "Rejected: unknown result"
    };
}
