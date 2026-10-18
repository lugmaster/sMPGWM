namespace sMPGWM.Scripts.TagSystem;

public sealed class GameplayTagDomain
{
    internal GameplayTagDomain(
        GameplayTagDomainId id,
        string domainKey,
        string rootPrefix,
        string description,
        int canonicalTagCount,
        int assignableTagCount,
        int domainMaskWordCount)
    {
        Id = id;
        DomainKey = domainKey;
        RootPrefix = rootPrefix;
        Description = description;
        CanonicalTagCount = canonicalTagCount;
        AssignableTagCount = assignableTagCount;
        DomainMaskWordCount = domainMaskWordCount;
        DomainMaskBitCapacity = domainMaskWordCount * 64;
        DomainMaskByteSize = domainMaskWordCount * sizeof(ulong);
    }

    public GameplayTagDomainId Id { get; }
    public string DomainKey { get; }
    public string RootPrefix { get; }
    public string Description { get; }
    public int CanonicalTagCount { get; }
    public int AssignableTagCount { get; }
    public int NonAssignableTagCount => CanonicalTagCount - AssignableTagCount;
    public int DomainMaskWordCount { get; }
    public int DomainMaskBitCapacity { get; }
    public int DomainMaskByteSize { get; }
}

public sealed class GameplayTagDefinition
{
    internal GameplayTagDefinition(
        GameplayTagId id,
        GameplayTagDomainId domainId,
        string canonicalName,
        ushort domainLocalIndex,
        bool isAssignable,
        bool isRoot,
        GameplayTagId parentId)
    {
        Id = id;
        DomainId = domainId;
        CanonicalName = canonicalName;
        DomainLocalIndex = domainLocalIndex;
        IsAssignable = isAssignable;
        IsRoot = isRoot;
        ParentId = parentId;
    }

    public GameplayTagId Id { get; }
    public GameplayTagDomainId DomainId { get; }
    public string CanonicalName { get; }
    public ushort DomainLocalIndex { get; }
    public bool IsAssignable { get; }
    public bool IsRoot { get; }
    public GameplayTagId ParentId { get; }
}

public sealed class CompiledTagQuery
{
    internal CompiledTagQuery(
        TagQueryId id,
        string queryKey,
        GameplayTagDomainId domainId,
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
    public GameplayTagDomainId DomainId { get; }
    public TagQueryMatchMode MatchMode { get; }
    public bool MatchAll { get; }
    public int AllTagCount { get; }
    public int AnyTagCount { get; }
    public int NoneTagCount { get; }

    internal ulong[] AllMask { get; }
    internal ulong[] AnyMask { get; }
    internal ulong[] NoneMask { get; }
}