using sMPGWM.Scripts.MovementSystem;

namespace sMPGWM.Scripts.ShipSystem;

public readonly record struct ShipMovementState(ShipInputState Input, long DiscontinuityRevision,
    byte AppliedFlags, byte PresentedFlags, long ConfigurationRevision, MovementConfiguration Configuration);
