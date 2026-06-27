using System;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public sealed class EffectDefinition
{
    public int EffectDefinitionId { get; init; }
    public int StackGroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public EffectSourceType SourceType { get; init; } = EffectSourceType.Skills;
    public LivingEntityCondition ClassificationConditions { get; init; } = LivingEntityCondition.None;
    public EffectTrigger Trigger { get; init; } = EffectTrigger.Immediate;
    public EffectDuration Duration { get; init; } = EffectDuration.Once;
    public SimulationLane SimulationLane { get; init; } = SimulationLane.Fast;
    public EffectTarget? Target { get; init; }
    public LivingEntityCondition ConditionAddRequest { get; init; } = LivingEntityCondition.None;
    public LivingEntityCondition ConditionRemoveRequest { get; init; } = LivingEntityCondition.None;
    public EffectRemovalRule? EffectRemovalRule { get; init; }
    public LivingEntityCondition BlockedBy { get; init; } = LivingEntityCondition.None;
    public bool CanTriggerWhileDead { get; init; }
    public EffectBlockPolicy BlockPolicy { get; init; } = EffectBlockPolicy.ConsumeOnFail;
    public EffectStackScope StackScope { get; init; } = EffectStackScope.SameSource;
    public EffectStackPolicy StackPolicy { get; init; } = EffectStackPolicy.Stack;
    public EffectStackBehavior StackBehavior { get; init; } = EffectStackBehavior.IndependentStacks;
    public int MaxStacks { get; init; } = 1;
    public MaxStackOverflowPolicy MaxStackOverflowPolicy { get; init; } = MaxStackOverflowPolicy.ReplaceOldest;
    public EffectVisibility Visibility { get; init; } = EffectVisibility.Internal;
    public int[] ChildEffectDefinitionIds { get; init; } = Array.Empty<int>();
    public bool HasDeterministicStrengthComparison { get; init; }

    public int StackBaseId => StackGroupId != 0 ? StackGroupId : EffectDefinitionId;
    public bool HasWork => Target.HasValue ||
                           ConditionAddRequest != LivingEntityCondition.None ||
                           ConditionRemoveRequest != LivingEntityCondition.None ||
                           EffectRemovalRule != null ||
                           ChildEffectDefinitionIds.Length > 0;
}
