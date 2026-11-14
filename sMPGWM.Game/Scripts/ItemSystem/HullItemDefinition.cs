using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record HullItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : ItemDefinition(id, displayName, categoryName, description, canStack, stackLimit, icon)
{
    public required double Armor { get; init; }
}
