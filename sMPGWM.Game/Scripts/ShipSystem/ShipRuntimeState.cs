using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.ShipSystem;

public sealed record ShipRuntimeState(
    ResolvedShipStats EquipmentInputs,
    ulong BatteryIdentity,
    double ShieldCurrent,
    double EnergyCurrent,
    long Revision,
    long ConfigurationRevision,
    long AuthoritativeTick,
    long LastResourceTick,
    long ShieldDowntimeDeadline,
    int FactionId,
    bool AfterburnerActive,
    bool Initialized,
    ShipComponentKind Components,
    ShipComponentKind? PendingComponents,
    IReadOnlyList<BehaviourInstance> Behaviours,
    IReadOnlyList<string> Conditions,
    IReadOnlyList<AbilityGrantState> Grants)
{
    public double HealthCapacity { get; init; }
    public double HealthCurrent { get; init; }
}
