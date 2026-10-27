namespace sMPGWM.Scripts.InventorySystem;

internal sealed record InventoryChangePlan(InventoryController Owner, InventorySnapshot Before, InventorySnapshot After);
