using System;
using System.Collections.Generic;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed class AbilityGrants
{
    private readonly Dictionary<StatContributionSource, string[]> _ownedSources = new();
    private readonly Dictionary<string, string[]> _sources = new(StringComparer.Ordinal);
    public void SetSource(string sourceId, IReadOnlyList<string> abilityIds)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A concrete grant source is required.", nameof(sourceId));
        var copy = new string[abilityIds.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = abilityIds[i];
        _sources[sourceId] = copy;
    }
    public void SetSource(StatContributionSource source, IReadOnlyList<string> abilityIds) => PrepareSource(source, abilityIds)();
    public Action PrepareSource(StatContributionSource source, IReadOnlyList<string> abilityIds)
    {
        if (source.Identity <= 0 || !Enum.IsDefined(source.Kind)) throw new ArgumentOutOfRangeException(nameof(source));
        var copy = new string[abilityIds.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = abilityIds[i];
        _ownedSources.EnsureCapacity(_ownedSources.Count + 64);
        return () => _ownedSources[source] = copy;
    }
    public bool RemoveSource(StatContributionSource source) => _ownedSources.Remove(source);
    internal void Clear() { _sources.Clear(); _ownedSources.Clear(); }
    public bool RemoveSource(string sourceId) => _sources.Remove(sourceId);
    public bool ContainsSource(string sourceId, string abilityId)
    {
        if (!_sources.TryGetValue(sourceId, out var ids))
        {
            if (!sourceId.StartsWith("item:", StringComparison.Ordinal) || !long.TryParse(sourceId.AsSpan(5), out var identity) ||
                !_ownedSources.TryGetValue(new(StatContributionKind.Equipment, identity), out ids)) return false;
        }
        foreach (var id in ids)
            if (id == abilityId) return true;
        return false;
    }
    public bool Contains(string abilityId)
    {
        foreach (var ids in _ownedSources.Values)
            foreach (var id in ids)
                if (id == abilityId) return true;
        foreach (var ids in _sources.Values)
            foreach (var id in ids)
                if (id == abilityId) return true;
        return false;
    }
}
