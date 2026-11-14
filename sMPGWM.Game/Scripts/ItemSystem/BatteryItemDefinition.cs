using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record BatteryItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : ItemDefinition(id, displayName, categoryName, description, canStack, stackLimit, icon)
{
    public required double EnergyStorage { get; init; }
    public required double EnergyConsumption { get; init; }
}
