using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Lightweight runtime facade for a tracked living-entity state.
/// </summary>
public sealed class LivingEntityRuntime
{
    /// <summary>
    /// Creates a runtime facade for the supplied living-entity state.
    /// </summary>
    public LivingEntityRuntime(LivingEntityState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <summary>
    /// Gets the mutable simulation state owned by this runtime.
    /// </summary>
    public LivingEntityState State { get; }

    /// <summary>
    /// Gets the current effective condition mask from the underlying state.
    /// </summary>
    public LivingEntityCondition Condition => State.Condition;

    /// <summary>
    /// Raised when a condition change is emitted through this runtime facade.
    /// </summary>
    public event Action<LivingEntityConditionChanged> ConditionChanged;

    /// <summary>
    /// Returns true when the state is not marked dead.
    /// </summary>
    public bool IsAlive()
    {
        return State.IsAlive();
    }

    /// <summary>
    /// Returns true when the state is targetable under the current condition mask.
    /// </summary>
    public bool IsTargetable()
    {
        return State.IsTargetable();
    }

    /// <summary>
    /// Returns a read-only stat snapshot for the requested stat type.
    /// </summary>
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
