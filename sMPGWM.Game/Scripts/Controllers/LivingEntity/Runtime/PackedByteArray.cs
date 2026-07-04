using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

/// <summary>
/// Project-local packed byte payload used at the state-engine delta boundary.
/// </summary>
public readonly struct PackedByteArray
{
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a packed byte payload from the supplied byte array.
    /// </summary>
    public PackedByteArray(byte[] bytes)
    {
        _bytes = bytes ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Gets the number of bytes in the payload.
    /// </summary>
    public int Count => _bytes?.Length ?? 0;

    /// <summary>
    /// Gets whether the payload has zero bytes.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Returns a copied byte array for transport, inspection, or tests.
    /// </summary>
    public byte[] ToArray()
    {
        if (_bytes == null || _bytes.Length == 0)
            return Array.Empty<byte>();

        var copy = new byte[_bytes.Length];
        Array.Copy(_bytes, copy, _bytes.Length);
        return copy;
    }

    /// <summary>
    /// Gets an empty packed payload.
    /// </summary>
    public static PackedByteArray Empty { get; } = new(Array.Empty<byte>());
}
