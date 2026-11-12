namespace sMPGWM.Scripts.UtilitySystem;

/// <summary>Process-local IDs shared by runtime entities, containers and UI operations.
/// Zero is reserved. Authoritative restoration reserves supplied IDs before creating new ones.</summary>
internal static class RuntimeIds
{
    private static readonly IdSequence Sequence = new();

    internal static ulong Next() => Sequence.Next();
    internal static void ReserveThrough(ulong value) => Sequence.ReserveThrough(value);
}
