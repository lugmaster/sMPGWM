using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.StatSystem;
namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ShipEffectState
{
    private readonly List<BehaviourInstance> _instances = new(ShipRuntime.MaxBehaviours);
    private readonly ShipStatState _stats;
    public IReadOnlyList<BehaviourInstance> Active { get; }
    internal ShipEffectState(ShipStatState stats) { _stats = stats; Active = _instances.AsReadOnly(); }
    internal bool Apply(SupportedBehaviour definition, long tick, long identity, out bool created)
    {
        if (tick < 0 || identity <= 0) throw new ArgumentOutOfRangeException(nameof(tick));
        var deadline = checked(tick + definition.DurationTicks);
        created = false;
        for (var i = 0; i < _instances.Count; i++)
        {
            var active = _instances[i];
            if (active.DefinitionId != definition.Definition.Id) continue;
            _instances[i] = active with { Deadline = deadline };
            return true;
        }
        if (_instances.Count >= ShipRuntime.MaxBehaviours) return false;
        _stats.SetContribution(new(StatContributionKind.Effect, identity), definition.Flat, definition.Fraction);
        _instances.Add(new(definition.Definition.Id, identity, deadline));
        created = true;
        return true;
    }
    internal bool Expire(long tick)
    {
        var changed = false;
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            var active = _instances[i];
            if (tick < active.Deadline) continue;
            _stats.RemoveSource(new(StatContributionKind.Effect, active.RuntimeId));
            _instances.RemoveAt(i);
            changed = true;
        }
        return changed;
    }
    internal void Confirm(IReadOnlyList<BehaviourInstance> instances)
    {
        _instances.Clear();
        for (var i = 0; i < instances.Count; i++) _instances.Add(instances[i]);
    }
    internal void Clear() => _instances.Clear();
}
