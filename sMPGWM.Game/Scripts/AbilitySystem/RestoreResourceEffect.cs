using System.Collections.Generic;

namespace sMPGWM.Scripts.AbilitySystem;

public sealed record RestoreResourceEffect(string Id, string DisplayName, string Description, string StatId, ResourceAmount Amount, IReadOnlyList<string> BlockedByConditions)
    : EffectDefinition(Id, DisplayName, Description);
