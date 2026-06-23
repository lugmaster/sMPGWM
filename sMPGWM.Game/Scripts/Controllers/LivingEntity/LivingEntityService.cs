using System;
using sMPGWM.Scripts.Autoload.Base;
using sMPGWM.Scripts.Controllers.LivingEntity.Effects;
using sMPGWM.Scripts.Enums.Game;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed partial class LivingEntityService : AbstractSingleton<LivingEntityService>
{
    private readonly LivingEntityServiceCore _core = new(new GodotLivingEntityLogger());

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

    public StatBreakdown GetStatBreakdown(int peerId, StatType statType)
    {
        return _core.GetStatBreakdown(peerId, statType);
    }

    public void ApplyEffect(int peerId, LivingEntityEffect effect)
    {
        _core.ApplyEffect(peerId, effect);
    }

    public Array GetDelta(int peerId, bool includePermanent)
    {
        return _core.GetDelta(peerId, includePermanent);
    }

    public Array GetDeltas(bool includePermanent)
    {
        return _core.GetDeltas(includePermanent);
    }

    public void ApplyDelta(Array delta)
    {
        _core.ApplyDelta(delta);
    }

    public void ApplyDeltas(Array deltas)
    {
        _core.ApplyDeltas(deltas);
    }

    public void TickActiveEffects(int peerId, float delta)
    {
        _core.TickActiveEffects(peerId, delta);
    }

    public void TickAllActiveEffects(float delta)
    {
        _core.TickAllActiveEffects(delta);
    }
}
