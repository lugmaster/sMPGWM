using FlatEarthUniverse.scripts.Controllers.LivingEntity;
using FlatEarthUniverse.scripts.Controllers.Player;
using FlatEarthUniverse.scripts.GameObjects.Ships;
using FlatEarthUniverse.scripts.Network;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController.Movement;

public class HostMovementController(
    MpController mpController,
    ShipStats shipStats,
    LivingEntityState
        livingEntityState,
    InputController inputController)
    : MovementController(mpController, shipStats, livingEntityState)
{
    public override void Process(float delta)
    {
        base.Process(delta);
        GatherInput();
        ProcessRotation(delta, TargetRotationAngle);
        ProcessDirection(delta, InputDirection, EnableFriction);
        MpController.MoveAndSlide();
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
}