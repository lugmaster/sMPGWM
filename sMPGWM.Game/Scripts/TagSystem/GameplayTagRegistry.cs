using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace sMPGWM.Scripts.TagSystem;

public sealed class GameplayTagRegistry
{
    internal GameplayTagRegistry(
        GameplayTagDomain[] domainsByIndex,
        GameplayTagDefinition[] tagsByIndex,
        GameplayTagDefinition[][] tagsByDomainLocalIndex,
        ulong[][] expansionMasksByTagIndex,
        CompiledTagQuery[] queriesByIndex,
        Dictionary<string, GameplayTagDomainId> domainIdsByKey,
        Dictionary<string, GameplayTagDomainId> domainIdsByRootPrefix,
        Dictionary<string, GameplayTagId> tagIdsByCanonicalName,
        Dictionary<string, TagQueryId> queryIdsByKey,
        ulong registryHash)
    {
        _domainsByIndex = domainsByIndex;
        _tagsByIndex = tagsByIndex;
        _tagsByDomainLocalIndex = tagsByDomainLocalIndex;
        _expansionMasksByTagIndex = expansionMasksByTagIndex;
        _queriesByIndex = queriesByIndex;
        Domains = Array.AsReadOnly(domainsByIndex);
        Tags = Array.AsReadOnly(tagsByIndex);
        Queries = Array.AsReadOnly(queriesByIndex);
        _domainIdsByKey = domainIdsByKey;
        _domainIdsByRootPrefix = domainIdsByRootPrefix;
        _tagIdsByCanonicalName = tagIdsByCanonicalName;
        _queryIdsByKey = queryIdsByKey;
        RegistryHash = registryHash;
    }

    private readonly GameplayTagDomain[] _domainsByIndex;
    private readonly GameplayTagDefinition[] _tagsByIndex;
    private readonly GameplayTagDefinition[][] _tagsByDomainLocalIndex;
    private readonly ulong[][] _expansionMasksByTagIndex;
    private readonly CompiledTagQuery[] _queriesByIndex;
    private readonly Dictionary<string, GameplayTagDomainId> _domainIdsByKey;
    private readonly Dictionary<string, GameplayTagDomainId> _domainIdsByRootPrefix;
    private readonly Dictionary<string, GameplayTagId> _tagIdsByCanonicalName;
    private readonly Dictionary<string, TagQueryId> _queryIdsByKey;

    public ulong RegistryHash { get; }
    public IReadOnlyList<GameplayTagDomain> Domains { get; }
    public IReadOnlyList<GameplayTagDefinition> Tags { get; }
    public IReadOnlyList<CompiledTagQuery> Queries { get; }

    public GameplayTagDomainId GetDomainId(string domainKey)
    {
        if (TryGetDomainId(domainKey, out var domainId))
        {
            return domainId;
        }

        throw new GameplayTagException($"Unknown gameplay tag domain '{domainKey}'.");
    }

    public bool TryGetDomainId(string domainKey, out GameplayTagDomainId domainId)
    {
        return _domainIdsByKey.TryGetValue(domainKey, out domainId);
    }

    public GameplayTagDomainId GetDomainIdByRootPrefix(string rootPrefix)
    {
        if (_domainIdsByRootPrefix.TryGetValue(rootPrefix, out var domainId))
        {
            return domainId;
        }

        throw new GameplayTagException($"Unknown gameplay tag root prefix '{rootPrefix}'.");
    }

    public GameplayTagDomain GetDomain(GameplayTagDomainId domainId)
    {
        if (!TryGetDomain(domainId, out var domain))
        {
            throw new GameplayTagException($"Invalid gameplay tag domain id '{domainId}'.");
        }

        return domain;
    }

    public bool TryGetDomain(GameplayTagDomainId domainId, out GameplayTagDomain domain)
    {
        if (!domainId.IsValid || domainId.Value > _domainsByIndex.Length)
        {
            domain = null!;
            return false;
        }

        domain = _domainsByIndex[domainId.Value - 1];
        return true;
    }

    public GameplayTagId GetTagId(string canonicalName)
    {
        if (TryGetTagId(canonicalName, out var tagId))
        {
            return tagId;
        }

        throw new GameplayTagException($"Unknown gameplay tag '{canonicalName}'.");
    }

    public bool TryGetTagId(string canonicalName, out GameplayTagId tagId)
    {
        return _tagIdsByCanonicalName.TryGetValue(canonicalName, out tagId);
    }

    public GameplayTagDefinition GetTag(GameplayTagId tagId)
    {
        if (!TryGetTag(tagId, out var tag))
        {
            throw new GameplayTagException($"Invalid gameplay tag id '{tagId}'.");
        }

        return tag;
    }

    public bool TryGetTag(GameplayTagId tagId, out GameplayTagDefinition tag)
    {
        if (!tagId.IsValid || tagId.Value > _tagsByIndex.Length)
        {
            tag = null!;
            return false;
        }

        tag = _tagsByIndex[tagId.Value - 1];
        return true;
    }

    public TagQueryId GetQueryId(string queryKey)
    {
        if (TryGetQueryId(queryKey, out var queryId))
        {
            return queryId;
        }

        throw new GameplayTagException($"Unknown gameplay tag query '{queryKey}'.");
    }

    public bool TryGetQueryId(string queryKey, out TagQueryId queryId)
    {
        return _queryIdsByKey.TryGetValue(queryKey, out queryId);
    }

    public CompiledTagQuery GetQuery(TagQueryId queryId)
    {
        if (!TryGetQuery(queryId, out var query))
        {
            throw new GameplayTagException($"Invalid gameplay tag query id '{queryId}'.");
        }

        return query;
    }

    public bool TryGetQuery(TagQueryId queryId, out CompiledTagQuery query)
    {
        if (!queryId.IsValid || queryId.Value > _queriesByIndex.Length)
        {
            query = null!;
            return false;
        }

        query = _queriesByIndex[queryId.Value - 1];
        return true;
    }

    public GameplayTagSet CreateTagSet(string domainKey)
    {
        return CreateTagSet(GetDomainId(domainKey));
    }

    public GameplayTagSet CreateTagSet(GameplayTagDomainId domainId)
    {
        var domain = GetDomain(domainId);
        return new GameplayTagSet(this, domain.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal GameplayTagDefinition GetTagByDomainLocalIndex(GameplayTagDomainId domainId, int domainLocalIndex)
    {
        return _tagsByDomainLocalIndex[domainId.Value - 1][domainLocalIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong[] GetExpansionMask(GameplayTagId tagId)
    {
        return _expansionMasksByTagIndex[tagId.Value - 1];
    }
}