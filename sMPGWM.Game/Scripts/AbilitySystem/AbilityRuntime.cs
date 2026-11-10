using System;
using sMPGWM.Scripts.GameplayDataSystem;
namespace sMPGWM.Scripts.AbilitySystem;
public sealed class AbilityRuntime : IDisposable
{
    private long _nextInstance;
    private bool _disposed;
    public GameplayCatalogues Definitions { get; }

    public AbilityRuntime(GameplayCatalogues definitions) { Definitions = definitions; }
    public AbilityActivationResult Activate(string abilityId, in AbilityActivationContext context, long tick)
    {
        if (_disposed) return AbilityActivationResult.SessionEnded;
        if (!Definitions.Abilities.TryGetPlan(abilityId, out var plan))
            return Definitions.Abilities.TryGetById(abilityId, out _) ? AbilityActivationResult.UnsupportedAbility : AbilityActivationResult.AbilityNotGranted;
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

    public void Dispose() => _disposed = true;
}
