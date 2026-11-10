using System;
using System.Collections.Generic;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.Items;

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

    public IReadOnlyList<string> GetSupportDiagnostics(IEnumerable<ItemDefinition> items)
    {
        var diagnostics = new List<string>();
        foreach (var reason in _unsupported.Values) diagnostics.Add(reason);
        foreach (var item in items)
        {
            foreach (var ability in item.AbilityIds)
                if (!IsSupported(ability, out var reason)) diagnostics.Add($"Item '{item.Id}': {reason}");
            foreach (var grant in item.BehaviourGrants)
                diagnostics.Add($"Item '{item.Id}' -> behaviour '{grant.BehaviourId}': passive item behaviour grants ({grant.While}, {grant.Lifetime}) are not implemented.");
        }
        return diagnostics.AsReadOnly();
    }

    public AbilityActivationResult Activate(string abilityId, in AbilityActivationContext context, long tick)
    {
        if (_disposed) return AbilityActivationResult.SessionEnded;
        if (!_plans.TryGetValue(abilityId, out var plan))
            return _unsupported.ContainsKey(abilityId) ? AbilityActivationResult.UnsupportedAbility : AbilityActivationResult.AbilityNotGranted;
        if (!context.Caster.Grants.Contains(abilityId) && plan.Definition.Availability != AbilityAvailability.AllShips)
            return AbilityActivationResult.AbilityNotGranted;
        if (AbilityEligibility.Rejection(plan.Definition, plan.Targeting, context) is { } rejection) return rejection;
        try
        {
            // Reserve identity before publishing state changes; observers may activate another ability.
            var instance = checked(++_nextInstance);
            if (!context.Target.ApplyBehaviour(plan.Behaviour, tick, instance, out _))
                return AbilityActivationResult.BehaviourLimit;
        }
        catch (Exception error) when (error is OverflowException or InvalidOperationException or ArgumentOutOfRangeException)
        { return AbilityActivationResult.InvalidGameplayValues; }
        // Observer failures must escape after commit, never masquerade as a rejected activation.
        context.Target.PublishChanges();
        return AbilityActivationResult.Accepted;
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
