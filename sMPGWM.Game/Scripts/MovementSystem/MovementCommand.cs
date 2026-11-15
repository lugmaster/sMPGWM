using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

public readonly record struct MovementCommand(long Tick, ShipInputState Input);
