using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.ShipSystem;

public sealed class ShipDefinitions(ShipData[] definitions)
    : DefinitionRegistry<ShipData>(definitions, "ships");
