using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

public static class EffectDefinitionRegistry
{
    private static readonly Dictionary<int, EffectDefinition> Definitions = new();
    private static readonly HashSet<int> InvalidDefinitionIds = new();

    public static void Clear()
    {
        Definitions.Clear();
        InvalidDefinitionIds.Clear();
    }

    public static void Register(EffectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definitions[definition.EffectDefinitionId] = definition;
        InvalidDefinitionIds.Remove(definition.EffectDefinitionId);
    }

    public static void RegisterRange(IEnumerable<EffectDefinition> definitions)
    {
        foreach (var definition in definitions)
            Register(definition);
    }

    public static bool TryGet(int effectDefinitionId, out EffectDefinition definition)
    {
        return Definitions.TryGetValue(effectDefinitionId, out definition) &&
               !InvalidDefinitionIds.Contains(effectDefinitionId);
    }

    public static bool Contains(int effectDefinitionId)
    {
        return Definitions.ContainsKey(effectDefinitionId);
    }

    public static EffectDefinition GetRequired(int effectDefinitionId)
    {
        if (TryGet(effectDefinitionId, out var definition))
            return definition;

        throw new InvalidOperationException($"EffectDefinition {effectDefinitionId} is not registered or is invalid.");
    }

    public static IReadOnlyCollection<EffectDefinition> All => Definitions.Values;

    public static void MarkInvalid(int effectDefinitionId)
    {
        InvalidDefinitionIds.Add(effectDefinitionId);
    }
}
