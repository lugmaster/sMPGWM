namespace sMPGWM.Scripts.InventorySystem;

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
