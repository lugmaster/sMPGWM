using System.Collections.Generic;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record PeriodicAccumulatorDefinition(double IntervalMs, PeriodicFirstTick FirstTick, bool TickAtExpiration, ModifierDefinition Modifier, IReadOnlyList<string> BlockedByConditions, BlockedTickPolicy BlockedTickPolicy);
