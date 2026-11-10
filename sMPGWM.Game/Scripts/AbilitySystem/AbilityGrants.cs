using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityGrants
{
    private readonly Dictionary<string, string[]> _sources = new(StringComparer.Ordinal);
    public void SetSource(string sourceId, IReadOnlyList<string> abilityIds)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A concrete grant source is required.", nameof(sourceId));
        var copy = new string[abilityIds.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = abilityIds[i];
        _sources[sourceId] = copy;
    }
    public bool RemoveSource(string sourceId) => _sources.Remove(sourceId);
    public bool ContainsSource(string sourceId, string abilityId)
    {
        if (!_sources.TryGetValue(sourceId, out var ids)) return false;
        foreach (var id in ids)
            if (id == abilityId) return true;
        return false;
    }
    public bool Contains(string abilityId)
    {
        foreach (var ids in _sources.Values)
            foreach (var id in ids)
                if (id == abilityId) return true;
        return false;
    }
}
