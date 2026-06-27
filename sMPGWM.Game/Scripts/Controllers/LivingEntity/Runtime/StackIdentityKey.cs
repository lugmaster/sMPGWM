namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

public readonly struct StackIdentityKey(int stackBaseId, long sourceId, long originId, bool includesOrigin)
{
    public int StackBaseId { get; } = stackBaseId;
    public long SourceId { get; } = sourceId;
    public long OriginId { get; } = originId;
    public bool IncludesOrigin { get; } = includesOrigin;

    public bool Equals(StackIdentityKey other)
    {
        return StackBaseId == other.StackBaseId &&
               SourceId == other.SourceId &&
               (!IncludesOrigin || !other.IncludesOrigin || OriginId == other.OriginId) &&
               IncludesOrigin == other.IncludesOrigin;
    }
}
