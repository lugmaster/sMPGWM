namespace sMPGWM.Scripts.InventorySystem;

internal sealed record InventoryChangePlan(InventoryImpl Owner, InventorySnapshot Before, InventorySnapshot After);
