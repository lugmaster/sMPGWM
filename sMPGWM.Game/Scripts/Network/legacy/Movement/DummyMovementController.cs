using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Controllers.LivingEntity;
using FlatEarthUniverse.scripts.GameObjects.Ships;
using FlatEarthUniverse.scripts.Network;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public class DummyMovementController(
    MpController mpController,
    ShipStats shipStats,
    LivingEntityState
        livingEntityState,
    bool isHost) : MovementController(mpController, shipStats, livingEntityState)
{
    private const int LogLevel = -1;
    private bool _stateChangeEncountered;

    public override void Process(float delta)
    {
        base.Process(delta);
        _stateChangeEncountered = MovementStateTracker.HasLastStateReceivedChangedToCurrent();
        if (isHost)
        {
            if (_stateChangeEncountered)
            {
                InputDirection = MovementStateTracker.GetLastStateReceived().InputDirection;
                TargetRotationAngle = MovementStateTracker.GetLastStateReceived().TargetRotationAngle;
                AfterBurnerPressed = MovementStateTracker.GetLastStateReceived().AfterBurnerPressed;
                EnableFriction = MovementStateTracker.GetLastStateReceived().EnableFriction;
                
            }
            ProcessRotation(delta, TargetRotationAngle);
            ProcessDirection(delta, InputDirection, EnableFriction);
            MovementStateTracker.UpdateCurrentState(
                new MovementState(
                    MpController.GlobalPosition,
                    MpController.Velocity,
                    MpController.Rotation,
                    delta,
                    MovementStateTracker.GetLastStateReceived().InputDirection,
                    MovementStateTracker.GetLastStateReceived().TargetRotationAngle,
                    MovementStateTracker.GetLastStateReceived().AfterBurnerPressed,
                    MovementStateTracker.GetLastStateReceived().EnableFriction,
                    NetworkTicker.GetTicksSinceStart(),
                    MovementStateTracker.GetLastStateReceived().StateId));

            MpController.MoveAndSlide();

            Logger.Log(
                $"Dummy Processed from {MovementStateTracker.GetLastStateReceived()} to {MovementStateTracker.GetCurrentState()} at {NetworkTicker.GetLogInfo()}",
                LogLevel);
        }
        else
        {
            if (_stateChangeEncountered)
            {
                ApplyFromState(MovementStateTracker.GetLastStateReceived());
                var lastState = MovementStateTracker.GetLastStateReceived();
                MovementStateTracker.UpdateCurrentStateFromLastReceived(NetworkTicker.GetTicksSinceStart());
                Logger.Log(
                    $"Dummy Processed from {lastState} to {MovementStateTracker.GetCurrentState()} at {NetworkTicker.GetLogInfo()}",
                    LogLevel);
            }
        }
    }

    private void ApplyFromState(MovementState state)
    {
        MpController.GlobalPosition = state.Position;
        MpController.Rotation = state.Rotation;
        MpController.Velocity = state.Velocity;
        InputDirection = state.InputDirection;
        TargetRotationAngle = state.TargetRotationAngle;
    }
}