using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using sMPGWM.Scripts.TagSystem.Definitions;
using sMPGWM.Scripts.TagSystem.Exceptions;
using sMPGWM.Scripts.TagSystem.Ids;
using sMPGWM.Scripts.TagSystem.Queries;

namespace sMPGWM.Scripts.TagSystem;

/// <summary>
/// Provides immutable compiled domains, tags, queries, and registry-relative identifier lookups.
/// </summary>
public sealed class TagRegistry
{
    /// <summary>
    /// Initializes the immutable compiled tables, read-only views, and lookup dictionaries.
    /// </summary>
    internal TagRegistry(
        TagDomain[] domainsByIndex,
        TagDefinition[] tagsByIndex,
        TagDefinition[][] tagsByDomainLocalIndex,
        ulong[][] expansionMasksByTagIndex,
        CompiledTagQuery[] queriesByIndex,
        Dictionary<string, TagDomainId> domainIdsByKey,
        Dictionary<string, TagDomainId> domainIdsByRootPrefix,
        Dictionary<string, TagId> tagIdsByCanonicalName,
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

    private readonly TagDomain[] _domainsByIndex;
    private readonly TagDefinition[] _tagsByIndex;
    private readonly TagDefinition[][] _tagsByDomainLocalIndex;
    private readonly ulong[][] _expansionMasksByTagIndex;
    private readonly CompiledTagQuery[] _queriesByIndex;
    private readonly Dictionary<string, TagDomainId> _domainIdsByKey;
    private readonly Dictionary<string, TagDomainId> _domainIdsByRootPrefix;
    private readonly Dictionary<string, TagId> _tagIdsByCanonicalName;
    private readonly Dictionary<string, TagQueryId> _queryIdsByKey;

    public ulong RegistryHash { get; }
    public IReadOnlyList<TagDomain> Domains { get; }
    public IReadOnlyList<TagDefinition> Tags { get; }
    public IReadOnlyList<CompiledTagQuery> Queries { get; }

    /// <summary>
    /// Resolves a domain key to its registry-relative identifier.
    /// </summary>
    /// <param name="domainKey">The registered domain key.</param>
    /// <returns>The domain identifier relative to this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagDomainId GetDomainId(string domainKey)
    {
        if (TryGetDomainId(domainKey, out var domainId))
        {
            return domainId;
        }

        throw new TagException($"Unknown gameplay tag domain '{domainKey}'.");
    }

    /// <summary>
    /// Attempts to resolve a domain key without throwing for an unknown key.
    /// </summary>
    /// <param name="domainKey">The registered domain key.</param>
    /// <param name="domainId">Receives the domain identifier, or the invalid identifier when the key is unknown.</param>
    /// <returns>True if the domain key is registered; otherwise, false.</returns>
    public bool TryGetDomainId(string domainKey, out TagDomainId domainId)
    {
        return _domainIdsByKey.TryGetValue(domainKey, out domainId);
    }

    /// <summary>
    /// Resolves a root prefix to its registry-relative domain identifier.
    /// </summary>
    /// <param name="rootPrefix">The canonical root prefix for the domain.</param>
    /// <returns>The domain identifier relative to this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagDomainId GetDomainIdByRootPrefix(string rootPrefix)
    {
        if (_domainIdsByRootPrefix.TryGetValue(rootPrefix, out var domainId))
        {
            return domainId;
        }

        throw new TagException($"Unknown gameplay tag root prefix '{rootPrefix}'.");
    }

    /// <summary>
    /// Returns the compiled domain identified by a registry-relative identifier.
    /// </summary>
    /// <param name="domainId">The domain identifier relative to this registry.</param>
    /// <returns>The immutable compiled domain.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagDomain GetDomain(TagDomainId domainId)
    {
        if (!TryGetDomain(domainId, out var domain))
        {
            throw new TagException($"Invalid gameplay tag domain id '{domainId}'.");
        }

        return domain;
    }

    /// <summary>
    /// Attempts to retrieve a compiled domain without throwing for an invalid identifier.
    /// </summary>
    /// <param name="domainId">The domain identifier relative to this registry.</param>
    /// <param name="domain">Receives the compiled domain, or null when the identifier is invalid.</param>
    /// <returns>True if the domain identifier is valid; otherwise, false.</returns>
    private bool TryGetDomain(TagDomainId domainId, out TagDomain domain)
    {
        if (!domainId.IsValid || domainId.Value > _domainsByIndex.Length)
        {
            domain = null!;
            return false;
        }

        domain = _domainsByIndex[domainId.Value - 1];
        return true;
    }

    /// <summary>
    /// Resolves a canonical tag name to its registry-relative identifier.
    /// </summary>
    /// <param name="canonicalName">The fully qualified canonical tag name.</param>
    /// <returns>The tag identifier relative to this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagId GetTagId(string canonicalName)
    {
        if (TryGetTagId(canonicalName, out var tagId))
        {
            return tagId;
        }

        throw new TagException($"Unknown gameplay tag '{canonicalName}'.");
    }

    /// <summary>
    /// Attempts to resolve a canonical tag name without throwing for an unknown name.
    /// </summary>
    /// <param name="canonicalName">The fully qualified canonical tag name.</param>
    /// <param name="tagId">Receives the tag identifier, or the invalid identifier when the name is unknown.</param>
    /// <returns>True if the canonical tag name is registered; otherwise, false.</returns>
    public bool TryGetTagId(string canonicalName, out TagId tagId)
    {
        return _tagIdsByCanonicalName.TryGetValue(canonicalName, out tagId);
    }

    /// <summary>
    /// Returns the compiled tag identified by a registry-relative identifier.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <returns>The immutable compiled tag definition.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagDefinition GetTag(TagId tagId)
    {
        if (!TryGetTag(tagId, out var tag))
        {
            throw new TagException($"Invalid gameplay tag id '{tagId}'.");
        }

        return tag;
    }

    /// <summary>
    /// Attempts to retrieve a compiled tag without throwing for an invalid identifier.
    /// </summary>
    /// <param name="tagId">The tag identifier relative to this registry.</param>
    /// <param name="tag">Receives the compiled tag, or null when the identifier is invalid.</param>
    /// <returns>True if the tag identifier is valid; otherwise, false.</returns>
    internal bool TryGetTag(TagId tagId, out TagDefinition tag)
    {
        if (!tagId.IsValid || tagId.Value > _tagsByIndex.Length)
        {
            tag = null!;
            return false;
        }

        tag = _tagsByIndex[tagId.Value - 1];
        return true;
    }

    /// <summary>
    /// Resolves a query key to its registry-relative identifier.
    /// </summary>
    /// <param name="queryKey">The unique query key.</param>
    /// <returns>The query identifier relative to this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagQueryId GetQueryId(string queryKey)
    {
        if (TryGetQueryId(queryKey, out var queryId))
        {
            return queryId;
        }

        throw new TagException($"Unknown gameplay tag query '{queryKey}'.");
    }

    /// <summary>
    /// Attempts to resolve a query key without throwing for an unknown key.
    /// </summary>
    /// <param name="queryKey">The unique query key.</param>
    /// <param name="queryId">Receives the query identifier, or the invalid identifier when the key is unknown.</param>
    /// <returns>True if the query key is registered; otherwise, false.</returns>
    public bool TryGetQueryId(string queryKey, out TagQueryId queryId)
    {
        return _queryIdsByKey.TryGetValue(queryKey, out queryId);
    }

    /// <summary>
    /// Returns the compiled query identified by a registry-relative identifier.
    /// </summary>
    /// <param name="queryId">The query identifier relative to this registry.</param>
    /// <returns>The compiled query owned by this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public CompiledTagQuery GetQuery(TagQueryId queryId)
    {
        if (!TryGetQuery(queryId, out var query))
        {
            throw new TagException($"Invalid gameplay tag query id '{queryId}'.");
        }

        return query;
    }

    /// <summary>
    /// Attempts to retrieve a compiled query without throwing for an invalid identifier.
    /// </summary>
    /// <param name="queryId">The query identifier relative to this registry.</param>
    /// <param name="query">The compiled query owned by this set's registry.</param>
    /// <returns>True if the query identifier is valid; otherwise, false.</returns>
    internal bool TryGetQuery(TagQueryId queryId, out CompiledTagQuery query)
    {
        if (!queryId.IsValid || queryId.Value > _queriesByIndex.Length)
        {
            query = null!;
            return false;
        }

        query = _queriesByIndex[queryId.Value - 1];
        return true;
    }

    /// <summary>
    /// Creates an empty mutable tag set for a domain in this registry.
    /// </summary>
    /// <param name="domainKey">The registered domain key.</param>
    /// <returns>An empty tag set owned by this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagSet CreateTagSet(string domainKey)
    {
        return CreateTagSet(GetDomainId(domainKey));
    }

    /// <summary>
    /// Creates an empty mutable tag set for a domain in this registry.
    /// </summary>
    /// <param name="domainId">The domain identifier relative to this registry.</param>
    /// <returns>An empty tag set owned by this registry.</returns>
    /// <exception cref="TagException">The requested key, name, or identifier is unknown or invalid.</exception>
    public TagSet CreateTagSet(TagDomainId domainId)
    {
        var domain = GetDomain(domainId);
        return new TagSet(this, domain.Id);
    }

    /// <summary>
    /// Returns the compiled tag at a validated domain-local index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TagDefinition GetTagByDomainLocalIndex(TagDomainId domainId, int domainLocalIndex)
    {
        return _tagsByDomainLocalIndex[domainId.Value - 1][domainLocalIndex];
    }

    /// <summary>
    /// Returns the compiled mask containing a validated tag and its ancestors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong[] GetExpansionMask(TagId tagId)
    {
        return _expansionMasksByTagIndex[tagId.Value - 1];
    }
}
