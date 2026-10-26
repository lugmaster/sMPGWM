namespace sMPGWM.Scripts.EquipmentSystem;

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
