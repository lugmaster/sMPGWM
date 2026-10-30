using System;
using System.Threading;

namespace sMPGWM.Scripts.Util;

/// <summary>Process-local IDs shared by runtime entities, containers and UI operations.
/// Zero is reserved. Saved or networked IDs require an authoritative persistent allocator.</summary>
internal static class RuntimeIds
{
    private static readonly IdSequence Sequence = new();

    internal static ulong Next() => Sequence.Next();
}

internal sealed class IdSequence(ulong lastIssued = 0)
{
    // Store the unsigned bit pattern in a long for Interlocked operations.
    private long _lastIssued = unchecked((long)lastIssued);

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
