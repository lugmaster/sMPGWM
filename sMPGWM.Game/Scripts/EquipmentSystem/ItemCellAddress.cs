

namespace sMPGWM.Scripts.EquipmentSystem;

/// <summary>A UI cell address. The transfer adapter resolves this to an ItemSystem container location.</summary>
public readonly record struct ItemCellAddress
{
    public int? InventoryAddress { get; }
    public string? EquipmentSlotKey { get; }

    private ItemCellAddress(int? inventoryAddress, string? equipmentSlotKey)
    {
        InventoryAddress = inventoryAddress;
        EquipmentSlotKey = equipmentSlotKey;
    }

    public static ItemCellAddress Inventory(int address) => new(address, null);
    public static ItemCellAddress Equipment(string key) => new(null, key);
}
