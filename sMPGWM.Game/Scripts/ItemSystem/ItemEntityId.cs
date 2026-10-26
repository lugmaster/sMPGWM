using System;

namespace sMPGWM.Scripts.ItemSystem;

public readonly record struct ItemEntityId
{
    public Guid Value { get; }

    private ItemEntityId(Guid value) => Value = value;

    public static ItemEntityId Create() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
