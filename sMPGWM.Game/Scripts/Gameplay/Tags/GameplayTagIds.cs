#nullable enable

using System;
using System.Globalization;

namespace sMPGWM.Scripts.Gameplay.Tags;

public readonly record struct GameplayTagId(ushort Value)
{
    public static readonly GameplayTagId Invalid = new(0);

    public bool IsValid => Value != 0;

    public override string ToString()
    {
        return IsValid ? Value.ToString(CultureInfo.InvariantCulture) : "Invalid";
    }
}

public readonly record struct GameplayTagDomainId(ushort Value)
{
    public static readonly GameplayTagDomainId Invalid = new(0);

    public bool IsValid => Value != 0;

    public override string ToString()
    {
        return IsValid ? Value.ToString(CultureInfo.InvariantCulture) : "Invalid";
    }
}

public readonly record struct TagQueryId(ushort Value)
{
    public static readonly TagQueryId Invalid = new(0);

    public bool IsValid => Value != 0;

    public override string ToString()
    {
        return IsValid ? Value.ToString(CultureInfo.InvariantCulture) : "Invalid";
    }
}

public enum TagQueryMatchMode
{
    Expanded = 0,
    Direct = 1
}
