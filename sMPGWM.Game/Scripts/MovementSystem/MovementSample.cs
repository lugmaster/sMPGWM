using Godot;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.MovementSystem;

public readonly record struct MovementSample(int Peer, string Entity, long Generation, long Revision,
    Vector2 Position, float Rotation, Vector2 Velocity, ShipInputState Input, long Arrival, byte AppliedFlags = 0,
    long ConfigurationRevision = 0, MovementConfiguration Configuration = default);
