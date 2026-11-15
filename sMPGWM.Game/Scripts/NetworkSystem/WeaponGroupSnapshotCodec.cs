using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class WeaponGroupSnapshotCodec
{
    internal static Array Encode(WeaponGroupConfiguration configuration)
    {
        var rows = new Array();
        foreach (var binding in configuration.Bindings)
        {
            using var row = new Array { binding.Slot, checked((long)binding.Item.Value), binding.Primary, binding.Secondary };
            rows.Add(row);
        }
        return rows;
    }

    internal static bool TryDecode(Array rows, out WeaponGroupConfiguration configuration)
    {
        configuration = WeaponGroupConfiguration.Empty;
        if (rows.Count > ItemContainer.MaxCapacity) return false;
        var bindings = new WeaponGroupBinding[rows.Count];
        var previousSlot = -1;
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].VariantType != Variant.Type.Array) return false;
            using var row = rows[i].AsGodotArray();
            if (row.Count != 4 || row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.Int ||
                row[2].VariantType != Variant.Type.Bool || row[3].VariantType != Variant.Type.Bool) return false;
            var slot = row[0].AsInt64();
            var item = row[1].AsInt64();
            if (slot <= previousSlot || slot >= ItemContainer.MaxCapacity || item <= 0) return false;
            previousSlot = (int)slot;
            bindings[i] = new WeaponGroupBinding((int)slot, ItemEntityId.FromNetwork(item), row[2].AsBool(), row[3].AsBool());
        }
        try { configuration = new WeaponGroupConfiguration(bindings); }
        catch (System.ArgumentException) { return false; }
        return true;
    }
}
