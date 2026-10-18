using sMPGWM.Scripts.TagSystem.Ids;

namespace sMPGWM.Scripts.TagSystem.Queries;

/// <summary>
/// Stores immutable query masks compiled for the registry instance that owns this query.
/// </summary>
public sealed class CompiledTagQuery
{
    /// <summary>
    /// Initializes a registry-owned query with its compiled selector masks and counts.
    /// </summary>
    internal CompiledTagQuery(
        TagQueryId id,
        string queryKey,
        TagDomainId domainId,
        TagQueryMatchMode matchMode,
        bool matchAll,
        ulong[] allMask,
        ulong[] anyMask,
        ulong[] noneMask,
        int allTagCount,
        int anyTagCount,
        int noneTagCount)
    {
        Id = id;
        QueryKey = queryKey;
        DomainId = domainId;
        MatchMode = matchMode;
        MatchAll = matchAll;
        AllMask = allMask;
        AnyMask = anyMask;
        NoneMask = noneMask;
        AllTagCount = allTagCount;
        AnyTagCount = anyTagCount;
        NoneTagCount = noneTagCount;
    }

    public TagQueryId Id { get; }
    public string QueryKey { get; }
    public TagDomainId DomainId { get; }
    public TagQueryMatchMode MatchMode { get; }
    public bool MatchAll { get; }
    public int AllTagCount { get; }
    public int AnyTagCount { get; }
    public int NoneTagCount { get; }

    internal ulong[] AllMask { get; }
    internal ulong[] AnyMask { get; }
    internal ulong[] NoneMask { get; }
}
