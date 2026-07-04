#nullable enable
using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Immutable designer-authored effect data referenced by runtime active effects.
/// </summary>
public sealed class EffectDefinition
{
    /// <summary>
    /// Gets the stable effect-definition identifier.
    /// </summary>
    public int EffectDefinitionId { get; init; }

    /// <summary>
    /// Gets the optional shared stack family identifier.
    /// </summary>
    public int StackGroupId { get; init; }

    /// <summary>
    /// Gets the display name used for UI-visible effects.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display or authoring description for the effect.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of gameplay source that authored this effect.
    /// </summary>
    public EffectSourceType SourceType { get; init; } = EffectSourceType.Skills;

    /// <summary>
    /// Gets condition flags used to classify this effect for UI, validation, and removal matching.
    /// </summary>
    public LivingEntityCondition ClassificationConditions { get; init; } = LivingEntityCondition.None;

    /// <summary>
    /// Gets when the effect executes while active.
    /// </summary>
    public EffectTrigger Trigger { get; init; } = EffectTrigger.Immediate;

    /// <summary>
    /// Gets how long the effect remains active.
    /// </summary>
    public EffectDuration Duration { get; init; } = EffectDuration.Once;

    /// <summary>
    /// Gets whether the effect belongs to the fast or slow simulation lane.
    /// </summary>
    public SimulationLane SimulationLane { get; init; } = SimulationLane.Fast;

    /// <summary>
    /// Gets the optional single stat target modified by this effect.
    /// </summary>
    public EffectTarget? Target { get; init; }

    /// <summary>
    /// Gets entity conditions requested when this effect executes or remains active.
    /// </summary>
    public LivingEntityCondition ConditionAddRequest { get; init; } = LivingEntityCondition.None;

    /// <summary>
    /// Gets entity conditions requested for removal when this effect executes.
    /// </summary>
    public LivingEntityCondition ConditionRemoveRequest { get; init; } = LivingEntityCondition.None;

    /// <summary>
    /// Gets the optional rule used to remove matching active effects.
    /// </summary>
    public EffectRemovalRule? EffectRemovalRule { get; init; }

    /// <summary>
    /// Gets condition flags that block this effect from executing.
    /// </summary>
    public LivingEntityCondition BlockedBy { get; init; } = LivingEntityCondition.None;

    /// <summary>
    /// Gets whether the effect can execute when the target is dead.
    /// </summary>
    public bool CanTriggerWhileDead { get; init; }

    /// <summary>
    /// Gets what happens when the effect is blocked.
    /// </summary>
    public EffectBlockPolicy BlockPolicy { get; init; } = EffectBlockPolicy.ConsumeOnFail;

    /// <summary>
    /// Gets which runtime source fields participate in duplicate stack identity.
    /// </summary>
    public EffectStackScope StackScope { get; init; } = EffectStackScope.SameSource;

    /// <summary>
    /// Gets whether duplicate effects stack, replace, or discard.
    /// </summary>
    public EffectStackPolicy StackPolicy { get; init; } = EffectStackPolicy.Stack;

    /// <summary>
    /// Gets how accepted stacks are represented and refreshed.
    /// </summary>
    public EffectStackBehavior StackBehavior { get; init; } = EffectStackBehavior.IndependentStacks;

    /// <summary>
    /// Gets the maximum accepted stacks for this stack family.
    /// </summary>
    public int MaxStacks { get; init; } = 1;

    /// <summary>
    /// Gets deterministic overflow behavior when max stacks are reached.
    /// </summary>
    public MaxStackOverflowPolicy MaxStackOverflowPolicy { get; init; } = MaxStackOverflowPolicy.ReplaceOldest;

    /// <summary>
    /// Gets where the effect is visible to player-facing UI.
    /// </summary>
    public EffectVisibility Visibility { get; init; } = EffectVisibility.Internal;

    /// <summary>
    /// Gets child effect-definition ids queued when this effect executes.
    /// </summary>
    public int[] ChildEffectDefinitionIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Gets whether ReplaceWeakest has deterministic strength-comparison support.
    /// </summary>
    public bool HasDeterministicStrengthComparison { get; init; }

    /// <summary>
    /// Gets the stack family id used for duplicate detection.
    /// </summary>
    public int StackBaseId => StackGroupId != 0 ? StackGroupId : EffectDefinitionId;

    /// <summary>
    /// Gets whether this definition contains any executable work.
    /// </summary>
    public bool HasWork => Target.HasValue ||
                           ConditionAddRequest != LivingEntityCondition.None ||
                           ConditionRemoveRequest != LivingEntityCondition.None ||
                           EffectRemovalRule != null ||
                           ChildEffectDefinitionIds.Length > 0;
}
