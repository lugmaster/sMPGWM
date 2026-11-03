namespace sMPGWM.Scripts.Items.Equipment;

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
    Disposed
}
