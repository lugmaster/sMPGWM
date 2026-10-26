using System;

namespace sMPGWM.Scripts.ItemSystem;

public readonly record struct ItemCategoryId
{
    public string Value { get; }

    public ItemCategoryId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A category ID is required.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct ItemDefinitionId
{
    public string Value { get; }

    public ItemDefinitionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("An item definition ID is required.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct ItemEntityId
{
    public Guid Value { get; }

    private ItemEntityId(Guid value) => Value = value;

    public static ItemEntityId Create() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
