using sMPGWM.Scripts.Controllers.LivingEntity.Definitions;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Effects;

/// <summary>
/// Provides example effect definitions that can be copied or adapted while designing skills.
/// </summary>
public static class EffectDefinitionFactory
{
    /// <summary>
    /// Gets a complete pseudo-value effect definition that documents every available field.
    /// </summary>
    public static EffectDefinition DefaultEffectDefinition => new()
    {
        EffectDefinitionId = 1001,
        StackGroupId = 0,

        Name = "Example Skill Effect",
        Description = "Describe what this effect does.",

        SourceType = EffectSourceType.Skills,
        ClassificationConditions = LivingEntityCondition.Buff,

        Trigger = new EffectTrigger(
            type: EffectTriggerType.Immediate,
            intervalTicks: 0,
            requiredCondition: LivingEntityCondition.None,
            trackedStat: StatType.Health),

        Duration = EffectDuration.Once,
        SimulationLane = SimulationLane.Fast,

        Target = new EffectTarget(
            targetStat: StatType.Health,
            targetValueKind: StatValueKind.Current,
            manipulation: StatManipulation.Raise,
            calculation: StatCalculation.Flat,
            value: 25f),

        ConditionAddRequest = LivingEntityCondition.None,
        ConditionRemoveRequest = LivingEntityCondition.None,

        EffectRemovalRule = null,

        BlockedBy = LivingEntityCondition.None,
        CanTriggerWhileDead = false,
        BlockPolicy = EffectBlockPolicy.ConsumeOnFail,

        StackScope = EffectStackScope.SameSource,
        StackPolicy = EffectStackPolicy.Stack,
        StackBehavior = EffectStackBehavior.IndependentStacks,
        MaxStacks = 1,
        MaxStackOverflowPolicy = MaxStackOverflowPolicy.ReplaceOldest,
        HasDeterministicStrengthComparison = false,

        Visibility = EffectVisibility.Hud,
        ChildEffectDefinitionIds = []
    };

    /// <summary>
    /// Creates a one-shot flat damage effect against the selected stat.
    /// </summary>
    public static EffectDefinition CreateInstantDamage(
        int effectDefinitionId = 1002,
        float damage = 40f,
        StatType targetStat = StatType.Health)
    {
        return new EffectDefinition
        {
            EffectDefinitionId = effectDefinitionId,
            Name = "Instant Damage",
            Description = "Immediately reduces the target stat by a flat amount.",
            SourceType = EffectSourceType.Skills,
            ClassificationConditions = LivingEntityCondition.Debuff,
            Trigger = EffectTrigger.Immediate,
            Duration = EffectDuration.Once,
            SimulationLane = SimulationLane.Fast,
            Target = new EffectTarget(
                targetStat,
                StatValueKind.Current,
                StatManipulation.Reduce,
                StatCalculation.Flat,
                damage),
            CanTriggerWhileDead = false,
            BlockPolicy = EffectBlockPolicy.ConsumeOnFail,
            StackPolicy = EffectStackPolicy.Stack,
            StackBehavior = EffectStackBehavior.IndependentStacks,
            MaxStacks = 1,
            MaxStackOverflowPolicy = MaxStackOverflowPolicy.ReplaceOldest,
            Visibility = EffectVisibility.Hud,
            ChildEffectDefinitionIds = []
        };
    }

    /// <summary>
    /// Creates a poison-style damage-over-time effect with independent stacking.
    /// </summary>
    public static EffectDefinition CreateDamageOverTime(
        int effectDefinitionId = 1003,
        float damagePerTick = 5f,
        int intervalTicks = 30,
        int durationTicks = 150,
        int maxStacks = 5)
    {
        return new EffectDefinition
        {
            EffectDefinitionId = effectDefinitionId,
            Name = "Damage Over Time",
            Description = "Periodically reduces health while the effect remains active.",
            SourceType = EffectSourceType.Skills,
            ClassificationConditions =
                LivingEntityCondition.Debuff |
                LivingEntityCondition.DamageOverTime |
                LivingEntityCondition.Poison,
            Trigger = new EffectTrigger(
                EffectTriggerType.OverTime,
                intervalTicks: intervalTicks),
            Duration = EffectDuration.TemporaryTicks(durationTicks),
            SimulationLane = SimulationLane.Fast,
            Target = new EffectTarget(
                StatType.Health,
                StatValueKind.Current,
                StatManipulation.Reduce,
                StatCalculation.Flat,
                damagePerTick),
            ConditionAddRequest = LivingEntityCondition.Poison,
            CanTriggerWhileDead = false,
            BlockPolicy = EffectBlockPolicy.ConsumeOnFail,
            StackScope = EffectStackScope.SameSource,
            StackPolicy = EffectStackPolicy.Stack,
            StackBehavior = EffectStackBehavior.IndependentStacks,
            MaxStacks = maxStacks,
            MaxStackOverflowPolicy = MaxStackOverflowPolicy.ReplaceOldest,
            Visibility = EffectVisibility.Hud,
            ChildEffectDefinitionIds = []
        };
    }

    /// <summary>
    /// Creates a temporary flat max-value buff that refreshes when reapplied from the same source.
    /// </summary>
    public static EffectDefinition CreateTemporaryMaxBuff(
        int effectDefinitionId = 1004,
        StatType targetStat = StatType.Shield,
        float amount = 50f,
        int durationTicks = 300)
    {
        return new EffectDefinition
        {
            EffectDefinitionId = effectDefinitionId,
            Name = "Temporary Max Buff",
            Description = "Temporarily raises a stat maximum by a flat amount.",
            SourceType = EffectSourceType.Skills,
            ClassificationConditions = LivingEntityCondition.Buff,
            Trigger = EffectTrigger.Immediate,
            Duration = EffectDuration.TemporaryTicks(durationTicks),
            SimulationLane = SimulationLane.Fast,
            Target = new EffectTarget(
                targetStat,
                StatValueKind.Max,
                StatManipulation.Raise,
                StatCalculation.Flat,
                amount),
            CanTriggerWhileDead = false,
            BlockPolicy = EffectBlockPolicy.ConsumeOnFail,
            StackScope = EffectStackScope.SameSource,
            StackPolicy = EffectStackPolicy.Replace,
            StackBehavior = EffectStackBehavior.RefreshDuration,
            MaxStacks = 1,
            MaxStackOverflowPolicy = MaxStackOverflowPolicy.ReplaceOldest,
            Visibility = EffectVisibility.Hud,
            ChildEffectDefinitionIds = []
        };
    }

    /// <summary>
    /// Creates a one-shot cleanse effect that removes matching effects by condition classification.
    /// </summary>
    public static EffectDefinition CreateCleanse(
        int effectDefinitionId = 1005,
        LivingEntityCondition requiredConditions = LivingEntityCondition.Debuff,
        int maxRemoveCount = 1)
    {
        return new EffectDefinition
        {
            EffectDefinitionId = effectDefinitionId,
            Name = "Cleanse",
            Description = "Removes active effects matching the requested classification conditions.",
            SourceType = EffectSourceType.Skills,
            ClassificationConditions = LivingEntityCondition.Buff,
            Trigger = EffectTrigger.Immediate,
            Duration = EffectDuration.Once,
            SimulationLane = SimulationLane.Fast,
            Target = null,
            EffectRemovalRule = new EffectRemovalRule
            {
                RequiredConditions = requiredConditions,
                IncludeChildConditions = true,
                SourceMatchPolicy = SourceMatchPolicy.AnySource,
                MaxRemoveCount = maxRemoveCount,
                RemoveOldestFirst = true
            },
            CanTriggerWhileDead = false,
            BlockPolicy = EffectBlockPolicy.ConsumeOnFail,
            StackPolicy = EffectStackPolicy.Stack,
            StackBehavior = EffectStackBehavior.IndependentStacks,
            MaxStacks = 1,
            MaxStackOverflowPolicy = MaxStackOverflowPolicy.ReplaceOldest,
            Visibility = EffectVisibility.Hud,
            ChildEffectDefinitionIds = []
        };
    }
}
