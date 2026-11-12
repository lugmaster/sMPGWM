using sMPGWM.Scripts.UtilitySystem;

namespace sMPGWM.Scripts.ItemSystem;

public readonly record struct ItemEntityId
{
    public ulong Value { get; }

    private ItemEntityId(ulong value) => Value = value;

    public static ItemEntityId Create() => new(RuntimeIds.Next());

    public static ItemEntityId FromNetwork(long value) => value > 0
        ? new((ulong)value) : throw new System.ArgumentOutOfRangeException(nameof(value));

    public override string ToString() => Value.ToString();
}
