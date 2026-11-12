namespace sMPGWM.Scripts.ItemSystem;

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
