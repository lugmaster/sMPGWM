using sMPGWM.Scripts.AbilitySystem;

namespace sMPGWM.Scripts.NetworkSystem;

internal sealed record GameplaySnapshotEntry(string Entity, long Revision, double Capacity, double Current, BehaviourInstance[] Behaviours);
