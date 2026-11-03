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
    public bool LocallyControlled { get; private set; }
    public event Action? Teleported;
    public Vector2 DisplayedPosition => GetNode<Node2D>("ShipVisuals").GlobalPosition;
    public void SetLocalControl(bool enabled)
    {
        LocallyControlled = enabled;
        ClearInput();
        SetPhysicsProcess(enabled);
    }
    public void SetInput(ShipInputState input) => _input = input;
    public void ClearInput() => _input = new(Vector2.Zero, 0, false, false, false, _input.AutomaticBraking);
    public void Teleport(Vector2 position, float rotation, Vector2 velocity)
    {
        GlobalPosition = position; GlobalRotation = rotation; Velocity = velocity;
        CaptureState(); Teleported?.Invoke();
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!LocallyControlled) return;
        var s = Data.Stats;
        if (_input.HasAim) Rotation = MovementPhysics.RotationStep(Rotation, _input.AimAngle, s.RotationSpeed, s.ShipMass, (float)delta);
        var acceleration = (_input.Afterburner ? s.AfterBurnerAcceleration : s.Acceleration) / s.ShipMass;
        Velocity = MovementPhysics.MovementStep(Velocity, _input.Brake ? Vector2.Zero : _input.Direction,
            Transform.X.Normalized(), Transform.Y.Normalized(), acceleration,
            _input.Afterburner ? s.AfterBurnerSpeed : s.Speed, s.Acceleration / s.ShipMass,
            (float)delta, _input.Brake || _input.AutomaticBraking);
        MoveAndSlide(); CaptureState();
    }
    internal void CaptureState()
    {
        State.Position = Position; State.Rotation = Rotation; State.Velocity = Velocity;
    }
}
