using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class LivingEntityRuntime
{
    private const string InitialConditionSourceId = "__initial__";
    private readonly Dictionary<LivingEntityCondition, HashSet<string>> _conditionSources = new();
    private readonly List<TimedConditionSource> _timedConditionSources = [];

    public LivingEntityRuntime(LivingEntityState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        SeedConditionSources(state.Condition);
    }

    public LivingEntityState State { get; }
    public LivingEntityCondition Condition => State.Condition;

    public event Action<LivingEntityConditionChanged> ConditionChanged;

    public bool IsAlive()
    {
        return State.IsAlive();
    }

    public bool IsTargetable()
    {
        return State.IsTargetable();
    }

    public Stat GetStatByType(StatType type)
    {
        return State.GetStatByType(type);
    }

    public StatBreakdown GetStatBreakdown(StatType type)
    {
        return State.GetStatBreakdown(type);
    }

    internal void SetConditionSource(LivingEntityCondition condition, string sourceId, bool isApplied)
    {
        if (condition == LivingEntityCondition.None)
            return;

        sourceId = string.IsNullOrWhiteSpace(sourceId) ? "unknown" : sourceId;
        var oldCondition = State.Condition;

        for (var i = 0; i < LivingEntityConditionExtensions.Flags.Length; i++)
        {
            var flag = LivingEntityConditionExtensions.Flags[i];
            if (!condition.Contains(flag))
                continue;

            if (!_conditionSources.TryGetValue(flag, out var sources))
            {
                sources = new HashSet<string>();
                _conditionSources[flag] = sources;
            }

            if (isApplied)
                sources.Add(sourceId);
            else
                sources.Remove(sourceId);
        }

        var newCondition = BuildConditionFromSources();
        State.SetCondition(newCondition);

        var added = newCondition & ~oldCondition;
        var removed = oldCondition & ~newCondition;
        if (added != LivingEntityCondition.None || removed != LivingEntityCondition.None)
        {
            ConditionChanged?.Invoke(new LivingEntityConditionChanged(
                State.LivingEntityId,
                added,
                removed,
                newCondition,
                sourceId));
        }
    }

    internal void SetTimedConditionSource(LivingEntityCondition condition, string sourceId, float duration)
    {
        if (duration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), "Timed condition duration must be greater than zero.");

        SetConditionSource(condition, sourceId, true);

        for (var i = 0; i < _timedConditionSources.Count; i++)
        {
            if (_timedConditionSources[i].SourceId != sourceId)
                continue;

            _timedConditionSources[i].Condition = condition;
            _timedConditionSources[i].TimeRemaining = duration;
            return;
        }

        _timedConditionSources.Add(new TimedConditionSource(condition, sourceId, duration));
    }

    internal void TickConditionSources(float delta)
    {
        for (var i = _timedConditionSources.Count - 1; i >= 0; i--)
        {
            var source = _timedConditionSources[i];
            source.TimeRemaining -= delta;

            if (source.TimeRemaining > 0f)
                continue;

            _timedConditionSources.RemoveAt(i);
            SetConditionSource(source.Condition, source.SourceId, false);
        }
    }

    internal void RebuildConditionSourcesFromState(string sourceId)
    {
        _conditionSources.Clear();
        for (var i = 0; i < LivingEntityConditionExtensions.Flags.Length; i++)
        {
            var flag = LivingEntityConditionExtensions.Flags[i];
            if (State.Condition.Contains(flag))
                _conditionSources[flag] = new HashSet<string> { sourceId };
        }
    }

    internal void EmitConditionChangedFromExternal(LivingEntityCondition oldCondition, string sourceId)
    {
        var newCondition = State.Condition;
        var added = newCondition & ~oldCondition;
        var removed = oldCondition & ~newCondition;
        if (added == LivingEntityCondition.None && removed == LivingEntityCondition.None)
            return;

        ConditionChanged?.Invoke(new LivingEntityConditionChanged(
            State.LivingEntityId,
            added,
            removed,
            newCondition,
            sourceId));
    }

    private void SeedConditionSources(LivingEntityCondition condition)
    {
        for (var i = 0; i < LivingEntityConditionExtensions.Flags.Length; i++)
        {
            var flag = LivingEntityConditionExtensions.Flags[i];
            if (condition.Contains(flag))
                _conditionSources[flag] = new HashSet<string> { InitialConditionSourceId };
        }
    }

    private LivingEntityCondition BuildConditionFromSources()
    {
        var result = LivingEntityCondition.None;
        foreach (var (condition, sources) in _conditionSources)
        {
            if (sources.Count > 0)
                result |= condition;
        }

        return result;
    }

    private sealed class TimedConditionSource(
        LivingEntityCondition condition,
        string sourceId,
        float timeRemaining)
    {
        public LivingEntityCondition Condition = condition;
        public string SourceId { get; } = sourceId;
        public float TimeRemaining = timeRemaining;
    }
}
