#nullable enable

using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed record ItemCellBinding(ItemLocation Location, ItemEntity? Entity, long Revision, int? HotbarPosition = null)
{
    internal int Address => Location.InventoryAddress ?? -1;
    internal string? SlotKey => Location.EquipmentSlotKey;
    internal bool IsHotbar => HotbarPosition.HasValue;
    internal static ItemCellBinding Bag(int address, ItemEntity? entity, long revision) =>
        new(ItemLocation.Inventory(address), entity, revision);
    internal static ItemCellBinding Equipment(string key, ItemEntity? entity, long revision) =>
        new(ItemLocation.Equipment(key), entity, revision);
    internal static ItemCellBinding Hotbar(int position, long revision) => new(default, null, revision, position);
}
