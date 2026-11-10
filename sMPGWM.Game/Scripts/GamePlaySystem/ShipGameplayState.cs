using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ShipGameplayState
{
    public const int MaxBehaviours = 64;
    private readonly List<BehaviourInstance> _behaviours = new();
    public IReadOnlyList<BehaviourInstance> ActiveBehaviours { get; }
    public AbilityGrants Grants { get; } = new();
    public ResourcePool Shield { get; }
    public long Revision { get; private set; } = 1;
    public event Action? Changed;

    public ShipGameplayState(double capacity, double current)
    {
        Shield = new(capacity, current);
        ActiveBehaviours = _behaviours.AsReadOnly();
    }

    internal bool ApplyBehaviour(SupportedBehaviour definition, long tick, long instance, out bool created)
    {
        if (tick < 0 || instance <= 0) throw new ArgumentOutOfRangeException(nameof(tick));
        var deadline = checked(tick + definition.DurationTicks);
        var nextRevision = checked(Revision + 1);
        created = false;
        for (var i = 0; i < _behaviours.Count; i++)
        {
            var active = _behaviours[i];
            if (active.DefinitionId != definition.Definition.Id) continue;
            _behaviours[i] = active with { Deadline = deadline };
            Revision = nextRevision;
            Changed?.Invoke();
            return true;
        }
        if (_behaviours.Count >= MaxBehaviours) return false;
        Shield.SetContribution(instance, definition.Flat, definition.Fraction);
        _behaviours.Add(new(definition.Definition.Id, instance, deadline));
        created = true;
        Revision = nextRevision;
        Changed?.Invoke();
        return true;
    }

    public bool Expire(long tick)
    {
        var changed = false;
        for (var i = _behaviours.Count - 1; i >= 0; i--)
        {
            var active = _behaviours[i];
            if (tick < active.Deadline) continue;
            Shield.RemoveOwner(active.RuntimeId);
            _behaviours.RemoveAt(i);
            changed = true;
        }
        if (!changed) return false;
        Revision = checked(Revision + 1);
        Changed?.Invoke();
        return true;
    }

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours)
    {
        if (revision <= Revision) return false;
        if (behaviours.Count > MaxBehaviours) throw new ArgumentException("Behaviour count exceeds protocol limit.");
        Shield.Confirm(capacity, current);
        _behaviours.Clear();
        for (var i = 0; i < behaviours.Count; i++) _behaviours.Add(behaviours[i]);
        Revision = revision;
        Changed?.Invoke();
        return true;
    }
}
