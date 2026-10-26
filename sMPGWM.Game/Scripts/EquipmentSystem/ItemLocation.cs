#nullable enable

namespace sMPGWM.Scripts.EquipmentSystem;

public readonly record struct ItemLocation
{
    public int? InventoryAddress { get; }
    public string? EquipmentSlotKey { get; }

    private ItemLocation(int? inventoryAddress, string? equipmentSlotKey)
    {
        InventoryAddress = inventoryAddress;
        EquipmentSlotKey = equipmentSlotKey;
    }

    public static ItemLocation Inventory(int address) => new(address, null);
    public static ItemLocation Equipment(string key) => new(null, key);
}
