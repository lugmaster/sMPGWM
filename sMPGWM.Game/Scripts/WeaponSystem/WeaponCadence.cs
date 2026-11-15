using System;

namespace sMPGWM.Scripts.WeaponSystem;

/// <summary>Item-owned cadence survives loadout changes; no supply or damage is applied here.</summary>
public sealed class WeaponCadence
{
    public double NextEligibleTick { get; private set; }
    public long Sequence { get; private set; }
    public long LastAcceptedTick { get; private set; } = -1;
    private long _lastEligibleTick = -2;

    public bool TryAccept(long tick, double attacksPerSecond, bool eligible)
    {
        if (tick < 0 || !double.IsFinite(attacksPerSecond) || attacksPerSecond <= 0 || attacksPerSecond > 60)
            throw new ArgumentOutOfRangeException(nameof(attacksPerSecond));
        var continuous = _lastEligibleTick == tick - 1;
        if (eligible) _lastEligibleTick = tick;
        else _lastEligibleTick = -2;
        if (!eligible || tick <= LastAcceptedTick || tick + 1e-9 < NextEligibleTick) return false;
        // Preserve fractional phase only within the immediately due physics step.
        var due = NextEligibleTick;
        if (!continuous || tick - due >= 1 || Sequence == 0) due = tick;
        NextEligibleTick = due + 60 / attacksPerSecond;
        LastAcceptedTick = tick;
        Sequence = checked(Sequence + 1);
        return true;
    }

    public void Restore(double next, long sequence, long last)
    {
        if (!double.IsFinite(next) || next < 0 || sequence < 0 || sequence == long.MaxValue ||
            last < -1 || last >= NetworkSystem.SnapshotEnvelope.MaxTick || next > NetworkSystem.SnapshotEnvelope.MaxTick ||
            (sequence == 0 ? last != -1 || next != 0 : last < 0 || next <= last))
            throw new ArgumentException("Invalid weapon cadence state.");
        NextEligibleTick = next;
        Sequence = sequence;
        LastAcceptedTick = last;
    }

    public WeaponCadence Capture()
    {
        var copy = new WeaponCadence();
        copy.Restore(NextEligibleTick, Sequence, LastAcceptedTick);
        return copy;
    }
}
