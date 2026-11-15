using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public readonly record struct WeaponGroupBinding(int Slot, ItemEntityId Item, bool Primary, bool Secondary);
