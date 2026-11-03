using Godot;

namespace sMPGWM.Scripts.Network;

public sealed record PlayerStartupData(string ShipId)
{
    public static PlayerStartupData Create() => new("fighter");
    public Variant ToVariant() => ShipId;
    public static bool TryFromVariant(Variant value, out PlayerStartupData data)
    {
        data = null!;
        if (value.VariantType != Variant.Type.String) return false;
        var id = value.AsString();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 256) return false;
        data = new(id);
        return true;
    }
}
