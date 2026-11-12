using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.MovementSystem;

internal static class MovementStateAdapter
{
    public static MovementSample Capture(int peer, PlayerSession owner, long arrival)
    {
        var ship = owner.ControlledShip!;
        return new(peer, ship.EntityId, owner.ControlGeneration, ship.DiscontinuityRevision,
            ship.GlobalPosition, ship.GlobalRotation, ship.Velocity, ship.InputState, arrival, ship.AppliedMovementFlags,
            ship.MovementConfigurationRevision, ship.MovementConfiguration);
    }
    public static void Restore(ShipController ship, MovementSample sample)
    {
        ship.InstallMovementConfiguration(sample.ConfigurationRevision, sample.Configuration);
        ship.Restore(sample.Position, sample.Rotation, sample.Velocity, sample.Input);
        ship.RestoreMovementMetadata(sample.Revision, sample.AppliedFlags);
    }
}
