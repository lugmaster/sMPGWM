using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Network;

/// <summary>All remote collision bodies must be current before any local historical step runs.</summary>
internal static class MovementSnapshotApplication
{
    public static void Apply(IReadOnlyList<MovementSample> samples, IReadOnlyDictionary<int, MpController> owners,
        Action<MpController, MovementSample> remote, Action<MpController, MovementSample> local)
    {
        var accepted = new List<(MpController Owner, MovementSample Sample)>();
        foreach (var sample in samples)
            if (owners.TryGetValue(sample.Peer, out var owner) && owner.ControlledShip?.EntityId == sample.Entity &&
                owner.ControlGeneration == sample.Generation) accepted.Add((owner, sample));
        foreach (var (owner, sample) in accepted)
            if (!owner.ControlledShip!.LocallyControlled) remote(owner, sample);
        foreach (var (owner, sample) in accepted)
            if (owner.ControlledShip!.LocallyControlled) local(owner, sample);
    }
}
