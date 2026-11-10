using System;
using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ConditionSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed class ShipGameplayState
{
    public const int MaxBehaviours = 64;
    public const int MaxConditions = 64;
    private readonly List<BehaviourInstance> _behaviours = new();
    private readonly List<string> _conditions = new();
    private readonly ResourcePool _shield;
    public IReadOnlyList<BehaviourInstance> ActiveBehaviours { get; }
    public IReadOnlyList<string> Conditions { get; }
    public AbilityGrants Grants { get; } = new();
    public ResourceState Shield => new(_shield.BaseCapacity, _shield.Capacity, _shield.Current);
    public int FactionId { get; private set; }
    public bool IsAlive => !_conditions.Contains("dead");
    public long Revision { get; private set; } = 1;
    public event Action? Changed;

    public ShipGameplayState(double capacity, double current)
    {
        _shield = new(capacity, current);
        ActiveBehaviours = _behaviours.AsReadOnly();
        Conditions = _conditions.AsReadOnly();
    }

    public bool IsAllyOf(ShipGameplayState other) => ReferenceEquals(this, other) ||
        FactionId != 0 && FactionId == other.FactionId;

    public bool SetFaction(int factionId)
    {
        if (factionId < 0) throw new ArgumentOutOfRangeException(nameof(factionId));
        if (FactionId == factionId) return false;
        var revision = checked(Revision + 1);
        FactionId = factionId;
        Revision = revision;
        PublishChanges();
        return true;
    }

    public bool SetCondition(ConditionDefinition condition, bool active)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (string.IsNullOrWhiteSpace(condition.Id) || condition.Id.Length > 64)
            throw new ArgumentException("Invalid condition ID.", nameof(condition));
        var index = _conditions.IndexOf(condition.Id);
        if (active == (index >= 0)) return false;
        if (active && _conditions.Count == MaxConditions) throw new InvalidOperationException("Condition limit reached.");
        var revision = checked(Revision + 1);
        if (active) _conditions.Add(condition.Id);
        else _conditions.RemoveAt(index);
        Revision = revision;
        PublishChanges();
        return true;
    }

    public bool SetShieldCurrent(double current)
    {
        if (current == _shield.Current) return false;
        var revision = checked(Revision + 1);
        _shield.SetCurrent(current);
        Revision = revision;
        PublishChanges();
        return true;
    }

    internal void PublishChanges() => Changed?.Invoke();

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
            return true;
        }
        if (_behaviours.Count >= MaxBehaviours) return false;
        _shield.SetContribution(instance, definition.Flat, definition.Fraction);
        _behaviours.Add(new(definition.Definition.Id, instance, deadline));
        created = true;
        Revision = nextRevision;
        return true;
    }

    public bool Expire(long tick)
    {
        var changed = false;
        for (var i = _behaviours.Count - 1; i >= 0; i--)
        {
            var active = _behaviours[i];
            if (tick < active.Deadline) continue;
            var revision = checked(Revision + 1);
            _shield.RemoveOwner(active.RuntimeId);
            _behaviours.RemoveAt(i);
            Revision = revision;
            changed = true;
        }
        if (!changed) return false;
        PublishChanges();
        return true;
    }

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours)
        => Confirm(revision, capacity, current, behaviours, FactionId, Conditions);

    public bool Confirm(long revision, double capacity, double current, IReadOnlyList<BehaviourInstance> behaviours,
        int factionId, IReadOnlyList<string> conditions)
    {
        if (revision <= Revision) return false;
        if (behaviours.Count > MaxBehaviours) throw new ArgumentException("Behaviour count exceeds protocol limit.");
        if (factionId < 0 || conditions.Count > MaxConditions) throw new ArgumentException("Invalid eligibility state.");
        _shield.Confirm(capacity, current);
        _behaviours.Clear();
        for (var i = 0; i < behaviours.Count; i++) _behaviours.Add(behaviours[i]);
        if (!ReferenceEquals(conditions, Conditions))
        {
            _conditions.Clear();
            for (var i = 0; i < conditions.Count; i++) _conditions.Add(conditions[i]);
        }
        FactionId = factionId;
        Revision = revision;
        PublishChanges();
        return true;
    }
}
