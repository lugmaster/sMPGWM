namespace sMPGWM.Scripts.WeaponSystem;

public sealed record WeaponDefinition(string Archetype, WeaponDelivery Delivery, WeaponFireMode FireMode,
    WeaponSupplyMode SupplyMode, string PresentationId, ResolvedWeaponStats Stats);
