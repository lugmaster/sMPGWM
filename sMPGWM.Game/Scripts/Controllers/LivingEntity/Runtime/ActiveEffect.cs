using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Runtime flags stored on active effects while they are processed by the state engine.
/// </summary>
[Flags]
public enum EffectRuntimeFlags
{
    None = 0,
    ExecutedOnce = 1 << 0,
    PendingRemoval = 1 << 1,
    BlockedLastExecution = 1 << 2
}

/// <summary>
/// Small runtime effect state that references immutable effect definition data by id.
/// </summary>
public struct ActiveEffect
{
    /// <summary>
    /// Stable runtime id for this active effect instance.
    /// </summary>
    public long RuntimeEffectId;

    /// <summary>
    /// Immutable effect definition id resolved by the registry.
    /// </summary>
    public int EffectDefinitionId;

    /// <summary>
    /// Concrete source id that produced this effect.
    /// </summary>
    public long SourceId;

    /// <summary>
    /// Owner or originator id behind the source.
    /// </summary>
    public long OriginId;

    /// <summary>
    /// Target living-entity id for this effect.
    /// </summary>
    public long TargetEntityId;

    /// <summary>
    /// Network tick when the effect was accepted.
    /// </summary>
    public long AppliedNetworkTick;

    /// <summary>
    /// Network tick for which the effect was submitted.
    /// </summary>
    public long EffectNetworkTick;

    /// <summary>
    /// Last network tick on which this effect executed.
    /// </summary>
    public long LastExecutedNetworkTick;

    /// <summary>
    /// Network tick at which a temporary effect expires.
    /// </summary>
    public long ExpireNetworkTick;

    /// <summary>
    /// Current combined stack count for non-independent stack representations.
    /// </summary>
    public int StackCount;

    /// <summary>
    /// Runtime id of the parent effect that queued this child effect.
    /// </summary>
    public long ParentRuntimeEffectId;

    /// <summary>
    /// Child-effect depth used to bound effect chains.
    /// </summary>
    public int ChildDepth;

    /// <summary>
    /// Runtime state flags used during processing and compaction.
    /// </summary>
    public EffectRuntimeFlags RuntimeFlags;

    /// <summary>
    /// Stable insertion sequence used for deterministic ordering.
    /// </summary>
    public long InsertionSequence;

    /// <summary>
    /// Stable child sequence within the parent effect definition.
    /// </summary>
    public int ChildSequence;

    /// <summary>
    /// Gets whether this effect has a non-zero runtime id.
    /// </summary>
    public bool HasRuntimeId => RuntimeEffectId != 0;
}
