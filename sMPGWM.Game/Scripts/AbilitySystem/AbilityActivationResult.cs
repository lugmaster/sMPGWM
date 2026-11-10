namespace sMPGWM.Scripts.AbilitySystem;

public enum AbilityActivationResult
{
    SessionEnded = 0,
    UnsupportedAbility = 1,
    AbilityNotGranted = 2,
    BehaviourLimit = 3,
    InvalidGameplayValues = 4,
    Accepted = 5,
    CasterIsDead = 6,
    BlockedByCondition = 7,
    TargetIsNotALivingAlly = 8,
    OutOfRange = 9,
    CasterChanged = 10,
    UnknownAbility = 11,
    AbilitySourceChanged = 12,
    Pending = 13,
    OldRequest = 14,
    RateLimit = 15,
    Busy = 16,
    SendFailed = 17,
}
