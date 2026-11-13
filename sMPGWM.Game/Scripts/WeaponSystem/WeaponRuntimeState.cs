using System;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class WeaponRuntimeState
{
    public ItemEntityId ItemId { get; }
    public long Revision { get; private set; } = 1;
    public int Rounds { get; private set; }
    public double NextFireTick { get; private set; }
    public long ReloadStartTick { get; private set; } = -1;
    public long ReloadDeadline { get; private set; } = -1;
    public ulong ShotSequence { get; private set; }
    private long _lastEligibleTick = -2;
    private bool _resetCadence;
    public bool Reloading => ReloadDeadline >= 0;

    internal WeaponRuntimeState(ItemEntityId item, int rounds) { ItemId = item; Rounds = rounds; }
    public WeaponStateSnapshot CaptureState() => new(ItemId, Revision, Rounds, NextFireTick, ReloadStartTick, ReloadDeadline, ShotSequence);
    internal void Install(WeaponStateSnapshot state)
    {
        Revision = state.Revision; Rounds = state.Rounds; NextFireTick = state.NextFireTick;
        ReloadStartTick = state.ReloadStartTick; ReloadDeadline = state.ReloadDeadline; ShotSequence = state.ShotSequence;
        _lastEligibleTick = -2;
    }

    public bool StartReload(long tick, ResolvedWeaponStats stats)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (stats.MagazineCapacity == 0 || Reloading || Rounds >= stats.MagazineCapacity) return false;
        var deadline = checked(tick + stats.ReloadTicks);
        Revision = checked(Revision + 1);
        ReloadStartTick = tick; ReloadDeadline = deadline;
        return true;
    }

    public void CancelReload()
    {
        if (!Reloading) return;
        Revision = checked(Revision + 1);
        ReloadStartTick = ReloadDeadline = -1;
    }

    internal void CompleteReload(long tick, ResolvedWeaponStats stats)
    {
        if (!Reloading || tick < ReloadDeadline) return;
        Revision = checked(Revision + 1);
        Rounds = stats.MagazineCapacity;
        ReloadStartTick = ReloadDeadline = -1;
        _lastEligibleTick = -2;
    }

    internal bool Ready(long tick)
    {
        var continuous = _lastEligibleTick == tick - 1;
        _lastEligibleTick = tick;
        _resetCadence = !continuous && NextFireTick < tick;
        return !Reloading && tick >= NextFireTick;
    }

    internal void Accept(long tick, ResolvedWeaponStats stats, bool ammunition)
    {
        Revision = checked(Revision + 1);
        ShotSequence = checked(ShotSequence + 1);
        NextFireTick = (_resetCadence ? tick : NextFireTick) + 60d / stats.AttackSpeed;
        if (!ammunition) return;
        Rounds--;
        if (Rounds == 0) StartReload(tick, stats);
    }
}
