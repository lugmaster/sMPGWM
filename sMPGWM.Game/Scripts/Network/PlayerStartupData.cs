using System;
using Godot;

namespace sMPGWM.Scripts.Network;

public sealed record PlayerStartupData(string Message)
{
    public static PlayerStartupData Create() => new($"here i am {Random.Shared.Next()}");
    public Variant ToVariant() => Message;

    public static bool TryFromVariant(Variant value, out PlayerStartupData data)
    {
        data = null!;
        if (value.VariantType != Variant.Type.String) return false;
        var message = value.AsString();
        const string prefix = "here i am ";
        if (message.Length > 32 || !message.StartsWith(prefix, StringComparison.Ordinal) ||
            !int.TryParse(message.AsSpan(prefix.Length), out var number) || number < 0) return false;
        data = new PlayerStartupData(message);
        return true;
    }
}
