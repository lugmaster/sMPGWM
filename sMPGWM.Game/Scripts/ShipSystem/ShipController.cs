using System;
using Godot;
using sMPGWM.Scripts.Movement;
namespace sMPGWM.Scripts.ShipSystem;
public readonly record struct ShipInputState(Vector2 Direction, float AimAngle, bool HasAim, bool Afterburner, bool Brake, bool AutomaticBraking);
public sealed class ShipState
{
    public Vector2 Position { get; internal set; }
    public float Rotation { get; internal set; }
    public Vector2 Velocity { get; internal set; }
}
public partial class ShipController : CharacterBody2D
{
    public string EntityId { get; internal set; } = "";
    public ShipData Data { get; internal set; } = null!;
    public ShipState State { get; } = new();
    private ShipInputState _input;
    public byte AppliedMovementFlags { get; private set; }
    public byte PresentedMovementFlags { get; set; }
    public ShipInputState InputState => _input;
    public long DiscontinuityRevision { get; private set; }
    public void Restore(Vector2 position, float rotation, Vector2 velocity, ShipInputState input)
    {
        GlobalPosition = position; GlobalRotation = rotation; Velocity = velocity; _input = input;
        CaptureState();
    }
    public void Present(Vector2 position, float rotation)
    {
        var visuals = GetNode<Node2D>("ShipVisuals");
        visuals.GlobalPosition = position; visuals.GlobalRotation = rotation;
        var camera = GetNodeOrNull<Camera2D>("LocalCamera");
        if (camera != null) camera.GlobalPosition = position;
    }
    public bool LocallyControlled { get; private set; }
    public event Action? Teleported;
    public Vector2 DisplayedPosition => GetNode<Node2D>("ShipVisuals").GlobalPosition;
    public void SetLocalControl(bool enabled)
    {
        LocallyControlled = enabled;
        ClearInput();
    }
    public void SetInput(ShipInputState input) => _input = input;
    public void ClearInput() => _input = new(Vector2.Zero, 0, false, false, false, _input.AutomaticBraking);
    public void Teleport(Vector2 position, float rotation, Vector2 velocity)
    {
        GlobalPosition = position; GlobalRotation = rotation; Velocity = velocity;
        checked { DiscontinuityRevision++; } CaptureState(); Teleported?.Invoke();
    }
    // The session coordinator owns tick ordering; ships do not tick independently.
    public void Simulate(double delta)
    {

        var previousRotation = GlobalRotation;
        var s = Data.Stats;
        if (_input.HasAim) GlobalRotation = MovementPhysics.RotationStep(GlobalRotation, _input.AimAngle, s.RotationSpeed, s.ShipMass, (float)delta);
        var acceleration = (_input.Afterburner ? s.AfterBurnerAcceleration : s.Acceleration) / s.ShipMass;
        Velocity = MovementPhysics.MovementStep(Velocity, _input.Brake ? Vector2.Zero : _input.Direction,
            GlobalTransform.X.Normalized(), GlobalTransform.Y.Normalized(), acceleration,
            _input.Afterburner ? s.AfterBurnerSpeed : s.Speed, s.Acceleration / s.ShipMass,
            (float)delta, _input.Brake || _input.AutomaticBraking);
        bool thrust = !_input.Brake && _input.Direction.LengthSquared() > .01f;
        AppliedMovementFlags = (byte)((thrust ? 1 : 0) |
            (GlobalRotation != previousRotation ? 2 : 0) | (thrust && _input.Afterburner ? 4 : 0));
        PresentedMovementFlags = AppliedMovementFlags;
        // Explicit displacement makes historical steps independent of the engine callback delta.
        var motion = Velocity * (float)delta;
        for (int slide = 0; slide < 4 && motion.LengthSquared() > 0.0000001f; slide++)
        {
            var collision = MoveAndCollide(motion);
            if (collision == null) break;
            var normal = collision.GetNormal();
            motion = collision.GetRemainder().Slide(normal);
            if (Velocity.Dot(normal) < 0) Velocity = Velocity.Slide(normal);
        }
        CaptureState();
    }
    internal void CaptureState()
    {
        State.Position = GlobalPosition; State.Rotation = GlobalRotation; State.Velocity = Velocity;
    }
}
