using System;
using System.Collections.Generic;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.GameplayDataSystem;

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
    public IReadOnlyList<AbilityGrantState> CaptureState()
    {
        var result = new AbilityGrantState[_sources.Count + _ownedSources.Count];
        var index = 0;
        foreach (var pair in _sources)
            result[index++] = new(pair.Key, null, Array.AsReadOnly((string[])pair.Value.Clone()));
        foreach (var pair in _ownedSources)
            result[index++] = new(null, pair.Key, Array.AsReadOnly((string[])pair.Value.Clone()));
        return Array.AsReadOnly(result);
    }

    internal Action PrepareRestore(IReadOnlyList<AbilityGrantState> state, GameplayCatalogues definitions)
    {
        var named = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var owned = new Dictionary<StatContributionSource, string[]>();
        foreach (var grant in state)
        {
            if (grant == null || (grant.Name == null) == (grant.Source == null) || grant.AbilityIds == null)
                throw new ArgumentException("Invalid restored ability grant source.");
            var ids = new string[grant.AbilityIds.Count];
            for (var i = 0; i < ids.Length; i++)
            {
                var id = grant.AbilityIds[i];
                if (string.IsNullOrWhiteSpace(id) || !definitions.Abilities.TryGetById(id, out _))
                    throw new ArgumentException("Unknown restored ability grant: " + id);
                ids[i] = id;
            }
            if (grant.Source is { } source)
            {
                if (source.Identity <= 0 || !Enum.IsDefined(source.Kind) || !owned.TryAdd(source, ids))
                    throw new ArgumentException("Invalid or duplicate restored ability grant source.");
            }
            else if (string.IsNullOrWhiteSpace(grant.Name) || !named.TryAdd(grant.Name, ids))
                throw new ArgumentException("Invalid or duplicate restored named ability grant.");
        }
        foreach (var pair in _ownedSources)
            if (pair.Key.Kind == StatContributionKind.Equipment &&
                (!owned.TryGetValue(pair.Key, out var ids) || !SameIds(pair.Value, ids)))
                throw new ArgumentException("Restored grants do not match installed equipment.");
        foreach (var pair in owned)
            if (pair.Key.Kind == StatContributionKind.Equipment && !_ownedSources.ContainsKey(pair.Key))
                throw new ArgumentException("Restored grant refers to missing equipment.");
        _sources.EnsureCapacity(named.Count);
        _ownedSources.EnsureCapacity(owned.Count);
        return () =>
        {
            _sources.Clear();
            _ownedSources.Clear();
            foreach (var pair in named) _sources.Add(pair.Key, pair.Value);
            foreach (var pair in owned) _ownedSources.Add(pair.Key, pair.Value);
        };
    }

    private static bool SameIds(string[] left, string[] right)
    {
        if (left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }
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
