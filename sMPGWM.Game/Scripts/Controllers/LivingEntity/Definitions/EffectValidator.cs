using System;
using System.Collections.Generic;
using System.Linq;
using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public static class EffectValidator
{
    public static ValidationReport[] ValidateRegistered(StateEngineConfig? config = null)
    {
        return Validate(EffectDefinitionRegistry.All, config ?? StateEngineConfig.Default);
    }

    public static ValidationReport[] Validate(IEnumerable<EffectDefinition> definitions, StateEngineConfig config)
    {
        var definitionArray = definitions.OrderBy(definition => definition.EffectDefinitionId).ToArray();
        var ids = new HashSet<int>();
        for (var i = 0; i < definitionArray.Length; i++)
            ids.Add(definitionArray[i].EffectDefinitionId);

        var reports = new List<ValidationReport>();
        for (var i = 0; i < definitionArray.Length; i++)
        {
            var definition = definitionArray[i];
            ValidateDefinition(definition, ids, config, reports);
        }

        var sorted = reports
            .OrderBy(report => report.EffectDefinitionId)
            .ThenBy(report => report.Code)
            .ThenBy(report => report.ReferencedChildEffectId ?? -1)
            .ToArray();

        for (var i = 0; i < sorted.Length; i++)
        {
            if (sorted[i].Severity == ValidationSeverity.Error)
                EffectDefinitionRegistry.MarkInvalid(sorted[i].EffectDefinitionId);
        }

        return sorted;
    }

    private static void ValidateDefinition(
        EffectDefinition definition,
        HashSet<int> knownIds,
        StateEngineConfig config,
        List<ValidationReport> reports)
    {
        if (!definition.HasWork)
        {
            Add(reports, definition, ValidationCode.EmptyEffect, "Effect definition contains no target, condition request, removal rule, or child effects.");
        }

        if (definition.StackPolicy == EffectStackPolicy.Stack && definition.MaxStacks < 1)
        {
            Add(reports, definition, ValidationCode.InvalidMaxStacks, "Stackable effect must allow at least one stack.");
        }

        if (definition.MaxStackOverflowPolicy == MaxStackOverflowPolicy.ReplaceWeakest &&
            !definition.HasDeterministicStrengthComparison)
        {
            Add(reports, definition, ValidationCode.ReplaceWeakestRequiresStrengthComparison,
                "ReplaceWeakest requires a deterministic strength comparison.");
        }

        if (definition.Duration.Type == EffectDurationType.Permanent &&
            definition.Trigger.Type == EffectTriggerType.OnExpire)
        {
            Add(reports, definition, ValidationCode.PermanentOnExpire, "Permanent effects cannot use OnExpire.");
        }

        if (!ConditionRegistry.IsKnown(definition.ClassificationConditions) ||
            !ConditionRegistry.IsKnown(definition.ConditionAddRequest) ||
            !ConditionRegistry.IsKnown(definition.ConditionRemoveRequest) ||
            !ConditionRegistry.IsKnown(definition.BlockedBy))
        {
            Add(reports, definition, ValidationCode.InvalidConditionHierarchy, "Effect references an unknown condition flag.");
        }

        for (var i = 0; i < definition.ChildEffectDefinitionIds.Length; i++)
        {
            var childId = definition.ChildEffectDefinitionIds[i];
            if (!knownIds.Contains(childId))
            {
                Add(reports, definition, ValidationCode.ChildReferenceMissing,
                    $"Child effect definition {childId} is not registered.", childId);
            }
        }

        ValidateChildDepth(definition, knownIds, config, reports);
    }

    private static void ValidateChildDepth(
        EffectDefinition root,
        HashSet<int> knownIds,
        StateEngineConfig config,
        List<ValidationReport> reports)
    {
        var visiting = new HashSet<int>();
        if (Visit(root, knownIds, config, reports, visiting, 0))
        {
            Add(reports, root, ValidationCode.ChildCycle, "Child effect graph contains a cycle.");
        }
    }

    private static bool Visit(
        EffectDefinition definition,
        HashSet<int> knownIds,
        StateEngineConfig config,
        List<ValidationReport> reports,
        HashSet<int> visiting,
        int depth)
    {
        if (depth > config.MaxChildEffectDepth)
        {
            Add(reports, definition, ValidationCode.ChildDepthExceeded,
                $"Child effect graph exceeds configured depth {config.MaxChildEffectDepth}.");
            return false;
        }

        if (!visiting.Add(definition.EffectDefinitionId))
            return true;

        for (var i = 0; i < definition.ChildEffectDefinitionIds.Length; i++)
        {
            var childId = definition.ChildEffectDefinitionIds[i];
            if (!knownIds.Contains(childId) || !EffectDefinitionRegistry.TryGet(childId, out var child))
                continue;

            if (Visit(child, knownIds, config, reports, visiting, depth + 1))
                return true;
        }

        visiting.Remove(definition.EffectDefinitionId);
        return false;
    }

    private static void Add(
        List<ValidationReport> reports,
        EffectDefinition definition,
        ValidationCode code,
        string message,
        int? referencedChildEffectId = null,
        StatType? referencedStat = null,
        LivingEntityCondition referencedCondition = LivingEntityCondition.None)
    {
        reports.Add(new ValidationReport(
            definition.EffectDefinitionId,
            ValidationSeverity.Error,
            code,
            message,
            referencedChildEffectId,
            referencedStat,
            referencedCondition));
    }
}
