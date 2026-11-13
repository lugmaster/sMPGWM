using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed record WeaponBinding(int Slot, ItemEntityId ItemId, WeaponDefinition Definition, WeaponStateStore Store)
{
    public WeaponRuntimeState State => Store.TryGet(ItemId, out var state) ? state : throw new System.InvalidOperationException("Missing weapon state.");
}
