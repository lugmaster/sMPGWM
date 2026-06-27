using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

[Flags]
public enum EffectRuntimeFlags
{
    None = 0,
    ExecutedOnce = 1 << 0,
    PendingRemoval = 1 << 1,
    BlockedLastExecution = 1 << 2
}

public struct ActiveEffect
{
    public long RuntimeEffectId;
    public int EffectDefinitionId;
    public long SourceId;
    public long OriginId;
    public long TargetEntityId;
    public long AppliedNetworkTick;
    public long EffectNetworkTick;
    public long LastExecutedNetworkTick;
    public long ExpireNetworkTick;
    public int StackCount;
    public long ParentRuntimeEffectId;
    public int ChildDepth;
    public EffectRuntimeFlags RuntimeFlags;
    public long InsertionSequence;
    public int ChildSequence;

    public bool HasRuntimeId => RuntimeEffectId != 0;
}
