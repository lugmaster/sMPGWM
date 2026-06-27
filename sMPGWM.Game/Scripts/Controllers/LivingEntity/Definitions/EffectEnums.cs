namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public enum StatValueKind
{
    Current,
    Max
}

public enum StatManipulation
{
    Raise,
    Reduce
}

public enum StatCalculation
{
    Flat,
    Percentage
}

public enum SimulationLane
{
    Fast,
    Slow
}

public enum EffectSourceType
{
    Skills,
    Items,
    UsableItems,
    Talents,
    Weapons,
    ShipSystems,
    Environment,
    OtherLivingEntities
}

public enum EffectTriggerType
{
    Immediate,
    OverTime,
    OnExpire,
    OnConditionPresent,
    OnStatReduced,
    OnStatRaised
}

public enum EffectDurationType
{
    Once,
    Temporary,
    Permanent
}

public enum EffectBlockPolicy
{
    ConsumeOnFail,
    Retry
}

public enum EffectStackScope
{
    SameSource,
    SameSourceAndOrigin
}

public enum EffectStackPolicy
{
    Stack,
    Replace,
    Discard
}

public enum EffectStackBehavior
{
    IndependentStacks,
    RefreshDuration,
    AddStackRefreshDuration,
    AddStackNoRefresh
}

public enum MaxStackOverflowPolicy
{
    RejectNewest,
    ReplaceOldest,
    RefreshOldest,
    ReplaceWeakest
}

public enum EffectVisibility
{
    Hud,
    Menu,
    Internal
}

public enum SourceMatchPolicy
{
    AnySource,
    SameSource,
    SameSourceAndOrigin,
    ExplicitSourceId,
    ExplicitSourceAndOrigin
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

public enum ValidationCode
{
    EmptyEffect,
    InvalidMaxStacks,
    ReplaceWeakestRequiresStrengthComparison,
    PermanentOnExpire,
    ChildReferenceMissing,
    ChildDepthExceeded,
    ChildCycle,
    DurationTooShort,
    InvalidConditionHierarchy
}

public enum CausalRecordType
{
    EffectExecuted,
    EffectBlocked,
    EffectExpired,
    EffectRemoved,
    StackAdded,
    StackRefreshed,
    StackReplaced,
    ChildEffectQueued,
    ChildEffectRejected,
    ConditionAddAccepted,
    ConditionRemoveAccepted,
    StatModificationRecord,
    CapacityOverflow
}
