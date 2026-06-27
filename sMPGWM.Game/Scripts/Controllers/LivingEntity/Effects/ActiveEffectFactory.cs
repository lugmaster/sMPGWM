using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Effects;

public static class ActiveEffectFactory
{
    public static ActiveEffect Create(
        int effectDefinitionId,
        long runtimeEffectId,
        long sourceId,
        long originId,
        long targetEntityId,
        long networkTick,
        long parentRuntimeEffectId = 0,
        int childDepth = 0,
        int childSequence = 0)
    {
        var definition = EffectDefinitionRegistry.GetRequired(effectDefinitionId);
        return Create(definition, runtimeEffectId, sourceId, originId, targetEntityId, networkTick,
            parentRuntimeEffectId, childDepth, childSequence);
    }

    public static ActiveEffect Create(
        EffectDefinition definition,
        long runtimeEffectId,
        long sourceId,
        long originId,
        long targetEntityId,
        long networkTick,
        long parentRuntimeEffectId = 0,
        int childDepth = 0,
        int childSequence = 0)
    {
        var expireTick = definition.Duration.Type == EffectDurationType.Temporary
            ? networkTick + definition.Duration.Ticks
            : long.MaxValue;

        return new ActiveEffect
        {
            RuntimeEffectId = runtimeEffectId,
            EffectDefinitionId = definition.EffectDefinitionId,
            SourceId = sourceId,
            OriginId = originId,
            TargetEntityId = targetEntityId,
            AppliedNetworkTick = networkTick,
            EffectNetworkTick = networkTick,
            LastExecutedNetworkTick = long.MinValue,
            ExpireNetworkTick = expireTick,
            StackCount = 1,
            ParentRuntimeEffectId = parentRuntimeEffectId,
            ChildDepth = childDepth,
            RuntimeFlags = EffectRuntimeFlags.None,
            ChildSequence = childSequence
        };
    }
}
