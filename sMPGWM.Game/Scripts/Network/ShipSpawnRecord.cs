using System.Diagnostics.CodeAnalysis;
using System;
using Godot;
namespace sMPGWM.Scripts.Network;
public sealed record ShipSpawnRecord(int PeerId, string EntityId, string ShipId, Vector2 Position, float Rotation, Vector2 Velocity)
{
    public Variant ToVariant() => new Godot.Collections.Array { PeerId, EntityId, ShipId, Position, Rotation, Velocity };
    public static bool TryFromVariant(Variant value, [NotNullWhen(true)] out ShipSpawnRecord? record)
    {
        record = null;
        if (value.VariantType != Variant.Type.Array) return false;
        var a = value.AsGodotArray();
        if (a.Count != 6 || a[0].VariantType != Variant.Type.Int || a[1].VariantType != Variant.Type.String ||
            a[2].VariantType != Variant.Type.String || a[3].VariantType != Variant.Type.Vector2 ||
            a[4].VariantType != Variant.Type.Float || a[5].VariantType != Variant.Type.Vector2) return false;
        var peer = a[0].AsInt64();
        if (peer <= 0 || peer > int.MaxValue || !Guid.TryParseExact(a[1].AsString(), "N", out _) ||
            !PlayerStartupData.TryFromVariant(a[2], out var data)) return false;
        var position = a[3].AsVector2(); var rotation = a[4].AsSingle(); var velocity = a[5].AsVector2();
        if (!position.IsFinite() || !float.IsFinite(rotation) || !velocity.IsFinite()) return false;
        record = new((int)peer, a[1].AsString(), data.ShipId, position, rotation, velocity);
        return true;
    }
}
