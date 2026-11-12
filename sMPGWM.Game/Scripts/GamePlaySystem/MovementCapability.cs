using sMPGWM.Scripts.StatSystem;
namespace sMPGWM.Scripts.GamePlaySystem;
public sealed class MovementCapability
{
    internal bool Available(ResolvedShipStats stats, bool powered) => stats.HasEngine && powered;
}
