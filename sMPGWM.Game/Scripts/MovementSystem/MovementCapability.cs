using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.MovementSystem;
public sealed class MovementCapability
{
    internal bool Available(ResolvedShipStats stats, bool powered) => stats.HasEngine && powered;
}
