using System;
using sMPGWM.Scripts.GameplayDataSystem;
namespace sMPGWM.Scripts.AbilitySystem;
internal static class AbilityPreparer
{
    internal static string? Prepare(GameplayCatalogues definitions, AbilityDefinition ability, out AbilityExecutionPlan? plan)
    {
        plan = null;
        var path = $"Ability '{ability.Id}'";
        if (ability.Id.Length > 64) return path + ": ID exceeds the 64-character protocol bound.";
        if (ability.Activation != ActivationKind.Instant || ability.CooldownMs != 0 || ability.Costs.Count != 0)
            return path + ": only instant, zero-cost, zero-cooldown activation is implemented.";
        var targeting = definitions.Targeting.GetById(ability.TargetingId);
        if (targeting.Relation is not (TargetRelation.Self or TargetRelation.SelfOrAlly) ||
            targeting.LifeState != TargetLifeState.Alive || targeting.MechanicalOnly || targeting.Radius != null)
            return path + $" → targeting '{targeting.Id}': targeting mode is not implemented.";
        if (targeting.MaxRange is { } range && (!double.IsFinite(range * range) || range <= 0))
            return path + ": range is outside supported numeric bounds.";
        if (definitions.Effects.GetById(ability.EffectId) is not ApplyBehaviourEffect apply)
            return path + $" → effect '{ability.EffectId}': operation is not implemented.";
        var behaviour = definitions.Behaviours.GetById(apply.BehaviourId);
        path += $" → effect '{apply.Id}' → behaviour '{behaviour.Id}'";
        if (behaviour.Id.Length > 64) return path + ": ID exceeds the 64-character protocol bound.";
        if (behaviour.Lifetime.Kind != BehaviourLifetimeKind.Timed ||
            behaviour.Lifetime.DurationMs is not { } duration || duration <= 0 || duration > 3600000 ||
            behaviour.Stacking.Policy != StackingPolicy.RefreshDuration || behaviour.Stacking.Scope != StackingScope.PerTargetDefinition)
            return path + ": lifetime or stacking policy is not implemented.";
        if (behaviour.OnApplyEffectId != null || behaviour.OnExpireEffectId != null || behaviour.OnRemove != null ||
            behaviour.LethalProtection != null || behaviour.PeriodicAccumulator != null ||
            behaviour.GrantedConditions.Count != 0 || behaviour.GrantedConditionsAfterFirstTick.Count != 0 ||
            behaviour.BlockedByConditions.Count != 0 || behaviour.BlockedPolicy != null ||
            behaviour.Interrupts.Count != 0 || behaviour.DisabledCapabilities.Count != 0)
            return path + ": hooks, condition contributions, and reactions are not implemented.";
        var flat = 0d; var fraction = 0d;
        foreach (var modifier in behaviour.Modifiers)
        {
            if (modifier.StatId != "shield" || modifier.ValueKind != ModifierValueKind.Maximum)
                return path + ": only Shield maximum modifiers are implemented.";
            switch (modifier.Operation)
            {
                case ModifierOperation.AddFlat: flat += modifier.Value; break;
                case ModifierOperation.AddBaselineFraction: fraction += modifier.Value; break;
                default: return path + ": modifier operation is not implemented.";
            }
        }
        if (!double.IsFinite(flat) || !double.IsFinite(fraction)) return path + ": modifier sum overflows.";
        plan = new(ability, targeting, new(behaviour, (long)Math.Ceiling(duration * 60 / 1000), flat, fraction));
        return null;
    }

}
