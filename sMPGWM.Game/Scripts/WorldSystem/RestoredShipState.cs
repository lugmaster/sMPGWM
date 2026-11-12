using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WorldSystem;

public sealed record RestoredShipState(EntitySpawnRecord Entity, ItemContainerState Equipment,
    ShipRuntimeState Runtime, ShipMovementState Movement);
