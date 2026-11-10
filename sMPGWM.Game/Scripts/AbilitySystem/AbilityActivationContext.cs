using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Scripts.AbilitySystem;

public readonly record struct AbilityActivationContext(
    ShipGameplayState Caster,
    ShipGameplayState Target,
    double DistanceSquared);
