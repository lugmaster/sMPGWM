using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Autoload;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;
using sMPGWM.Scripts.Enums.Game;
using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class LivingEntityServiceCore
{
    private const string DerivedEnergyDepletedSourceId = "__derived_energy_depleted__";

    private readonly Dictionary<int, LivingEntityRuntime> _runtimes = new();
    private readonly Dictionary<int, LivingEntityStateTracker> _trackers = new();

    public event Action<LivingEntityConditionChanged> ConditionChanged;

    public void RegisterPeer(int peerId, LivingEntityState state)
    {
        if (_runtimes.TryGetValue(peerId, out var existingRuntime))
            existingRuntime.ConditionChanged -= OnRuntimeConditionChanged;

        var runtime = new LivingEntityRuntime(state);
        runtime.ConditionChanged += OnRuntimeConditionChanged;

        _runtimes[peerId] = runtime;
        _trackers[peerId] = new LivingEntityStateTracker(state);

        Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(RegisterPeer)} registered living entity state for {nameof(peerId)}={peerId}.");
    }

    public LivingEntityRuntime RegisterPeerWithDefaultState(int peerId)
    {
        var state = LivingEntityStateFactory.CreateDefaultPlayerState(peerId);
        RegisterPeer(peerId, state);
        return _runtimes[peerId];
    }

    public void UnregisterPeer(int peerId)
    {
        if (_runtimes.Remove(peerId, out var runtime))
            runtime.ConditionChanged -= OnRuntimeConditionChanged;

        _trackers.Remove(peerId);
        Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(UnregisterPeer)} unregistered living entity state for {nameof(peerId)}={peerId}.");
    }

    public bool HasPeer(int peerId)
    {
        return _runtimes.ContainsKey(peerId);
    }

    public LivingEntityRuntime GetRuntime(int peerId)
    {
        return GetRequiredRuntime(peerId);
    }

    public LivingEntityState GetState(int peerId)
    {
        return GetRequiredRuntime(peerId).State;
    }

    public StatBreakdown GetStatBreakdown(int peerId, StatType statType)
    {
        return GetRequiredRuntime(peerId).GetStatBreakdown(statType);
    }

    public void ApplyEffect(int peerId, LivingEntityEffect effect)
    {
        var target = GetRequiredRuntime(peerId);
        ApplyEffect(target, effect);
    }

    public GCol.Array GetDelta(int peerId, bool includePermanent)
    {
        var tracker = GetRequiredTracker(peerId);
        return tracker.CreateCombinedDelta(includePermanent);
    }

    public GCol.Array GetDeltas(bool includePermanent)
    {
        var result = new GCol.Array();
        foreach (var (_, tracker) in _trackers)
        {
            var delta = tracker.CreateCombinedDelta(includePermanent);
            if (LivingEntityState.DeltaVariantHasChanges(delta))
                result.Add(delta);
        }

        return result;
    }

    public void ApplyDelta(GCol.Array delta)
    {
        if (delta.Count == 0)
        {
            Logger.Error($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyDelta)} received an empty delta.");
            throw new ArgumentException("Invalid empty living entity delta.", nameof(delta));
        }

        var peerId = delta[0].AsInt32();
        var runtime = GetRequiredRuntime(peerId);
        var oldCondition = runtime.Condition;

        runtime.State.ApplyDeltaVariant(delta);
        runtime.RebuildConditionSourcesFromState("delta");
        runtime.EmitConditionChangedFromExternal(oldCondition, "delta");

        Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyDelta)} applied living entity delta for {nameof(peerId)}={peerId}.");
    }

    public void ApplyDeltas(GCol.Array deltas)
    {
        for (var i = 0; i < deltas.Count; i++)
            ApplyDelta(deltas[i].AsGodotArray());
    }

    public void TickActiveEffects(int peerId, float delta)
    {
        TickActiveEffects(GetRequiredRuntime(peerId), delta);
    }

    public void TickAllActiveEffects(float delta)
    {
        foreach (var runtime in _runtimes.Values)
            TickActiveEffects(runtime, delta);
    }

    private void ApplyEffect(LivingEntityRuntime target, LivingEntityEffect effect)
    {
        switch (effect)
        {
            case ReduceEffect reduce:
                ApplyReduce(target, reduce.SourceId, reduce.StatType, reduce.Amount);
                break;
            case RaiseEffect raise:
                ApplyRaise(target, raise.SourceId, raise.StatType, raise.Amount);
                break;
            case StatModifierEffect modifier:
                ApplyModifier(target, modifier.SourceId, modifier.StatType, modifier.Modifier);
                break;
            case ConditionEffect condition:
                SetCondition(target, condition.EffectInstanceId, condition.SourceId, condition.Condition,
                    condition.IsApplied, condition.Duration);
                break;
            default:
                Logger.Error($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyEffect)} reached unhandled {nameof(effect)}={effect}.");
                throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unhandled living entity effect.");
        }
    }

    private void ApplyReduce(LivingEntityRuntime target, string sourceId, StatType statType, float amount)
    {
        if (amount <= 0f)
        {
            Logger.Error($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyReduce)} received invalid {nameof(amount)}={amount}.");
            throw new ArgumentOutOfRangeException(nameof(amount), "Reduce amount must be greater than zero.");
        }

        if (!target.IsAlive())
        {
            Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyReduce)} ignored reduce from {nameof(sourceId)}={sourceId}; target {target.State.LivingEntityId} is already dead.");
            return;
        }

        if (statType == StatType.Health && target.Condition.Contains(LivingEntityCondition.Invulnerable))
        {
            Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyReduce)} ignored health reduce from {nameof(sourceId)}={sourceId}; target {target.State.LivingEntityId} is invulnerable.");
            return;
        }

        var stat = target.GetStatByType(statType);
        stat.Reduce(amount);

        Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyReduce)} reduced {target.State.LivingEntityId}.{statType} by {amount:0.###} from {sourceId}. " +
                $"Current={stat.CurrentValue:0.###}/{stat.MaxValue:0.###}");

        if (statType == StatType.Health && stat.CurrentValue <= 0f)
            ApplyZeroHealthOutcome(target, sourceId);

        ReconcileDerivedConditions(target);
    }

    private void ApplyRaise(LivingEntityRuntime target, string sourceId, StatType statType, float amount)
    {
        if (amount <= 0f)
        {
            Logger.Error($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyRaise)} received invalid {nameof(amount)}={amount}.");
            throw new ArgumentOutOfRangeException(nameof(amount), "Raise amount must be greater than zero.");
        }

        var stat = target.GetStatByType(statType);
        stat.Raise(amount);

        Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyRaise)} raised {target.State.LivingEntityId}.{statType} by {amount:0.###} from {sourceId}. " +
                $"Current={stat.CurrentValue:0.###}/{stat.MaxValue:0.###}");

        ReconcileDerivedConditions(target);
    }

    private void ApplyModifier(LivingEntityRuntime target, string sourceId, StatType statType, StatModifier modifier)
    {
        var stat = target.GetStatByType(statType);
        stat.AddModifier(modifier);

        Logger.Debug(
            $"{nameof(LivingEntityServiceCore)}.{nameof(ApplyModifier)} applied modifier {modifier.DisplayName} from {sourceId} to {target.State.LivingEntityId}.{statType}. " +
            $"Current={stat.CurrentValue:0.###}/{stat.MaxValue:0.###}");

        ReconcileDerivedConditions(target);
    }

    private void SetCondition(
        LivingEntityRuntime target,
        string effectInstanceId,
        string sourceId,
        LivingEntityCondition condition,
        bool isApplied,
        float duration = 0f)
    {
        if (isApplied && duration > 0f)
            target.SetTimedConditionSource(condition, effectInstanceId, duration);
        else
            target.SetConditionSource(condition, effectInstanceId, isApplied);

        Logger.Debug(
            $"{nameof(LivingEntityServiceCore)}.{nameof(SetCondition)} {(isApplied ? "added" : "removed")} condition {condition} from {target.State.LivingEntityId}. Source={sourceId}");
    }

    private void TickActiveEffects(LivingEntityRuntime target, float delta)
    {
        target.TickConditionSources(delta);
        target.State.Health.Tick(delta);
        target.State.Shield.Tick(delta);
        target.State.Energy.Tick(delta);
        target.State.HealthRegeneration.Tick(delta);
        target.State.ShieldRegeneration.Tick(delta);
        target.State.EnergyRegeneration.Tick(delta);
        target.State.Speed.Tick(delta);
        target.State.Acceleration.Tick(delta);
        target.State.AfterBurnerSpeed.Tick(delta);
        target.State.AfterBurnerAcceleration.Tick(delta);

        ReconcileDerivedConditions(target);
    }

    private void ApplyZeroHealthOutcome(LivingEntityRuntime target, string sourceId)
    {
        if (!target.IsAlive())
            return;

        target.SetConditionSource(LivingEntityCondition.Dead, $"zero-health:{sourceId}", true);
        target.State.ReduceStatToZero(StatType.Shield);
        target.State.ReduceStatToZero(StatType.Energy);

        Logger.Debug($"{nameof(LivingEntityServiceCore)}.{nameof(ApplyZeroHealthOutcome)} living entity {target.State.LivingEntityId} reached zero health. Source={sourceId}");
    }

    private void ReconcileDerivedConditions(LivingEntityRuntime target)
    {
        target.SetConditionSource(
            LivingEntityCondition.EnergyDepleted,
            DerivedEnergyDepletedSourceId,
            target.State.Energy.CurrentValue <= 0f);
    }

    private LivingEntityRuntime GetRequiredRuntime(int peerId)
    {
        if (_runtimes.TryGetValue(peerId, out var runtime))
            return runtime;

        Logger.Error($"{nameof(LivingEntityServiceCore)}.{nameof(GetRequiredRuntime)} found no living entity runtime for {nameof(peerId)}={peerId}.");
        throw new InvalidOperationException($"No living entity runtime registered for peer {peerId}.");
    }

    private LivingEntityStateTracker GetRequiredTracker(int peerId)
    {
        if (_trackers.TryGetValue(peerId, out var tracker))
            return tracker;

        Logger.Error($"{nameof(LivingEntityServiceCore)}.{nameof(GetRequiredTracker)} found no living entity tracker for {nameof(peerId)}={peerId}.");
        throw new InvalidOperationException($"No living entity tracker registered for peer {peerId}.");
    }

    private void OnRuntimeConditionChanged(LivingEntityConditionChanged change)
    {
        ConditionChanged?.Invoke(change);
    }
}
