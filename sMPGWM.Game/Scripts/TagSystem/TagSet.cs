using System;
using System.Runtime.CompilerServices;
using sMPGWM.Scripts.TagSystem.Definitions;
using sMPGWM.Scripts.TagSystem.Exceptions;
using sMPGWM.Scripts.TagSystem.Ids;
using sMPGWM.Scripts.TagSystem.Queries;

namespace sMPGWM.Scripts.TagSystem;

/// <summary>
/// Stores mutable direct tag assignments and their ancestor expansion for one registry domain.
/// </summary>
public sealed class TagSet
{
    /// <summary>
    /// Initializes an empty tag set owned by the specified registry and domain.
    /// </summary>
    internal TagSet(TagRegistry registry, TagDomainId domainId)
    {
        _registry = registry;
        DomainId = domainId;

        var domain = registry.GetDomain(domainId);
        _directMask = new ulong[domain.DomainMaskWordCount];
        _expandedMask = new ulong[domain.DomainMaskWordCount];
    }

    private readonly TagRegistry _registry;
    private readonly ulong[] _directMask;
    private readonly ulong[] _expandedMask;

    public TagDomainId DomainId { get; }
    public ReadOnlySpan<ulong> DirectMask => _directMask;
    public ReadOnlySpan<ulong> ExpandedMask => _expandedMask;

    /// <summary>
    /// Adds an assignable tag to the direct assignments and expands its ancestors.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>True if a new direct assignment was added; otherwise, false.</returns>
    /// <exception cref="TagException">The tag is invalid, belongs to another domain, or is not assignable.</exception>
    public bool Add(TagId tagId)
    {
        var tag = ValidateTagForMutation(tagId, requireAssignable: true);
        return AddValidated(tag);
    }

    /// <summary>
    /// Attempts to add an assignable tag, returning false for invalid tags or existing assignments.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>True if a new direct assignment was added; otherwise, false.</returns>
    public bool TryAdd(TagId tagId)
    {
        return TryValidateTagForMutation(tagId, requireAssignable: true, out var tag) && AddValidated(tag);
    }

    /// <summary>
    /// Removes a direct assignment and rebuilds the ancestor expansion.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>True if a direct assignment was removed; otherwise, false.</returns>
    /// <exception cref="TagException">The tag is invalid, belongs to another domain, or is not assignable.</exception>
    public bool Remove(TagId tagId)
    {
        var tag = ValidateTagForMutation(tagId, requireAssignable: true);
        return RemoveValidated(tag);
    }

    /// <summary>
    /// Attempts to remove an assignable tag, returning false for invalid tags or absent assignments.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>True if a direct assignment was removed; otherwise, false.</returns>
    public bool TryRemove(TagId tagId)
    {
        return TryValidateTagForMutation(tagId, requireAssignable: true, out var tag) && RemoveValidated(tag);
    }

    /// <summary>
    /// Removes all direct assignments and expanded ancestor membership.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_directMask, 0, _directMask.Length);
        Array.Clear(_expandedMask, 0, _expandedMask.Length);
    }

    /// <summary>
    /// Determines whether a tag is directly assigned.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>True if the tag is directly assigned; otherwise, false.</returns>
    /// <exception cref="TagException">An examined tag identifier is invalid or belongs to another domain.</exception>
    public bool HasDirect(TagId tagId)
    {
        var tag = ValidateTagForRead(tagId);
        return IsSet(_directMask, tag.DomainLocalIndex);
    }

    /// <summary>
    /// Attempts to read direct membership, returning false for an invalid or cross-domain tag.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <param name="hasTag">Receives the membership result, or false when the tag is invalid.</param>
    /// <returns>True if the tag is valid for this set; otherwise, false.</returns>
    public bool TryHasDirect(TagId tagId, out bool hasTag)
    {
        if (!TryValidateTagForRead(tagId, out var tag))
        {
            hasTag = false;
            return false;
        }

        hasTag = IsSet(_directMask, tag.DomainLocalIndex);
        return true;
    }

    /// <summary>
    /// Determines whether a tag is directly assigned or included through an assigned descendant.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>True if the tag has direct or ancestor membership; otherwise, false.</returns>
    /// <exception cref="TagException">An examined tag identifier is invalid or belongs to another domain.</exception>
    public bool HasExpanded(TagId tagId)
    {
        var tag = ValidateTagForRead(tagId);
        return IsSet(_expandedMask, tag.DomainLocalIndex);
    }

    /// <summary>
    /// Attempts to read expanded membership, returning false for an invalid or cross-domain tag.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <param name="hasTag">Receives the membership result, or false when the tag is invalid.</param>
    /// <returns>True if the tag is valid for this set; otherwise, false.</returns>
    public bool TryHasExpanded(TagId tagId, out bool hasTag)
    {
        if (!TryValidateTagForRead(tagId, out var tag))
        {
            hasTag = false;
            return false;
        }

        hasTag = IsSet(_expandedMask, tag.DomainLocalIndex);
        return true;
    }

    /// <summary>
    /// Determines whether any supplied tag has expanded membership; an empty input returns false.
    /// </summary>
    /// <param name="tagIds">The tag identifiers to examine, relative to this registry.</param>
    /// <returns>True if any supplied tag has expanded membership.</returns>
    /// <exception cref="TagException">An examined tag identifier is invalid or belongs to another domain.</exception>
    public bool HasAnyExpanded(ReadOnlySpan<TagId> tagIds)
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

    /// <summary>
    /// Determines whether every supplied tag has expanded membership; an empty input returns true.
    /// </summary>
    /// <param name="tagIds">The tag identifiers to examine, relative to this registry.</param>
    /// <returns>True if all supplied tags have expanded membership.</returns>
    /// <exception cref="TagException">An examined tag identifier is invalid or belongs to another domain.</exception>
    public bool HasAllExpanded(ReadOnlySpan<TagId> tagIds)
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

    /// <summary>
    /// Determines whether no supplied tag has expanded membership; an empty input returns true.
    /// </summary>
    /// <param name="tagIds">The tag identifiers to examine, relative to this registry.</param>
    /// <returns>True if no supplied tag has expanded membership.</returns>
    /// <exception cref="TagException">An examined tag identifier is invalid or belongs to another domain.</exception>
    public bool HasNoneExpanded(ReadOnlySpan<TagId> tagIds)
    {
        return !HasAnyExpanded(tagIds);
    }

    /// <summary>
    /// Evaluates a query owned by this set's registry and belonging to this set's domain.
    /// </summary>
    /// <param name="queryId">The query identifier relative to this registry.</param>
    /// <returns>True if the query matches this set; otherwise, false.</returns>
    /// <exception cref="TagException">The query is invalid, belongs to another registry, or targets another domain.</exception>
    public bool Matches(TagQueryId queryId)
    {
        return Matches(_registry.GetQuery(queryId));
    }

    /// <summary>
    /// Attempts query evaluation, returning false for an invalid, foreign, or cross-domain query.
    /// </summary>
    /// <param name="queryId">The query identifier relative to this registry.</param>
    /// <param name="matches">Receives the match result, or false when evaluation cannot be performed.</param>
    /// <returns>True if the query can be evaluated; otherwise, false.</returns>
    public bool TryMatches(TagQueryId queryId, out bool matches)
    {
        if (!_registry.TryGetQuery(queryId, out var query))
        {
            matches = false;
            return false;
        }

        return TryMatches(query, out matches);
    }

    /// <summary>
    /// Evaluates a query owned by this set's registry and belonging to this set's domain.
    /// </summary>
    /// <param name="query">The compiled query owned by this set's registry.</param>
    /// <returns>True if the query matches this set; otherwise, false.</returns>
    /// <exception cref="TagException">The query is invalid, belongs to another registry, or targets another domain.</exception>
    public bool Matches(CompiledTagQuery query)
    {
        if (!_registry.TryGetQuery(query.Id, out var registeredQuery) || !ReferenceEquals(query, registeredQuery))
        {
            throw new TagException($"Query '{query.QueryKey}' does not belong to this tag set's registry.");
        }

        if (query.DomainId != DomainId)
        {
            throw new TagException(
                $"Query '{query.QueryKey}' belongs to domain '{query.DomainId}', but tag set belongs to domain '{DomainId}'.");
        }

        return MatchQuery(query);
    }

    /// <summary>
    /// Attempts query evaluation, returning false for an invalid, foreign, or cross-domain query.
    /// </summary>
    /// <param name="query">The compiled query owned by this set's registry.</param>
    /// <param name="matches">Receives the match result, or false when evaluation cannot be performed.</param>
    /// <returns>True if the query can be evaluated; otherwise, false.</returns>
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

    /// <summary>
    /// Retrieves a tag in this set's domain and enforces assignment rules.
    /// </summary>
    private TagDefinition ValidateTagForMutation(TagId tagId, bool requireAssignable)
    {
        var tag = ValidateTagForRead(tagId);
        if (requireAssignable && !tag.IsAssignable)
        {
            throw new TagException($"Gameplay tag '{tag.CanonicalName}' is not assignable.");
        }

        return tag;
    }

    /// <summary>
    /// Attempts to retrieve a tag in this set's domain while enforcing assignment rules.
    /// </summary>
    private bool TryValidateTagForMutation(TagId tagId, bool requireAssignable, out TagDefinition tag)
    {
        if (!TryValidateTagForRead(tagId, out tag))
        {
            return false;
        }

        return !requireAssignable || tag.IsAssignable;
    }

    /// <summary>
    /// Retrieves a tag and requires it to belong to this set's domain.
    /// </summary>
    private TagDefinition ValidateTagForRead(TagId tagId)
    {
        var tag = _registry.GetTag(tagId);
        if (tag.DomainId != DomainId)
        {
            throw new TagException(
                $"Gameplay tag '{tag.CanonicalName}' belongs to domain '{tag.DomainId}', but tag set belongs to domain '{DomainId}'.");
        }

        return tag;
    }

    /// <summary>
    /// Attempts to retrieve a tag belonging to this set's domain.
    /// </summary>
    private bool TryValidateTagForRead(TagId tagId, out TagDefinition tag)
    {
        if (!_registry.TryGetTag(tagId, out tag))
        {
            return false;
        }

        return tag.DomainId == DomainId;
    }

    /// <summary>
    /// Adds a validated tag if absent and unions its expansion into the expanded mask.
    /// </summary>
    private bool AddValidated(TagDefinition tag)
    {
        if (IsSet(_directMask, tag.DomainLocalIndex))
        {
            return false;
        }

        SetBit(_directMask, tag.DomainLocalIndex);
        OrMask(_expandedMask, _registry.GetExpansionMask(tag.Id));
        return true;
    }

    /// <summary>
    /// Removes a validated direct assignment if present and rebuilds expanded membership.
    /// </summary>
    private bool RemoveValidated(TagDefinition tag)
    {
        if (!IsSet(_directMask, tag.DomainLocalIndex))
        {
            return false;
        }

        ClearBit(_directMask, tag.DomainLocalIndex);
        RebuildExpandedMask();
        return true;
    }

    /// <summary>
    /// Recomputes ancestor membership from the remaining direct assignments.
    /// </summary>
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

    /// <summary>
    /// Evaluates the compiled All, Any, and None masks against the selected membership mask.
    /// </summary>
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

    /// <summary>
    /// Determines whether the bit at a domain-local tag index is set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSet(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        var bit = 1UL << (domainLocalIndex & 63);
        return (mask[wordIndex] & bit) != 0;
    }

    /// <summary>
    /// Sets the bit at a domain-local tag index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBit(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        mask[wordIndex] |= 1UL << (domainLocalIndex & 63);
    }

    /// <summary>
    /// Clears the bit at a domain-local tag index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearBit(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        mask[wordIndex] &= ~(1UL << (domainLocalIndex & 63));
    }

    /// <summary>
    /// Determines whether the source mask contains every required bit.
    /// </summary>
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

    /// <summary>
    /// Determines whether the source mask contains any required bit.
    /// </summary>
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

    /// <summary>
    /// Combines a source mask into the destination mask using bitwise union.
    /// </summary>
    private static void OrMask(ulong[] targetMask, ulong[] sourceMask)
    {
        for (var i = 0; i < targetMask.Length; i++)
        {
            targetMask[i] |= sourceMask[i];
        }
    }
}
