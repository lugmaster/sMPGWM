namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Identifies which stat value bucket an effect target modifies.
/// </summary>
public enum StatValueKind
{
    Current,
    Max
}

/// <summary>
/// Identifies whether an effect raises or reduces a stat value.
/// </summary>
public enum StatManipulation
{
    Raise,
    Reduce
}

/// <summary>
/// Identifies whether an effect applies a flat or additive percentage value.
/// </summary>
public enum StatCalculation
{
    Flat,
    Percentage
}

/// <summary>
/// Identifies the fast or slow simulation lane used by an effect.
/// </summary>
public enum SimulationLane
{
    Fast,
    Slow
}

/// <summary>
/// Identifies the gameplay system category that produced an effect.
/// </summary>
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

/// <summary>
/// Identifies when an active effect executes.
/// </summary>
public enum EffectTriggerType
{
    Immediate,
    OverTime,
    OnExpire,
    OnConditionPresent,
    OnStatReduced,
    OnStatRaised
}

/// <summary>
/// Identifies how long an active effect remains active.
/// </summary>
public enum EffectDurationType
{
    Once,
    Temporary,
    Permanent
}

/// <summary>
/// Identifies what happens when an effect is blocked.
/// </summary>
public enum EffectBlockPolicy
{
    ConsumeOnFail,
    Retry
}

/// <summary>
/// Identifies runtime source fields used for duplicate stack identity.
/// </summary>
public enum EffectStackScope
{
    SameSource,
    SameSourceAndOrigin
}

/// <summary>
/// Identifies whether duplicate stack-family effects stack, replace, or discard.
/// </summary>
public enum EffectStackPolicy
{
    Stack,
    Replace,
    Discard
}

/// <summary>
/// Identifies how accepted stacks are represented and refreshed.
/// </summary>
public enum EffectStackBehavior
{
    IndependentStacks,
    RefreshDuration,
    AddStackRefreshDuration,
    AddStackNoRefresh
}

/// <summary>
/// Identifies deterministic behavior when max stacks are reached.
/// </summary>
public enum MaxStackOverflowPolicy
{
    RejectNewest,
    ReplaceOldest,
    RefreshOldest,
    ReplaceWeakest
}

/// <summary>
/// Identifies where an effect is visible to player-facing UI.
/// </summary>
public enum EffectVisibility
{
    Hud,
    Menu,
    Internal
}

/// <summary>
/// Identifies source/origin matching policy for effect-removal rules.
/// </summary>
public enum SourceMatchPolicy
{
    AnySource,
    SameSource,
    SameSourceAndOrigin,
    ExplicitSourceId,
    ExplicitSourceAndOrigin
}

/// <summary>
/// Identifies validation report severity.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Identifies stable effect-definition validation failures.
/// </summary>
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

/// <summary>
/// Identifies causal events emitted while processing state-engine effects.
/// </summary>
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
