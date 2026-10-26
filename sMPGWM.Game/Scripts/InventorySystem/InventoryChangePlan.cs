namespace sMPGWM.Scripts.InventorySystem;

internal sealed record InventoryChangePlan(InventorySystem Owner, InventorySnapshot Before, InventorySnapshot After);
