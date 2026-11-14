using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record WeaponItemDefinition(string id, string displayName, string categoryName, string description,
    bool canStack, int stackLimit, IconDefinition icon)
    : ItemDefinition(id, displayName, categoryName, description, canStack, stackLimit, icon)
{
    public required string Archetype { get; init; }
    public required WeaponDeliveryType DeliveryType { get; init; }
    public required string PresentationId { get; init; }
    public required string DamageSource { get; init; }
    public required double Damage { get; init; }
    public required double AttackSpeed { get; init; }
    public required double Range { get; init; }
    public required double EnergyConsumption { get; init; }
    public required WeaponSupplyDefinition Supply { get; init; }
    public double SpreadDegrees { get; init; }
}
