namespace FlatEarthUniverse.scripts.Controllers.MultiplayerController;

public partial class DummyController
    : MpController
{

    protected override void OnProcess(float delta)
    {
        // Movement and Actions if dummy on client
        if (!IsHost)
        {
            MovementController.Process(delta);
            ActionController.ProcessActions();
        }
        
        // Animation 
        MpAnimationController.ApplyAnimation(
            MovementController.GetInputDirection());
        
        // Sound
        MpSoundController.Condition = MovementController.GetMovingCondition();
    }

    protected override void OnPhysicsProcess(float delta)
    {
        // Movement and Actions if dummy on host
        if (!IsHost) return;
        MovementController.Process(delta);
        ActionController.ProcessActions();
    }

}