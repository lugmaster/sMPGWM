using System.Collections.Generic;
using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Controllers.LivingEntity;
using FlatEarthUniverse.scripts.Controllers.Player;
using FlatEarthUniverse.scripts.GameObjects.Ships;
using FlatEarthUniverse.scripts.Network;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public class ClientMovementController(
    MpController mpController,
    ShipStats shipStats,
    LivingEntityState
        livingEntityState,
    InputController inputController)
    : MovementController(mpController, shipStats, livingEntityState)
{
    private const int LogLevel = -1;
    private List<MovementState> _statesToCorrect = new(30);

    public override void Process(float delta)
    {
        base.Process(delta);
        Logger.Log($"Processing at {NetworkTicker.GetLogInfo()}", LogLevel);
        ApplyHostCorrection();
        GatherInput();
        SyncStateToHost(delta);
        ProcessRotation(delta, TargetRotationAngle);
        ProcessDirection(delta, InputDirection, EnableFriction);
        MpController.MoveAndSlide();
        Logger.Log(
            $"Received {MovementStateTracker.GetLastStateReceived().TickNumber} -- Current {MovementStateTracker.GetCurrentState().TickNumber} at {NetworkTicker.GetLogInfo()}",
            LogLevel);
    }

    private void ApplyHostCorrection()
    {
        if (MovementStateTracker.ShouldApplySnap())
        {
            MpController.GlobalPosition = MovementStateTracker.GetLastStateReceived().Position;
            MpController.Velocity = MovementStateTracker.GetLastStateReceived().Velocity;
            MpController.Rotation = MovementStateTracker.GetLastStateReceived().Rotation;
            MovementStateTracker.ClearState();
            Logger.Log($"Applying Snap at {NetworkTicker.GetLogInfo()} form lastReceived: \n+{MovementStateTracker.GetLastStateReceived()}", LogLevel);
            return;
        }

        MovementState? correctedState = null;
        MovementState? stateCurrent = null;

        _statesToCorrect = MovementStateTracker.GetStatesToCorrect();

        if (_statesToCorrect.Count != 0)
        {
            stateCurrent = MovementStateTracker.CreateStateSnapShot(MpController, InputDirection, TargetRotationAngle);
            var correctedStates = new Queue<MovementState>(_statesToCorrect.Count);
            MovementStateTracker.ApplyStateSnapShot(MpController, _statesToCorrect[0]);

            foreach (var state in _statesToCorrect)
            {
                ProcessRotation(state.Delta, state.TargetRotationAngle);
                ProcessDirection(state.Delta, state.InputDirection, EnableFriction);
                correctedStates.Enqueue(MovementStateTracker.CreateStateSnapShot(MpController, state.InputDirection,
                    state.TargetRotationAngle, state.AfterBurnerPressed, state.EnableFriction, state.Delta, state.TickNumber, state.StateId));
            }

            MovementStateTracker.ReApplyCorrectedStates(correctedStates);

            correctedState =
                MovementStateTracker.CreateStateSnapShot(MpController, InputDirection, TargetRotationAngle, AfterBurnerPressed, EnableFriction);
            Logger.Log(
                $"Correcting state {stateCurrent} with past states: \n{string.Join(" ,\n", _statesToCorrect)}\n" +
                $"from last received: {MovementStateTracker.GetLastStateReceived()} to corrected state: {correctedState} at {NetworkTicker.GetLogInfo()}",
                LogLevel);

            stateCurrent = stateCurrent.Value.CorrectStateTo(correctedState.Value);
            MovementStateTracker.ApplyStateSnapShot(MpController, stateCurrent.Value);
            Logger.Log(
                $"CurrentState: {stateCurrent.Value} is now lerped from {correctedState.Value} at {NetworkTicker.GetLogInfo()}",
                LogLevel);
        }
    }

    private void GatherInput()
    {
        if (inputController.AlternateActionPressed)
        {
            TargetRotationAngle = (MpController.GetGlobalMousePosition() - MpController.GlobalPosition).Angle();
        }
        AfterBurnerPressed = inputController.AfterBurnerPressed;
        InputDirection = inputController.Direction;
        EnableFriction = inputController.AutoBreakingEnabled || inputController.BreakPressed;
    }

    private void SyncStateToHost(float delta)
    {
        MovementStateTracker.UpdateCurrentState(
            new MovementState(
                MpController.GlobalPosition,
                MpController.Velocity,
                MpController.Rotation,
                delta,
                InputDirection,
                TargetRotationAngle,
                AfterBurnerPressed,
                EnableFriction,
                NetworkTicker.GetTicksSinceStart(),
                MovementStateTracker.GenerateStateId()));

        Logger.Log($"Sending state {MovementStateTracker.GetCurrentState()} to host at {NetworkTicker.GetLogInfo()}",
            LogLevel);

        MovementSynchronizer.SyncToHost(MovementStateTracker.GetCurrentState().ToVariant());
        MovementStateTracker.UpdateLastStateSentFromCurrent();
    }
}