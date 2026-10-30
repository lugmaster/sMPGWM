using sMPGWM.Scripts.TagSystem.Ids;

namespace sMPGWM.Scripts.TagSystem.Definitions;

/// <summary>
/// Describes an immutable canonical tag, its parent, and its assignment rules.
/// </summary>
public sealed class TagDefinition
{
    /// <summary>
    /// Initializes canonical tag metadata and its parent relationship.
    /// </summary>
    internal TagDefinition(
        TagId id,
        TagDomainId domainId,
        string canonicalName,
        int domainLocalIndex,
        bool isAssignable,
        bool isRoot,
        TagId parentId)
    {
        Id = id;
        DomainId = domainId;
        CanonicalName = canonicalName;
        DomainLocalIndex = domainLocalIndex;
        IsAssignable = isAssignable;
        IsRoot = isRoot;
        ParentId = parentId;
    }

    public TagId Id { get; }
    public TagDomainId DomainId { get; }
    public string CanonicalName { get; }
    public int DomainLocalIndex { get; }
    public bool IsAssignable { get; }
    public bool IsRoot { get; }
    public TagId ParentId { get; }
}
