using System;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

/// <summary>
/// Godot singleton facade for living-entity state-engine registration, processing, and UI inspection.
/// </summary>
public sealed partial class LivingEntityService : AbstractSingleton<LivingEntityService>
{
    private readonly LivingEntityServiceCore _core = new();

    /// <summary>
    /// Raised when a registered runtime reports an effective condition change.
    /// </summary>
    public event Action<LivingEntityConditionChanged> ConditionChanged
    {
        add => _core.ConditionChanged += value;
        remove => _core.ConditionChanged -= value;
    }

    /// <summary>
    /// Registers a living-entity state for processing under the supplied peer id.
    /// </summary>
    public void RegisterPeer(int peerId, LivingEntityState state)
    {
        _core.RegisterPeer(peerId, state);
    }

    /// <summary>
    /// Creates and registers a default player-controlled living entity.
    /// </summary>
    public LivingEntityRuntime RegisterPeerWithDefaultState(int peerId)
    {
        return _core.RegisterPeerWithDefaultState(peerId);
    }

    /// <summary>
    /// Removes the runtime registered for the supplied peer id.
    /// </summary>
    public void UnregisterPeer(int peerId)
    {
        _core.UnregisterPeer(peerId);
    }

    /// <summary>
    /// Returns true when a peer id has a registered living-entity runtime.
    /// </summary>
    public bool HasPeer(int peerId)
    {
        return _core.HasPeer(peerId);
    }

    /// <summary>
    /// Gets the runtime facade registered for the supplied peer id.
    /// </summary>
    public LivingEntityRuntime GetRuntime(int peerId)
    {
        return _core.GetRuntime(peerId);
    }

    /// <summary>
    /// Gets the living-entity state registered for the supplied peer id.
    /// </summary>
    public LivingEntityState GetState(int peerId)
    {
        return _core.GetState(peerId);
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
        return _core.ProcessLivingEntity(peerId, networkTick, activeEffects, activeEffectCount);
    }

    /// <summary>
    /// Builds a pull-based UI DTO for the registered peer.
    /// </summary>
    public UiDto GetUpdateForUiLoop(int peerId)
    {
        return _core.GetUpdateForUiLoop(peerId);
    }

    /// <summary>
    /// Applies an authoritative packed delta to the registered peer.
    /// </summary>
    public void ApplyDelta(int peerId, PackedByteArray delta)
    {
        _core.ApplyDelta(peerId, delta);
    }
}
