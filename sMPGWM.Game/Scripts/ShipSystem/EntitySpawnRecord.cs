using System;
using Godot;

namespace sMPGWM.Scripts.ShipSystem;

/// <summary>Construction state without participant ownership. Bindings are carried separately.</summary>
public sealed record EntitySpawnRecord(string EntityId, string ShipId, Vector2 Position, float Rotation, Vector2 Velocity)
{
    public Godot.Collections.Array ToVariant() => new() { EntityId, ShipId, Position, Rotation, Velocity };
    public static EntitySpawnRecord From(ShipSpawnRecord record) =>
        new(record.EntityId, record.ShipId, record.Position, record.Rotation, record.Velocity);
    public static bool TryRead(Variant value, out EntitySpawnRecord record)
    {
        record = null!;
        if (value.VariantType != Variant.Type.Array) return false;
        var a = value.AsGodotArray();
        if (a.Count != 5 || a[0].VariantType != Variant.Type.String || a[1].VariantType != Variant.Type.String ||
            a[2].VariantType != Variant.Type.Vector2 || a[3].VariantType != Variant.Type.Float || a[4].VariantType != Variant.Type.Vector2 ||
            !Guid.TryParseExact(a[0].AsString(), "N", out _) || !PlayerStartupData.TryFromVariant(a[1], out var data)) return false;
        var position = a[2].AsVector2(); float rotation = a[3].AsSingle(); var velocity = a[4].AsVector2();
        if (!position.IsFinite() || !float.IsFinite(rotation) || !velocity.IsFinite()) return false;
        record = new(a[0].AsString(), data.ShipId, position, rotation, velocity);
        return true;
    }
}
