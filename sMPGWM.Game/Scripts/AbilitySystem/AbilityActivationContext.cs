using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.AbilitySystem;

public readonly record struct AbilityActivationContext(
    ShipRuntime Caster,
    ShipRuntime Target,
    double DistanceSquared);
