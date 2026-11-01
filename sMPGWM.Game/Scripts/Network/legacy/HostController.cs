using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Controllers.Player;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController;

public partial class HostController
    : PlayerController
{
    public override void Initialize()
    {
        base.Initialize();
        GlobalStateManager.PlayerController.EmitPlayerSpawnedEvent();
    }

    protected override void OnProcess(float delta)
    {
        // Actions
        ActionController.ProcessActions(InputController);

        //Animations
        MpAnimationController.ApplyAnimation(InputController.Direction);

        // Sound
        MpSoundController.Condition = MovementController.GetMovingCondition();
    }

    protected override void OnPhysicsProcess(float delta)
    {
        // Movement
        MovementController.Process(delta);
    }

    public override void Cleanup()
    {
        InputController.QueueFree();
        base.Cleanup();
    }
}