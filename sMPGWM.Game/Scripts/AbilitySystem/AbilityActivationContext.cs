using System.Collections.Generic;
using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Scripts.AbilitySystem;

public readonly record struct AbilityActivationContext(ShipGameplayState Caster, ShipGameplayState Target,
    bool TargetIsLivingAlly, double DistanceSquared, IReadOnlyList<string> CasterConditions);
