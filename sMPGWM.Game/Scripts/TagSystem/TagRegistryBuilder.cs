using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using sMPGWM.Scripts.TagSystem.Definitions;
using sMPGWM.Scripts.TagSystem.Exceptions;
using sMPGWM.Scripts.TagSystem.Ids;
using sMPGWM.Scripts.TagSystem.Internal;
using sMPGWM.Scripts.TagSystem.Queries;

namespace sMPGWM.Scripts.TagSystem;

/// <summary>
/// Collects tag registrations and freezes after a successful deterministic compilation.
/// </summary>
public sealed class TagRegistryBuilder
{
    private readonly List<DomainRegistration> _domains = [];
    private readonly List<TagRegistration> _tags = [];
    private readonly List<AliasRegistration> _aliases = [];
    private readonly List<QueryRegistration> _queries = [];
    private bool _isFrozen;

    /// <summary>
    /// Registers a domain for validation during compilation.
    /// </summary>
    /// <param name="domainKey">The registered domain key.</param>
    /// <param name="rootPrefix">The canonical root prefix for the domain.</param>
    /// <param name="description">The human-readable domain description.</param>
    /// <returns>This builder for chained registration.</returns>
    /// <exception cref="TagException">The builder has already compiled successfully.</exception>
    public TagRegistryBuilder RegisterDomain(string domainKey, string rootPrefix, string description)
    {
        EnsureMutable();
        _domains.Add(new DomainRegistration(domainKey, rootPrefix, description, _domains.Count));
        return this;
    }

    /// <summary>
    /// Registers a fully qualified canonical tag with an optional assignment override.
    /// </summary>
    /// <param name="domainKey">The registered domain key.</param>
    /// <param name="canonicalName">The fully qualified canonical tag name.</param>
    /// <param name="isAssignable">Whether assignment is allowed; null uses the leaf-tag default.</param>
    /// <returns>This builder for chained registration.</returns>
    /// <exception cref="TagException">The builder has already compiled successfully.</exception>
    public TagRegistryBuilder RegisterTag(string domainKey, string canonicalName, bool? isAssignable = null)
    {
        EnsureMutable();
        _tags.Add(new TagRegistration(domainKey, canonicalName, isAssignable, _tags.Count));
        return this;
    }

    /// <summary>
    /// Registers an alias that resolves to a canonical tag in the same domain.
    /// </summary>
    /// <param name="aliasName">The fully qualified alias name.</param>
    /// <param name="canonicalTargetName">The canonical tag targeted by the alias.</param>
    /// <returns>This builder for chained registration.</returns>
    /// <exception cref="TagException">The builder has already compiled successfully.</exception>
    public TagRegistryBuilder RegisterAlias(string aliasName, string canonicalTargetName)
    {
        EnsureMutable();
        _aliases.Add(new AliasRegistration(aliasName, canonicalTargetName, _aliases.Count));
        return this;
    }

    /// <summary>
    /// Copies and registers query selectors for compilation.
    /// </summary>
    /// <param name="queryKey">The unique query key.</param>
    /// <param name="domainKey">The registered domain key.</param>
    /// <param name="all">Tags that must all match.</param>
    /// <param name="any">Tags of which at least one must match.</param>
    /// <param name="none">Tags that must not match.</param>
    /// <param name="matchMode">Whether to examine direct or expanded membership.</param>
    /// <param name="matchAll">Whether to allow a query without All or Any selectors.</param>
    /// <returns>This builder for chained registration.</returns>
    /// <exception cref="TagException">The builder has already compiled successfully.</exception>
    public TagRegistryBuilder RegisterQuery(
        string queryKey,
        string domainKey,
        IEnumerable<string>? all = null,
        IEnumerable<string>? any = null,
        IEnumerable<string>? none = null,
        TagQueryMatchMode matchMode = TagQueryMatchMode.Expanded,
        bool matchAll = false)
    {
        EnsureMutable();
        _queries.Add(new QueryRegistration(
            queryKey,
            domainKey,
            CopyList(all),
            CopyList(any),
            CopyList(none),
            matchMode,
            matchAll));
        return this;
    }

    /// <summary>
    /// Validates registrations, compiles deterministic identifiers and masks, and freezes this builder on success.
    /// </summary>
    /// <returns>The immutable compiled registry.</returns>
    /// <exception cref="TagException">The builder has already compiled successfully.</exception>
    /// <exception cref="TagValidationException">The registrations fail validation or exceed supported capacities.</exception>
    public TagRegistry CompileAndFreeze()
    {
        EnsureMutable();

        var diagnostics = new List<TagValidationDiagnostic>();
        if (_domains.Count == 0)
        {
            diagnostics.Add(new TagValidationDiagnostic(
                "NoDomainsRegistered",
                "Domains",
                "At least one gameplay tag domain must be registered."));
        }

        var domainByKey = BuildDomainLookup(diagnostics);
        var domainByRoot = BuildRootLookup(domainByKey.Values, diagnostics);
        var aliasByName = BuildAliasLookup(domainByRoot, diagnostics);
        var tagInputs = BuildTagInputs(domainByKey, domainByRoot, diagnostics);

        ValidateAliases(aliasByName, tagInputs, domainByRoot, diagnostics);
        ValidateTagParents(tagInputs, diagnostics);
        ValidateQueryRegistrations(domainByKey, domainByRoot, aliasByName, tagInputs, diagnostics);

        if (diagnostics.Count > 0)
        {
            throw new TagValidationException(diagnostics);
        }

        var orderedDomains = new List<DomainRegistration>(domainByKey.Values);
        orderedDomains.Sort((left, right) => string.CompareOrdinal(left.DomainKey, right.DomainKey));

        var domainIdsByKey = new Dictionary<string, TagDomainId>(StringComparer.Ordinal);
        var domainIdsByRoot = new Dictionary<string, TagDomainId>(StringComparer.Ordinal);
        for (var i = 0; i < orderedDomains.Count; i++)
        {
            var domainId = new TagDomainId(checked((ushort)(i + 1)));
            domainIdsByKey.Add(orderedDomains[i].DomainKey, domainId);
            domainIdsByRoot.Add(orderedDomains[i].RootPrefix, domainId);
        }

        var orderedTagInputs = new List<TagCompileInput>(tagInputs.Values);
        orderedTagInputs.Sort((left, right) => string.CompareOrdinal(left.CanonicalName, right.CanonicalName));

        if (orderedTagInputs.Count > ushort.MaxValue)
        {
            throw new TagValidationException([
                new TagValidationDiagnostic(
                    "TooManyTags",
                    "Tags",
                    "A compiled gameplay tag registry cannot exceed 65,535 tags.",
                    orderedTagInputs.Count.ToString(CultureInfo.InvariantCulture))
            ]);
        }

        var childrenByParent = CountChildren(orderedTagInputs);
        var tagIdsByName = new Dictionary<string, TagId>(StringComparer.Ordinal);
        for (var i = 0; i < orderedTagInputs.Count; i++)
        {
            tagIdsByName.Add(orderedTagInputs[i].CanonicalName, new TagId(checked((ushort)(i + 1))));
        }

        var definitionsByName = BuildTagDefinitions(
            orderedTagInputs,
            childrenByParent,
            domainIdsByKey,
            tagIdsByName,
            out var tagsByIndex,
            out var tagsByDomainLocalIndex);

        var domainsByIndex = BuildDomains(orderedDomains, tagsByDomainLocalIndex, domainIdsByKey);
        var expansionMasks = BuildExpansionMasks(tagsByIndex, definitionsByName, domainsByIndex);
        var queriesByIndex = BuildQueries(domainIdsByKey, domainByKey, domainByRoot, aliasByName, tagIdsByName,
            definitionsByName, domainsByIndex);
        var queryIdsByKey = BuildQueryIdLookup(queriesByIndex);
        var registryHash = ComputeRegistryHash(domainsByIndex, tagsByIndex, aliasByName, queriesByIndex);

        _isFrozen = true;
        return new TagRegistry(
            domainsByIndex,
            tagsByIndex,
            tagsByDomainLocalIndex,
            expansionMasks,
            queriesByIndex,
            domainIdsByKey,
            domainIdsByRoot,
            tagIdsByName,
            queryIdsByKey,
            registryHash);
    }

    /// <summary>
    /// Builds a domain-key lookup and records invalid or duplicate domain registrations.
    /// </summary>
    private Dictionary<string, DomainRegistration> BuildDomainLookup(List<TagValidationDiagnostic> diagnostics)
    {
        var result = new Dictionary<string, DomainRegistration>(StringComparer.Ordinal);
        for (var i = 0; i < _domains.Count; i++)
        {
            var domain = _domains[i];
            var path = $"Domains[{i}]";

            if (!IsValidIdentifierSegment(domain.DomainKey))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "InvalidDomainKey",
                    $"{path}.DomainKey",
                    "DomainKey must be a non-empty identifier segment.",
                    domain.DomainKey));
                continue;
            }

            if (!IsValidIdentifierSegment(domain.RootPrefix))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "InvalidRootPrefix",
                    $"{path}.RootPrefix",
                    "RootPrefix must be a non-empty identifier segment.",
                    domain.RootPrefix));
                continue;
            }

            if (!result.TryAdd(domain.DomainKey, domain))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "DuplicateDomainKey",
                    $"{path}.DomainKey",
                    "DomainKey values must be unique.",
                    domain.DomainKey));
            }
        }

        return result;
    }

    /// <summary>
    /// Builds a root-prefix lookup and records duplicate root prefixes.
    /// </summary>
    private static Dictionary<string, DomainRegistration> BuildRootLookup(
        IEnumerable<DomainRegistration> domains,
        List<TagValidationDiagnostic> diagnostics)
    {
        var result = new Dictionary<string, DomainRegistration>(StringComparer.Ordinal);
        foreach (var domain in domains)
        {
            if (!result.TryAdd(domain.RootPrefix, domain))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "DuplicateRootPrefix",
                    $"Domains[{domain.Sequence}].RootPrefix",
                    "RootPrefix values must be globally unique.",
                    domain.RootPrefix));
            }
        }

        return result;
    }

    /// <summary>
    /// Builds an alias lookup and records malformed names or duplicate aliases.
    /// </summary>
    private Dictionary<string, AliasRegistration> BuildAliasLookup(
        Dictionary<string, DomainRegistration> domainByRoot,
        List<TagValidationDiagnostic> diagnostics)
    {
        var result = new Dictionary<string, AliasRegistration>(StringComparer.Ordinal);
        for (var i = 0; i < _aliases.Count; i++)
        {
            var alias = _aliases[i];
            var path = $"Aliases[{i}]";

            ValidateFullNameShape(alias.AliasName, $"{path}.AliasName", domainByRoot, diagnostics);
            ValidateFullNameShape(alias.CanonicalTargetName, $"{path}.CanonicalTargetName", domainByRoot, diagnostics);

            if (!result.TryAdd(alias.AliasName, alias))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "DuplicateAlias",
                    $"{path}.AliasName",
                    "Alias names must be unique.",
                    alias.AliasName));
            }
        }

        return result;
    }

    /// <summary>
    /// Creates domain roots and collects validated canonical tag inputs.
    /// </summary>
    private Dictionary<string, TagCompileInput> BuildTagInputs(
        Dictionary<string, DomainRegistration> domainByKey,
        Dictionary<string, DomainRegistration> domainByRoot,
        List<TagValidationDiagnostic> diagnostics)
    {
        var result = new Dictionary<string, TagCompileInput>(StringComparer.Ordinal);

        foreach (var domain in domainByKey.Values)
        {
            result[domain.RootPrefix] = new TagCompileInput(domain.DomainKey, domain.RootPrefix, false, true);
        }

        for (var i = 0; i < _tags.Count; i++)
        {
            var tag = _tags[i];
            var path = $"Tags[{i}]";
            if (!domainByKey.TryGetValue(tag.DomainKey, out var domain))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "UnknownDomain",
                    $"{path}.DomainKey",
                    "Tag references an unknown domain.",
                    tag.DomainKey));
                continue;
            }

            if (!TryValidateCanonicalNameForDomain(
                    tag.CanonicalName,
                    domain,
                    domainByRoot,
                    diagnostics,
                    $"{path}.CanonicalName"))
            {
                continue;
            }

            if (tag.CanonicalName == domain.RootPrefix)
            {
                if (tag.IsAssignable == true)
                {
                    diagnostics.Add(new TagValidationDiagnostic(
                        "DomainRootCannotBeAssignable",
                        $"{path}.IsAssignable",
                        "Domain root tags are not assignable.",
                        tag.CanonicalName));
                }

                continue;
            }

            if (!result.TryAdd(tag.CanonicalName, new TagCompileInput(
                    domain.DomainKey,
                    tag.CanonicalName,
                    tag.IsAssignable,
                    false)))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "DuplicateCanonicalName",
                    $"{path}.CanonicalName",
                    "Canonical gameplay tag names must be unique.",
                    tag.CanonicalName));
            }
        }

        return result;
    }

    /// <summary>
    /// Records aliases with chained, missing, or cross-domain targets.
    /// </summary>
    private void ValidateAliases(
        Dictionary<string, AliasRegistration> aliasByName,
        Dictionary<string, TagCompileInput> tagInputs,
        Dictionary<string, DomainRegistration> domainByRoot,
        List<TagValidationDiagnostic> diagnostics)
    {
        foreach (var alias in aliasByName.Values)
        {
            var path = $"Aliases[{alias.Sequence}]";
            if (aliasByName.ContainsKey(alias.CanonicalTargetName))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "AliasChainOrCycle",
                    $"{path}.CanonicalTargetName",
                    "Alias targets must be canonical tag names, not another alias.",
                    alias.CanonicalTargetName));
                continue;
            }

            if (!tagInputs.ContainsKey(alias.CanonicalTargetName))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "UnknownAliasTarget",
                    $"{path}.CanonicalTargetName",
                    "Alias target must be a declared canonical tag.",
                    alias.CanonicalTargetName));
                continue;
            }

            var aliasRoot = GetRootPrefix(alias.AliasName);
            var targetRoot = GetRootPrefix(alias.CanonicalTargetName);
            if (aliasRoot is null || targetRoot is null ||
                !domainByRoot.TryGetValue(aliasRoot, out var aliasDomain) ||
                !domainByRoot.TryGetValue(targetRoot, out var targetDomain))
            {
                continue;
            }

            if (aliasDomain.DomainKey != targetDomain.DomainKey)
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "CrossDomainAlias",
                    $"{path}.CanonicalTargetName",
                    "Alias target must stay within the alias domain.",
                    alias.CanonicalTargetName));
            }
        }
    }

    /// <summary>
    /// Records missing ancestors required by the strict canonical tag hierarchy.
    /// </summary>
    private static void ValidateTagParents(
        Dictionary<string, TagCompileInput> tagInputs,
        List<TagValidationDiagnostic> diagnostics)
    {
        foreach (var tag in tagInputs.Values)
        {
            if (tag.IsRoot)
            {
                continue;
            }

            var parent = GetParentName(tag.CanonicalName);
            while (parent is not null)
            {
                if (!tagInputs.ContainsKey(parent))
                {
                    diagnostics.Add(new TagValidationDiagnostic(
                        "MissingParentTag",
                        $"Tags.{tag.CanonicalName}",
                        "Strict dot-path hierarchy requires every parent tag to be explicitly declared.",
                        parent));
                }

                parent = GetParentName(parent);
            }
        }
    }

    /// <summary>
    /// Records invalid query keys, domains, selectors, and missing match-all opt-ins.
    /// </summary>
    private void ValidateQueryRegistrations(
        Dictionary<string, DomainRegistration> domainByKey,
        Dictionary<string, DomainRegistration> domainByRoot,
        Dictionary<string, AliasRegistration> aliasByName,
        Dictionary<string, TagCompileInput> tagInputs,
        List<TagValidationDiagnostic> diagnostics)
    {
        var queryKeys = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < _queries.Count; i++)
        {
            var query = _queries[i];
            var path = $"Queries[{i}]";

            if (!IsValidQueryKey(query.QueryKey))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "InvalidQueryKey",
                    $"{path}.QueryKey",
                    "QueryKey must be non-empty and may not contain whitespace.",
                    query.QueryKey));
            }

            if (!queryKeys.Add(query.QueryKey))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "DuplicateQueryKey",
                    $"{path}.QueryKey",
                    "QueryKey values must be unique.",
                    query.QueryKey));
            }

            if (!domainByKey.TryGetValue(query.DomainKey, out var domain))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "UnknownDomain",
                    $"{path}.DomainKey",
                    "Query references an unknown domain.",
                    query.DomainKey));
                continue;
            }

            var allCount = ValidateQueryTags(query.All, $"{path}.All", domain, domainByRoot, aliasByName, tagInputs,
                diagnostics);
            var anyCount = ValidateQueryTags(query.Any, $"{path}.Any", domain, domainByRoot, aliasByName, tagInputs,
                diagnostics);
            ValidateQueryTags(query.None, $"{path}.None", domain, domainByRoot, aliasByName, tagInputs, diagnostics);

            if (allCount == 0 && anyCount == 0 && !query.MatchAll)
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "EmptyQueryRequiresMatchAll",
                    path,
                    "A query with no All and no Any tags must set MatchAll to true.",
                    query.QueryKey));
            }
        }
    }

    /// <summary>
    /// Validates query selector names after resolving aliases and counts valid references.
    /// </summary>
    private static int ValidateQueryTags(
        IReadOnlyList<string> tagNames,
        string fieldPath,
        DomainRegistration queryDomain,
        Dictionary<string, DomainRegistration> domainByRoot,
        Dictionary<string, AliasRegistration> aliasByName,
        Dictionary<string, TagCompileInput> tagInputs,
        List<TagValidationDiagnostic> diagnostics)
    {
        var validCount = 0;
        for (var i = 0; i < tagNames.Count; i++)
        {
            var inputName = tagNames[i];
            var resolvedName = ResolveAlias(inputName, aliasByName);
            if (!TryValidateCanonicalNameForDomain(
                    resolvedName,
                    queryDomain,
                    domainByRoot,
                    diagnostics,
                    $"{fieldPath}[{i}]"))
            {
                continue;
            }

            if (!tagInputs.ContainsKey(resolvedName))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "UnknownTag",
                    $"{fieldPath}[{i}]",
                    "Query references an unknown tag.",
                    resolvedName));
                continue;
            }

            validCount++;
        }

        return validCount;
    }

    /// <summary>
    /// Counts immediate children of each canonical parent tag.
    /// </summary>
    private static Dictionary<string, int> CountChildren(List<TagCompileInput> orderedTagInputs)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < orderedTagInputs.Count; i++)
        {
            var parent = GetParentName(orderedTagInputs[i].CanonicalName);
            if (parent is null)
            {
                continue;
            }

            result.TryGetValue(parent, out var count);
            result[parent] = count + 1;
        }

        return result;
    }

    /// <summary>
    /// Builds immutable tag definitions with deterministic registry IDs and domain-local indexes.
    /// </summary>
    private static Dictionary<string, TagDefinition> BuildTagDefinitions(
        List<TagCompileInput> orderedTagInputs,
        Dictionary<string, int> childrenByParent,
        Dictionary<string, TagDomainId> domainIdsByKey,
        Dictionary<string, TagId> tagIdsByName,
        out TagDefinition[] tagsByIndex,
        out TagDefinition[][] tagsByDomainLocalIndex)
    {
        var definitionsByName = new Dictionary<string, TagDefinition>(StringComparer.Ordinal);
        var domainGroups = new Dictionary<TagDomainId, List<TagCompileInput>>();

        for (var i = 0; i < orderedTagInputs.Count; i++)
        {
            var domainId = domainIdsByKey[orderedTagInputs[i].DomainKey];
            if (!domainGroups.TryGetValue(domainId, out var group))
            {
                group = [];
                domainGroups.Add(domainId, group);
            }

            group.Add(orderedTagInputs[i]);
        }

        foreach (var group in domainGroups.Values)
        {
            group.Sort((left, right) => string.CompareOrdinal(left.CanonicalName, right.CanonicalName));
        }

        var localIndexByName = new Dictionary<string, ushort>(StringComparer.Ordinal);
        foreach (var group in domainGroups.Values)
        {
            for (var i = 0; i < group.Count; i++)
            {
                localIndexByName[group[i].CanonicalName] = checked((ushort)i);
            }
        }

        tagsByIndex = new TagDefinition[orderedTagInputs.Count];
        for (var i = 0; i < orderedTagInputs.Count; i++)
        {
            var tagInput = orderedTagInputs[i];
            var id = tagIdsByName[tagInput.CanonicalName];
            var domainId = domainIdsByKey[tagInput.DomainKey];
            var parentName = GetParentName(tagInput.CanonicalName);
            var parentId = parentName is null ? TagId.Invalid : tagIdsByName[parentName];
            var hasChildren = childrenByParent.ContainsKey(tagInput.CanonicalName);
            var isAssignable = tagInput.IsAssignable ?? (!tagInput.IsRoot && !hasChildren);

            var definition = new TagDefinition(
                id,
                domainId,
                tagInput.CanonicalName,
                localIndexByName[tagInput.CanonicalName],
                isAssignable,
                tagInput.IsRoot,
                parentId);

            tagsByIndex[id.Value - 1] = definition;
            definitionsByName.Add(tagInput.CanonicalName, definition);
        }

        tagsByDomainLocalIndex = new TagDefinition[domainGroups.Count][];
        foreach (var (domainId, group) in domainGroups)
        {
            var domainTags = new TagDefinition[group.Count];
            for (var i = 0; i < group.Count; i++)
            {
                domainTags[i] = definitionsByName[group[i].CanonicalName];
            }

            tagsByDomainLocalIndex[domainId.Value - 1] = domainTags;
        }

        return definitionsByName;
    }

    /// <summary>
    /// Builds domain metadata and calculates domain-local mask capacities.
    /// </summary>
    private static TagDomain[] BuildDomains(
        List<DomainRegistration> orderedDomains,
        TagDefinition[][] tagsByDomainLocalIndex,
        Dictionary<string, TagDomainId> domainIdsByKey)
    {
        var domainsByIndex = new TagDomain[orderedDomains.Count];
        for (var i = 0; i < orderedDomains.Count; i++)
        {
            var registration = orderedDomains[i];
            var domainId = domainIdsByKey[registration.DomainKey];
            var tags = tagsByDomainLocalIndex[domainId.Value - 1];
            var assignableCount = 0;
            for (var tagIndex = 0; tagIndex < tags.Length; tagIndex++)
            {
                if (tags[tagIndex].IsAssignable)
                {
                    assignableCount++;
                }
            }

            var wordCount = Math.Max(1, (tags.Length + 63) / 64);
            domainsByIndex[domainId.Value - 1] = new TagDomain(
                domainId,
                registration.DomainKey,
                registration.RootPrefix,
                registration.Description,
                tags.Length,
                assignableCount,
                wordCount);
        }

        return domainsByIndex;
    }

    /// <summary>
    /// Builds each tag's mask containing itself and all canonical ancestors.
    /// </summary>
    private static ulong[][] BuildExpansionMasks(
        TagDefinition[] tagsByIndex,
        Dictionary<string, TagDefinition> definitionsByName,
        TagDomain[] domainsByIndex)
    {
        var expansionMasks = new ulong[tagsByIndex.Length][];
        for (var i = 0; i < tagsByIndex.Length; i++)
        {
            var tag = tagsByIndex[i];
            var domain = domainsByIndex[tag.DomainId.Value - 1];
            var mask = new ulong[domain.DomainMaskWordCount];
            var currentName = tag.CanonicalName;
            while (currentName is not null)
            {
                var currentTag = definitionsByName[currentName];
                SetBit(mask, currentTag.DomainLocalIndex);
                currentName = GetParentName(currentName);
            }

            expansionMasks[i] = mask;
        }

        return expansionMasks;
    }

    /// <summary>
    /// Compiles query selectors into domain-local masks in deterministic query-key order.
    /// </summary>
    private CompiledTagQuery[] BuildQueries(
        Dictionary<string, TagDomainId> domainIdsByKey,
        Dictionary<string, DomainRegistration> domainByKey,
        Dictionary<string, DomainRegistration> domainByRoot,
        Dictionary<string, AliasRegistration> aliasByName,
        Dictionary<string, TagId> tagIdsByName,
        Dictionary<string, TagDefinition> definitionsByName,
        TagDomain[] domainsByIndex)
    {
        var orderedQueries = new List<QueryRegistration>(_queries);
        orderedQueries.Sort((left, right) => string.CompareOrdinal(left.QueryKey, right.QueryKey));

        var result = new CompiledTagQuery[orderedQueries.Count];
        for (var i = 0; i < orderedQueries.Count; i++)
        {
            var query = orderedQueries[i];
            var domainId = domainIdsByKey[query.DomainKey];
            var domain = domainsByIndex[domainId.Value - 1];
            var allMask = new ulong[domain.DomainMaskWordCount];
            var anyMask = new ulong[domain.DomainMaskWordCount];
            var noneMask = new ulong[domain.DomainMaskWordCount];
            var queryDomain = domainByKey[query.DomainKey];

            var allCount = CompileQueryMask(query.All, queryDomain, domainByRoot, aliasByName, tagIdsByName,
                definitionsByName, allMask);
            var anyCount = CompileQueryMask(query.Any, queryDomain, domainByRoot, aliasByName, tagIdsByName,
                definitionsByName, anyMask);
            var noneCount = CompileQueryMask(query.None, queryDomain, domainByRoot, aliasByName, tagIdsByName,
                definitionsByName, noneMask);

            result[i] = new CompiledTagQuery(
                new TagQueryId(checked((ushort)(i + 1))),
                query.QueryKey,
                domainId,
                query.MatchMode,
                query.MatchAll,
                allMask,
                anyMask,
                noneMask,
                allCount,
                anyCount,
                noneCount);
        }

        return result;
    }

    /// <summary>
    /// Sets selector bits after resolving aliases and counts valid references.
    /// </summary>
    private static int CompileQueryMask(
        IReadOnlyList<string> tagNames,
        DomainRegistration queryDomain,
        Dictionary<string, DomainRegistration> domainByRoot,
        Dictionary<string, AliasRegistration> aliasByName,
        Dictionary<string, TagId> tagIdsByName,
        Dictionary<string, TagDefinition> definitionsByName,
        ulong[] mask)
    {
        var count = 0;
        for (var i = 0; i < tagNames.Count; i++)
        {
            var resolvedName = ResolveAlias(tagNames[i], aliasByName);
            if (!IsCanonicalNameForDomain(resolvedName, queryDomain, domainByRoot) ||
                !tagIdsByName.TryGetValue(resolvedName, out _))
            {
                continue;
            }

            var tag = definitionsByName[resolvedName];
            SetBit(mask, tag.DomainLocalIndex);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Maps compiled query keys to their registry-relative identifiers.
    /// </summary>
    private static Dictionary<string, TagQueryId> BuildQueryIdLookup(CompiledTagQuery[] queriesByIndex)
    {
        var result = new Dictionary<string, TagQueryId>(StringComparer.Ordinal);
        for (var i = 0; i < queriesByIndex.Length; i++)
        {
            result.Add(queriesByIndex[i].QueryKey, queriesByIndex[i].Id);
        }

        return result;
    }

    /// <summary>
    /// Hashes deterministic domain, tag, alias, and query data using the established format.
    /// </summary>
    private static ulong ComputeRegistryHash(
        TagDomain[] domainsByIndex,
        TagDefinition[] tagsByIndex,
        Dictionary<string, AliasRegistration> aliasByName,
        CompiledTagQuery[] queriesByIndex)
    {
        var builder = new StringBuilder();
        builder.Append("GameplayTagRegistry:v1.1.0:hash-v2\n");

        for (var i = 0; i < domainsByIndex.Length; i++)
        {
            var domain = domainsByIndex[i];
            builder
                .Append("D|").Append(domain.Id.Value.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(domain.DomainKey)
                .Append('|').Append(domain.RootPrefix)
                .Append('|').Append(domain.CanonicalTagCount.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(domain.AssignableTagCount.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(domain.DomainMaskWordCount.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }

        for (var i = 0; i < tagsByIndex.Length; i++)
        {
            var tag = tagsByIndex[i];
            builder
                .Append("T|").Append(tag.Id.Value.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(tag.DomainId.Value.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(tag.DomainLocalIndex.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(tag.CanonicalName)
                .Append('|').Append(tag.IsAssignable ? '1' : '0')
                .Append('|').Append(tag.ParentId.Value.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }

        var aliases = new List<AliasRegistration>(aliasByName.Values);
        aliases.Sort((left, right) => string.CompareOrdinal(left.AliasName, right.AliasName));
        for (var i = 0; i < aliases.Count; i++)
        {
            builder
                .Append("A|").Append(aliases[i].AliasName)
                .Append('|').Append(aliases[i].CanonicalTargetName)
                .Append('\n');
        }

        for (var i = 0; i < queriesByIndex.Length; i++)
        {
            var query = queriesByIndex[i];
            builder
                .Append("Q|").Append(query.Id.Value.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(query.QueryKey)
                .Append('|').Append(query.DomainId.Value.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(query.MatchMode)
                .Append('|').Append(query.MatchAll ? '1' : '0')
                .Append('|').Append(query.AllTagCount.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(query.AnyTagCount.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(query.NoneTagCount.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
            AppendQueryMask(builder, "All", query.AllMask);
            AppendQueryMask(builder, "Any", query.AnyMask);
            AppendQueryMask(builder, "None", query.NoneMask);
        }

        return ComputeFnv1A64(builder.ToString());
    }

    /// <summary>
    /// Appends a labeled query mask with invariant word values and an LF terminator.
    /// </summary>
    private static void AppendQueryMask(StringBuilder builder, string label, ulong[] mask)
    {
        builder.Append(label).Append('|').Append(mask.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var word in mask)
        {
            builder.Append('|').Append(word.ToString(CultureInfo.InvariantCulture));
        }

        builder.Append('\n');
    }

    /// <summary>
    /// Computes the established 64-bit FNV-1a hash over both bytes of each UTF-16 character.
    /// </summary>
    private static ulong ComputeFnv1A64(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;

        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            hash ^= (byte)(character & 0xFF);
            hash *= prime;
            hash ^= (byte)(character >> 8);
            hash *= prime;
        }

        return hash;
    }

    /// <summary>
    /// Copies selector values so later caller mutations cannot change registrations.
    /// </summary>
    private static List<string> CopyList(IEnumerable<string>? values)
    {
        return values is null ? [] : new List<string>(values);
    }

    /// <summary>
    /// Throws if this builder has already compiled successfully.
    /// </summary>
    private void EnsureMutable()
    {
        if (_isFrozen)
        {
            throw new TagException("TagRegistryBuilder is frozen after successful compilation.");
        }
    }

    /// <summary>
    /// Validates a fully qualified tag name and records shape or domain errors.
    /// </summary>
    private static bool TryValidateCanonicalNameForDomain(
        string candidate,
        DomainRegistration domain,
        Dictionary<string, DomainRegistration> domainByRoot,
        List<TagValidationDiagnostic> diagnostics,
        string fieldPath)
    {
        if (!ValidateFullNameShape(candidate, fieldPath, domainByRoot, diagnostics))
        {
            return false;
        }

        if (!IsCanonicalNameForDomain(candidate, domain, domainByRoot))
        {
            var root = GetRootPrefix(candidate);
            var code = root is null || !domainByRoot.ContainsKey(root)
                ? "ShortNamesDisabled"
                : "DomainMismatch";

            diagnostics.Add(new TagValidationDiagnostic(
                code,
                fieldPath,
                "Gameplay tag references must be fully qualified and belong to the field domain.",
                candidate));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates canonical dot-path segments and the registered root prefix.
    /// </summary>
    private static bool ValidateFullNameShape(
        string candidate,
        string fieldPath,
        Dictionary<string, DomainRegistration> domainByRoot,
        List<TagValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            diagnostics.Add(new TagValidationDiagnostic(
                "InvalidTagName",
                fieldPath,
                "Gameplay tag names must be non-empty.",
                candidate));
            return false;
        }

        var segments = candidate.Split('.');
        if (segments.Length == 1)
        {
            diagnostics.Add(new TagValidationDiagnostic(
                "ShortNamesDisabled",
                fieldPath,
                "Short gameplay tag names are disabled; use canonical fully qualified names.",
                candidate));
            return false;
        }

        for (var i = 0; i < segments.Length; i++)
        {
            if (!IsValidIdentifierSegment(segments[i]))
            {
                diagnostics.Add(new TagValidationDiagnostic(
                    "InvalidTagName",
                    fieldPath,
                    "Gameplay tag names must use strict dot-path identifier segments.",
                    candidate));
                return false;
            }
        }

        if (!domainByRoot.ContainsKey(segments[0]))
        {
            diagnostics.Add(new TagValidationDiagnostic(
                "UnknownRootPrefix",
                fieldPath,
                "Gameplay tag root prefix is not registered.",
                candidate));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a canonical name belongs to the specified registered domain.
    /// </summary>
    private static bool IsCanonicalNameForDomain(
        string candidate,
        DomainRegistration domain,
        Dictionary<string, DomainRegistration> domainByRoot)
    {
        var root = GetRootPrefix(candidate);
        return root is not null &&
               domainByRoot.TryGetValue(root, out var candidateDomain) &&
               candidateDomain.DomainKey == domain.DomainKey &&
               (candidate == domain.RootPrefix ||
                candidate.StartsWith(domain.RootPrefix + ".", StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns an alias's canonical target, or the original name when no alias exists.
    /// </summary>
    private static string ResolveAlias(string inputName, Dictionary<string, AliasRegistration> aliasByName)
    {
        return aliasByName.TryGetValue(inputName, out var alias)
            ? alias.CanonicalTargetName
            : inputName;
    }

    /// <summary>
    /// Returns the first canonical name segment, or null for an empty name.
    /// </summary>
    private static string? GetRootPrefix(string canonicalName)
    {
        if (string.IsNullOrEmpty(canonicalName))
        {
            return null;
        }

        var dotIndex = canonicalName.IndexOf('.');
        return dotIndex < 0 ? canonicalName : canonicalName[..dotIndex];
    }

    /// <summary>
    /// Returns the immediate canonical parent name, or null for a root.
    /// </summary>
    private static string? GetParentName(string canonicalName)
    {
        var dotIndex = canonicalName.LastIndexOf('.');
        return dotIndex < 0 ? null : canonicalName[..dotIndex];
    }

    /// <summary>
    /// Checks for a leading letter followed by letters, digits, or underscores.
    /// </summary>
    private static bool IsValidIdentifierSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!char.IsLetter(value[0]))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var character = value[i];
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks that a query key is nonempty and contains no whitespace.
    /// </summary>
    private static bool IsValidQueryKey(string queryKey)
    {
        if (string.IsNullOrWhiteSpace(queryKey))
        {
            return false;
        }

        for (var i = 0; i < queryKey.Length; i++)
        {
            if (char.IsWhiteSpace(queryKey[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Sets the bit at a domain-local tag index.
    /// </summary>
    private static void SetBit(ulong[] mask, int domainLocalIndex)
    {
        var wordIndex = domainLocalIndex >> 6;
        mask[wordIndex] |= 1UL << (domainLocalIndex & 63);
    }
}
