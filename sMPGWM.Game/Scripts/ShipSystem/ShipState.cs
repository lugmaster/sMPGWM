using Godot;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipState
{
    public Vector2 Position { get; internal set; }
    public float Rotation { get; internal set; }
    public Vector2 Velocity { get; internal set; }
}
