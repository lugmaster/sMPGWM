using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Constants;
using FlatEarthUniverse.scripts.Controllers.LivingEntity;
using FlatEarthUniverse.scripts.GameObjects.Ships;
using FlatEarthUniverse.scripts.Network;
using Godot;
using GCol = Godot.Collections;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public abstract class MovementController
{
    // Constants / static
    private const int LogClassLevel = -1;
    protected static MovementSynchronizer MovementSynchronizer => GlobalStateManager.MovementSynchronizer;

    // Dependencies (readonly)
    protected readonly MpController MpController;
    private readonly ShipStats _shipStats;
    private readonly LivingEntityState _livingEntityState;
    protected readonly MovementStateTracker MovementStateTracker;

    // Readonly computed parameters
    private readonly float _currentRotationInertia;
    private readonly float _currentFriction;

    // Exposed/protected state
    protected Vector2 InputDirection;
    protected bool EnableFriction;
    protected float TargetRotationAngle;
    protected bool AfterBurnerPressed;

    // Private state
    private LivingEntityMovingCondition _movingCondition;
    private bool _isMoving;
    private bool _isAccelerating;
    private bool _flagForRespawn;
    private bool _movementFrozen;

    // Ctor
    protected MovementController(
        MpController mpController,
        ShipStats shipStats,
        LivingEntityState livingEntityState)
    {
        MpController = mpController;
        _shipStats = shipStats;
        _livingEntityState = livingEntityState;

        MovementStateTracker = new MovementStateTracker(
            mpController.GlobalPosition,
            mpController.Velocity,
            mpController.Rotation,
            0f,
            Vector2.Zero,
            mpController.Rotation,
            false,
            true,
            NetworkTicker.GetTicksSinceStart());

        _currentRotationInertia = 1 + _shipStats.ShipMass * 1.05f;
        _currentFriction = 0.15f / (1 + _shipStats.ShipMass * 0.95f);
    }

    // Public API
    public virtual void Process(float delta)
    {
        UpdateIsMoving();
        UpdateIsAccelerating();
        UpdateMovingCondition();
    }

    public void FreezeMovement()
    {
        _movementFrozen = true;
        MovementStateTracker.ClearState();
        MpController.Velocity = Vector2.Zero;
    }

    public void UnfreezeMovement()
    {
        _movementFrozen = false;
    }

    public void ReinitializeForRespawn(Vector2 newPosition)
    {
        _flagForRespawn = true;
        MpController.GlobalPosition = newPosition;
        MovementStateTracker.ReinitializeForRespawn(
            MpController.GlobalPosition,
            MpController.Velocity,
            MpController.Rotation,
            -1f,
            Vector2.Zero,
            MpController.Rotation,
            false,
            true,
            NetworkTicker.GetTicksSinceStart());
    }

    public void AddStateForClientSync(GCol.Dictionary<int, Variant> statesToSend, int peerId)
    {
        var shouldSend =
            MovementStateTracker.HasLastStateSentChangedToCurrent()
            || _flagForRespawn;

        if (!shouldSend) return;

        if (_flagForRespawn)
            _flagForRespawn = false;

        statesToSend.Add(peerId, MovementStateTracker.GetCurrentState().ToVariant());
        MovementStateTracker.UpdateLastStateSentFromCurrent();
    }

    public void ApplyReceivedMovementFromClient(GCol.Array receivedMovement)
    {
        var state = MovementState.ToMovementState(receivedMovement);
        Logger.Log($"Received movement from client: {state} at {NetworkTicker.GetLogInfo()}", LogClassLevel);
        MovementStateTracker.UpdateLastStateReceived(MovementState.ToMovementState(receivedMovement));
    }

    public void ApplyUpdatesFromHost(GCol.Array updatedState)
    {
        var state = MovementState.ToMovementState(updatedState);
        Logger.Log($"\n\nReceived updates from host: {state} at {NetworkTicker.GetLogInfo()}", LogClassLevel);
        MovementStateTracker.UpdateLastStateReceived(MovementState.ToMovementState(updatedState));
    }

    public LivingEntityMovingCondition GetMovingCondition() => _movingCondition;

    public Vector2 GetInputDirection() => InputDirection;

    // Protected methods
    protected void ProcessRotation(float delta, float targetRotationAngle)
    {
        MpController.Rotation = MovementPhysics.RotationStep(
            currentRotation:        MpController.Rotation,
            targetRotationAngle:    targetRotationAngle,
            rotationSpeedPerSecond: _shipStats.RotationSpeed,
            rotationInertia:        _currentRotationInertia,
            delta:                  delta
        );
    }

    protected void ProcessDirection(float delta, Vector2 inputDirection, bool enableFriction)
    {
        MpController.Velocity = MovementPhysics.MovementStep(
            currentVelocity:      MpController.Velocity,
            inputDirection:       inputDirection,
            basisX:               MpController.Transform.X,
            basisY:               MpController.Transform.Y,
            currentAcceleration:  _livingEntityState.GetCurrentAcceleration(AfterBurnerPressed),
            maxSpeed:             _livingEntityState.GetCurrentSpeed(AfterBurnerPressed),
            currentFriction:      _currentFriction,
            delta:                delta,
            enableFriction:       enableFriction
        );
    }

    // Private helpers
    private void UpdateIsMoving()
    {
        _isMoving = !_movementFrozen && MpController.Velocity.LengthSquared() > 0.01f;
    }

    private void UpdateIsAccelerating()
    {
        _isAccelerating = !_movementFrozen && InputDirection != Vector2.Zero;
    }

    private void UpdateMovingCondition()
    {
        _movingCondition = _isMoving switch
        {
            true when _isAccelerating && AfterBurnerPressed => LivingEntityMovingCondition.AfterBurning,
            true when _isAccelerating => LivingEntityMovingCondition.Accelerating,
            true => LivingEntityMovingCondition.Moving,
            _ => LivingEntityMovingCondition.Idle
        };
    }
}
