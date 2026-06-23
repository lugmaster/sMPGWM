using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public class LivingEntityStateTracker
{
    public LivingEntityState CurrentState { get; }
    private readonly LivingEntityState _lastState;

    public LivingEntityStateTracker(LivingEntityState currentState)
    {
        CurrentState = currentState;
        _lastState = CurrentState.DeepCopy();
    }

    public void ApplyFullInitialState(GCol.Array livingEntityStateVariant)
    {
        CurrentState.UpdateFullState(livingEntityStateVariant);
        _lastState.UpdateFullState(livingEntityStateVariant);
    }

    public GCol.Array CreateCombinedDelta(bool includePermanent)
    {
        var delta = CurrentState.CreateDeltaVariant(_lastState, includePermanent);
        if (LivingEntityState.DeltaVariantHasChanges(delta))
            _lastState.ApplyDeltaVariant(delta);

        return delta;
    }
}
