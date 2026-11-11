using System;
using Godot;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.World;

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
    internal PlayerSession? ControllingPlayer { get; set; }
    public string EntityId { get; internal set; } = "";
    public sMPGWM.Scripts.GamePlaySystem.ShipGameplayState Gameplay { get; private set; } = null!;
    private ShipData _data = null!;
    private float _rotationInertia;
    private float _friction;
    public ShipData Data
    {
        get => _data;
        internal set
        {
            _data = value;
            Gameplay ??= new();
            DefinitionChanged?.Invoke();
        }
    }
    public ShipState State { get; } = new();
    public event Action? DefinitionChanged;
    public MovementConfiguration MovementConfiguration { get; private set; }
    public long MovementConfigurationRevision { get; private set; }

    public void RefreshMovementConfiguration()
    {
        var stats = Gameplay.Stats;
        var configuration = new MovementConfiguration(stats.Speed, stats.AfterBurnerSpeed, stats.RotationSpeed,
            stats.Acceleration, stats.AfterBurnerAcceleration, (float)stats.Mass, Gameplay.MovementAvailable);
        InstallMovementConfiguration(Gameplay.ConfigurationRevision, configuration);
    }

    internal void InstallMovementConfiguration(long revision, MovementConfiguration configuration)
    {
        if (revision < MovementConfigurationRevision) return;
        if (revision < 0 || !configuration.IsValid) throw new ArgumentException("Invalid movement configuration.");
        MovementConfiguration = configuration;
        MovementConfigurationRevision = revision;
        _rotationInertia = (float)Math.Min(float.MaxValue, 1d + configuration.Mass * 1.05d);
        _friction = (float)(0.15d / (1d + configuration.Mass * 0.95d));
    }
    private ShipInputState _input;
    public byte AppliedMovementFlags { get; private set; }
    public byte PresentedMovementFlags { get => Presentation.MovementFlags; set => Presentation.MovementFlags = value; }
    private ShipPresentation? _presentation;
    public ShipPresentation Presentation => _presentation ??= new(this);
    internal void RestoreMovementMetadata(long revision, byte flags)
    { DiscontinuityRevision = revision; AppliedMovementFlags = flags; }
    public ShipInputState InputState => _input;
    public long DiscontinuityRevision { get; private set; }
    public void Restore(Vector2 position, float rotation, Vector2 velocity, ShipInputState input)
    {
        GlobalPosition = position; GlobalRotation = rotation; Velocity = velocity; _input = input;
        CaptureState();
    }
    public bool LocallyControlled { get; private set; }
    public event Action? Teleported;
    public Vector2 DisplayedPosition => Presentation.Position;
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
        var s = MovementConfiguration;
        if (s.Enabled && _input.HasAim) GlobalRotation = MovementPhysics.RotationStep(GlobalRotation, _input.AimAngle, s.RotationSpeed, _rotationInertia, (float)delta);
        var acceleration = _input.Afterburner ? s.AfterBurnerAcceleration : s.Acceleration;
        Velocity = MovementPhysics.MovementStep(Velocity, _input.Brake ? Vector2.Zero : _input.Direction,
            GlobalTransform.X.Normalized(), GlobalTransform.Y.Normalized(), acceleration,
            _input.Afterburner ? s.AfterBurnerSpeed : s.Speed, _friction,
            (float)delta, _input.Brake || _input.AutomaticBraking, s.Enabled);
        bool thrust = s.Enabled && !_input.Brake && _input.Direction.LengthSquared() > .01f;
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
