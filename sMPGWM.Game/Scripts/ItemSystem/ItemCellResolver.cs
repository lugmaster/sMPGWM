using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemCellResolver
{
    public static bool TryResolve(ItemCellAddress cell, InventoryController inventory, EquipmentController equipment, out ItemLocation location)
    {
        var isInventory = cell.InventoryAddress.HasValue;
        var index = isInventory ? cell.InventoryAddress!.Value : equipment.FindSlot(cell.EquipmentSlotKey!);
        var container = isInventory ? inventory.Container : equipment.Container;
        location = new ItemLocation(container.Id, index);
        return index >= 0 && index < container.Capacity;
    }

}
