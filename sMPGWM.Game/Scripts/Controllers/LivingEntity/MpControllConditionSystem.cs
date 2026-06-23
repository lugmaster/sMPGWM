namespace sMPGWM.Scripts.Controllers.LivingEntity;

// This is just a reference or idea giver. Read it carefully. Take the Concept from a Single File, where all system 
// side effects regarding living entities are handled. 
public class MpControllerConditionSystem
{
    // private LivingEntityCondition _current;
    // private readonly bool _isHost;
    // private readonly bool _isPlayer;
    // private readonly InputController _inputController;
    // private readonly MpController _mpController;
    //
    // public MpControllerConditionSystem(MpController mpController, LivingEntityCondition condition, bool isHost)
    // {
    //     _mpController = mpController;
    //     _current = condition;
    //     _isHost = isHost;
    //     _isPlayer = mpController.MpControllerType.IsPlayer();
    //     _inputController = _isPlayer ? ((PlayerController)mpController).InputController : null;
    // }
    //
    //
    // public void UpdateCondition(LivingEntityCondition newCondition, bool applyVisualEffects)
    // {
    //     if (_current == newCondition)
    //         return;
    //
    //     var removed = _current & ~newCondition;
    //     var added = newCondition & ~_current;
    //
    //     _current = newCondition;
    //
    //     foreach (LivingEntityCondition flag in Enum.GetValues(typeof(LivingEntityCondition)))
    //     {
    //         if (flag == LivingEntityCondition.None)
    //         {
    //             continue;
    //         }
    //
    //         if (removed.Contains(flag))
    //             OnConditionRemoved(flag, applyVisualEffects);
    //
    //         if (added.Contains(flag))
    //             OnConditionAdded(flag, applyVisualEffects);
    //     }
    // }
    //
    //
    // private void OnConditionAdded(LivingEntityCondition condition, bool applyVisualEffects)
    // {
    //     switch (condition)
    //     {
    //         case LivingEntityCondition.Dead:
    //             _mpController.CollisionPolygon2D.Disabled = true;
    //             _mpController.MpTextureController.HideCurrent();
    //             _mpController.MpAnimationController.StopAndReset();
    //             _mpController.MovementController.FreezeMovement();
    //             _mpController.ActionController.FreezeAction();
    //             if (_isHost)
    //             {
    //                 _mpController.LivingEntityState.SetVitalsToZero();
    //             }
    //
    //             if (_isPlayer)
    //             {
    //                 _inputController.DisableInput();
    //                 GlobalStateManager.UiManager.ShowNotification("You Died - Get ready to respawn!", true);
    //                 Logger.Log(
    //                     $"Player with type {_mpController.MpControllerType} and owningPeerId {_mpController.OwningPeerId} is calling Respawn",
    //                     0);
    //                 GlobalStateManager.MpInstantiator.RespawnMpController(_mpController.OwningPeerId);
    //             }
    //
    //             if (applyVisualEffects)
    //             {
    //                 GlobalStateManager.EffectController.PlayAt("Explosion", _mpController.GetPosition());
    //             }
    //
    //             break;
    //
    //         case LivingEntityCondition.Immobilized:
    //         case LivingEntityCondition.Invulnerable:
    //         case LivingEntityCondition.Invisible:
    //         case LivingEntityCondition.Untargetable:
    //         case LivingEntityCondition.EnergyDepleted:
    //             Logger.Log($"MpControllerConditionSystem is trying to process unhandled condition: {condition}", 1);
    //             break;
    //         case LivingEntityCondition.None:
    //         default:
    //             throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
    //     }
    // }
    //
    // private void OnConditionRemoved(LivingEntityCondition condition, bool applyVisualEffects)
    // {
    //     switch (condition)
    //     {
    //         case LivingEntityCondition.Dead:
    //             _mpController.MovementController.UnfreezeMovement();
    //             _mpController.CollisionPolygon2D.Disabled = false;
    //             _mpController.MpTextureController.ShowCurrent();
    //             if (_isHost)
    //             {
    //                 _mpController.LivingEntityState.RestoreVitalsToMax();
    //             }
    //
    //             if (_isPlayer)
    //             {
    //                 _inputController.EnableInput();
    //                 _mpController.MovementController.UnfreezeMovement();
    //                 _mpController.ActionController.UnfreezeAction();
    //             }
    //
    //             break;
    //
    //         case LivingEntityCondition.Immobilized:
    //         case LivingEntityCondition.Invulnerable:
    //         case LivingEntityCondition.Invisible:
    //         case LivingEntityCondition.Untargetable:
    //         case LivingEntityCondition.EnergyDepleted:
    //             Logger.Log($"MpControllerConditionSystem is trying to process unhandled condition: {condition}", 1);
    //             break;
    //         default:
    //         case LivingEntityCondition.None:
    //             throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
    //     }
    // }
}