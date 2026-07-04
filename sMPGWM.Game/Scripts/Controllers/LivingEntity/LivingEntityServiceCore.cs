using System;
using System.Collections.Generic;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Non-node service facade that registers living entities and forwards work to the static state engine.
/// </summary>
public sealed class LivingEntityServiceCore
{
    private readonly Dictionary<int, LivingEntityRuntime> _runtimes = new();

    /// <summary>
    /// Raised when a registered runtime reports an effective condition change.
    /// </summary>
    public event Action<LivingEntityConditionChanged> ConditionChanged;

    /// <summary>
    /// Registers a living-entity state for state-engine processing under the supplied peer id.
    /// </summary>
    public void RegisterPeer(int peerId, LivingEntityState state)
    {
        if (_runtimes.TryGetValue(peerId, out var existingRuntime))
            existingRuntime.ConditionChanged -= OnRuntimeConditionChanged;

        var runtime = new LivingEntityRuntime(state);
        runtime.ConditionChanged += OnRuntimeConditionChanged;
        _runtimes[peerId] = runtime;
        StateEngine.AddLivingEntity(state);
    }

    /// <summary>
    /// Creates and registers a default player-controlled living entity.
    /// </summary>
    public LivingEntityRuntime RegisterPeerWithDefaultState(int peerId)
    {
        var state = LivingEntityStateFactory.CreateDefaultPlayerState(peerId);
        RegisterPeer(peerId, state);
        return _runtimes[peerId];
    }

    /// <summary>
    /// Removes a previously registered peer runtime from the service facade.
    /// </summary>
    public void UnregisterPeer(int peerId)
    {
        if (_runtimes.Remove(peerId, out var runtime))
            runtime.ConditionChanged -= OnRuntimeConditionChanged;
    }

    /// <summary>
    /// Returns true when a peer id is registered with this service facade.
    /// </summary>
    public bool HasPeer(int peerId)
    {
        return _runtimes.ContainsKey(peerId);
    }

    /// <summary>
    /// Gets the runtime facade registered for the supplied peer id.
    /// </summary>
    public LivingEntityRuntime GetRuntime(int peerId)
    {
        return GetRequiredRuntime(peerId);
    }

    /// <summary>
    /// Gets the living-entity state registered for the supplied peer id.
    /// </summary>
    public LivingEntityState GetState(int peerId)
    {
        return GetRequiredRuntime(peerId).State;
    }

    /// <summary>
    /// Processes ordered active effects for a registered peer and returns the packed delta payload.
    /// </summary>
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

    /// <summary>
    /// Builds a pull-based UI DTO for the registered peer.
    /// </summary>
    public UiDto GetUpdateForUiLoop(int peerId)
    {
        return StateEngine.GetUpdateForUiLoop(GetRequiredRuntime(peerId).State.EntityId);
    }

    /// <summary>
    /// Applies an authoritative packed delta to the registered peer.
    /// </summary>
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
