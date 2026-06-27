using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class LivingEntityRuntime
{
    public LivingEntityRuntime(LivingEntityState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
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

    internal void EmitConditionChanged(LivingEntityCondition oldCondition, string sourceId)
    {
        var current = State.Condition;
        var added = current & ~oldCondition;
        var removed = oldCondition & ~current;
        if (added == LivingEntityCondition.None && removed == LivingEntityCondition.None)
            return;

        ConditionChanged?.Invoke(new LivingEntityConditionChanged(
            State.LivingEntityId,
            added,
            removed,
            current,
            sourceId));
    }
}
