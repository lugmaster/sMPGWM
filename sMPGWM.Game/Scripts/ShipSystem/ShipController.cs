using sMPGWM.Scripts.StatSystem;
using System;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.MovementSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.ShipSystem;
public partial class ShipController : CharacterBody2D
{
    internal PlayerSession? ControllingPlayer { get; set; }
    public string EntityId { get; internal set; } = "";
    public ShipRuntime Runtime { get; private set; } = null!;
    private ShipData _data = null!;
    private readonly MovementMotor _motor = new();
    private ShipLoadout? _loadout;
    private CollisionPolygon2D? _hullCollider;
    private CollisionShape2D? _shieldCollider;
    public bool ShieldColliderActive { get; private set; }
    private bool _shieldRecoveryPending;
    private bool _shieldCollisionPending;

    internal bool ConsumeShieldCollision()
    {
        var pending = _shieldCollisionPending;
        _shieldCollisionPending = false;
        return pending;
    }

    private void RecordShieldCollision(KinematicCollision2D collision)
    {
        if (ShieldColliderActive) _shieldCollisionPending = true;
        if (collision.GetCollider() is ShipController other && other.ShieldColliderActive)
            other._shieldCollisionPending = true;
    }

    internal void PrepareShieldCollider()
        => PrepareShieldCollider(null);

    internal void PrepareShieldCollider(Shape2D? preparedShape)
    {
        var geometry = Data.ShieldGeometry ?? throw new InvalidOperationException(
            $"Ship '{Data.Name}' requires shield_geometry. Run python tools/shield_geometry/bake_shield_geometry.py --write.");
        _hullCollider = GetNode<CollisionPolygon2D>("CollisionPolygon2D");
        _shieldCollider = GetNode<CollisionShape2D>("ShieldCollider");
        _shieldCollider.Shape = preparedShape ?? ShieldPhysicsGeometry.GetShape(geometry);
        _shieldCollider.Position = geometry.Center;
        _shieldCollider.RotationDegrees = geometry.RotationDegrees;
        _shieldCollider.Disabled = true;
    }

    public void SynchronizeShieldCollider()
    {
        var active = Runtime.IsAlive && Runtime.ShieldProtectionAvailable;
        var changed = active != ShieldColliderActive;
        if (changed)
        {
            Runtime.AdvanceMovementConfigurationRevision();
            ApplyShieldCollider(active);
        }
        RefreshMovementConfiguration();
        if (changed) Runtime.PublishChanges();
    }

    internal void InstallUncontrolledColliderState()
    {
        if (ControllingPlayer != null || Runtime.ConfigurationRevision < MovementConfigurationRevision) return;
        var stats = Runtime.Stats;
        InstallMovementConfiguration(Runtime.ConfigurationRevision, new MovementConfiguration(stats.Speed,
            stats.AfterBurnerSpeed, stats.RotationSpeed, stats.Acceleration, stats.AfterBurnerAcceleration,
            (float)stats.Mass, Runtime.MovementAvailable, Runtime.IsAlive && Runtime.ShieldProtectionAvailable));
    }

    private void ApplyShieldCollider(bool active)
    {
        if (_shieldCollider == null || _hullCollider == null) return;
        if (active == ShieldColliderActive) return;
        // Coordinator calls outside physics query flushing make this change visible to the next ray/motion query.
        _hullCollider.Disabled = active;
        _shieldCollider.Disabled = !active;
        ShieldColliderActive = active;
        _shieldRecoveryPending = active;
        if (IsInsideTree())
        {
            ForceUpdateTransform();
            // A zero-distance body test flushes pending broadphase shape updates before direct rays.
            TestMove(GlobalTransform, Vector2.Zero);
        }
    }

    internal void RecoverShieldOverlap()
    {
        if (!_shieldRecoveryPending || !ShieldColliderActive || !IsInsideTree()) return;
        for (var i = 0; i < 4; i++)
        {
            using var collision = MoveAndCollide(Vector2.Zero, recoveryAsCollision: true);
            if (collision == null || collision.GetTravel().LengthSquared() < 0.0000001f)
            {
                _shieldRecoveryPending = false;
                break;
            }
            RecordShieldCollision(collision);
        }
    }

    public ShipLoadout Loadout => _loadout ?? throw new InvalidOperationException("Ship loadout has not been configured.");
    public EquipmentController Equipment => Loadout.Equipment;
    public ResolvedShipStats Stats => Runtime.Stats;
    public ShipResources Resources => Runtime.Resources;

    public void ConfigureLoadout(ItemStore store, bool authoritative)
    {
        if (_loadout == null) _loadout = new ShipLoadout(this, store, authoritative);
        else
        {
            if (!ReferenceEquals(_loadout.Store, store)) throw new InvalidOperationException("Ship equipment already belongs to another store.");
            _loadout.SetAuthoritative(authoritative);
        }
    }

    public override void _ExitTree()
    {
        _loadout?.Dispose();
        Runtime?.Dispose();
        DefinitionChanged = null;
        Teleported = null;
    }
    public ShipData Data
    {
        get => _data;
        internal set
        {
            _data = value;
            Runtime ??= new(value.Health);
            DefinitionChanged?.Invoke();
        }
    }
    public ShipState State { get; } = new();
    public event Action? DefinitionChanged;
    public MovementConfiguration MovementConfiguration { get; private set; }
    public long MovementConfigurationRevision { get; private set; }

    public void RefreshMovementConfiguration()
    {
        var stats = Runtime.Stats;
        var configuration = new MovementConfiguration(stats.Speed, stats.AfterBurnerSpeed, stats.RotationSpeed,
            stats.Acceleration, stats.AfterBurnerAcceleration, (float)stats.Mass, Runtime.MovementAvailable, ShieldColliderActive);
        InstallMovementConfiguration(Runtime.ConfigurationRevision, configuration);
    }

    internal void InstallMovementConfiguration(long revision, MovementConfiguration configuration)
    {
        if (revision < MovementConfigurationRevision) return;
        if (revision < 0 || !configuration.IsValid) throw new ArgumentException("Invalid movement configuration.");
        ApplyShieldCollider(configuration.ShieldColliderActive);
        MovementConfiguration = configuration;
        MovementConfigurationRevision = revision;
        _motor.Configure(configuration);
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
    public ShipMovementState CaptureMovementState() => new(_input, DiscontinuityRevision,
        AppliedMovementFlags, PresentedMovementFlags, MovementConfigurationRevision, MovementConfiguration);

    public void RestoreMovementState(ShipMovementState state)
    {
        var stats = Runtime.Stats;
        var expected = new MovementConfiguration(stats.Speed, stats.AfterBurnerSpeed, stats.RotationSpeed,
            stats.Acceleration, stats.AfterBurnerAcceleration, (float)stats.Mass, Runtime.MovementAvailable, Runtime.IsAlive && Runtime.ShieldProtectionAvailable);
        if (!state.Configuration.IsValid || state.Configuration != expected || state.ConfigurationRevision != Runtime.ConfigurationRevision ||
            state.DiscontinuityRevision < 0 || state.DiscontinuityRevision == long.MaxValue ||
            state.AppliedFlags > 7 || state.PresentedFlags > 7 || !MovementWire.Valid(state.Input))
            throw new ArgumentException("Invalid restored movement state.");
        MovementConfigurationRevision = 0;
        InstallMovementConfiguration(state.ConfigurationRevision, state.Configuration);
        DiscontinuityRevision = state.DiscontinuityRevision;
        AppliedMovementFlags = state.AppliedFlags;
        _input = state.Input;
        CaptureState();
        Presentation.Snap();
        PresentedMovementFlags = state.PresentedFlags;
    }
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
        RecoverShieldOverlap();
        if (!Runtime.IsAlive) { Velocity = Vector2.Zero; ClearInput(); }
        var previousRotation = GlobalRotation;
        var s = MovementConfiguration;
        if (s.Enabled)
        {
            GlobalRotation = _motor.Rotate(GlobalRotation, _input, (float)delta);
            Velocity = _motor.Drive(Velocity, GlobalTransform.X.Normalized(), GlobalTransform.Y.Normalized(), _input, (float)delta);
        }
        bool thrust = s.Enabled && !_input.Brake && _input.Direction.LengthSquared() > .01f;
        AppliedMovementFlags = (byte)((thrust ? 1 : 0) |
            (GlobalRotation != previousRotation ? 2 : 0) | (thrust && _input.Afterburner ? 4 : 0));
        PresentedMovementFlags = AppliedMovementFlags;
        // Explicit displacement makes historical steps independent of the engine callback delta.
        var motion = Velocity * (float)delta;
        for (int slide = 0; slide < 4 && motion.LengthSquared() > 0.0000001f; slide++)
        {
            using var collision = MoveAndCollide(motion);
            if (collision == null) break;
            RecordShieldCollision(collision);
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
