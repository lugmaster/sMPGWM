using System;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed partial class LivingEntityService : AbstractSingleton<LivingEntityService>
{
    private readonly LivingEntityServiceCore _core = new();

    public event Action<LivingEntityConditionChanged> ConditionChanged
    {
        add => _core.ConditionChanged += value;
        remove => _core.ConditionChanged -= value;
    }

    public void RegisterPeer(int peerId, LivingEntityState state)
    {
        _core.RegisterPeer(peerId, state);
    }

    public LivingEntityRuntime RegisterPeerWithDefaultState(int peerId)
    {
        return _core.RegisterPeerWithDefaultState(peerId);
    }

    public void UnregisterPeer(int peerId)
    {
        _core.UnregisterPeer(peerId);
    }

    public bool HasPeer(int peerId)
    {
        return _core.HasPeer(peerId);
    }

    public LivingEntityRuntime GetRuntime(int peerId)
    {
        return _core.GetRuntime(peerId);
    }

    public LivingEntityState GetState(int peerId)
    {
        return _core.GetState(peerId);
    }

    public PackedByteArray ProcessLivingEntity(
        int peerId,
        long networkTick,
        ActiveEffect[] activeEffects,
        int activeEffectCount)
    {
        return _core.ProcessLivingEntity(peerId, networkTick, activeEffects, activeEffectCount);
    }

    public UiDto GetUpdateForUiLoop(int peerId)
    {
        return _core.GetUpdateForUiLoop(peerId);
    }

    public void ApplyDelta(int peerId, PackedByteArray delta)
    {
        _core.ApplyDelta(peerId, delta);
    }
}
