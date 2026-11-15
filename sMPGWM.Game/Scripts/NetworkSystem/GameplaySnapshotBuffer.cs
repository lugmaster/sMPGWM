using System.Collections.Generic;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.NetworkSystem;

internal sealed class GameplaySnapshotBuffer
{
    public string Entity = "";
    public long Revision, ShieldDowntimeDeadline, AuthoritativeTick, ConfigurationRevision;
    public double Capacity, Current, EnergyCurrent, HealthCapacity, HealthCurrent;
    public int FactionId;
    public bool EnergyDepleted, ShieldProtectionAvailable, AfterburnerActive;
    public ShipComponentKind Components;
    public ResolvedShipStats Stats;
    public readonly List<BehaviourInstance> Behaviours = new();
    public readonly List<string> Conditions = new();

    public GameplaySnapshotEntry ToEntry() => new(Entity, Revision, Capacity, Current, Behaviours.ToArray(),
        FactionId, Conditions.ToArray(), Stats, EnergyCurrent, EnergyDepleted, ShieldProtectionAvailable,
        ShieldDowntimeDeadline, AuthoritativeTick, ConfigurationRevision)
    {
        AfterburnerActive = AfterburnerActive, Components = Components,
        HealthCapacity = HealthCapacity, HealthCurrent = HealthCurrent
    };
}
