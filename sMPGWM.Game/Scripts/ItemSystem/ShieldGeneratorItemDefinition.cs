using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ShieldGeneratorItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : ItemDefinition(id, displayName, categoryName, description, canStack, stackLimit, icon)
{
    public required double ShieldCapacity { get; init; }
    public required double RechargeTime { get; init; }
    public required double DownTime { get; init; }
    public required double EnergyConsumption { get; init; }
}
