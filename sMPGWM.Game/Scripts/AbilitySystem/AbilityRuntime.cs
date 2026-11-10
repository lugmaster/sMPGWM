using System;
using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityRuntime : IDisposable
{
    private readonly Dictionary<string, AbilityExecutionPlan> _plans = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _unsupported = new(StringComparer.Ordinal);
    private long _nextInstance;
    private bool _disposed;
    public GameplayCatalogues Definitions { get; }

    public AbilityRuntime(GameplayCatalogues definitions)
    {
        Definitions = definitions;
        foreach (var ability in definitions.Abilities.GetAll())
        {
            var reason = Compile(ability, out var plan);
            if (reason == null) _plans.Add(ability.Id, plan!);
            else _unsupported.Add(ability.Id, reason);
        }
    }

    public bool IsSupported(string abilityId, out string reason)
    {
        if (_plans.ContainsKey(abilityId)) { reason = ""; return true; }
        reason = _unsupported.TryGetValue(abilityId, out var unsupported) ? unsupported : $"Unknown ability '{abilityId}'.";
        return false;
    }

    public string Activate(string abilityId, in AbilityActivationContext context, long tick)
    {
        if (_disposed) return "Rejected: session ended";
        if (!_plans.TryGetValue(abilityId, out var plan))
            return _unsupported.ContainsKey(abilityId) ? "Rejected: unsupported ability" : "Rejected: ability not granted";
        if (!context.Caster.Grants.Contains(abilityId) && plan.Definition.Availability != AbilityAvailability.AllShips)
            return "Rejected: ability not granted";
        foreach (var condition in context.CasterConditions)
            foreach (var blocked in plan.Definition.BlockedByConditions)
                if (condition == blocked) return "Rejected: blocked by condition";
        if (!context.TargetIsLivingAlly ||
            plan.Targeting.Relation == TargetRelation.Self && context.Target != context.Caster)
            return "Rejected: target is not a living ally";
        if (!double.IsFinite(context.DistanceSquared) || context.DistanceSquared < 0 ||
            plan.Targeting.MaxRange is { } range && context.DistanceSquared > range * range)
            return "Rejected: out of range";
        try
        {
            // Reserve identity before publishing state changes; observers may activate another ability.
            var instance = checked(++_nextInstance);
            if (!context.Target.ApplyBehaviour(plan.Behaviour, tick, instance, out _))
                return "Rejected: behaviour limit";
            return "Accepted";
        }
        catch (Exception error) when (error is OverflowException or InvalidOperationException or ArgumentOutOfRangeException)
        { return "Rejected: invalid gameplay values"; }
    }

    private string? Compile(AbilityDefinition ability, out AbilityExecutionPlan? plan)
    {
        plan = null;
        var path = $"Ability '{ability.Id}'";
        if (ability.Id.Length > 64) return path + ": ID exceeds the 64-character protocol bound.";
        if (ability.Activation != ActivationKind.Instant || ability.CooldownMs != 0 || ability.Costs.Count != 0)
            return path + ": only instant, zero-cost, zero-cooldown activation is implemented.";
        var targeting = Definitions.Targeting.GetById(ability.TargetingId);
        if (targeting.Relation is not (TargetRelation.Self or TargetRelation.SelfOrAlly) ||
            targeting.LifeState != TargetLifeState.Alive || targeting.MechanicalOnly || targeting.Radius != null)
            return path + $" → targeting '{targeting.Id}': targeting mode is not implemented.";
        if (targeting.MaxRange is { } range && (!double.IsFinite(range * range) || range <= 0))
            return path + ": range is outside supported numeric bounds.";
        if (Definitions.Effects.GetById(ability.EffectId) is not ApplyBehaviourEffect apply)
            return path + $" → effect '{ability.EffectId}': operation is not implemented.";
        var behaviour = Definitions.Behaviours.GetById(apply.BehaviourId);
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

    public void Dispose()
    {
        _disposed = true;
        _plans.Clear();
        _unsupported.Clear();
    }
}
