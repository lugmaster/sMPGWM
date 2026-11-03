using sMPGWM.Scripts.Util;

namespace sMPGWM.Scripts.Items;

public readonly record struct ItemEntityId
{
    public ulong Value { get; }

    private ItemEntityId(ulong value) => Value = value;

    public static ItemEntityId Create() => new(RuntimeIds.Next());

    public override string ToString() => Value.ToString();
}
