namespace sMPGWM.Scripts.ItemSystem;

public enum TransferResult
{
    Success,
    NoChange,
    InvalidSession,
    InvalidLocation,
    StaleRevision,
    StaleEntity,
    IncompatiblePlacement,
    CapacityExceeded,
    PublicationInProgress,
    Disposed,
    Pending
}
