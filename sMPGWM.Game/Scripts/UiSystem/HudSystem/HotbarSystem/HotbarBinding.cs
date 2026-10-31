namespace sMPGWM.Scripts.UiSystem.HudSystem.HotbarSystem;

public abstract record HotbarBinding;

public sealed record ConsumableHotbarBinding(string DefinitionId) : HotbarBinding;

public sealed record WeaponHotbarBinding(string SlotKey) : HotbarBinding;

public sealed record ActionHotbarBinding(string ActionKey) : HotbarBinding;

public enum HotbarResult
{
    Success,
    NoChange,
    InvalidSession,
    StaleRevision,
    InvalidPosition,
    InvalidLocation,
    IneligibleSource,
    Unavailable,
    PublicationInProgress,
    Disposed
}

