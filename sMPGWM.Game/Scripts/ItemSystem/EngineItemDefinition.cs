using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record EngineItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : ItemDefinition(id, displayName, categoryName, description, canStack, stackLimit, icon)
{
    public required double EnergyConsumption { get; init; }
    public required double Speed { get; init; }
    public required double AfterBurnerSpeed { get; init; }
    public required double RotationSpeed { get; init; }
    public required double Acceleration { get; init; }
    public required double AfterBurnerAcceleration { get; init; }
}
