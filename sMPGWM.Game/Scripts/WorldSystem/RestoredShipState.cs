using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WorldSystem;

public sealed record RestoredShipState(EntitySpawnRecord Entity, ItemContainerState Equipment,
    ShipRuntimeState Runtime, ShipMovementState Movement)
{
    public WeaponGroupConfiguration WeaponGroups { get; init; } = WeaponGroupConfiguration.Empty;
}
