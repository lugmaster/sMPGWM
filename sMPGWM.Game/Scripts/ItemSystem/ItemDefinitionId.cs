using System;

namespace sMPGWM.Scripts.ItemSystem;

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
