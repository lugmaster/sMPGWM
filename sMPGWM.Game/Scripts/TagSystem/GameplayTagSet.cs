

using System;
using System.Runtime.CompilerServices;

namespace sMPGWM.Scripts.TagSystem;

public sealed class GameplayTagSet
{
    internal GameplayTagSet(GameplayTagRegistry registry, GameplayTagDomainId domainId)
    {
        _registry = registry;
        DomainId = domainId;

        var domain = registry.GetDomain(domainId);
        _directMask = new ulong[domain.DomainMaskWordCount];
        _expandedMask = new ulong[domain.DomainMaskWordCount];
    }

    private readonly GameplayTagRegistry _registry;
    private readonly ulong[] _directMask;
    private readonly ulong[] _expandedMask;

    public GameplayTagDomainId DomainId { get; }
    public ReadOnlySpan<ulong> DirectMask => _directMask;
    public ReadOnlySpan<ulong> ExpandedMask => _expandedMask;

    public bool Add(GameplayTagId tagId)
    {
        var tag = ValidateTagForMutation(tagId, requireAssignable: true);
        return AddValidated(tag);
    }

    public bool TryAdd(GameplayTagId tagId)
    {
        return TryValidateTagForMutation(tagId, requireAssignable: true, out var tag) && AddValidated(tag);
    }

    public bool Remove(GameplayTagId tagId)
    {
        var tag = ValidateTagForMutation(tagId, requireAssignable: true);
        return RemoveValidated(tag);
    }

    public bool TryRemove(GameplayTagId tagId)
    {
        return TryValidateTagForMutation(tagId, requireAssignable: true, out var tag) && RemoveValidated(tag);
    }

    public void Clear()
    {
        Array.Clear(_directMask, 0, _directMask.Length);
        Array.Clear(_expandedMask, 0, _expandedMask.Length);
    }

    public bool HasDirect(GameplayTagId tagId)
    {
        var tag = ValidateTagForRead(tagId);
        return IsSet(_directMask, tag.DomainLocalIndex);
    }

    public bool TryHasDirect(GameplayTagId tagId, out bool hasTag)
    {
        if (!TryValidateTagForRead(tagId, out var tag))
        {
            hasTag = false;
            return false;
        }

        hasTag = IsSet(_directMask, tag.DomainLocalIndex);
        return true;
    }

    public bool HasExpanded(GameplayTagId tagId)
    {
        var tag = ValidateTagForRead(tagId);
        return IsSet(_expandedMask, tag.DomainLocalIndex);
    }

    public bool TryHasExpanded(GameplayTagId tagId, out bool hasTag)
    {
        if (!TryValidateTagForRead(tagId, out var tag))
        {
            hasTag = false;
            return false;
        }

        hasTag = IsSet(_expandedMask, tag.DomainLocalIndex);
        return true;
    }

    public bool HasAnyExpanded(ReadOnlySpan<GameplayTagId> tagIds)
    {
        for (var i = 0; i < tagIds.Length; i++)
        {
            if (HasExpanded(tagIds[i]))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAllExpanded(ReadOnlySpan<GameplayTagId> tagIds)
    {
        for (var i = 0; i < tagIds.Length; i++)
        {
            if (!HasExpanded(tagIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasNoneExpanded(ReadOnlySpan<GameplayTagId> tagIds)
    {
        return !HasAnyExpanded(tagIds);
    }

    public bool Matches(TagQueryId queryId)
    {
        return Matches(_registry.GetQuery(queryId));
    }

    public bool TryMatches(TagQueryId queryId, out bool matches)
    {
        if (!_registry.TryGetQuery(queryId, out var query))
        {
            matches = false;
            return false;
        }

        return TryMatches(query, out matches);
    }

    public bool Matches(CompiledTagQuery query)
    {
        if (!_registry.TryGetQuery(query.Id, out var registeredQuery) || !ReferenceEquals(query, registeredQuery))
        {
            throw new GameplayTagException($"Query '{query.QueryKey}' does not belong to this tag set's registry.");
        }

        if (query.DomainId != DomainId)
        {
            throw new GameplayTagException(
                $"Query '{query.QueryKey}' belongs to domain '{query.DomainId}', but tag set belongs to domain '{DomainId}'.");
        }

        return MatchQuery(query);
    }

    public bool TryMatches(CompiledTagQuery query, out bool matches)
    {
        if (!_registry.TryGetQuery(query.Id, out var registeredQuery) || !ReferenceEquals(query, registeredQuery))
        {
            matches = false;
            return false;
        }

        if (query.DomainId != DomainId)
        {
            matches = false;
            return false;
        }

        matches = MatchQuery(query);
        return true;
    }

    private GameplayTagDefinition ValidateTagForMutation(GameplayTagId tagId, bool requireAssignable)
    {
        var tag = ValidateTagForRead(tagId);
        if (requireAssignable && !tag.IsAssignable)
        {
            throw new GameplayTagException($"Gameplay tag '{tag.CanonicalName}' is not assignable.");
        }

        return tag;
    }

    private bool TryValidateTagForMutation(GameplayTagId tagId, bool requireAssignable, out GameplayTagDefinition tag)
    {
        if (!TryValidateTagForRead(tagId, out tag))
        {
            return false;
        }

        return !requireAssignable || tag.IsAssignable;
    }

    private GameplayTagDefinition ValidateTagForRead(GameplayTagId tagId)
    {
        var tag = _registry.GetTag(tagId);
        if (tag.DomainId != DomainId)
        {
            throw new GameplayTagException(
                $"Gameplay tag '{tag.CanonicalName}' belongs to domain '{tag.DomainId}', but tag set belongs to domain '{DomainId}'.");
        }

        return tag;
    }

    private bool TryValidateTagForRead(GameplayTagId tagId, out GameplayTagDefinition tag)
    {
        if (!_registry.TryGetTag(tagId, out tag))
        {
            return false;
        }

        return tag.DomainId == DomainId;
    }

    private bool AddValidated(GameplayTagDefinition tag)
    {
        if (IsSet(_directMask, tag.DomainLocalIndex))
        {
            return false;
        }

        SetBit(_directMask, tag.DomainLocalIndex);
        OrMask(_expandedMask, _registry.GetExpansionMask(tag.Id));
        return true;
    }

    private bool RemoveValidated(GameplayTagDefinition tag)
    {
        if (!IsSet(_directMask, tag.DomainLocalIndex))
        {
            return false;
        }

        ClearBit(_directMask, tag.DomainLocalIndex);
        RebuildExpandedMask();
        return true;
    }

    private void RebuildExpandedMask()
    {
        Array.Clear(_expandedMask, 0, _expandedMask.Length);

        var domain = _registry.GetDomain(DomainId);
        for (var localIndex = 0; localIndex < domain.CanonicalTagCount; localIndex++)
        {
            if (!IsSet(_directMask, localIndex))
            {
                continue;
            }

            var tag = _registry.GetTagByDomainLocalIndex(DomainId, localIndex);
            OrMask(_expandedMask, _registry.GetExpansionMask(tag.Id));
        }
    }

    private bool MatchQuery(CompiledTagQuery query)
    {
        var sourceMask = query.MatchMode == TagQueryMatchMode.Direct ? _directMask : _expandedMask;

        if (query.AllTagCount > 0 && !ContainsAll(sourceMask, query.AllMask))
        {
            return false;
        }

        if (query.AnyTagCount > 0 && !ContainsAny(sourceMask, query.AnyMask))
        {
            return false;
        }

        if (query.AllTagCount == 0 && query.AnyTagCount == 0 && !query.MatchAll)
        {
            return false;
        }

        return query.NoneTagCount == 0 || !ContainsAny(sourceMask, query.NoneMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSet(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        var bit = 1UL << (domainLocalIndex & 63);
        return (mask[wordIndex] & bit) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBit(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        mask[wordIndex] |= 1UL << (domainLocalIndex & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearBit(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        mask[wordIndex] &= ~(1UL << (domainLocalIndex & 63));
    }

    private static bool ContainsAll(ulong[] sourceMask, ulong[] requiredMask)
    {
        for (var i = 0; i < sourceMask.Length; i++)
        {
            if ((sourceMask[i] & requiredMask[i]) != requiredMask[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsAny(ulong[] sourceMask, ulong[] requiredMask)
    {
        for (var i = 0; i < sourceMask.Length; i++)
        {
            if ((sourceMask[i] & requiredMask[i]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void OrMask(ulong[] targetMask, ulong[] sourceMask)
    {
        for (var i = 0; i < targetMask.Length; i++)
        {
            targetMask[i] |= sourceMask[i];
        }
    }
}
