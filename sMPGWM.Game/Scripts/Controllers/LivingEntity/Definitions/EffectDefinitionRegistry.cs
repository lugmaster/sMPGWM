using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Definitions;

/// <summary>
/// Process-wide registry for immutable effect definitions accepted by the state engine.
/// </summary>
public static class EffectDefinitionRegistry
{
    private static readonly Dictionary<int, EffectDefinition> Definitions = new();
    private static readonly HashSet<int> InvalidDefinitionIds = new();

    /// <summary>
    /// Removes all registered and invalidated effect definitions.
    /// </summary>
    public static void Clear()
    {
        Definitions.Clear();
        InvalidDefinitionIds.Clear();
    }

    /// <summary>
    /// Registers or replaces an effect definition.
    /// </summary>
    public static void Register(EffectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definitions[definition.EffectDefinitionId] = definition;
        InvalidDefinitionIds.Remove(definition.EffectDefinitionId);
    }

    /// <summary>
    /// Registers each effect definition in the supplied sequence.
    /// </summary>
    public static void RegisterRange(IEnumerable<EffectDefinition> definitions)
    {
        foreach (var definition in definitions)
            Register(definition);
    }

    /// <summary>
    /// Attempts to retrieve a registered and valid effect definition.
    /// </summary>
    public static bool TryGet(int effectDefinitionId, out EffectDefinition definition)
    {
        return Definitions.TryGetValue(effectDefinitionId, out definition) &&
               !InvalidDefinitionIds.Contains(effectDefinitionId);
    }

    /// <summary>
    /// Returns true when a definition id has been registered, even if later marked invalid.
    /// </summary>
    public static bool Contains(int effectDefinitionId)
    {
        return Definitions.ContainsKey(effectDefinitionId);
    }

    /// <summary>
    /// Gets a registered and valid definition or throws if it is missing or invalid.
    /// </summary>
    public static EffectDefinition GetRequired(int effectDefinitionId)
    {
        if (TryGet(effectDefinitionId, out var definition))
            return definition;

        throw new InvalidOperationException($"EffectDefinition {effectDefinitionId} is not registered or is invalid.");
    }

    /// <summary>
    /// Gets all registered effect definitions.
    /// </summary>
    public static IReadOnlyCollection<EffectDefinition> All => Definitions.Values;

    /// <summary>
    /// Marks a definition id as invalid so runtime processing rejects it deterministically.
    /// </summary>
    public static void MarkInvalid(int effectDefinitionId)
    {
        InvalidDefinitionIds.Add(effectDefinitionId);
    }
}
