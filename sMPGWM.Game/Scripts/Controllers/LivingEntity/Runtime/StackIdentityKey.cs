namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Deterministic key describing a stack family plus selected source/origin identity.
/// </summary>
public readonly struct StackIdentityKey(int stackBaseId, long sourceId, long originId, bool includesOrigin)
{
    /// <summary>
    /// Gets the effect definition or shared stack group id.
    /// </summary>
    public int StackBaseId { get; } = stackBaseId;

    /// <summary>
    /// Gets the source id included in stack identity.
    /// </summary>
    public long SourceId { get; } = sourceId;

    /// <summary>
    /// Gets the origin id included when origin-sensitive identity is enabled.
    /// </summary>
    public long OriginId { get; } = originId;

    /// <summary>
    /// Gets whether origin id participates in equality.
    /// </summary>
    public bool IncludesOrigin { get; } = includesOrigin;

    /// <summary>
    /// Returns true when another key identifies the same stack family and source scope.
    /// </summary>
    public bool Equals(StackIdentityKey other)
    {
        return StackBaseId == other.StackBaseId &&
               SourceId == other.SourceId &&
               (!IncludesOrigin || !other.IncludesOrigin || OriginId == other.OriginId) &&
               IncludesOrigin == other.IncludesOrigin;
    }
}
