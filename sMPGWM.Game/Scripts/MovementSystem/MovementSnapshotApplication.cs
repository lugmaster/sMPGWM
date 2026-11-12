using System;
using System.Collections.Generic;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.MovementSystem;

/// <summary>All remote collision bodies must be current before any local historical step runs.</summary>
internal static class MovementSnapshotApplication
{
    public static void Apply(IReadOnlyList<MovementSample> samples, IReadOnlyDictionary<int, PlayerSession> owners,
        Action<PlayerSession, MovementSample> remote, Action<PlayerSession, MovementSample> local)
    {
        foreach (var sample in samples)
            if (owners.TryGetValue(sample.Peer, out var owner) && owner.ControlledShip?.EntityId == sample.Entity &&
                owner.ControlGeneration == sample.Generation && !owner.ControlledShip.LocallyControlled) remote(owner, sample);
        foreach (var sample in samples)
            if (owners.TryGetValue(sample.Peer, out var owner) && owner.ControlledShip?.EntityId == sample.Entity &&
                owner.ControlGeneration == sample.Generation && owner.ControlledShip.LocallyControlled) local(owner, sample);
    }
}
