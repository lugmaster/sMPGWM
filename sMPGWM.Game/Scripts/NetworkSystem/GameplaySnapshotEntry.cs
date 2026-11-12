using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.NetworkSystem;

internal sealed record GameplaySnapshotEntry(
    string Entity,
    long Revision,
    double Capacity,
    double Current,
    BehaviourInstance[] Behaviours,
    int FactionId,
    string[] Conditions,
    ResolvedShipStats Stats,
    double EnergyCurrent,
    bool EnergyDepleted,
    bool ShieldProtectionAvailable,
    long ShieldDowntimeDeadline,
    long AuthoritativeTick,
    long ConfigurationRevision)
{
    public ShipComponentKind Components { get; init; } = ShipComponentKind.All;
    public bool AfterburnerActive { get; init; }
}
