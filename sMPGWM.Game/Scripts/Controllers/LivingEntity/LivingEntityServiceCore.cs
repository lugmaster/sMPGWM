using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class LivingEntityServiceCore
{
    private readonly Dictionary<int, LivingEntityRuntime> _runtimes = new();

    public event Action<LivingEntityConditionChanged> ConditionChanged;

    public void RegisterPeer(int peerId, LivingEntityState state)
    {
        if (_runtimes.TryGetValue(peerId, out var existingRuntime))
            existingRuntime.ConditionChanged -= OnRuntimeConditionChanged;

        var runtime = new LivingEntityRuntime(state);
        runtime.ConditionChanged += OnRuntimeConditionChanged;
        _runtimes[peerId] = runtime;
        StateEngine.AddLivingEntity(state);
    }

    public LivingEntityRuntime RegisterPeerWithDefaultState(int peerId)
    {
        var state = LivingEntityStateFactory.CreateDefaultPlayerState(peerId);
        RegisterPeer(peerId, state);
        return _runtimes[peerId];
    }

    public void UnregisterPeer(int peerId)
    {
        if (_runtimes.Remove(peerId, out var runtime))
            runtime.ConditionChanged -= OnRuntimeConditionChanged;
    }

    public bool HasPeer(int peerId)
    {
        return _runtimes.ContainsKey(peerId);
    }

    public LivingEntityRuntime GetRuntime(int peerId)
    {
        return GetRequiredRuntime(peerId);
    }

    public LivingEntityState GetState(int peerId)
    {
        return GetRequiredRuntime(peerId).State;
    }

    public PackedByteArray ProcessLivingEntity(
        int peerId,
        long networkTick,
        ActiveEffect[] activeEffects,
        int activeEffectCount)
    {
        var runtime = GetRequiredRuntime(peerId);
        var oldCondition = runtime.Condition;
        var delta = StateEngine.ProcessLivingEntity(runtime.State.EntityId, networkTick, activeEffects, activeEffectCount);
        runtime.EmitConditionChanged(oldCondition, "state-engine");
        return delta;
    }

    public UiDto GetUpdateForUiLoop(int peerId)
    {
        return StateEngine.GetUpdateForUiLoop(GetRequiredRuntime(peerId).State.EntityId);
    }

    public void ApplyDelta(int peerId, PackedByteArray delta)
    {
        var runtime = GetRequiredRuntime(peerId);
        var oldCondition = runtime.Condition;
        StateEngine.ApplyDelta(runtime.State.EntityId, delta);
        runtime.EmitConditionChanged(oldCondition, "delta");
    }

    private LivingEntityRuntime GetRequiredRuntime(int peerId)
    {
        if (_runtimes.TryGetValue(peerId, out var runtime))
            return runtime;

        throw new InvalidOperationException($"No living entity runtime registered for peer {peerId}.");
    }

    private void OnRuntimeConditionChanged(LivingEntityConditionChanged change)
    {
        ConditionChanged?.Invoke(change);
    }
}
