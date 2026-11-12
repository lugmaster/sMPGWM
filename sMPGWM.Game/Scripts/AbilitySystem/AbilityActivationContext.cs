using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Scripts.AbilitySystem;

public readonly record struct AbilityActivationContext(
    ShipRuntime Caster,
    ShipRuntime Target,
    double DistanceSquared);
