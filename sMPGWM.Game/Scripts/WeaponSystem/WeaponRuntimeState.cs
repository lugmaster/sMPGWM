using System;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class WeaponRuntimeState
{
    private readonly WeaponCadence _cadence = new();
    private readonly WeaponSupplyDefinition _supply;
    public long Revision { get; private set; } = 1;
    public double NextEligibleTick => _cadence.NextEligibleTick;
    public long Sequence => _cadence.Sequence;
    public long LastAcceptedTick => _cadence.LastAcceptedTick;
    public int Rounds { get; private set; }
    public int Capacity { get; }
    public long ReloadDurationTicks { get; }
    public long ReloadStartTick { get; private set; } = -1;
    public long ReloadDeadlineTick { get; private set; } = -1;
    public bool IsReloading => ReloadStartTick >= 0;

    public WeaponRuntimeState(WeaponSupplyDefinition supply)
    {
        ArgumentNullException.ThrowIfNull(supply);
        _supply = supply;
        if (supply is not AmmunitionSupplyDefinition ammunition) return;
        Capacity = ammunition.AmmoCapacity;
        Rounds = Capacity;
        var duration = Math.Ceiling(ammunition.ReloadDurationMs * 60 / 1000);
        if (!double.IsFinite(duration) || duration > SnapshotEnvelope.MaxTick)
            throw new ArgumentOutOfRangeException(nameof(supply));
        ReloadDurationTicks = Math.Max(1, (long)duration);
    }

    public bool CanAccept(long tick, double attacksPerSecond) => tick >= 0 &&
        double.IsFinite(attacksPerSecond) && attacksPerSecond > 0 && attacksPerSecond <= 60 &&
        !IsReloading && (Capacity == 0 || Rounds > 0) && tick > LastAcceptedTick && tick + 1e-9 >= NextEligibleTick;

    public void ValidateAcceptance(long tick, double attacksPerSecond)
    {
        if (!CanAccept(tick, attacksPerSecond)) throw new InvalidOperationException("Weapon cannot accept this shot.");
        if (tick > SnapshotEnvelope.MaxTick - 60 / attacksPerSecond || Sequence >= long.MaxValue - 1 || Revision >= long.MaxValue - 1 ||
            Capacity > 0 && Rounds == 1 && tick > SnapshotEnvelope.MaxTick - ReloadDurationTicks)
            throw new OverflowException("Weapon state exceeds its supported domain.");
    }

    public bool TryAccept(long tick, double attacksPerSecond, bool eligible)
    {
        eligible &= !IsReloading && (Capacity == 0 || Rounds > 0);
        if (eligible && CanAccept(tick, attacksPerSecond)) ValidateAcceptance(tick, attacksPerSecond);
        if (!_cadence.TryAccept(tick, attacksPerSecond, eligible)) return false;
        if (Capacity > 0 && --Rounds == 0)
        {
            ReloadStartTick = tick;
            ReloadDeadlineTick = tick + ReloadDurationTicks;
        }
        Revision++;
        return true;
    }

    public bool TryStartReload(long tick)
    {
        if (Capacity == 0 || Rounds == Capacity || IsReloading) return false;
        if (tick < 0 || tick > SnapshotEnvelope.MaxTick - ReloadDurationTicks) throw new ArgumentOutOfRangeException(nameof(tick));
        var revision = checked(Revision + 1);
        if (revision == long.MaxValue) throw new OverflowException();
        ReloadStartTick = tick;
        ReloadDeadlineTick = tick + ReloadDurationTicks;
        Revision = revision;
        return true;
    }

    public bool CancelReload()
    {
        if (!IsReloading) return false;
        var revision = checked(Revision + 1);
        if (revision == long.MaxValue) throw new OverflowException();
        ReloadStartTick = ReloadDeadlineTick = -1;
        Revision = revision;
        return true;
    }

    public bool CompleteReload(long tick)
    {
        if (!IsReloading || tick < ReloadDeadlineTick) return false;
        CancelReload();
        Rounds = Capacity;
        return true;
    }

    public void Restore(long revision, double next, long sequence, long last, int rounds, long start, long deadline)
    {
        if (revision < 1 || revision == long.MaxValue || revision <= sequence || rounds < 0 || rounds > Capacity ||
            (start == -1 ? deadline != -1 : Capacity == 0 || rounds == Capacity || start < 0 ||
                start < last || start > SnapshotEnvelope.MaxTick - ReloadDurationTicks || deadline != start + ReloadDurationTicks))
            throw new ArgumentException("Invalid weapon supply state.");
        _cadence.Restore(next, sequence, last);
        Revision = revision;
        Rounds = rounds;
        ReloadStartTick = start;
        ReloadDeadlineTick = deadline;
    }

    public bool SameAs(WeaponRuntimeState other) => Revision == other.Revision && NextEligibleTick == other.NextEligibleTick &&
        Sequence == other.Sequence && LastAcceptedTick == other.LastAcceptedTick && Rounds == other.Rounds &&
        ReloadStartTick == other.ReloadStartTick && ReloadDeadlineTick == other.ReloadDeadlineTick;

    public WeaponRuntimeState Capture()
    {
        var copy = new WeaponRuntimeState(_supply);
        copy.Restore(Revision, NextEligibleTick, Sequence, LastAcceptedTick, Rounds, ReloadStartTick, ReloadDeadlineTick);
        return copy;
    }
}
