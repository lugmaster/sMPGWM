using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class ShipWeaponController(ShipController ship)
{
    public static void ValidateDefinition(ItemDefinition definition)
    {
        if (definition.Id is not ("vulcan_mk1" or "laser_mk1")) return;
        if (definition is not WeaponItemDefinition weapon || weapon.DeliveryType != WeaponDeliveryType.HitScan ||
            weapon.PresentationId is not ("smg_standard" or "laser_standard") ||
            !double.IsFinite(weapon.AttackSpeed) || weapon.AttackSpeed <= 0 || weapon.AttackSpeed > 60 ||
            !double.IsFinite(weapon.Range) || weapon.Range <= 0 || weapon.Range > float.MaxValue)
            throw new System.ArgumentException("Unsupported executable weapon definition: " + definition.Id);
    }

    private EquipmentSnapshot? _snapshot;
    private readonly Binding[] _bindings = new Binding[9];
    internal int Count { get; private set; }
    internal Binding GetBinding(int index) => _bindings[index];

    internal void Refresh()
    {
        var snapshot = ship.Equipment.Snapshot;
        if (ReferenceEquals(snapshot, _snapshot)) return;
        System.Array.Clear(_bindings);
        _snapshot = snapshot;
        Count = 0;
        foreach (var group in snapshot.WeaponGroups.Bindings)
        {
            var item = snapshot.Slots[group.Slot];
            if (item?.Definition is not WeaponItemDefinition weapon || item.Id != group.Item ||
                weapon.DeliveryType != WeaponDeliveryType.HitScan || item.WeaponState == null ||
                weapon.Id is not ("vulcan_mk1" or "laser_mk1")) continue;
            var profile = weapon.PresentationId switch { "smg_standard" => 0, "laser_standard" => 1, _ => -1 };
            if (profile < 0 || Count == _bindings.Length) continue;
            _bindings[Count++] = new(item, weapon, profile, (byte)((group.Primary ? 1 : 0) | (group.Secondary ? 2 : 0)));
        }
    }

    internal readonly record struct Binding(ItemEntity Item, WeaponItemDefinition Definition, int Profile, byte Groups);
}
