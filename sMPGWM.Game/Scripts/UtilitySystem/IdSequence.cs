using System.Threading;

namespace sMPGWM.Scripts.UtilitySystem;

internal sealed class IdSequence(ulong lastIssued = 0)
{
    // Store the unsigned bit pattern in a long for Interlocked operations.
    private long _lastIssued = unchecked((long)lastIssued);

    internal void ReserveThrough(ulong value)
    {
        while (true)
        {
            var current = Interlocked.Read(ref _lastIssued);
            if (unchecked((ulong)current) >= value) return;
            if (Interlocked.CompareExchange(ref _lastIssued, unchecked((long)value), current) == current) return;
        }
    }

    internal ulong Next()
    {
        while (true)
        {
            var current = Interlocked.Read(ref _lastIssued);
            var next = checked(unchecked((ulong)current) + 1);
            if (Interlocked.CompareExchange(ref _lastIssued, unchecked((long)next), current) == current)
                return next;
        }
    }
}
