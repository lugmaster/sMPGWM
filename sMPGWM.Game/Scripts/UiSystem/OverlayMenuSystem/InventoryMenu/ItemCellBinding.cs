using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Equipment;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed record ItemCellBinding(ItemCellAddress Location, ItemEntity? Entity, long Revision, int? HotbarPosition = null)
{
    internal int? VisualSlot { get; init; }
    internal int Address => Location.InventoryAddress ?? -1;
    internal string? SlotKey => Location.EquipmentSlotKey;
    internal bool IsHotbar => HotbarPosition.HasValue;
    internal static ItemCellBinding Bag(int address, ItemEntity? entity, long revision) =>
        new(ItemCellAddress.Inventory(address), entity, revision);
    internal static ItemCellBinding Equipment(string key, ItemEntity? entity, long revision) =>
        new(ItemCellAddress.Equipment(key), entity, revision);
    internal static ItemCellBinding Hotbar(int position, long revision) => new(default, null, revision, position);
}
