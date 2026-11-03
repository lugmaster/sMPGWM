namespace sMPGWM.Scripts.Items.Inventory;

public enum InventoryResult
{
    Success,
    NoChange,
    NegativeQuantity,
    UnknownDefinition,
    CapacityExceeded,
    StaleRevision,
    PublicationInProgress,
    Disposed
}
