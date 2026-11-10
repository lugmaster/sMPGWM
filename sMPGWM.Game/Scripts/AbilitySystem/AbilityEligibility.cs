using System;

namespace sMPGWM.Scripts.AbilitySystem;

public static class AbilityEligibility
{
    public static AbilityActivationResult? Rejection(AbilityDefinition ability, TargetingDefinition targeting,
        in AbilityActivationContext context)
    {
        if (!context.Caster.IsAlive) return AbilityActivationResult.CasterIsDead;
        foreach (var condition in context.Caster.Conditions)
            foreach (var blocked in ability.BlockedByConditions)
                if (condition == blocked) return AbilityActivationResult.BlockedByCondition;
        if (!context.Target.IsAlive || !context.Target.IsAllyOf(context.Caster) ||
            targeting.Relation == TargetRelation.Self && context.Target != context.Caster)
            return AbilityActivationResult.TargetIsNotALivingAlly;
        if (!double.IsFinite(context.DistanceSquared) || context.DistanceSquared < 0 ||
            targeting.MaxRange is { } range && context.DistanceSquared > range * range)
            return AbilityActivationResult.OutOfRange;
        return null;
    }
}
