using FlatEarthUniverse.scripts.Autoloads;
using FlatEarthUniverse.scripts.Controllers.Player;

namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController;

public partial class ClientController
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

        // Movement
        MovementController.Process(delta);

        // Animations
        MpAnimationController.ApplyAnimation(InputController.Direction);

        // Sound
        MpSoundController.Condition = MovementController.GetMovingCondition();
    }

    public override void Cleanup()
    {
        InputController.QueueFree();
        base.Cleanup();
    }
}