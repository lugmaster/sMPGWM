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
