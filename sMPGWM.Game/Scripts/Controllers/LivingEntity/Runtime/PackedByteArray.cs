using System;

namespace sMPGWM.Scripts.Controllers.LivingEntity.Runtime;

public readonly struct PackedByteArray
{
    private readonly byte[] _bytes;

    public PackedByteArray(byte[] bytes)
    {
        _bytes = bytes ?? Array.Empty<byte>();
    }

    public int Count => _bytes?.Length ?? 0;
    public bool IsEmpty => Count == 0;

    public byte[] ToArray()
    {
        if (_bytes == null || _bytes.Length == 0)
            return Array.Empty<byte>();

        var copy = new byte[_bytes.Length];
        Array.Copy(_bytes, copy, _bytes.Length);
        return copy;
    }

    public static PackedByteArray Empty { get; } = new(Array.Empty<byte>());
}
