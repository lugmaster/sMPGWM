using Godot;
using sMPGWM.Scripts.Controllers.LivingEntity;
using GCol = Godot.Collections;

namespace sMPGWM.Scripts.Enums.Game;

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

    public GCol.Dictionary<int, Variant> CreatePermanentDelta()
    {
        var delta = CurrentState.CreatePermanentDelta(_lastState);
        if (delta.Count > 0)
        {
            _lastState.ApplyPermanentDelta(delta);
        }

        return delta;
    }

    public GCol.Dictionary<int, Variant> CreateCurrentDelta()
    {
        var delta = CurrentState.CreateCurrentDelta(_lastState);
        if (delta.Count > 0)
        {
            _lastState.ApplyCurrentDelta(delta);
        }

        return delta;
    }
}