using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.Network;

internal static class MovementStateAdapter
{
    public static MovementSample Capture(int peer, MpController owner, long arrival)
    {
        var ship = owner.ControlledShip!;
        return new(peer, ship.EntityId, owner.ControlGeneration, ship.DiscontinuityRevision,
            ship.GlobalPosition, ship.GlobalRotation, ship.Velocity, ship.InputState, arrival, ship.AppliedMovementFlags);
    }
    public static void Restore(ShipController ship, MovementSample sample)
    {
        ship.Restore(sample.Position, sample.Rotation, sample.Velocity, sample.Input);
        ship.RestoreMovementMetadata(sample.Revision, sample.AppliedFlags);
    }
}
