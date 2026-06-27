using System;
using sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

namespace sMPGWM.Scripts.Controllers.LivingEntity;

public sealed class LivingEntityStateTracker
{
    private readonly StateEngineConfig _config;
    private readonly LivingEntityState[] _snapshots;
    private readonly long[] _snapshotTicks;
    private readonly ActiveEffect[][] _effectHistory;
    private readonly int[] _effectHistoryCounts;
    private readonly long[] _effectHistoryTicks;

    public LivingEntityStateTracker(LivingEntityState currentState, StateEngineConfig config)
    {
        CurrentState = currentState ?? throw new ArgumentNullException(nameof(currentState));
        _config = config;
        var retention = config.ClampedSnapshotRetentionTicks + 1;
        _snapshots = new LivingEntityState[retention];
        _snapshotTicks = new long[retention];
        _effectHistory = new ActiveEffect[retention][];
        _effectHistoryCounts = new int[retention];
        _effectHistoryTicks = new long[retention];

        for (var i = 0; i < retention; i++)
        {
            _snapshots[i] = new LivingEntityState(currentState.ActiveEffectCapacity);
            _snapshotTicks[i] = long.MinValue;
            _effectHistory[i] = new ActiveEffect[config.MaxIncomingEffectsPerEntityPerTick];
            _effectHistoryTicks[i] = long.MinValue;
        }
    }

    public LivingEntityState CurrentState { get; }
    public long CurrentNetworkTick => CurrentState.NetworkTick;

    public void StoreSnapshot(long networkTick)
    {
        var index = Index(networkTick);
        CurrentState.CopyTo(_snapshots[index]);
        _snapshots[index].ClearDirty();
        _snapshotTicks[index] = networkTick;
    }

    public bool TryGetSnapshot(long networkTick, out LivingEntityState snapshot)
    {
        var index = Index(networkTick);
        if (_snapshotTicks[index] == networkTick)
        {
            snapshot = _snapshots[index];
            return true;
        }

        snapshot = null!;
        return false;
    }

    public void StoreEffectHistory(long networkTick, ActiveEffect[] effects, int effectCount)
    {
        var index = Index(networkTick);
        var count = Math.Min(effectCount, _effectHistory[index].Length);
        if (count > 0)
            Array.Copy(effects, _effectHistory[index], count);

        _effectHistoryCounts[index] = count;
        _effectHistoryTicks[index] = networkTick;
    }

    public bool TryGetEffectHistory(long networkTick, out ActiveEffect[] effects, out int effectCount)
    {
        var index = Index(networkTick);
        if (_effectHistoryTicks[index] == networkTick)
        {
            effects = _effectHistory[index];
            effectCount = _effectHistoryCounts[index];
            return true;
        }

        effects = Array.Empty<ActiveEffect>();
        effectCount = 0;
        return false;
    }

    public void DiscardOlderThan(long networkTick)
    {
        for (var i = 0; i < _snapshotTicks.Length; i++)
        {
            if (_snapshotTicks[i] < networkTick)
                _snapshotTicks[i] = long.MinValue;

            if (_effectHistoryTicks[i] < networkTick)
            {
                _effectHistoryTicks[i] = long.MinValue;
                _effectHistoryCounts[i] = 0;
            }
        }
    }

    private int Index(long networkTick)
    {
        return (int)(Math.Abs(networkTick) % _snapshots.Length);
    }
}
