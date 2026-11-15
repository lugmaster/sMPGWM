using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class ShipWeaponController(ShipController ship)
{
    public static void ValidateDefinition(ItemDefinition definition) => WeaponDefinitionCompiler.Prepare(definition);

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
                item.WeaponPlan is not { } plan || item.WeaponState == null) continue;
            var profile = plan.Profile;
            if (Count == _bindings.Length) throw new System.InvalidOperationException("Weapon binding capacity exceeded.");
            _bindings[Count++] = new(item, weapon, profile, (byte)((group.Primary ? 1 : 0) | (group.Secondary ? 2 : 0)));
        }
    }

    internal readonly record struct Binding(ItemEntity Item, WeaponItemDefinition Definition, int Profile, byte Groups);
}
