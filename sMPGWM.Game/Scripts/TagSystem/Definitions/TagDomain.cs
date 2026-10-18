using sMPGWM.Scripts.TagSystem.Ids;

namespace sMPGWM.Scripts.TagSystem.Definitions;

/// <summary>
/// Describes a compiled tag domain and its domain-local mask capacity.
/// </summary>
public sealed class TagDomain
{
    /// <summary>
    /// Initializes the domain identity, description, tag counts, and mask capacity.
    /// </summary>
    internal TagDomain(
        TagDomainId id,
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

    public TagDomainId Id { get; }
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
